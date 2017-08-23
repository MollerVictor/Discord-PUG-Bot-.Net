using System;
using System.Collections.Generic;
using System.Text;

namespace OWPugs.Models
{
    public class UsersMatchesRelation
    {
		public int MatchId { get; set; }
		public Matches Match { get; set; }

		public int UserId { get; set; }
		public Users User { get; set; }

		public bool IsCaptain { get; set; }
		public int Team { get; set; }
		public double SkillRating { get; set; }
	}
}
