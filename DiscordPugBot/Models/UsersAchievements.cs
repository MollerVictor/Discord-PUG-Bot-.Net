using DiscordPugBot.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DiscordPugBot.Models
{
    public class UsersAchievements
    {		
		public int UserId { get; set; }
		public Users User { get; set; }

		
		public int AchievementId { get; set; }
		public Achievements Achievement { get; set; }		
	}
}
