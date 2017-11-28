using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace DiscordPugBot.Models
{
	public class Achievements
	{		
		public int Id { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }


		public virtual ICollection<UsersAchievements> UsersAchievements { get; set; }
	}
}
