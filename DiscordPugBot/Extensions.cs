using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using DiscordPugBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public static class Extensions
{
	public static IQueryable<Users> UsersWithMatches(this MyDBContext context)
	{
		return context.Users.Include(b => b.UserMatches);
	}

	public static IQueryable<Matches> MatchesWithUsers(this MyDBContext context)
	{
		return context.Matches.Include(b => b.UserMatches);
	}

	public static string GetName(this IUser user)
	{
		SocketGuildUser guildUser = user as SocketGuildUser;

		return guildUser.Nickname ?? guildUser.Username;
	}



	public static string GetName(this SocketUser user)
	{
		SocketGuildUser guildUser = user as SocketGuildUser;

		return guildUser.Nickname ?? guildUser.Username;
	}

	public static string GetName(this SocketGuildUser user)
	{
		return user.Nickname ?? user.Username;
	}
}