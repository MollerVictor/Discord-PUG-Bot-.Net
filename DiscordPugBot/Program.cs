using System.Threading.Tasks;
using System.Reflection;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.DependencyInjection;
using DiscordBot.Services;
using System.IO;
using Microsoft.Extensions.Options;

public class Program
{
	private CommandService _commands;
	private DiscordSocketClient _client;
	private IConfigurationRoot _config;

	static void Main(string[] args)
	{
		new Program().Start().GetAwaiter().GetResult();
	}

	public async Task Start()
	{
		_config = BuildConfig();

		_client = new DiscordSocketClient();
		_commands = new CommandService();

		var services = ConfigureServices();

		services.GetRequiredService<LogService>();
		await services.GetRequiredService<CommandHandlingService>().InitializeAsync(services);

		var appConfig = services.GetService<IOptions<AppConfig>>().Value;

		await _client.LoginAsync(TokenType.Bot, appConfig.DiscordBotToken);
		await _client.StartAsync();

		await Task.Delay(-1);
	}

	private IServiceProvider ConfigureServices()
	{
		return new ServiceCollection()
			// Base
			.AddSingleton(_client)
			.AddSingleton<CommandService>()
			.AddSingleton<CommandHandlingService>()
			// Logging
			.AddLogging()
			.AddSingleton<LogService>()
			// Add additional services here...
			.AddOptions()
			.AddSingleton<DataStore>()
			.Configure<AppConfig>(options => _config.GetSection("AppConfig").Bind(options))
			.BuildServiceProvider();
	}


	private IConfigurationRoot BuildConfig()
	{
		return new ConfigurationBuilder()
			.SetBasePath(Directory.GetCurrentDirectory())
			.AddJsonFile("config.json")
			.Build();
	}
}