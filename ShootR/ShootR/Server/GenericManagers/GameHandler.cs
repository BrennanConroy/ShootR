using System.Threading.Tasks;

namespace ShootR
{
    public class GameHandler
    {
        public BulletManager BulletManager { get; set; } = new BulletManager();

        private readonly ShipManager _shipManager;
        private readonly PowerupManager _powerupManager;
        private CollisionManager _collisionManager;
        private readonly Map _space;

        public GameHandler(Map space, Game game)
        {
            _space = space;
            _shipManager = new ShipManager(this, game);
            _collisionManager = new CollisionManager(space);

            _powerupManager = new PowerupManager();
        }

        public void AddShipToGame(Ship ship)
        {
            if (ship != null)
            {
                _shipManager.Add(ship);
                _collisionManager.Monitor(ship);
            }
        }

        public void RemoveShipFromGame(Ship ship)
        {
            if (ship != null)
            {
                _shipManager.Remove(ship.Host.ConnectionID);
                _collisionManager.UnMonitor(ship);
            }
        }

        public void AddBulletToGame(Bullet bullet)
        {
            _space.Insert(bullet);
        }

        public void AddPowerupToGame(Powerup powerup)
        {
            _powerupManager.Add(powerup);
            _space.Insert(powerup);
        }

        public async Task Update(GameTime gameTime)
        {
            await _shipManager.Update(gameTime);
            _powerupManager.Update(gameTime);
            BulletManager.Update(gameTime);

            _collisionManager.Update(gameTime);
        }
    }
}