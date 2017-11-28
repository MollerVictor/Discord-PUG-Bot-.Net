using DiscordPugBot.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

public class Pug
{
	public PugUser Captain1;
	public PugUser Captain2;

	public List<PugUser> UsersInPug = new List<PugUser>();

	public Maps MapPicked;

	public List<Maps> VoteableMaps;
	public Region Region;
}

