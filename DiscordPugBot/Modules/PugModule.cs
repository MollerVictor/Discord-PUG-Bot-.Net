using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordPugBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Pomelo.EntityFrameworkCore.MySql;
using Pomelo.EntityFrameworkCore;
using System.IO;
using System.Diagnostics;
using Glicko2;
using Microsoft.Extensions.Configuration;
using DiscordPugBot.Properties;
using Microsoft.Extensions.Options;


public class PugModule : ModuleBase<SocketCommandContext>
{
	//TODO Move these to the config file
	
	private const int MAP_VOTE_TIME = 60;
	private const int GAMEMODE_VOTE_TIME = 45;
	private const int READY_UP_TIME = 75;
	private readonly Color EMBED_MESSAGE_COLOR = new Color(120, 40, 40);

	private const bool CHANGE_DISCORD_LOGO = false;

	private List<DataStore.Teams> PickOrder = new List<DataStore.Teams> {  DataStore.Teams.TeamOne,
																			DataStore.Teams.TeamTwo,
																			DataStore.Teams.TeamOne,
																			DataStore.Teams.TeamTwo,
																			DataStore.Teams.TeamOne,
																			DataStore.Teams.TeamTwo,
																			DataStore.Teams.TeamTwo,
																			DataStore.Teams.TeamOne,};

	int _maxPlayers;

	public DataStore datastore;
	AppConfig _appConfig;

	Image[] _logoImages;

	public PugModule(DataStore ds, IOptions<AppConfig> appConfig)
	{
		datastore = ds;
		_appConfig = appConfig.Value;

		_maxPlayers = _appConfig.PlayersPerTeam * 2;

		if (CHANGE_DISCORD_LOGO)
		{
			_logoImages = new Image[13];
			for (int i = 0; i <= 12; i++)
			{
				_logoImages[i] = new Image($@"C:\test\logo{i}.png");
			}
		}
	}


	[Command("add"), AllowedChannelsService]
	[Alias("a")]
	public async Task AddUserToPug()
	{
		await Context.Message.DeleteAsync();

		if (datastore.CurrentPugState != DataStore.PugState.WaitingForPlayer)
		{
			await SendEmbededMessageAsync(string.Format(Resources.ErrorWaitUntilTheyFinishedPicking, Context.User.GetName()));

			return;
		}

		if (_appConfig.UseRegionEU)
		{
			await AddToQueueList(datastore._euSignupUsers, "EU");
		}

		if(_appConfig.UseRegionNA)
		{
			await AddToQueueList(datastore._naSignupUsers, "NA");
		}
		

		await SetChannelNameToCurrentUsers();
	}

	[Command("add"), AllowedChannelsService]
	[Alias("a")]
	public async Task AddUserToPug(string region)
	{
		await Context.Message.DeleteAsync();

		if (datastore.CurrentPugState != DataStore.PugState.WaitingForPlayer)
		{
			await SendEmbededMessageAsync(string.Format(Resources.ErrorWaitUntilTheyFinishedPicking, Context.User.GetName()));
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
				string replayString = string.Format(Resources.ErrorWrongRegionAdd, Context.User.GetName());
				await SendEmbededMessageAsync(replayString);

				return;
		}

		await SetChannelNameToCurrentUsers();
	}

	private async Task AddToQueueList(List<PugUser> signupList, string regionText)
	{
		var user = Context.User;

		if (signupList.Count >= _maxPlayers)
		{
			string replayString = string.Format(Resources.ErrorPugIsFull, Context.User.GetName());
			await SendEmbededMessageAsync(replayString);
		}
		else
		{
			if (signupList.Select(x => x.IUser.Id).Contains(user.Id))
			{
				string replayString = string.Format(Resources.ErrorYouAreAlreadySignedUp, Context.User, regionText,signupList.Count,_maxPlayers);
				await SendEmbededMessageAsync(replayString);
			}
			else
			{
				var pugUser = new PugUser(user, datastore.GetOrCreateUser(user));

				signupList.Add(pugUser);

				string replayString = string.Format(Resources.PlayerJoinedTheQueue, regionText, signupList.Count, _maxPlayers,Context.User.GetName()) + string.Join("\n", signupList.Select(x => string.Format(Resources.NameAndRating, x.IUser.GetName(), x.GetDisplayRanking())));

				await SendEmbededMessageAsync(replayString);

				if (signupList.Count >= _maxPlayers)
				{
					datastore.SignedUpUsers = signupList;

					datastore.CurrentPug = new Pug()
					{
						Region = regionText == "EU" ? Region.EU : Region.NA
					};
					string replayString2 = string.Format(Resources.PugIsNowFull, regionText);

					datastore.CurrentPugState = DataStore.PugState.Readyup;

					var autoEvent = new AutoResetEvent(false);
					datastore.ReadyUpTimer = new Timer(new TimerCallback(ReadyUpTimerProc), autoEvent, READY_UP_TIME * 1000, 0);

					await SendEmbededMessageAsync(replayString2);
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

		if (_appConfig.UseRegionEU)
		{
			await RemoveFromQueueList(datastore._euSignupUsers, "EU");
		}

		if(_appConfig.UseRegionNA)
		{
			await RemoveFromQueueList(datastore._naSignupUsers, "NA");
		}
		

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
				string replayString = string.Format(Resources.ErrorWrongRegionLeave, Context.User.GetName());
				await SendEmbededMessageAsync(replayString);
				
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
			string replayString = string.Format(Resources.PlayerLeftTheQueue, Context.User.GetName(),regionText,signupList.Count,_maxPlayers) + string.Join("\n", signupList.Select(x => string.Format(Resources.NameAndRating, x.IUser.GetName(), x.GetDisplayRanking())));
			await SendEmbededMessageAsync(replayString);
		}
		else
		{
			string replayString = string.Format(Resources.ErrorLeavePugYouareNotIn, Context.User.GetName(),regionText, signupList.Count, _maxPlayers) + string.Join("\n", signupList.Select(x => string.Format(Resources.NameAndRating, x.IUser.GetName(), x.GetDisplayRanking())));

			await SendEmbededMessageAsync(replayString);
		}
	}


	private async Task SetChannelNameToCurrentUsers()
	{
		if (Context.Client.GetChannel(_appConfig.AllowedChannel) is SocketTextChannel channel)
		{
			if (datastore._euSignupUsers.Any() || datastore._naSignupUsers.Any())
			{
				if (CHANGE_DISCORD_LOGO)
				{
					await Context.Guild.ModifyAsync(x =>
					{
						x.Icon = _logoImages[datastore._euSignupUsers.Count];
					});
				}

				string baseString = $"{_appConfig.ChannelDisplayName}";
				string euRegion = _appConfig.UseRegionEU ? $"_eu{ datastore._euSignupUsers.Count()}" : "";
				string naRegion = _appConfig.UseRegionNA ? $"_na{ datastore._naSignupUsers.Count()}" : "";

				string channelName = baseString + euRegion + naRegion;

				await channel.ModifyAsync(x =>
				{
					x.Name = channelName;
				});
			}
			else
			{
				if(CHANGE_DISCORD_LOGO)
				{
					await Context.Guild.ModifyAsync(x =>
					{
						x.Icon = _logoImages[0];
					});
				}


				await channel.ModifyAsync(x =>
				{
					x.Name = _appConfig.ChannelDisplayName;
				});
			}
		}
	}

	private void ReadyUpTimerProc(object state)
	{
		if(datastore.CurrentPugState == DataStore.PugState.Readyup)
		{
			datastore.CurrentPugState = DataStore.PugState.WaitingForPlayer;

			datastore.SignedUpUsers.RemoveAll(x => !x.IsReady);
			datastore.SignedUpUsers.ForEach(x => x.IsReady = false);

			string replayString = string.Format(Resources.RemovingPlayerThatIsNotReady, datastore.SignedUpUsers.Count, _maxPlayers) + string.Join("\n", datastore.SignedUpUsers.Select(x => string.Format(Resources.NameAndRating, x.IUser.GetName(), x.GetDisplayRanking())));

			SendEmbededMessageAsync(replayString);

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
			await SendEmbededMessageAsync(string.Format(Resources.ErrorCantReadyUpRightNow, Context.User.GetName()));
			return;
		}
			
		
		var user = Context.User;

		var pugUser = datastore.GetUserInPugInNotStartedPug(user);

		if(pugUser != null)
		{
			pugUser.IsReady = true;

			if(datastore.SignedUpUsers.Count(x => x.IsReady) == _maxPlayers)
			{
				datastore.CurrentPugState = DataStore.PugState.PickingPlayers;
				
				datastore._euSignupUsers = datastore._euSignupUsers.Where(x => !datastore.SignedUpUsers.Select(y => y.DatabaseUser.Id).Contains(x.DatabaseUser.Id)).ToList();
				datastore._naSignupUsers = datastore._naSignupUsers.Where(x => !datastore.SignedUpUsers.Select(y => y.DatabaseUser.Id).Contains(x.DatabaseUser.Id)).ToList();

				await SendEmbededMessageAsync(Resources.EveryoneReady);				

				await ChooseCaptains();
			}
			else
			{
				string replayString = string.Format(Resources.PleaseReadyUp, datastore.SignedUpUsers.Count(x => x.IsReady), _maxPlayers) + string.Join("\n", datastore.SignedUpUsers.Where(x => x.IsReady == false).Select(x => x.IUser.Mention));

				await SendEmbededMessageAsync(replayString);

				//Alternating between editing message and sending a new message, to send notice beeps but avoid beeing spam limited.
				/*if (datastore._signupUsers.Count(x => x.IsReady) % 3 == 0)
				{
					

					//Delete the last ready up messagee, to avoid cluttering the chat
					if (datastore.LastReadyMessage.Any())
					{
						//await datastore.LastReadyMessage.Dequeue().DeleteAsync();
					}
					datastore.LastReadyMessage.Enqueue(readyMessage);
				}
				else
				{
					if (datastore.LastReadyMessage.Any())
					{
						await datastore.LastReadyMessage.Peek().ModifyAsync(x =>
						{
							x.Content = replayString;
						});
					}		
				}*/				
			}
		}
		else
		{
			await SendEmbededMessageAsync(Resources.ErrorYouAreNotInThePug);
		}
	}

	public async Task ChooseCaptains()
	{
		//Tries to find captain that have alteast 15 games
		var chooseableCapatins = datastore.SignedUpUsers.Where(x => (x.DatabaseUser.Wins + x.DatabaseUser.Loses) >= 15);

		//If it cant find 2 that can do that, just include everyone
		if(chooseableCapatins.Count() < 2)
		{
			chooseableCapatins = datastore.SignedUpUsers;
		}

		int numOfChoosable = Math.Min(chooseableCapatins.Count(), 5);

		var orderList = chooseableCapatins.OrderByDescending(x => x.DatabaseUser.SkillRating).Take(numOfChoosable);

		Random random = new Random();

		//First captain is random between the 5 highest ranked players
		int indexCapt1 = random.Next(orderList.Count());
		PugUser capt1 = orderList.ToList()[indexCapt1];

		//Second captain is the one cloest in rating to the first captain.
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

		datastore.Captain1 = capt1;
		datastore.Captain2 = capt2;


		int curId = 1;
		datastore.SignedUpUsers.Where(x => x.IsPicked == false).ToList().ForEach(x =>
		{
			x.PickID = curId;
			curId++;
		});

		await SendEmbededMessageAsync(string.Format(Resources.CaptainsWithSkill, capt1.IUser.Mention, capt1.GetDisplayRanking(), capt2.IUser.Mention, capt2.GetDisplayRanking()));

		await SendEmbededMessageAsync(Resources.PickNumber +
			string.Join("\n", datastore.SignedUpUsers.Where(x => x.IsPicked == false).Select(x => string.Format(Resources.UserPickingInfo, x.PickID, x.IUser.GetName(), x.GetDisplayRanking(), x.DatabaseUser.Info ?? ""))) +
			"\n" + string.Format(Resources.PlayersTurnToPick, capt1.IUser.Mention));
	}


	[Command("pick"), AllowedChannelsService]
	[Alias("p", "ichooseyou")]
	public async Task PickPlayer(int playerNummer)
	{
		if (datastore.CurrentPugState != DataStore.PugState.PickingPlayers)
			return;

		var user = Context.User;

		var wannaPickUser = datastore.SignedUpUsers.FirstOrDefault(x => x.PickID == playerNummer && x.IsPicked == false);

		DataStore.Teams currentTeamPick = PickOrder[datastore.CurrentTeamPickingIndex];

		if (PickOrder[datastore.CurrentTeamPickingIndex] == DataStore.Teams.TeamOne && user.Id == datastore.Captain1.IUser.Id ||
				PickOrder[datastore.CurrentTeamPickingIndex] == DataStore.Teams.TeamTwo && user.Id == datastore.Captain2.IUser.Id)
		{
			if (wannaPickUser != null)
			{
				wannaPickUser.IsPicked = true;
				wannaPickUser.Team = currentTeamPick;

				int unpickedPlayers = datastore.SignedUpUsers.Count(x => x.IsPicked == false);

				if (unpickedPlayers == 1)
				{
					var lastPlayer = datastore.SignedUpUsers.First(x => x.IsPicked == false);

					lastPlayer.IsPicked = true;
					lastPlayer.Team = DataStore.Teams.TeamOne;

					await StartPug();
				}
				else
				{
					datastore.CurrentTeamPickingIndex++;

					DataStore.Teams nextTeamsPick = PickOrder[datastore.CurrentTeamPickingIndex];

					var nextCaptainToPick = nextTeamsPick == DataStore.Teams.TeamOne ? datastore.Captain1 : datastore.Captain2;

					await SendEmbededMessageAsync(string.Format(Resources.PlayersTurnToPick, nextCaptainToPick.IUser.Mention) + "\n" + string.Join("\n", datastore.SignedUpUsers.Where(x => x.IsPicked == false).Select(x => string.Format(Resources.UserPickingInfo, x.PickID, x.IUser.GetName(), x.GetDisplayRanking(), x.DatabaseUser.Info ?? ""))));
				}
			}
			else
			{
				await SendEmbededMessageAsync(Resources.ErrorCantPickThatPlayer);
			}
		}
		else
		{
			await SendEmbededMessageAsync(Resources.ErrorYouAreNotAllowedToPick);
		}
	}

	async Task StartPug()
	{
		datastore.CurrentPugState = DataStore.PugState.MapVoting;

		Random rnd = new Random();

		datastore.CurrentPug.VoteableMaps = datastore.AllMaps.OrderBy(x => rnd.Next()).Take(3).ToList();

		string team1String = string.Join("\n", datastore.SignedUpUsers.Where(x => x.Team == DataStore.Teams.TeamOne).Select(x => string.Format(Resources.NameAndRating, x.IUser.GetName(), x.GetDisplayRanking())));
		string team2String = string.Join("\n", datastore.SignedUpUsers.Where(x => x.Team == DataStore.Teams.TeamTwo).Select(x => string.Format(Resources.NameAndRating, x.IUser.GetName(), x.GetDisplayRanking())));

		string replayString = string.Format(Resources.TeamLineups, team1String, team2String);

		int i = 1;
		replayString += string.Format(Resources.VoteMap, MAP_VOTE_TIME) + string.Join("\n", datastore.CurrentPug.VoteableMaps.Select(x =>  $"{i++}. {x.Name}"));

		await SendEmbededMessageAsync(replayString);
		
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

		if(_appConfig.UseGameModes)
		{
			datastore.GameModeVoteTimer = new Timer(new TimerCallback(GameModeVoteTimerProcAsync), autoEvent, GAMEMODE_VOTE_TIME * 1000, 0);

			string replayString = string.Format(Resources.MapVoteFinishedVoteForGameMode, datastore.CurrentPug.MapPicked.Name, GAMEMODE_VOTE_TIME) + string.Join("\n", datastore.AllGameModes.Select(x => $"{x.Id}. {x.Name}"));

			datastore.CurrentPugState = DataStore.PugState.GameModeVoting;

			await SendEmbededMessageAsync(replayString);
		}
		else
		{
			StartMatch(null);
		}
	}


	private async void GameModeVoteTimerProcAsync(object state)
	{
		var firstInListMap = datastore.AllGameModes.OrderByDescending(x => x.Votes).FirstOrDefault();

		Random random = new Random();

		var allGamemodesWithSameAmountVotes = datastore.AllGameModes.Where(x => x.Votes == firstInListMap.Votes);
		var mostVotedGameMode = allGamemodesWithSameAmountVotes.ElementAt(random.Next(0, allGamemodesWithSameAmountVotes.Count()));


		StartMatch(mostVotedGameMode);	
	}

	async void StartMatch(GameModes gameMode)
	{
		var allUsers = datastore.SignedUpUsers.ToList();

		Matches match = new Matches()
		{
			PlayedDate = DateTime.UtcNow,
			GameMode = gameMode,
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
			if (datastore.Captain1.DatabaseUser.Id == user.DatabaseUser.Id || datastore.Captain2.DatabaseUser.Id == user.DatabaseUser.Id)
			{
				rel.IsCaptain = true;
			}

			match.UserMatches.Add(rel);
		}
		match.Region = datastore.CurrentPug.Region;

		datastore.db.Matches.Add(match);

		await datastore.db.SaveChangesAsync();

		string team1String = string.Join("\n", datastore.SignedUpUsers.Where(x => x.Team == DataStore.Teams.TeamOne).Select(x => x.IUser.GetName() + " " + x.GetDisplayRanking()));
		string team2String = string.Join("\n", datastore.SignedUpUsers.Where(x => x.Team == DataStore.Teams.TeamTwo).Select(x => x.IUser.GetName() + " " + x.GetDisplayRanking()));

		string replayString = string.Format(Resources.TeamLineups, team1String, team2String);

		await SendEmbededMessageAsync(string.Format(Resources.GameModeVoteFinished, datastore.CurrentPug.Region.ToString(), datastore.CurrentPug.MapPicked.Name, gameMode?.Name, match.Id) + replayString);

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
				
						await SendEmbededMessageAsync(string.Format(Resources.ErrorInvaildMapNumber, user.GetName()));
						
					}
					else
					{
						var map = datastore.CurrentPug.VoteableMaps[mapId - 1];

						if (map != null)
						{
							map.Votes++;
							pugUser.HaveVotedForMap = true;

							await SendEmbededMessageAsync(string.Format(Resources.PlayerVotedForMap, user.GetName(), map.Name));
						}
						else
						{
							await SendEmbededMessageAsync(string.Format(Resources.ErrorInvaildMapNumber, user.GetName()));
						}
					}
				}
				else
				{
					await SendEmbededMessageAsync(string.Format(Resources.ErrorAlreadyVotedForMap, user.GetName()));
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

						await SendEmbededMessageAsync(string.Format(Resources.PlayerVotedForMap, user.GetName(), gameMode.Name));
					}
					else
					{
						await SendEmbededMessageAsync(string.Format(Resources.ErrorInvaildMapNumber, user.GetName()));
					}
				}
				else
				{
					await SendEmbededMessageAsync(string.Format(Resources.ErrorAlreadyVotedForMap, user.GetName()));
				}
			}

		}		
		else
		{
			await SendEmbededMessageAsync(string.Format(Resources.ErrorOnlyPeopleInThePugCanVote, user.GetName()));
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

				string userRatingsChange = $"Avg SR: {winningTeamAvgSkillRating.ToString("F0")}\n" +  
					string.Join("\n", usersWon.Select(x => $"{x.UserName} {x.SkillRating.ToString("F0")} ({(x.SkillRating - x.PreviousSkillRating).ToString("F0")})")) 
					+ "\n\n" + 
					$"Avg SR: {loosingTeamAvgSkillRating.ToString("F0")}\n" + 
					string.Join("\n", usersLost.Select(x => $"{x.UserName} {x.SkillRating.ToString("F0")} ({(x.SkillRating - x.PreviousSkillRating).ToString("F0")})"));

				await SendEmbededMessageAsync(string.Format(Resources.MatchFinished, winTeam.ToString(),match.Id, match.Map.Name, match.GameMode?.Name, match.Region.ToString(), userRatingsChange));
			}			
			else
			{
				await SendEmbededMessageAsync(string.Format(Resources.ErrorOnlyCaptainsCanEnterResult,Context.User.GetName()));
			}
		}
		else
		{
			await SendEmbededMessageAsync(Resources.ErrorResultWinnerWrong);
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

		await SendEmbededMessageAsync(Resources.PugCanceled);
	}

	[Command("adminreportscore"), AllowedChannelsService]
	[RequireUserPermission(ChannelPermission.ManageChannel)]
	public async Task AdminReportScore(int matchId, int teamWinner)
	{
		if(!(teamWinner == 1 || teamWinner == 2))
		{
			await SendEmbededMessageAsync("**Invalid teamWinner*");
			return;
		}

		var match = datastore.db.MatchesWithUsers().FirstOrDefault(x => x.Id == matchId);

		await SendEmbededMessageAsync(match.Id + " " + match.PlayedDate + string.Join("\n", match.GetAllUsers(datastore).Select(x => x.UserName)));

		
		if(match != null)
		{
			match.TeamWinner = teamWinner;
			//Warning this is bugged
			UpdatePlayersRating(match);
			UpdatePlayersRating(match);

			datastore.db.Matches.Update(match);

			await datastore.db.SaveChangesAsync();

			await SendEmbededMessageAsync("**Pug Score fixed**");
		}
		else
		{
			await SendEmbededMessageAsync("**Invaild mapid*");
		}
	}


	void ResetPug()
	{
		datastore.SignedUpUsers.Clear();
		datastore.CurrentPugState = DataStore.PugState.WaitingForPlayer;
		datastore.AllMaps.ForEach(x =>
		{
			x.Votes = 0;
		});

		datastore.AllGameModes.ForEach(x =>
		{
			x.Votes = 0;
		});

		datastore.CurrentTeamPickingIndex = 0;

		SetChannelNameToCurrentUsers();
	}


	async Task<IUserMessage> SendEmbededMessageAsync(string message)
	{
		return await SendEmbededMessageAsync("Pug", message);
	}

	async Task<IUserMessage> SendEmbededMessageAsync(string title, string message)
	{
		var embed = new EmbedBuilder()
			.WithColor(EMBED_MESSAGE_COLOR)
			.WithTitle(title)
			.WithDescription(message)
			.Build();

		return await ReplyAsync("", false, embed);
	}
}
