using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace ShootR
{
    public class GameHub : Hub
    {
        private readonly Game _game;
        private readonly ILogger _logger;
        public GameHub(Game game, ILogger<GameHub> logger)
        {
            _game = game;
            _logger = logger;
        }

        public override Task OnConnectedAsync()
        {
            _game.ConnectionManager.OnConnected(Context.ConnectionId);
            return Task.CompletedTask;
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            return _game.ConnectionManager.OnDisconnectedAsync(Context.ConnectionId);
        }

        /// <summary>
        /// Called when a ship fire's a bullet
        /// </summary>
        public double fire()
        {
            try
            {
                if (_game.UserHandler.UserExistsAndReady(Context.ConnectionId))
                {

                    Ship ship = _game.UserHandler.GetUserShip(Context.ConnectionId);

                    if (ship.Controllable.Value)
                    {
                        ship.WeaponController.Fire(DateTime.UtcNow);
                    }
                    return ship.WeaponController.Energy;

                }
                throw new Exception("Could not find user when firing.");

            }
            catch (Exception e)
            {
                _logger.LogError("fire", e);
            }

            return 0;
        }

        /// <summary>
        /// Called when a ship starts firing a stream of bullet at the maximum possible rate
        /// </summary>
        public void startFire()
        {
            if (_game.UserHandler.UserExistsAndReady(Context.ConnectionId))
            {
                try
                {
                    Ship ship = _game.UserHandler.GetUserShip(Context.ConnectionId);

                    if (ship.Controllable.Value)
                    {
                        ship.WeaponController.AutoFire = true;
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError("startFire", e);
                }
            }
        }

        /// <summary>
        /// Called when a ship stops firing a stream of bullet
        /// </summary>
        public double stopFire()
        {
            if (_game.UserHandler.UserExistsAndReady(Context.ConnectionId))
            {
                try
                {
                    Ship ship = _game.UserHandler.GetUserShip(Context.ConnectionId);

                    ship.WeaponController.AutoFire = false;
                    return ship.WeaponController.Energy;
                }
                catch (Exception e)
                {
                    _logger.LogError("stopFire", e);
                }
            }

            return 0;
        }

        public void readyForPayloads()
        {
            try
            {
                _game.UserHandler.GetUser(Context.ConnectionId).ReadyForPayloads = true;
            }
            catch (Exception e)
            {
                _logger.LogError("readyForPayloads", e);
            }
        }

        public object initializeClient(string registrationID)
        {
            if (_game.RegistrationHandler.RegistrationExists(registrationID))
            {
                return _game.initializeClient(Context.ConnectionId, _game.RegistrationHandler.RemoveRegistration(registrationID));
            }

            return null;
        }

        public virtual void syncMovement(Vector2 at, double angle, Vector2 velocity)
        {
            if (_game.UserHandler.UserExistsAndReady(Context.ConnectionId))
            {
                try
                {
                    _game.UserHandler.GetUserShip(Context.ConnectionId).SyncMovement(at, angle, velocity);
                }
                catch (Exception e)
                {
                    _logger.LogError("syncMovement", e);
                }
            }
        }

        /// <summary>
        /// Resets all movement flags on the ship
        /// </summary>
        /// <param name="pingBack"></param>
        public async Task resetMovement(List<string> movementList, Vector2 at, double angle, Vector2 velocity, bool pingBack)
        {
            if (_game.UserHandler.UserExistsAndReady(Context.ConnectionId))
            {
                try
                {
                    if (pingBack)
                    {
                        await Clients.Client(Context.ConnectionId).SendAsync("pingBack");
                    }

                    Ship ship = _game.UserHandler.GetUserShip(Context.ConnectionId);

                    if (ship.Controllable.Value)
                    {
                        List<Movement> result = new List<Movement>();
                        foreach (string where in movementList)
                        {
                            result.Add((Movement)Enum.Parse(typeof(Movement), where));
                        }

                        ship.ResetMoving(result, at, angle, velocity);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError("resetMovement", e);
                }
            }
        }

        public async Task startAndStopMovement(string toStop, string toStart, Vector2 at, double angle, Vector2 velocity, bool pingBack)
        {
            if (_game.UserHandler.UserExistsAndReady(Context.ConnectionId))
            {
                try
                {
                    if (pingBack)
                    {
                        await Clients.Client(Context.ConnectionId).SendAsync("pingBack");
                    }

                    Ship ship = _game.UserHandler.GetUserShip(Context.ConnectionId);

                    if (ship.Controllable.Value)
                    {
                        Movement whereToStop = (Movement)Enum.Parse(typeof(Movement), toStop);
                        Movement whereToStart = (Movement)Enum.Parse(typeof(Movement), toStart);
                        ship.StopMoving(whereToStop, at, angle, velocity);
                        ship.StartMoving(whereToStart, at, angle, velocity);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError("startAndStopMovement", e);
                }
            }
        }

        /// <summary>
        /// Registers the start of a movement on a client. Fires when the client presses a movement hotkey.
        /// </summary>
        /// <param name="movement">Direction to start moving</param>
        public async Task registerMoveStart(string movement, Vector2 at, double angle, Vector2 velocity, bool pingBack)
        {
            if (_game.UserHandler.UserExistsAndReady(Context.ConnectionId))
            {
                try
                {
                    if (pingBack)
                    {
                        await Clients.Client(Context.ConnectionId).SendAsync("pingBack");
                    }

                    Ship ship = _game.UserHandler.GetUserShip(Context.ConnectionId);

                    if (ship.Controllable.Value)
                    {
                        var where = (Movement)Enum.Parse(typeof(Movement), movement);
                        ship.StartMoving(where, at, angle, velocity);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError("registerMoveStart", e);
                }
            }
        }

        /// <summary>
        /// Registers the stop of a movement on a client. Fires when the client presses a movement hotkey.
        /// </summary>
        /// <param name="movement">Direction to stop moving</param>
        public async Task registerMoveStop(string movement, Vector2 at, double angle, Vector2 velocity, bool pingBack)
        {
            if (_game.UserHandler.UserExistsAndReady(Context.ConnectionId))
            {
                try
                {
                    if (pingBack)
                    {
                        await Clients.Client(Context.ConnectionId).SendAsync("pingBack");
                    }

                    Ship ship = _game.UserHandler.GetUserShip(Context.ConnectionId);

                    if (ship.Controllable.Value)
                    {
                        var where = (Movement)Enum.Parse(typeof(Movement), movement);
                        ship.StopMoving(where, at, angle, velocity);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError("registerMoveStop", e);
                }
            }
        }

        public async Task registerAbilityStart(string abilityName, Vector2 at, double angle, Vector2 velocity, bool pingBack)
        {
            if (_game.UserHandler.UserExistsAndReady(Context.ConnectionId))
            {
                try
                {
                    if (pingBack)
                    {
                        await Clients.Client(Context.ConnectionId).SendAsync("pingBack");
                    }

                    Ship ship = _game.UserHandler.GetUserShip(Context.ConnectionId);

                    if (ship.Controllable.Value)
                    {
                        ship.ActivateAbility(abilityName, at, angle, velocity);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError("registerAbilityStart", e);
                }
            }
        }

        public async Task registerAbilityStop(string abilityName, Vector2 at, double angle, Vector2 velocity, bool pingBack)
        {
            if (_game.UserHandler.UserExistsAndReady(Context.ConnectionId))
            {
                try
                {
                    if (pingBack)
                    {
                        await Clients.Client(Context.ConnectionId).SendAsync("pingBack");
                    }

                    Ship ship = _game.UserHandler.GetUserShip(Context.ConnectionId);

                    if (ship.Controllable.Value)
                    {
                        ship.DeactivateAbility(abilityName, at, angle, velocity);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError("registerAbilityStop", e);
                }
            }
        }

        public void changeViewport(int viewportWidth, int viewportHeight)
        {
            try
            {
                if (_game.UserHandler.UserExistsAndReady(Context.ConnectionId))
                {
                    _game.UserHandler.GetUser(Context.ConnectionId).Viewport = new Size(viewportWidth, viewportHeight);
                }
            }
            catch (Exception e)
            {
                _logger.LogError("changeViewport", e);
            }
        }

        public async Task readyForLeaderboardPayloads()
        {
            try
            {
                if (_game.UserHandler.UserExistsAndReady(Context.ConnectionId))
                {
                    _game.UserHandler.GetUser(Context.ConnectionId).IdleManager.RecordActivity();
                    await _game.Leaderboard.RequestLeaderboardAsync(Context.ConnectionId);
                }
            }
            catch (Exception e)
            {
                _logger.LogError("readyForLeaderboardPayloads", e);
            }
        }

        public async Task stopLeaderboardPayloads()
        {
            try
            {
                if (_game.UserHandler.UserExistsAndReady(Context.ConnectionId))
                {
                    _game.UserHandler.GetUser(Context.ConnectionId).IdleManager.RecordActivity();
                    await _game.Leaderboard.StopRequestingLeaderboardAsync(Context.ConnectionId);
                }
            }
            catch (Exception e)
            {
                _logger.LogError("stopLeaderboardPayloads", e);
            }
        }

        public async Task sendMessage(string message)
        {
            try
            {
                if (_game.UserHandler.UserExistsAndReady(Context.ConnectionId))
                {
                    Ship ship = _game.UserHandler.GetUserShip(Context.ConnectionId);
                    var from = ship.Name;

                    //TODO: send a message to #shootr using the jabbr c# client later
                    await Clients.AllExcept(new List<string>{ Context.ConnectionId }).SendAsync("chatMessage", from, message, 0 /* standard message */);
                }
            }
            catch (Exception e)
            {
                _logger.LogError("sendMessage", e);
            }
        }
    }
}