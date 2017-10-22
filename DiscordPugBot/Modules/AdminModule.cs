using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using OWPugs.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Glicko2;
using System.Diagnostics;

public class AdminModule : ModuleBase
{

	public DataStore _datastore;
	public AdminModule(DataStore datastore)
	{
		_datastore = datastore;
	}

	//TODO Change dice_discord_id to check if admin instead.
	/*
	[Command("RecalculateRatings"), AllowedChannelsService]
	public async Task RecalculateRatings()
	{
		if (Context.User.Id == DICE_DISCORD_ID)
		{
			var allUsers = _datastore.db.Matches;

			var calculator = new RatingCalculator();

			// Instantiate a RatingPeriodResults object.
			var results = new RatingPeriodResults();

			var ratingsPlayers = new List<Tuple<Users,Rating>>();

			foreach (var match in _datastore.db.MatchesWithUsers())
			{
				double team1Rating = match.GetTeam1Users(_datastore).Sum(x => x.SkillRating) / match.GetTeam1Users(_datastore).Count;
				double team2Rating = match.GetTeam2Users(_datastore).Sum(x => x.SkillRating) / match.GetTeam2Users(_datastore).Count;

				Debug.WriteLine(team1Rating + " " + team2Rating);

				double team1RatingsDeviation = match.GetTeam1Users(_datastore).Sum(x => x.RatingsDeviation) / match.GetTeam1Users(_datastore).Count;
				double team2RatingsDeviation = match.GetTeam2Users(_datastore).Sum(x => x.RatingsDeviation) / match.GetTeam2Users(_datastore).Count;

				double team1Volatility = match.GetTeam1Users(_datastore).Sum(x => x.Volatility) / match.GetTeam1Users(_datastore).Count;
				double team2Volatility = match.GetTeam2Users(_datastore).Sum(x => x.Volatility) / match.GetTeam2Users(_datastore).Count;

				var team1RatingCalc = new Rating(calculator, team1Rating, team1RatingsDeviation, team1Volatility);
				var team2RatingCalc = new Rating(calculator, team2Rating, team2RatingsDeviation, team2Volatility);

				foreach (var player in match.GetTeam1Users(_datastore))
				{
					var playerRating = new Rating(calculator, player.SkillRating, player.RatingsDeviation, player.Volatility);

					ratingsPlayers.Add(new Tuple<Users, Rating>(player,playerRating));

					if ((DataStore.Teams)match.TeamWinner == DataStore.Teams.TeamOne)
					{
						results.AddResult(playerRating, team2RatingCalc);
					}
					else if ((DataStore.Teams)match.TeamWinner == DataStore.Teams.TeamTwo)
					{
						results.AddResult(team2RatingCalc, playerRating);
					}
				}

				foreach (var player in match.GetTeam2Users(_datastore))
				{
					var playerRating = new Rating(calculator, player.SkillRating, player.RatingsDeviation, player.Volatility);

					if ((DataStore.Teams)match.TeamWinner == DataStore.Teams.TeamOne)
					{
						results.AddResult(team1RatingCalc, playerRating);
					}
					else if ((DataStore.Teams)match.TeamWinner == DataStore.Teams.TeamTwo)
					{
						results.AddResult(playerRating, team1RatingCalc);
					}
				}

				calculator.UpdateRatings(results);

				foreach (var player in ratingsPlayers)
				{
					player.Item1.SkillRating = player.Item2.GetRating();
					player.Item1.RatingsDeviation = player.Item2.GetRatingDeviation();
					player.Item1.Volatility = player.Item2.GetVolatility();
				}
			}
			_datastore.db.SaveChanges();
			Console.WriteLine("Completed");
		}
	}
	*/
	}