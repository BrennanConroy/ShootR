using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace ShootR
{
    public class Game
    {
        public ConnectionManager ConnectionManager { get; private set; }
        public RegistrationHandler RegistrationHandler { get; private set; } = new RegistrationHandler();
        public GameHandler GameHandler { get; private set; }
        public UserHandler UserHandler { get; private set; }

        public GameConfigurationManager Configuration { get; set; }
        public Leaderboard Leaderboard { get; private set; }

        private PayloadManager _payloadManager;
        private HighFrequencyTimer _gameLoop;
        private GameTime _gameTime;
        private readonly Map _space;
        private readonly SemaphoreSlim _gameLock = new SemaphoreSlim(1, 1);
        private long _actualFPS = 0;
        public const int AIShipsToSpawn = 5;
        public const int SpawnsPerInterval = 1;
        private int _spawned = 0;
        private DateTime _lastSpawn = DateTime.UtcNow;
        private long _drawCount = 0;
        private long _drawFPS = 0;
        private int DRAW_AFTER;
        private IHubContext<GameHub> _gameHub;
        private Timer _leaderboardLoop;

        public static Random GEN = new Random();

        public Game(IHubContext<GameHub> gameHub)
        {
            Configuration = new GameConfigurationManager();
            
            _gameLoop = new HighFrequencyTimer(1000 / Configuration.gameConfig.UPDATE_INTERVAL, async id => await Update(id), () => { }, () => { }, (fps) =>
            {
                _actualFPS = fps;
            });
            _gameHub = gameHub;

            _leaderboardLoop = new Timer(UpdateLeaderboard, null, Configuration.gameConfig.LEADERBOARD_PUSH_INTERVAL, Configuration.gameConfig.LEADERBOARD_PUSH_INTERVAL);

            DRAW_AFTER = Configuration.gameConfig.DRAW_INTERVAL / Configuration.gameConfig.UPDATE_INTERVAL;
            _gameTime = new GameTime();
            _space = new Map(this, gameHub);
            GameHandler = new GameHandler(_space, this);
            _payloadManager = new PayloadManager();
            UserHandler = new UserHandler(GameHandler);
            Leaderboard = new Leaderboard(UserHandler, gameHub);
            ConnectionManager = new ConnectionManager(UserHandler, _gameLock, gameHub);

            _gameLoop.Start();
        }

        private async Task<long> Update(long id)
        {
            await _gameLock.WaitAsync();
            try
            {
                var utcNow = DateTime.UtcNow;
                if ((utcNow - _lastSpawn).TotalSeconds >= 1 && _spawned < AIShipsToSpawn)
                {
                    _spawned += SpawnsPerInterval;
                    SpawnAIShips(SpawnsPerInterval);
                    _lastSpawn = utcNow;
                }

                _gameTime.Update(utcNow);

                await GameHandler.Update(_gameTime);

                _space.Update();

                if (_actualFPS <= _drawFPS || (++_drawCount) % DRAW_AFTER == 0)
                {
                    await DrawAsync();
                    _drawCount = 0;
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

            return id;
        }

        public void SpawnAIShips(int number)
        {
            for (int i = 0; i < number; i++)
            {
                string connectionidAI = Guid.NewGuid().ToString();
                ShipAI shipAI = new ShipAI(RespawnManager.GetRandomStartPosition(), GameHandler.BulletManager, this);
                UserAI userAI = new UserAI(connectionidAI, shipAI) { Controller = false };
                UserHandler.AddUser(userAI);
                GameHandler.AddShipToGame(shipAI);
            }
        }

        /// <summary>
        /// Sends down batches of data to the clients in order to update their screens
        /// </summary>
        private async Task DrawAsync()
        {
            await _space.CheckIncreaseMapSizeAsync(UserHandler.TotalActiveUsers);
            UserHandler.Update();

            var payloads = _payloadManager.GetGamePayloads(UserHandler.GetUsers(), UserHandler.TotalActiveUsers, GameHandler.BulletManager.Bullets.Count, _space);

            foreach (string connectionID in payloads.Keys)
            {
                await UserHandler.GetUser(connectionID).PushToClientAsync(payloads[connectionID], _gameHub);
            }
        }

        /// <summary>
        /// Retrieves the game's configuration
        /// </summary>
        /// <returns>The game's configuration</returns>
        public object initializeClient(string connectionId, RegisteredClient rc)
        {
            if (!UserHandler.UserExistsAndReady(connectionId))
            {
                _gameLock.Wait();
                try
                {
                    User user = null;//UserHandler.FindUserByIdentity(rc.Identity);
                    Ship ship = null;

                    if (user == null)
                    {
                        if (UserHandler.TotalActiveUsers >= 50/*RuntimeConfiguration.MaxServerUsers*/)
                        {
                            return new
                            {
                                ServerFull = true
                            };
                        }
                        else
                        {
                            ship = new Ship(this, RespawnManager.GetRandomStartPosition(), GameHandler.BulletManager);
                            ship.Name = rc.DisplayName;
                            user = new User(connectionId, ship, rc) { Controller = false };
                            UserHandler.AddUser(user);
                        }
                    }

                    GameHandler.AddShipToGame(ship);

                    return new
                    {
                        Configuration = Configuration,
                        ServerFull = false,
                        CompressionContracts = new
                        {
                            PayloadContract = _payloadManager.Compressor.PayloadCompressionContract,
                            CollidableContract = _payloadManager.Compressor.CollidableCompressionContract,
                            ShipContract = _payloadManager.Compressor.ShipCompressionContract,
                            BulletContract = _payloadManager.Compressor.BulletCompressionContract,
                            LeaderboardEntryContract = _payloadManager.Compressor.LeaderboardEntryCompressionContract,
                            PowerupContract = _payloadManager.Compressor.PowerupCompressionContract
                        },
                        ShipID = UserHandler.GetUserShip(connectionId).ID,
                        ShipName = UserHandler.GetUserShip(connectionId).Name
                    };
                }
                catch
                { }
                finally
                {
                    _gameLock.Release();
                }
            }
            return null;
        }

        private async void UpdateLeaderboard(object state)
        {
            // This will in-frequently throw an error due to race conditions.  Instead of locking I'd rather it fail in means of maintaining speed.
            try
            {
                var leaderboardEntries = _payloadManager.GetLeaderboardPayloads(Leaderboard.GetAndUpdateLeaderboard());

                await PushLeaderboardAsync(leaderboardEntries);
            }
            catch (Exception e)
            {
                //ErrorLog.Instance.Log(e);
            }
        }

        private async Task PushLeaderboardAsync (List<object> leaderboard)
        {
            await _gameHub.Clients.Group(Leaderboard.LEADERBOARD_REQUESTEE_GROUP).SendAsync("l", leaderboard);
        }
    }
}