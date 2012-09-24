﻿namespace ShootR
{
    /// <summary>
    /// A ship on the game field.  Only the owner of the ship can control the ship.  Ownership is decided via the connection id.
    /// </summary>
    public class Ship : Collidable
    {
        public const int WIDTH = 50;
        public const int HEIGHT = 50;
        public const int SCREEN_WIDTH = 1280;
        public const int SCREEN_HEIGHT = 600;

        private string _connectionID;

        public Ship(string connectionID, Vector2 position, BulletManager bm)
            : base(WIDTH, HEIGHT)
        {
            _connectionID = connectionID;
            MovementController = new ShipMovementController(position);
            WeaponController = new ShipWeaponController(this, bm);
        }

        public string GetConnectionID()
        {
            return _connectionID;
        }

        public ShipMovementController MovementController
        {
            get
            {
                return (ShipMovementController)base.MovementController;
            }
            set
            {
                base.MovementController = value;
            }
        }

        public ShipWeaponController WeaponController { get; set; }

        public void Update(GameTime gameTime)
        {
            MovementController.Update(gameTime);
            base.Update();
        }

        public override void HandleCollisionWith(Collidable c, Map space)
        {
        }
    }
}