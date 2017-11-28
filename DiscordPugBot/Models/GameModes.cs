using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace DiscordPugBot.Models
{
	public partial class GameModes
	{
		public int Id { get; set; }
		public string Name { get; set; }

		[NotMapped]
		public int Votes { get; set; }
	}
}
