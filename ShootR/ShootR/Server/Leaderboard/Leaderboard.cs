using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace ShootR
{
    public class Leaderboard
    {
        public const int LEADERBOARD_SIZE = 4;
        public const string LEADERBOARD_REQUESTEE_GROUP = "LeaderboardRequestees";

        private readonly UserHandler _userHandler;
        private readonly IHubContext<GameHub> _gameHub;

        public Leaderboard(UserHandler userHandler, IHubContext<GameHub> gameHub)
        {
            _userHandler = userHandler;
            _gameHub = gameHub;
        }

        public Task RequestLeaderboardAsync(string connectionId)
        {
            return _gameHub.Groups.AddToGroupAsync(connectionId, LEADERBOARD_REQUESTEE_GROUP);
        }

        public Task StopRequestingLeaderboardAsync(string connectionId)
        {
            return _gameHub.Groups.RemoveFromGroupAsync(connectionId, LEADERBOARD_REQUESTEE_GROUP);
        }

        public IEnumerable<LeaderboardEntry> GetAndUpdateLeaderboard()
        {
            IEnumerable<LeaderboardEntry> currentLeaderboard = 
                (from user in _userHandler.GetActiveUsers()
                where user.MyShip != null
                select user.MyShip)
                .Select(ship => new LeaderboardEntry()
                {
                    Name = ship.Name,
                    Level = ship.LevelManager.Level,
                    Kills = ship.StatRecorder.Kills,
                    Deaths = ship.StatRecorder.Deaths,
                    DamageDealt = ship.StatRecorder.DamageDealt,
                    DamageTaken = ship.StatRecorder.DamageTaken,
                    KillDeathRatio = (Convert.ToDouble(ship.StatRecorder.Kills) / Math.Max((ship.StatRecorder.Kills + ship.StatRecorder.Deaths), 1))*100,
                    Photo = ship.Host.RegistrationTicket.Photo,
                    ID = ship.ID,
                    ConnectionID = ship.Host.ConnectionID
                })
                .OrderByDescending(entry => entry.Level)
                .ThenByDescending(entry => entry.KillDeathRatio)
                .ThenByDescending(entry => entry.Kills);

            int i = 1;

            foreach (LeaderboardEntry entry in currentLeaderboard)
            {
                _userHandler.GetUser(entry.ConnectionID).CurrentLeaderboardPosition = i++;
            }

            return currentLeaderboard.Take(LEADERBOARD_SIZE);
        }
    }
}