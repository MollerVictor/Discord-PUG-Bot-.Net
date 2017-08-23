using Discord;
using System;
using System.Collections.Generic;
using System.Text;
using OWPugs.Models;

public class PugUser
{
	public IUser IUser;
	public bool IsReady;
	public bool IsPicked;
	public bool HaveVotedForMap;
	public bool HaveVotedForGameMode;
	public int PickID = -1;
	public DataStore.Teams Team = DataStore.Teams.Invalid;


	public Users DatabaseUser;

	public int GamesPlayed { get { return DatabaseUser.Wins + DatabaseUser.Loses; } }

	public PugUser(IUser user, Users dbUser)
	{
		IUser = user;

		DatabaseUser = dbUser;
	}

	public PugUser(Users dbUser)
	{
		DatabaseUser = dbUser;
	}

	public string GetDisplayRanking()
	{
		if (DatabaseUser.Matches() >= 8)
		{
			return DatabaseUser.SkillRating.ToString("F0");
		}
		else if(DatabaseUser.Matches() >= 3)
		{
			if (DatabaseUser.SkillRating >= 1700)
			{
				return "Unranked++";
			}
			else if (DatabaseUser.SkillRating >= 1600)
			{
				return "Unranked+";
			}
			else if (DatabaseUser.SkillRating <= 1400)
			{
				return "Unranked-";
			}
		}
		
		return "Unranked";
	}
}

