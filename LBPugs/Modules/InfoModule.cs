using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OWPugs.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LBPugs.Properties;

public class InfoModule : ModuleBase<SocketCommandContext>
{
	public DataStore datastore;
	public InfoModule(DataStore ds)
	{
		datastore = ds;
	}

	[Command("info"), Summary("Get commands"), AllowedChannelsService]
	[Alias("command", "commands", "help")]
	public async Task PugCommands()
	{
		await ReplyAsync(Resources.HelpInfo);
	}
	
	[Command("status"), Summary("Get pug status."), AllowedChannelsService]
	public async Task PugStatus()
	{
		string addString = "";

		var runningMatches = datastore.db.MatchesWithUsers().Where(x => x.MatchState == 0);
		foreach (var pug in runningMatches)
		{
			var team1Players = pug.GetTeam1Users(datastore);
			var team2Players = pug.GetTeam2Users(datastore);

			string team1String = string.Join("\n", team1Players.Select(x => string.Format(Resources.UserPickingInfo, x.UserName, x.SkillRating.ToString("F0"),(string.IsNullOrWhiteSpace(x.Info) ? "" : $"`{x.Info}`") )));

			string team2String = string.Join("\n", team2Players.Select(x => string.Format(Resources.UserPickingInfo, x.UserName, x.SkillRating.ToString("F0"), (string.IsNullOrWhiteSpace(x.Info) ? "" : $"`{x.Info}`"))));


			addString += string.Format(Resources.MatchInfo, pug.Id, pug.Region.ToString(), (DateTime.UtcNow - pug.PlayedDate).TotalMinutes.ToString("F0"));
		
			addString += string.Format(Resources.TeamLineups,team1String, team2String);
		}

		await ReplyAsync(datastore.CurrentPugState.ToString() + "\n" + addString);
	}


	[Command("maps"), Summary("Get all maps."), AllowedChannelsService]
	public async Task ShowMaps()
	{
		string replayString = "\n" + string.Join("\n", datastore.AllMaps.Select(x => $"{x.Id}. {x.Name}"));

		await ReplyAsync(replayString);
	}


	[Command("userinfo"), Summary("Returns info about the current user, or the user parameter, if one passed."), AllowedChannelsService]
	[Alias("user", "whois")]
	public async Task UserInfo([Summary("The (optional) user to get info for")] IUser user = null)
	{
		Context.Message.DeleteAsync();

		var userInfo = user ?? Context.User;

		var infoUser = datastore.GetOrCreateUser(userInfo);		

		string userInfoText = string.IsNullOrWhiteSpace(infoUser.Info) ? "" : $"\n({infoUser.Info})";

		string replayString = string.Format(Resources.PlayerFullInfo, userInfo.Username, infoUser.SkillRating.ToString("F0"), infoUser.RatingsDeviation.ToString("F0"), infoUser.Wins, infoUser.Loses, infoUser.WinsAsCaptain, infoUser.LosesAsCaptain, userInfoText, infoUser.LookingForTeam.ToString());

		Context.User.SendMessageAsync(replayString);
	}

	[Command("setinfo"), AllowedChannelsService]	
	public async Task SetUserInfo([RemainderAttribute]string infoText)
	{
		Context.Message.DeleteAsync();

		var userInfo = Context.User;

		infoText = "`" + infoText.Replace(Environment.NewLine, "").Replace("\n", "") + "`";

		if (infoText.Length > 75)
		{
			userInfo.SendMessageAsync(Resources.ErrorUserInfoToLong);
		}
		else
		{
			var infoUser = datastore.GetOrCreateUser(userInfo);

			infoUser.Info = infoText;
			datastore.db.Update(infoUser);

			await datastore.db.SaveChangesAsync();

			userInfo.SendMessageAsync(Resources.UserInfoUpdated);
		}
	}

	[Command("setsteamid"), AllowedChannelsService]
	[Alias("setsteam", "addsteamid", "addsteam")]
	public async Task SetSteamId(string steamId)
	{
		Context.Message.DeleteAsync();

		var userInfo = Context.User;
		if (steamId.Length > 80)
		{
			userInfo.SendMessageAsync(Resources.ErrorSteamIDToLong);
		}
		else
		{
			var infoUser = datastore.GetOrCreateUser(userInfo);

			infoUser.SteamId = steamId;
			datastore.db.Update(infoUser);

			await datastore.db.SaveChangesAsync();

			userInfo.SendMessageAsync(Resources.SteamIDUpdated);		
		}
	}

	[Command("tooglelookingforteam"), AllowedChannelsService]
	[Alias("lookingforteam", "lookingteam")]
	public async Task ToogleLookingForTeam()
	{
		Context.Message.DeleteAsync();

		var userInfo = Context.User;

		var infoUser = datastore.GetOrCreateUser(userInfo);

		bool newStatus = !infoUser.LookingForTeam;

		infoUser.LookingForTeam = newStatus;
		datastore.db.Update(infoUser);

		await datastore.db.SaveChangesAsync();

		userInfo.SendMessageAsync(string.Format(Resources.LookingForTeamUpdated, Context.User.GetName(), newStatus.ToString()));
	}


	//Note this one is allowed in every channel.
	[Command("top")]
	[Alias("top10", "best")]
	public async Task Top10()
	{
		var topUsers = datastore.db.Users.OrderByDescending(x => x.SkillRating).Where(x => x.GamesPlayed() >= 10).Take(10);

		string topPlayersInfo = string.Join("\n", topUsers.Select(x => string.Format(Resources.NameAndSkillAndDeviation, x.UserName, x.SkillRating.ToString("F0"), x.RatingsDeviation.ToString("F0"))));

		await ReplyAsync(string.Format(Resources.InfoTopPlayers, topPlayersInfo));
	}

	[Command("mostgamesplayed"), AllowedChannelsService]
	[Alias("nolife", "nolifer", "nolifers")]
	public async Task MostGamesPlayed()
	{
		var topUsers = datastore.db.Users.OrderByDescending(x => (x.Wins + x.Loses)).Take(10);

		string playersInfo = string.Join("\n", topUsers.Select(x => string.Format(Resources.NameAndGamesPlayed, x.UserName, x.GamesPlayed())));

		await ReplyAsync(string.Format(Resources.InfoMostActivePlayers, playersInfo));
	}

	[Command("noteamsplayers"), AllowedChannelsService]
	[Alias("l4t", "teamplayers", "findplayers", "findteammembers", "noteamplayers")]
	public async Task PrintPlayersLookingForTeam()
	{
		var topUsers = datastore.db.Users.OrderByDescending(x => x.SkillRating).Where(x => x.LookingForTeam && x.GamesPlayed() >= 5).Take(20);

		string playersInfo = string.Join("\n", topUsers.Select(x => string.Format(Resources.PlayerLongInfo, x.UserName, x.SkillRating.ToString("F0"), x.RatingsDeviation.ToString("F0"), x.Info)));

		await ReplyAsync(string.Format(Resources.InfoPlayersLookingForTeam, playersInfo)); 
	}

	[Command("mapstats"), AllowedChannelsService]
	public async Task PrintMapStats()
	{
		var allMatches = datastore.db.Matches.GroupBy(x => x.Map).OrderByDescending(x => x.Count());

		string mapsStats = "";
		foreach (var map in allMatches)
		{
			mapsStats += $"**{map.Key.Name}:** {map.Count()}\n";
		}

		await ReplyAsync(string.Format(Resources.InfoMapStats, datastore.db.Matches.Count(), mapsStats));
	}

	[Command("gamemodestats"), AllowedChannelsService]
	public async Task PrintGameModeStats()
	{
		var allMatches = datastore.db.Matches.GroupBy(x => x.GameMode).OrderByDescending(x => x.Count());
	
		string gameModeStats = "";
		foreach (var gameMode in allMatches)
		{
			gameModeStats += $"**{gameMode.Key.Name}:** {gameMode.Count()}\n";
		}

		await ReplyAsync(string.Format(Resources.InfoMapStats, datastore.db.Matches.Count(), gameModeStats));
	}
}