using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShootR
{
    public class ShipManager
    {
        public ConcurrentDictionary<string, Ship> Ships { get; } = new ConcurrentDictionary<string, Ship>();

        private readonly RespawnManager _respawnManager;

        public ShipManager(GameHandler gameHandler, Game game)
        {
            _respawnManager = new RespawnManager(gameHandler, game);
        }

        /// <summary>
        /// Adds a ship and returns the added ship.  Used to chain methods together.
        /// </summary>
        /// <param name="ship">The ship to add</param>
        public void Add(Ship ship)
        {
            // Only enable respawn if it hasn't been enabled yet
            if (!ship.RespawnEnabled)
            {
                ship.RespawnEnabled = true;
                ship.OnDeath += new DeathEventHandler(_respawnManager.StartRespawnCountdown);
            }

            Ships.TryAdd(ship.Host.ConnectionID, ship);
        }

        /// <summary>
        /// Removes ship from the game handler.  This is used when a ship is destroyed and no longer needs to be monitored.
        /// </summary>
        /// <param name="key"></param>
        public void Remove(string key)
        {
            Ships.TryRemove(key, out var _);
        }

        public async Task Update(GameTime gameTime)
        {
            await _respawnManager.Update();

            var keysToRemove = new List<string>(Ships.Count);
            Parallel.ForEach(Ships, currentShip =>
            {
                if (!currentShip.Value.Disposed)
                {
                    currentShip.Value.Update(gameTime);
                }
                else
                {
                    keysToRemove.Add(currentShip.Key);
                }

            });

            for (int i = keysToRemove.Count - 1; i >= 0; i--)
            {
                Remove(keysToRemove[i]);
            }
        }
    }
}