using Discord;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using OWPugs.Models;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

public class DataStore
{
	public enum PugState
	{
		WaitingForPlayer,
		Readyup,
		PickingPlayers,
		MapVoting,
		GameModeVoting,
		Playing
	}

	public enum Teams
	{
		Invalid,
		TeamOne,
		TeamTwo
	}
	
	public PugState CurrentPugState;

	//TODO Move these to the Pug class
	public PugUser Captain1;
	public PugUser Captain2;

	public int CurrentTeamPickingIndex;


	public List<PugUser> SignedUpUsers = new List<PugUser>();

	public List<PugUser> _naSignupUsers = new List<PugUser>();
	public List<PugUser> _euSignupUsers = new List<PugUser>();

	public Timer MapVoteTimer;
	public Timer GameModeVoteTimer;
	public Timer ReadyUpTimer;
	public Queue<IUserMessage> LastReadyMessage = new Queue<IUserMessage>();

	public MyDBContext db;

	public List<Maps> AllMaps;
	public List<GameModes> AllGameModes;

	public Pug CurrentPug;

	public DataStore(IOptions<AppConfig> appConfig)
	{
		db = new MyDBContext(appConfig);
		bool createdNewDB = db.Database.EnsureCreated();
		if (createdNewDB)
		{
			Console.WriteLine("Couln't find database");
			Console.WriteLine("Creating new Database");
		}
		AllMaps = db.Maps.ToList();
		AllGameModes = db.GameModes.ToList();
	}

	public bool RemoveUserFromNotStartedPug(List<PugUser> list, IUser user)
	{
		if (!list.Any()) return false;

		var u = list.FirstOrDefault(x => x.IUser.Id == user.Id);

		if (u == null)
			return false;


		return list.Remove(u);		
	}


	public PugUser GetUserInPugInNotStartedPug(IUser user)
	{
		return SignedUpUsers.FirstOrDefault(x => x.IUser.Id == user.Id);
	}

	public PugUser GetUserInPugInNotStartedPug(List<PugUser> list, IUser user)
	{
		return list.FirstOrDefault(x => x.IUser.Id == user.Id);
	}

	public bool IsCaptain(IUser user)
	{
		if (Captain1.IUser.Id == user.Id || Captain2.IUser.Id == user.Id) return true;

		return false;
	}

	public Users GetOrCreateUser(IUser iUser)
	{
		var infoUser = db.Users.FirstOrDefault(x => (ulong)x.DiscordId == iUser.Id);

		if (infoUser == null)
		{
			Users newUser = new Users
			{
				DiscordId = (long)iUser.Id,
				UserName = iUser.Username,
			};

			infoUser = newUser;

			db.Users.Add(newUser);

			db.SaveChanges();
		}

		return infoUser;
	}
}

