using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace ShootR
{
    public class ConnectionManager
    {
        private readonly SemaphoreSlim _gameLock;
        private readonly UserHandler _userHandler;
        private readonly IHubContext<GameHub> _gameHub;

        public ConnectionManager(UserHandler userHandler, SemaphoreSlim gameLock, IHubContext<GameHub> gameHub)
        {
            _gameLock = gameLock;
            _userHandler = userHandler;
            _gameHub = gameHub;
        }

        public void OnConnected(string connectionId)
        {
        }

        public async Task OnDisconnectedAsync(string connectionId)
        {
            await _gameLock.WaitAsync();
            try
            {
                if (_userHandler.UserExistsAndReady(connectionId))
                {
                    User user = _userHandler.GetUser(connectionId);

                    //It's possible for a controller to disconnect without a ship
                    if (!user.Controller)
                    {
                        user.MyShip.Dispose();
                        user.Connected = false;
                    }
                    else
                    {
                        // Remove me from the ship hosts remote controllers
                        if (user.MyShip != null)
                        {
                            user.MyShip.Host.RemoteControllers.Remove(user);
                            user.MyShip.Host.NotificationManager.Notify("Detached controller.");
                            user.MyShip = null;
                        }

                        _userHandler.RemoveUser(connectionId);
                    }

                    // Leave the leaderboard group just in case user was in it
                    await _gameHub.Groups.RemoveFromGroupAsync(connectionId, Leaderboard.LEADERBOARD_REQUESTEE_GROUP);

                    // Clear controllers
                    foreach (User u in user.RemoteControllers)
                    {
                        u.MyShip = null;
                        await _gameHub.Clients.Client(u.ConnectionID).SendAsync("stopController", "Primary account has been stopped!");
                    }

                    user.RemoteControllers.Clear();
                }
            }
            catch (Exception e)
            {
                //ErrorLog.Instance.Log(e);
            }
            finally
            {
                _gameLock.Release();
            }
        }
    }
}