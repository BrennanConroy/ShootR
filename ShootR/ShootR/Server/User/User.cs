using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace ShootR
{
    public class User
    {
        public const int MAX_SCREEN_WIDTH = 2000;
        public const int MAX_SCREEN_HEIGHT = 2000;
        public const int MIN_SCREEN_WIDTH = 1000;
        public const int MIN_SCREEN_HEIGHT = 660;

        public User(string connectionID, Ship ship, RegisteredClient rc)
        {
            RegistrationTicket = rc;
            MyShip = ship;
            ConnectionID = connectionID;

            Viewport = new Size(0, 0); // Initialize the viewport to 0 by 0

            NotificationManager = new NotificationManager();
            IdleManager = new IdleManager(ship, NotificationManager);
            Connected = true;
            ReadyForPayloads = false;

            if (ship != null)
            {
                ship.Host = this;
            }
        }

        public Ship MyShip { get; set; }
        public string ConnectionID { get; set; }
        public bool Controller { get; set; }
        public bool Connected { get; set; }
        public RegisteredClient RegistrationTicket { get; set; }
        public NotificationManager NotificationManager { get; private set; }

        public IdleManager IdleManager { get; private set; }
        public int CurrentLeaderboardPosition { get; set; }
        public bool DeathOccured { get; set; }
        public bool ReadyForPayloads { get; set; }

        public List<User> RemoteControllers { get; } = new List<User>();

        private Size _viewport;
        public Size Viewport
        {
            get
            {
                return _viewport;
            }
            set
            {
                if (value.Width > MAX_SCREEN_WIDTH)
                {
                    value.Width = MAX_SCREEN_WIDTH;
                }
                if (value.Height > MAX_SCREEN_HEIGHT)
                {
                    value.Height = MAX_SCREEN_HEIGHT;
                }

                _viewport = value;
            }
        }

        public virtual Task PushToClientAsync(object[] payload, IHubContext<GameHub> context)
        {
            return context.Clients.Client(ConnectionID).SendAsync("d", payload);
        }

        public void Update()
        {
            if (MyShip != null)
            {
                IdleManager.Update();
            }
        }
    }
}