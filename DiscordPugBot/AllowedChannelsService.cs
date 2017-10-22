using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using DiscordBot.Services;
using Microsoft.Extensions.DependencyInjection;

// Inherit from PreconditionAttribute
public class AllowedChannelsService : PreconditionAttribute
{
	private readonly RequireContextAttribute _contextType = new RequireContextAttribute(ContextType.DM);
	
	public override async Task<PreconditionResult> CheckPermissions(ICommandContext context, CommandInfo command, IServiceProvider services)
	{
		AppConfig appConfig = services.GetService<IOptions<AppConfig>>().Value;


		//Check if it's a direct message to the bot
		var isDM = await _contextType.CheckPermissions(context, command, services);
		
		// If this command was executed by that user, return a success
		if (context.Channel.Id == appConfig.AllowedChannel || isDM.IsSuccess)
			return PreconditionResult.FromSuccess();
		// Since it wasn't, fail
		else
			return PreconditionResult.FromError("");
	}
}