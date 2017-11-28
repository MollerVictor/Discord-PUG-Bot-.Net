using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace DiscordPugBot.Models
{
    public class Users
    {
        public int Id { get; set; }
        public long? DiscordId { get; set; }
        public double SkillRating { get; set; }
		[NotMapped]
		public double PreviousSkillRating { get; set; }
		public double RatingsDeviation { get; set; }
		public double Volatility { get; set; }
		public int PlayerHeroes { get; set; }
        public string UserName { get; set; }
		public string SteamId { get; set; }
		public string Info { get; set; } = "";
		public bool LookingForTeam { get; set; }

		public int Wins { get; set; }
        public int Loses { get; set; }
		public int WinsAsCaptain { get; set; }
		public int LosesAsCaptain { get; set; }
		public int FatKided { get; set; }
		public int TimesLeftAPug { get; set; }


		public List<UsersMatchesRelation> UserMatches { get; set; }

		public List<Matches> GetAllMatches(DataStore datastore)
		{
			var matchesId = UserMatches.Select(x => x.MatchId).ToList();
			return datastore.db.Matches.Where(x => matchesId.Contains(x.Id)).ToList(); 
		}

		public int GamesPlayed()
		{
			return Wins + Loses;
		}
	}
}
