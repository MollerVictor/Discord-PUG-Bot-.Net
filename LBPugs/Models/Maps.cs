using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace OWPugs.Models
{
    public class Maps
    {
        public int Id { get; set; }
        public string Name { get; set; }

		[NotMapped]
		public int Votes { get; set; }
    }
}
