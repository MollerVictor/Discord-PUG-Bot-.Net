using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace DiscordPugBot.Models
{
	public enum Region
	{
		Unknown,
		EU,
		NA
	}

	public class Matches
	{
		public int Id { get; set; }
		public DateTime PlayedDate { get; set; }
		public Maps Map { get; set; }
		public GameModes GameMode { get; set; }
		public int TeamWinner { get; set; }
		public int MatchState { get; set; }
		public Region Region { get; set; }
		


		public List<UsersMatchesRelation> UserMatches { get; set; }

		public List<Users> GetAllUsers(DataStore datastore)
		{
			var userId = UserMatches.Select(x => x.UserId).ToList();

			return datastore.db.Users.Where(x => userId.Contains(x.Id)).ToList();
		}

		public List<Users> GetTeam1Users(DataStore datastore)
		{
			var userId = UserMatches.Where(x => x.Team == 1).Select(x => x.UserId).ToList();

			return datastore.db.Users.Where(x => userId.Contains(x.Id)).ToList();
		}

		public List<Users> GetTeam2Users(DataStore datastore)
		{
			var userId = UserMatches.Where(x => x.Team == 2).Select(x => x.UserId).ToList();

			return datastore.db.Users.Where(x => userId.Contains(x.Id)).ToList();
		}
	}
}
