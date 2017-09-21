using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace ShootR
{
    public class UserHandler
    {
        private readonly ConcurrentDictionary<string, User> _userList = new ConcurrentDictionary<string, User>();
        private readonly GameHandler _gameHandler;

        public int TotalActiveUsers { get; set; }

        public UserHandler(GameHandler gameHandler)
        {
            _gameHandler = gameHandler;
        }

        public void AddUser(User user)
        {
            _userList.TryAdd(user.ConnectionID, user);
            user.IdleManager.OnIdle += _gameHandler.RemoveShipFromGame;
            //user.IdleManager.OnIdleTimeout += DisconnectUser;
            user.IdleManager.OnComeBack += _gameHandler.AddShipToGame;

            if (!user.Controller)
            {
                user.MyShip.OnFire += _gameHandler.AddBulletToGame;
            }
        }

        public void RemoveUser(string connectionId)
        {
            if (_userList.TryRemove(connectionId, out var u) && !u.Controller && u.MyShip != null)
            {
                u.MyShip.Dispose();
                u.MyShip.Host = null; // Remove linking from the ship
            }
        }

        public Ship GetUserShip(string connectionId)
        {
            return _userList[connectionId].MyShip;
        }

        // public User FindUserByIdentity(string identity)
        // {
        //     return (from user in _userList.Values
        //             where user.RegistrationTicket.Identity == identity && !user.Controller
        //             select user).FirstOrDefault();
        // }

        public bool UserExistsAndReady(string connectionId)
        {
            return _userList.ContainsKey(connectionId) && _userList[connectionId].MyShip != null;
        }

        public User GetUser(string connectionId)
        {
            return _userList[connectionId];
        }

        public List<User> GetUsers()
        {
            return _userList.Values.ToList();
        }

        public List<User> GetActiveUsers()
        {
            List<User> activeUsers = (from user in _userList.Values
                                            where !user.Controller && user.Connected && !user.IdleManager.Idle
                                            select user).ToList();

            TotalActiveUsers = activeUsers.Count;

            return activeUsers;
        }

        public void Update()
        {
            foreach (User user in _userList.Values)
            {
                user.Update();
            }
        }
    }
}