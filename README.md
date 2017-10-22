# Discord-PUG-Bot-.Net

## Install
Require MySQL &  [.NET Core 1.1 or newer](https://www.microsoft.com/net/download/core#/runtime)  
1. Setup MySQL.
2. Enter connection details in the config.json file.
3. Go to https://discordapp.com/developers/docs/intro
4. Create a new app.
5. Create a new bot user.
6. Copy to bot token into "discordBotToken" in the config.json file.
7. Open the channel you want the bot to be in with your browser.
8. Then copy the last part of the url, and add it in allowedChannel in config.json file.  
Exemple: https://discordapp.com/channels/298267127004004353/312576903653490690  
Here you should copy 312576903653490690
9. Now open cmd in the current folder, and run "dotnet DiscordPugBot.dll"
10. Done! You can now test the bot by writing ".add"
11. Connect to the MySQL Server with a client, and add maps and gamesmodes rows for the game you are using it for.


[Discord.Net Repo](https://github.com/RogueException/Discord.Net)  
[Glicko2 Repo](https://github.com/MaartenStaa/glicko2-csharp)
