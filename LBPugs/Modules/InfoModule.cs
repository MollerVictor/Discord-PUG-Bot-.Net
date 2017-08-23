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
		await ReplyAsync("**Dices Pug Bot\nStats Website:** http://dpl.victormoller.com/ \n**To sign up for a pug write .add\nCommands:** \n.help\n.status\n.userinfo <username>\n.setinfo <info>\n.add\n.remove\n.ready\n.pick <player>\n.vote <map/gamemode>\n.result <win/lose/cancel>\n.lookingforteam\n.top10\n.noteamplayers\n.nolifer\n.gamemodestats\n.mapstats");
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

			string team1String = string.Join("\n", team1Players.Select(x =>	$"{x.UserName} **{x.SkillRating.ToString("F0")}**" + (string.IsNullOrWhiteSpace(x.Info) ? "" : $"`{x.Info}`") ));

			string team2String = string.Join("\n", team2Players.Select(x => $"{x.UserName} **{x.SkillRating.ToString("F0")}**"  + (string.IsNullOrWhiteSpace(x.Info) ? "" : $"`{ x.Info}`")));


			addString += $"\n**Match: {pug.Id} {pug.Region.ToString()}** Duration: {(DateTime.UtcNow - pug.PlayedDate).TotalMinutes.ToString("F0")}min\n";
			addString += "**Team 1:**\n" + team1String + "\n**Team 2:**\n" + team2String + "\n";
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

		string replayString = $"**{userInfo.Username}**\n**Rating:** {infoUser.SkillRating.ToString("F0")} (±{infoUser.RatingsDeviation.ToString("F0")})\n**Wins:** {infoUser.Wins}\n**Losses:** {infoUser.Loses}\n**Wins as captain:** {infoUser.WinsAsCaptain}\n**Loses as captain:** {infoUser.LosesAsCaptain}{userInfoText}\n**Looking for a team?:** {infoUser.LookingForTeam.ToString()}";

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
			userInfo.SendMessageAsync($"Userinfo to long.");
		}
		else
		{
			var infoUser = datastore.GetOrCreateUser(userInfo);

			infoUser.Info = infoText;
			datastore.db.Update(infoUser);

			await datastore.db.SaveChangesAsync();

			userInfo.SendMessageAsync($"Userinfo updated.");
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
			await ReplyAsync($"SteamId to long.");
			userInfo.SendMessageAsync($"SteamId to long.");
		}
		else
		{
			var infoUser = datastore.GetOrCreateUser(userInfo);

			infoUser.SteamId = steamId;
			datastore.db.Update(infoUser);

			await datastore.db.SaveChangesAsync();

			userInfo.SendMessageAsync($"SteamId updated.");		
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

		userInfo.SendMessageAsync($"{Context.User.GetName()}, Looking for team: {newStatus.ToString()}");
	}


	//Note this one is allowed in every channel.
	[Command("top")]
	[Alias("top10", "best")]
	public async Task Top10()
	{
		var topUsers = datastore.db.Users.OrderByDescending(x => x.SkillRating).Where(x => x.Matches() >= 10).Take(10);

		await ReplyAsync("Top players:\n" + string.Join("\n", topUsers.Select(x => $"**{x.UserName}** Skill rating: {x.SkillRating.ToString("F0")} (±{x.RatingsDeviation.ToString("F0")})")));
	}

	[Command("mostgamesplayed"), AllowedChannelsService]
	[Alias("nolife", "nolifer", "nolifers")]
	public async Task MostGamesPlayed()
	{
		var topUsers = datastore.db.Users.OrderByDescending(x => (x.Wins + x.Loses)).Take(10);

		await ReplyAsync("Most active players:\n" + string.Join("\n", topUsers.Select(x => $"**{x.UserName}** Games Played: {(x.Wins + x.Loses)}")));
	}

	[Command("noteamsplayers"), AllowedChannelsService]
	[Alias("l4t", "teamplayers", "findplayers", "findteammembers", "noteamplayers")]
	public async Task PrintPlayersLookingForTeam()
	{
		var topUsers = datastore.db.Users.OrderByDescending(x => x.SkillRating).Where(x => x.LookingForTeam && x.Matches() >= 5).Take(20);

		await ReplyAsync("Players looking for team:\n" + string.Join("\n", topUsers.Select(x => $"**{x.UserName}** Skill rating: {x.SkillRating.ToString("F0")} (±{x.RatingsDeviation.ToString("F0")}) {x.Info}")));
	}

	[Command("mapstats"), AllowedChannelsService]
	public async Task PrintMapStats()
	{
		string addString = "";

		var allMatches = datastore.db.Matches.GroupBy(x => x.Map).OrderByDescending(x => x.Count());

		addString += $"**Total games played:** {datastore.db.Matches.Count()}\n";
		foreach (var map in allMatches)
		{
			addString += $"**{map.Key.Name}:** {map.Count()}\n";
		}

		await ReplyAsync(addString);
	}

	[Command("gamemodestats"), AllowedChannelsService]
	public async Task PrintGameModeStats()
	{
		string addString = "";

		var allMatches = datastore.db.Matches.GroupBy(x => x.GameMode).OrderByDescending(x => x.Count());

		addString += $"**Total games played:** {datastore.db.Matches.Count()}\n";
		foreach (var gameMode in allMatches)
		{
			addString += $"**{gameMode.Key.Name}:** {gameMode.Count()}\n";
		}

		await ReplyAsync(addString);
	}
}