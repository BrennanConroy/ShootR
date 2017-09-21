using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Web;
using System.Threading.Tasks;

namespace ShootR
{
    public class RespawnManager
    {
        public const int RESPAWN_TIMER = 7;
        
        private List<KeyValuePair<Ship, DateTime>> _respawningShips;
        private readonly GameHandler _gameHandler;
        private readonly Game _game;
        private static Random _gen;

        public RespawnManager(GameHandler gameHandler, Game game)
        {
            _respawningShips = new List<KeyValuePair<Ship, DateTime>>();
            _gameHandler = gameHandler;
             _gen = new Random();
             _game = game;
        }

        public async Task<bool> TryRespawn(Ship ship, DateTime startedAt)
        {
            // We should respawn
            // We also check the ships host to ensure that we did not remove the user
            if ((DateTime.UtcNow - startedAt).TotalSeconds >= RESPAWN_TIMER && ship.Host != null)
            {
                ship.LifeController.HealFull();
                ship.MovementController.Position = GetRandomStartPosition();
                ship.MovementController.Velocity = Vector2.Zero;
                ship.MovementController.Rotation = _gen.Next(0, 360);
                ship.MovementController.StopMovement();
                ship.Disposed = false;
                ship.Controllable.Value = true;
                _gameHandler.AddShipToGame(ship);

                await _game.Leaderboard.StopRequestingLeaderboardAsync(ship.Host.ConnectionID);
                return true;
            }
            else
            {
                return false;
            }
        }

        public void StartRespawnCountdown(object sender, DeathEventArgs e)
        {
            Ship ship = sender as Ship;
            // Don't want to subscribe AI to retrieve leaderboard info
            if(!(ship is ShipAI))
            {
                _game.Leaderboard.RequestLeaderboardAsync(ship.Host.ConnectionID).GetAwaiter().GetResult();                
            }

            ship.Controllable.Value = false;
            _respawningShips.Add(new KeyValuePair<Ship, DateTime>(ship, DateTime.UtcNow));
        }

        public static Vector2 GetRandomStartPosition()
        {
            return new Vector2(_gen.Next(Ship.WIDTH * 2, Map.WIDTH - Ship.WIDTH * 2), _gen.Next(Ship.HEIGHT * 2, Map.HEIGHT - Ship.HEIGHT * 2));
        }

        public async Task Update()
        {
            for (int i = 0; i < _respawningShips.Count; i++)
            {
                // If respawn was successful
                if (await TryRespawn(_respawningShips[i].Key, _respawningShips[i].Value))
                {
                    _respawningShips.RemoveAt(i--);
                }
            }
        }
    }
}