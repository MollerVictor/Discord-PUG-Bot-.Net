﻿public class AppConfig
{

	public string DiscordBotToken { get; set; }
	public string ConnectionString { get; set; }

	public ulong AllowedChannel { get; set; }

	public string ChannelDisplayName { get; set; }

	public int PlayersPerTeam { get; set; }
}