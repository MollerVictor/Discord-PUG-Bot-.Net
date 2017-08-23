using Discord;
using Discord.Commands;
using Discord.WebSocket;
using OWPugs.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Pomelo.EntityFrameworkCore.MySql;
using Pomelo.EntityFrameworkCore.Extensions;
using System.IO;
using System.Diagnostics;
using Glicko2;
using Microsoft.Extensions.Configuration;

public class PugModule : ModuleBase<SocketCommandContext>
{
	//TODO Move these to the config file
	public const int MAX_PLAYERS = PLAYERS_PER_TEAM * 2;
	private const int PLAYERS_PER_TEAM = 5;
	private const int MAP_VOTE_TIME = 60;
	private const int GAMEMODE_VOTE_TIME = 45;
	private const int READY_UP_TIME = 75;
	private const string CHANNEL_NAME = "pugs";


	private  List<DataStore.Teams> PickOrder = new List<DataStore.Teams> {	DataStore.Teams.TeamOne,																			
																			DataStore.Teams.TeamTwo,
																			DataStore.Teams.TeamOne,																			
																			DataStore.Teams.TeamTwo,
																			DataStore.Teams.TeamOne,
																			DataStore.Teams.TeamTwo,
																			DataStore.Teams.TeamTwo,
																			DataStore.Teams.TeamOne,};

	public DataStore datastore;
	private AppConfig _appConfig;


	public PugModule(DataStore ds, AppConfig appConfig)
	{
		datastore = ds;
		_appConfig = appConfig;
	}


	[Command("add"), AllowedChannelsService]
	[Alias("a")]
	public async Task AddUserToPug()
	{
		await Context.Message.DeleteAsync();

		if (datastore.CurrentPugState != DataStore.PugState.WaitingForPlayer)
		{
			await ReplyAsync("Wait until they have finished picking teams on the last pug, before typing .add");
			return;
		}

		await AddToQueueList(datastore._euSignupUsers, "EU");
		await AddToQueueList(datastore._naSignupUsers, "NA");

		await SetChannelNameToCurrentUsers();
	}

	[Command("add"), AllowedChannelsService]
	[Alias("a")]
	public async Task AddUserToPug(string region)
	{
		await Context.Message.DeleteAsync();

		if (datastore.CurrentPugState != DataStore.PugState.WaitingForPlayer)
		{
			await ReplyAsync($"**{Context.User.GetName()}**, Wait until they have finished picking teams on the last pug, before typing .add");
			return;
		}

		region = region.ToLower();
		switch (region)
		{
			case "na":
				await AddToQueueList(datastore._naSignupUsers, "NA");
				break;
			case "eu":
				await AddToQueueList(datastore._euSignupUsers, "EU");
				break;
			case "b":
			case "both":
				await AddToQueueList(datastore._euSignupUsers, "EU");
				await AddToQueueList(datastore._naSignupUsers, "NA");
				break;
			default:
				string replayString = string.Format($"{Context.User.GetName()}, Wrong region use .a <na/eu/both>.");
				await ReplyAsync(replayString);

				return;
		}

		await SetChannelNameToCurrentUsers();
	}

	private async Task AddToQueueList(List<PugUser> signupList, string regionText)
	{
		var user = Context.User;

		if (signupList.Count >= MAX_PLAYERS)
		{
			string replayString = $"**{Context.User.GetName()}**Pug is full.";
			await ReplyAsync(replayString);
		}
		else
		{
			if (signupList.Select(x => x.IUser.Id).Contains(user.Id))
			{
				string replayString = $"{Context.User}, you are already signed up. {regionText} {signupList.Count}/{MAX_PLAYERS} players.";
				await ReplyAsync(replayString);
			}
			else
			{
				var pugUser = new PugUser(user, datastore.GetOrCreateUser(user));

				signupList.Add(pugUser);

				string replayString = $"**{regionText}** {signupList.Count}/{MAX_PLAYERS} players. **{Context.User.GetName()}** joined the queue.\n" + string.Join("\n", signupList.Select(x => $"**{x.IUser.GetName()}** Rating: {x.GetDisplayRanking()}"));

				await ReplyAsync(replayString);

				if (signupList.Count >= MAX_PLAYERS)
				{
					datastore._signupUsers = signupList;

					datastore.CurrentPug = new Pug()
					{
						Region = regionText == "EU" ? Region.EU : Region.NA
					};
					string replayString2 = $"**{regionText}** Pug is now full, everyone ready up. (.ready)";

					datastore.CurrentPugState = DataStore.PugState.Readyup;

					var autoEvent = new AutoResetEvent(false);
					datastore.ReadyUpTimer = new Timer(new TimerCallback(ReadyUpTimerProc), autoEvent, READY_UP_TIME * 1000, 0);

					await ReplyAsync(replayString2);
				}

				
			}
		}
	}



	[Command("remove"), AllowedChannelsService]
	[Alias("leave", "l")]
	public async Task RemoveUserFromPug()
	{
		await Context.Message.DeleteAsync();

		if (datastore.CurrentPugState != DataStore.PugState.WaitingForPlayer)
			return;

		await RemoveFromQueueList(datastore._euSignupUsers, "EU");
		await RemoveFromQueueList(datastore._naSignupUsers, "NA");

		await SetChannelNameToCurrentUsers();
	}

	[Command("remove"), AllowedChannelsService]
	[Alias("leave", "l")]
	public async Task RemoveUserFromPug(string region)
	{
		await Context.Message.DeleteAsync();

		if (datastore.CurrentPugState != DataStore.PugState.WaitingForPlayer)
			return;

		region = region.ToLower();

		switch (region)
		{
			case "na":
				await RemoveFromQueueList(datastore._naSignupUsers, "NA");
				break;
			case "eu":
				await RemoveFromQueueList(datastore._euSignupUsers, "EU");
				break;
			case "b":
			case "both":
				await RemoveFromQueueList(datastore._euSignupUsers, "EU");
				await RemoveFromQueueList(datastore._naSignupUsers, "NA");
				break;
			default:
				string replayString = string.Format($"{Context.User.GetName()}, Wrong region use .l <na/eu/both>.");
				await ReplyAsync(replayString);

				return;
		}

		await SetChannelNameToCurrentUsers();
	}

	private async Task RemoveFromQueueList(List<PugUser> signupList, string regionText)
	{
		var user = Context.User;

		bool removed = datastore.RemoveUserFromNotStartedPug(signupList, user);

		if (removed)
		{
			string replayString = $"**{Context.User.GetName()}**, you have left the pug. **{regionText}** {signupList.Count}/{MAX_PLAYERS} players.\n" + string.Join("\n", signupList.Select(x => $"**{x.IUser.GetName()}**"));
			await ReplyAsync(replayString);
		}
		else
		{
			string replayString = $"**{Context.User.GetName()}**, You can't leave since you are not in the {regionText} queue. {regionText} {signupList.Count}/{MAX_PLAYERS}\n" + string.Join("\n", signupList.Select(x => $"**{x.IUser.GetName()}**"));

			// ReplyAsync is a method on ModuleBase
			await ReplyAsync(replayString);
		}
	}


	private async Task SetChannelNameToCurrentUsers()
	{
		if (Context.Client.GetChannel(_appConfig.AllowedChannel) is SocketTextChannel channel)
		{
			if (datastore._euSignupUsers.Any() || datastore._naSignupUsers.Any())
			{
				await channel.ModifyAsync(x =>
				{
					x.Name = $"{CHANNEL_NAME}_eu{datastore._euSignupUsers.Count()}_na{datastore._naSignupUsers.Count()}";
				});
			}
			else
			{
				await channel.ModifyAsync(x =>
				{
					x.Name = CHANNEL_NAME;
				});
			}
		}
	}

	private void ReadyUpTimerProc(object state)
	{
		if(datastore.CurrentPugState == DataStore.PugState.Readyup)
		{
			datastore.CurrentPugState = DataStore.PugState.WaitingForPlayer;

			datastore._signupUsers.RemoveAll(x => !x.IsReady);
			datastore._signupUsers.ForEach(x => x.IsReady = false);

			string replayString2 = "Removing users that didn't ready up.\n";

			string replayString = string.Format(" {0}/{1} players.\n", datastore._signupUsers.Count, MAX_PLAYERS) + string.Join("\n", datastore._signupUsers.Select(x => x.IUser.Mention + " Rating:" + x.GetDisplayRanking()));

			ReplyAsync(replayString2 + replayString);

			SetChannelNameToCurrentUsers();
		}
	}
	
	[Command("ready"), AllowedChannelsService]
	[Alias("r", "go", "letsfuckingdothis")]
	public async Task PlayerReady()
	{
		await Context.Message.DeleteAsync();

		if (datastore.CurrentPugState != DataStore.PugState.Readyup)
		{
			await ReplyAsync($"**{Context.User.GetName()}**, You can't ready up at this time");
			return;
		}
			
		
		var user = Context.User;

		var pugUser = datastore.GetUserInPugInNotStartedPug(user);

		if(pugUser != null)
		{
			pugUser.IsReady = true;

			if(datastore._signupUsers.Count(x => x.IsReady) == MAX_PLAYERS)
			{
				datastore.CurrentPugState = DataStore.PugState.PickingPlayers;

				string everyoneReady = "Everyone is ready, randoming captains.";

				//TODO This dosen't remove the players for some reason.
				datastore._euSignupUsers = datastore._euSignupUsers.Where(x => !datastore._signupUsers.Select(y => y.DatabaseUser.Id).Contains(x.DatabaseUser.Id)).ToList(); // Except(x => x. datastore._signupUsers).ToList();
				datastore._naSignupUsers = datastore._naSignupUsers.Where(x => !datastore._signupUsers.Select(y => y.DatabaseUser.Id).Contains(x.DatabaseUser.Id)).ToList();

				await ReplyAsync(everyoneReady);				

				await ChooseCaptains();
			}
			else
			{


				string replayString = string.Format(" {0}/{1} players ready. Write .r to ready up.\n", datastore._signupUsers.Count(x => x.IsReady), MAX_PLAYERS) + string.Join("\n", datastore._signupUsers.Where(x => x.IsReady == false).Select(x => x.IUser.Mention));


				// ReplyAsync is a method on ModuleBase
				var readyMessage = await ReplyAsync(replayString);

				if (datastore.LastReadyMessage.Any())
				{
					await datastore.LastReadyMessage.Dequeue().DeleteAsync();
				}

				datastore.LastReadyMessage.Enqueue(readyMessage);
			}
		}
		else
		{
			await ReplyAsync("You are not in the pug");
		}
	}

	public async Task ChooseCaptains()
	{
		var chooseableCapatins = datastore._signupUsers.Where(x => (x.DatabaseUser.Wins + x.DatabaseUser.Loses) >= 15);

		if(chooseableCapatins.Count() < 2)
		{
			chooseableCapatins = datastore._signupUsers;
		}

		int numOfChoosable = Math.Min(chooseableCapatins.Count(), 5);

		var orderList = chooseableCapatins.OrderByDescending(x => x.DatabaseUser.SkillRating).Take(numOfChoosable);

		Random random = new Random();
		int indexCapt1 = random.Next(orderList.Count());

		PugUser capt1 = orderList.ToList()[indexCapt1];

		int indexAbove = indexCapt1 - 1;
		int indexBelow = indexCapt1 + 1;

		double skillDiffAbove = double.MaxValue;
		double skillDiffBelow = double.MaxValue;

		var playerAbove = orderList.ElementAtOrDefault(indexAbove);
		var playerBelow = orderList.ElementAtOrDefault(indexBelow);

		if (playerAbove != null)
		{
			skillDiffAbove = Math.Abs(capt1.DatabaseUser.SkillRating - playerAbove.DatabaseUser.SkillRating);
		}

		if (playerBelow != null)
		{
			skillDiffBelow = Math.Abs(capt1.DatabaseUser.SkillRating - playerBelow.DatabaseUser.SkillRating);
		}

		PugUser capt2;

		if(skillDiffAbove < skillDiffBelow)
		{
			capt2 = playerAbove;
		}
		else
		{
			capt2 = playerBelow;
		}
	
		

		//Make sure so the lowest skillrating of the captains get first pick.
		if(capt1.DatabaseUser.SkillRating > capt2.DatabaseUser.SkillRating)
		{
			var tempCap = capt1;
			capt1 = capt2;
			capt2 = tempCap;
		}

		capt1.IsPicked = true;
		capt1.Team = DataStore.Teams.TeamOne;
		capt2.IsPicked = true;
		capt2.Team = DataStore.Teams.TeamTwo;

		datastore._captain1 = capt1;
		datastore._captain2 = capt2;

		//datastore._currentTeamPicking = DataStore.Teams.TeamOne;


		int curId = 1;
		datastore._signupUsers.Where(x => x.IsPicked == false).ToList().ForEach(x =>
		{
			x.PickID = curId;
			curId++;
		});

		await ReplyAsync($"Captain 1: {capt1.IUser.Mention} Skill: {capt1.GetDisplayRanking()}\nCaptain 2: {capt2.IUser.Mention} Skill: {capt2.GetDisplayRanking()}");

		await ReplyAsync("(.pick <number>)\n" + string.Join("\n", datastore._signupUsers.Where(x => x.IsPicked == false).Select(x => $"**{x.PickID}.** {x.IUser.GetName()} Rating: **{x.GetDisplayRanking()}** {x.DatabaseUser.Info ?? ""}")) + "\n" + capt1.IUser.Mention + " turn to pick.");
	}


	[Command("pick"), AllowedChannelsService]
	[Alias("p", "ichooseyou")]
	public async Task PickPlayer(int playerNummer)
	{
		if (datastore.CurrentPugState != DataStore.PugState.PickingPlayers)
			return;

		var user = Context.User;

		var wannaPickUser = datastore._signupUsers.FirstOrDefault(x => x.PickID == playerNummer && x.IsPicked == false);

		DataStore.Teams currentTeamPick = PickOrder[datastore._currentTeamPickingIndex];

		if (PickOrder[datastore._currentTeamPickingIndex] == DataStore.Teams.TeamOne && user.Id == datastore._captain1.IUser.Id ||
				PickOrder[datastore._currentTeamPickingIndex] == DataStore.Teams.TeamTwo && user.Id == datastore._captain2.IUser.Id)
		{
			if (wannaPickUser != null)
			{
				wannaPickUser.IsPicked = true;
				wannaPickUser.Team = currentTeamPick;

				//datastore._currentTeamPicking = datastore._currentTeamPicking == DataStore.Teams.TeamOne ? DataStore.Teams.TeamTwo : DataStore.Teams.TeamOne;

				int unpickedPlayers = datastore._signupUsers.Count(x => x.IsPicked == false);

				if (unpickedPlayers == 1)
				{
					var lastPlayer = datastore._signupUsers.First(x => x.IsPicked == false);

					lastPlayer.IsPicked = true;
					lastPlayer.Team = DataStore.Teams.TeamOne;

					await StartPug();
				}
				else
				{
					datastore._currentTeamPickingIndex++;

					DataStore.Teams nextTeamsPick = PickOrder[datastore._currentTeamPickingIndex];

					var nextCaptainToPick = nextTeamsPick == DataStore.Teams.TeamOne ? datastore._captain1 : datastore._captain2;

					await ReplyAsync($"{nextCaptainToPick.IUser.Mention} turn to pick.\n" + string.Join("\n", datastore._signupUsers.Where(x => x.IsPicked == false).Select(x => $"**{x.PickID}.** {x.IUser.GetName()} Rating: **{x.GetDisplayRanking()}** {x.DatabaseUser.Info ?? ""}")));
				}
			}
			else
			{
				await ReplyAsync("Can't pick that player");
			}
		}
		else
		{
			await ReplyAsync("You are not allowed to pick.");
		}
	}

	async Task StartPug()
	{
		datastore.CurrentPugState = DataStore.PugState.MapVoting;

		

		Random rnd = new Random();

		datastore.CurrentPug.VoteableMaps = datastore.AllMaps.OrderBy(x => rnd.Next()).Take(3).ToList();

		string team1String = string.Join("\n", datastore._signupUsers.Where(x => x.Team == DataStore.Teams.TeamOne).Select(x => x.IUser.GetName() + " " + x.GetDisplayRanking()));
		string team2String = string.Join("\n", datastore._signupUsers.Where(x => x.Team == DataStore.Teams.TeamTwo).Select(x => x.IUser.GetName() + " " + x.GetDisplayRanking()));

		string replayString = "**Team 1:**\n" + team1String + "\n**Team 2:**\n" + team2String;

		int i = 1;
		replayString += $"\n**Vote for map, you have {MAP_VOTE_TIME}sec.** (.vote <number>)\n" + string.Join("\n", datastore.CurrentPug.VoteableMaps.Select(x =>  $"{i++}. {x.Name}"));

		await ReplyAsync(replayString);
		
		var autoEvent = new AutoResetEvent(false);

		datastore.MapVoteTimer = new Timer(new TimerCallback(MapVoteTimerProcAsync), autoEvent, MAP_VOTE_TIME * 1000, 0);
	}

	private async void MapVoteTimerProcAsync(object state)
	{
		var firstInListMap = datastore.CurrentPug.VoteableMaps.OrderByDescending(x => x.Votes).FirstOrDefault();

		Random random = new Random();

		var allMapsWithSameAmountVotes = datastore.CurrentPug.VoteableMaps.Where(x => x.Votes == firstInListMap.Votes);
		var mostVotedMap = allMapsWithSameAmountVotes.ElementAt(random.Next(0, allMapsWithSameAmountVotes.Count()));

		datastore.CurrentPug.MapPicked = mostVotedMap;

		var autoEvent = new AutoResetEvent(false);

		datastore.GameModeVoteTimer = new Timer(new TimerCallback(GameModeVoteTimerProcAsync), autoEvent, GAMEMODE_VOTE_TIME * 1000, 0);

		string replayString = "\n Map: ** " + datastore.CurrentPug.MapPicked.Name + $" ** \n**Vote for gamemode, you have {GAMEMODE_VOTE_TIME}sec.** (.vote <number>)\n" + string.Join("\n", datastore.AllGameModes.Select(x => $"{x.Id}. {x.Name}"));

		datastore.CurrentPugState = DataStore.PugState.GameModeVoting;

		await ReplyAsync(replayString);
	}


	private async void GameModeVoteTimerProcAsync(object state)
	{
		var firstInListMap = datastore.AllGameModes.OrderByDescending(x => x.Votes).FirstOrDefault();

		Random random = new Random();

		var allGamemodesWithSameAmountVotes = datastore.AllGameModes.Where(x => x.Votes == firstInListMap.Votes);
		var mostVotedGameMode = allGamemodesWithSameAmountVotes.ElementAt(random.Next(0, allGamemodesWithSameAmountVotes.Count()));


		var allUsers = datastore._signupUsers.ToList();

		Matches match = new Matches()
		{
			PlayedDate = DateTime.UtcNow,
			GameMode = mostVotedGameMode,
			Map = datastore.CurrentPug.MapPicked,
		};

		match.UserMatches = new List<UsersMatchesRelation>();
		foreach (var user in allUsers)
		{
			var rel = new UsersMatchesRelation()
			{
				Match = match,
				User = user.DatabaseUser,
				Team = (int)user.Team,
				SkillRating = user.DatabaseUser.SkillRating
			};
			if (datastore._captain1.DatabaseUser.Id == user.DatabaseUser.Id || datastore._captain2.DatabaseUser.Id == user.DatabaseUser.Id)
			{
				rel.IsCaptain = true;
			}

			match.UserMatches.Add(rel);
		}
		match.Region = datastore.CurrentPug.Region;
		
		datastore.db.Matches.Add(match);

		await datastore.db.SaveChangesAsync();

		string team1String = string.Join("\n", datastore._signupUsers.Where(x => x.Team == DataStore.Teams.TeamOne).Select(x => x.IUser.GetName() + " " + x.GetDisplayRanking()));
		string team2String = string.Join("\n", datastore._signupUsers.Where(x => x.Team == DataStore.Teams.TeamTwo).Select(x => x.IUser.GetName() + " " + x.GetDisplayRanking()));

		string replayString = "**Team 1:**\n" + team1String + "\n**Team 2:**\n" + team2String;

		await ReplyAsync($"Voting ended. **{datastore.CurrentPug.Region.ToString()} {datastore.CurrentPug.MapPicked.Name} ({mostVotedGameMode.Name})** won the vote. Match Id: **{match.Id}**\n**GL HF**\nCaptains don't forget to enter match results after the match. (.result <win/lose>)\n" + replayString);

		ResetPug();		
	}


	[Command("map"), AllowedChannelsService]
	[Alias("m", "vote", "v")]
	public async Task VoteMap(int mapId)
	{
		if (datastore.CurrentPugState != DataStore.PugState.MapVoting && datastore.CurrentPugState != DataStore.PugState.GameModeVoting)
			return;

		var user = Context.User;
		var pugUser = datastore.GetUserInPugInNotStartedPug(user);

		if (pugUser != null)
		{
			await Context.Message.DeleteAsync();

			if (datastore.CurrentPugState == DataStore.PugState.MapVoting)
			{
				if (!pugUser.HaveVotedForMap)
				{
					if(mapId <= 0 || mapId > datastore.CurrentPug.VoteableMaps.Count)
					{
				
						await ReplyAsync($"{user.GetName()}, Invalid map number.");
						
					}
					else
					{
						var map = datastore.CurrentPug.VoteableMaps[mapId - 1];

						if (map != null)
						{
							map.Votes++;
							pugUser.HaveVotedForMap = true;

							await ReplyAsync($"{user.GetName()}, voted for **{map.Name}**.");
						}
						else
						{
							await ReplyAsync($"{user.GetName()}, Invalid map number.");
						}
					}
				}
				else
				{
					await ReplyAsync($"{user.GetName()}, You have already voted for a map.");
				}
			}
			else if(datastore.CurrentPugState == DataStore.PugState.GameModeVoting)
			{
				if (!pugUser.HaveVotedForGameMode)
				{

					var gameMode = datastore.AllGameModes.FirstOrDefault(x => x.Id == mapId);

					if (gameMode != null)
					{
						gameMode.Votes++;
						pugUser.HaveVotedForGameMode = true;

						await ReplyAsync($"{user.GetName()}, voted for **{gameMode.Name}**.");
					}
					else
					{
						await ReplyAsync($"{user.GetName()}, Invalid map number.");
					}
				}
				else
				{
					await ReplyAsync($"{user.GetName()}, You have already voted for a map.");
				}
			}

		}		
		else
		{
			await ReplyAsync($"{user.GetName()}, Only people in the pug can vote for map/gamemode.");
		}
	}

	[Command("reportmatch"), AllowedChannelsService]
	[Alias("enterscore", "choosewinner", "result")]
	public async Task EnterPugWinner(string winner)
	{
		string winString = winner.ToLower();
				
		if (winString == "win" || winString == "lose")
		{			
			var user = datastore.GetOrCreateUser(Context.User);

			var notDoneMatches = datastore.db.MatchesWithUsers().Where(x => x.MatchState == 0);

			var captainOfMatch = notDoneMatches.SelectMany(x => x.UserMatches).Where(x => x.IsCaptain == true && x.UserId == user.Id).FirstOrDefault();

			if (captainOfMatch != null)
			{
				Matches match = notDoneMatches.Where(x => x.Id == captainOfMatch.MatchId).FirstOrDefault();// captainOfMatch.Match;
				if (winString == "cancel")
				{
					match.MatchState = -1;

					datastore.db.Matches.Update(match);

					datastore.db.SaveChangesAsync();
					return;
				}

				DataStore.Teams winTeam;
				if (winString == "win")
				{
					winTeam = (DataStore.Teams)captainOfMatch.Team;
				}
				else
				{
					winTeam = (DataStore.Teams)captainOfMatch.Team == DataStore.Teams.TeamOne ? DataStore.Teams.TeamTwo : DataStore.Teams.TeamOne;
				}

				var team1Players = match.GetTeam1Users(datastore);
				var team2Players = match.GetTeam2Users(datastore);
				var team1Captain = match.UserMatches.First(x => x.IsCaptain && x.Team == 1).User;
				var team2Captain = match.UserMatches.First(x => x.IsCaptain && x.Team == 2).User;

				Debug.Assert(team1Players.Any(), "No team 1 players");
				Debug.Assert(team2Players.Any(), "No team 2 players");

				Debug.Assert(team1Captain != null, "No team 1 captain");
				Debug.Assert(team2Captain != null, "No team 2 captain");

				List<Users> usersWon ;
				List<Users> usersLost;

				if (winTeam == DataStore.Teams.TeamOne)
				{
					usersWon = team1Players;
					usersLost = team2Players;

					team1Captain.WinsAsCaptain++;
					team2Captain.LosesAsCaptain++;
				}
				else
				{
					usersWon = team2Players;
					usersLost = team1Players;

					team1Captain.LosesAsCaptain++;
					team2Captain.WinsAsCaptain++;
				}
					
				usersWon.ForEach(x =>
				{
					x.Wins++;
				});
					
				usersLost.ForEach(x =>
				{
					x.Loses++;
				});

				datastore.db.Users.UpdateRange(usersWon);
				datastore.db.Users.UpdateRange(usersLost);

				match.MatchState = 1;
				match.TeamWinner = (int)winTeam;				

				datastore.db.Matches.Update(match);

				UpdatePlayersRating(match);

				double winningTeamAvgSkillRating = usersWon.Sum(x => x.PreviousSkillRating) / usersWon.Count;
				double loosingTeamAvgSkillRating = usersLost.Sum(x => x.PreviousSkillRating) / usersLost.Count;

				string userRatingsChange = $"Avg SR: {winningTeamAvgSkillRating.ToString("F0")}\n" +  string.Join("\n", usersWon.Select(x => $"{x.UserName} {x.SkillRating.ToString("F0")} ({(x.SkillRating - x.PreviousSkillRating).ToString("F0")})")) + "\n\n" + $"Avg SR: {loosingTeamAvgSkillRating.ToString("F0")}\n" + string.Join("\n", usersLost.Select(x => $"{x.UserName} {x.SkillRating.ToString("F0")} ({(x.SkillRating - x.PreviousSkillRating).ToString("F0")})"));

				await ReplyAsync($"Match finished **{ winTeam.ToString()} Won!** {match.Id} {match.Map.Name} ({match.GameMode.Name}) {match.Region.ToString()}\n{ userRatingsChange}");
			}			
			else
			{
				await ReplyAsync($"{Context.User.GetName()}, Only captains can enter match results.");
			}
		}
		else
		{
			await ReplyAsync("\"Win\" if you team won, and \"Lose\" if your team lost. It's not that hard.");
		}	
	}

	private void UpdatePlayersRating(Matches match)
	{
		foreach(var player in match.GetAllUsers(datastore))
		{
			player.PreviousSkillRating = player.SkillRating;
		}


		var calculator = new RatingCalculator(/* initVolatility, tau */);

		// Instantiate a RatingPeriodResults object.
		var results = new RatingPeriodResults();

		var ratingsPlayers = new List<Tuple<Users, Rating>>();
				
		double team1Rating = match.GetTeam1Users(datastore).Sum(x => x.SkillRating) / match.GetTeam1Users(datastore).Count;
		double team2Rating = match.GetTeam2Users(datastore).Sum(x => x.SkillRating) / match.GetTeam2Users(datastore).Count;

		double team1RatingsDeviation = match.GetTeam1Users(datastore).Sum(x => x.RatingsDeviation) / match.GetTeam1Users(datastore).Count;
		double team2RatingsDeviation = match.GetTeam2Users(datastore).Sum(x => x.RatingsDeviation) / match.GetTeam2Users(datastore).Count;

		double team1Volatility = match.GetTeam1Users(datastore).Sum(x => x.Volatility) / match.GetTeam1Users(datastore).Count;
		double team2Volatility = match.GetTeam2Users(datastore).Sum(x => x.Volatility) / match.GetTeam2Users(datastore).Count;

		var team1RatingCalc = new Rating(calculator, team1Rating, team1RatingsDeviation, team1Volatility);
		var team2RatingCalc = new Rating(calculator, team2Rating, team2RatingsDeviation, team2Volatility);

		foreach (var player in match.GetTeam1Users(datastore))
		{
			var playerRating = new Rating(calculator, player.SkillRating, player.RatingsDeviation, player.Volatility);

			ratingsPlayers.Add(new Tuple<Users, Rating>(player, playerRating));

			if ((DataStore.Teams)match.TeamWinner == DataStore.Teams.TeamOne)
			{
				results.AddResult(playerRating, team2RatingCalc);
			}
			else if ((DataStore.Teams)match.TeamWinner == DataStore.Teams.TeamTwo)
			{
				results.AddResult(team2RatingCalc, playerRating);
			}
		}

		foreach (var player in match.GetTeam2Users(datastore))
		{
			var playerRating = new Rating(calculator, player.SkillRating, player.RatingsDeviation, player.Volatility);

			ratingsPlayers.Add(new Tuple<Users, Rating>(player, playerRating));

			if ((DataStore.Teams)match.TeamWinner == DataStore.Teams.TeamOne)
			{
				results.AddResult(team1RatingCalc, playerRating);
			}
			else if ((DataStore.Teams)match.TeamWinner == DataStore.Teams.TeamTwo)
			{
				results.AddResult(playerRating, team1RatingCalc);
			}
		}

		calculator.UpdateRatings(results);

		foreach (var player in ratingsPlayers)
		{
			player.Item1.SkillRating = player.Item2.GetRating();
			player.Item1.RatingsDeviation = player.Item2.GetRatingDeviation();
			player.Item1.Volatility = player.Item2.GetVolatility();
		}

		datastore.db.SaveChangesAsync();
	}

	[Command("cancel"), AllowedChannelsService]
	[RequireUserPermission(ChannelPermission.ManageChannel)]
	public async Task CancelPug()
	{
		ResetPug();

		await ReplyAsync("**Pug canceled**");
	}

	[Command("adminreportscore"), AllowedChannelsService]
	[RequireUserPermission(ChannelPermission.ManageChannel)]
	public async Task AdminReportScore(int matchId, int teamWinner)
	{
		if(!(teamWinner == 1 || teamWinner == 2))
		{
			await ReplyAsync("**Invalid teamWinner*");
			return;
		}

		var match = datastore.db.MatchesWithUsers().FirstOrDefault(x => x.Id == matchId);

		await ReplyAsync(match.Id + " " + match.PlayedDate + string.Join("\n", match.GetAllUsers(datastore).Select(x => x.UserName)));

		
		if(match != null)
		{
			match.TeamWinner = teamWinner;
			//Warning this is bugged
			UpdatePlayersRating(match);
			UpdatePlayersRating(match);

			datastore.db.Matches.Update(match);

			await datastore.db.SaveChangesAsync();

			await ReplyAsync("**Pug Score fixed**");
		}
		else
		{
			await ReplyAsync("**Invaild mapid*");
		}
	}


	void ResetPug()
	{
		datastore._signupUsers.Clear();
		datastore.CurrentPugState = DataStore.PugState.WaitingForPlayer;
		datastore.AllMaps.ForEach(x =>
		{
			x.Votes = 0;
		});

		datastore.AllGameModes.ForEach(x =>
		{
			x.Votes = 0;
		});

		datastore._currentTeamPickingIndex = 0;

		SetChannelNameToCurrentUsers();
	}
}
