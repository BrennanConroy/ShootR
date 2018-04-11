
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace ShootR
{
    public class Map
    {
        public static int WIDTH = 5000;
        public static int HEIGHT = 5000;
        public static int MAX_SHIPS = 40;
        public static int MIN_PARTITION_WIDTH = 156;
        public static int MIN_PARTITION_HEIGHT = 156;
        public const double BARRIER_DEPRECATION = .75;

        private QuadTree _space;
        private static MapBoundary _boundary;
        private List<Collidable> _allObjects;
        private Game _game;
        private IHubContext<GameHub> _gameHub;

        private object _insertLock = new object();

        public Map(Game game, IHubContext<GameHub> gameHub)
        {
            // Double collidable for fast removes/inserts
            _allObjects = new List<Collidable>();
            _space = new QuadTree(WIDTH, HEIGHT, MIN_PARTITION_WIDTH, MIN_PARTITION_HEIGHT);
            _boundary = new MapBoundary(WIDTH, HEIGHT);
            _game = game;
            _gameHub = gameHub;
        }

        public void Insert(Collidable obj)
        {
            lock (_insertLock)
            {
                _allObjects.Add(obj);
                _space.Insert(obj);
            }
        }

        public List<Collidable> GetPartialCollisionCheckList(Collidable obj)
        {
            return _space.GetPartialCollisionCheckList(obj);
        }

        public List<Collidable> Query(Rectangle queryArea)
        {
            return _space.Query(queryArea);
        }

        public bool Contains(Collidable obj)
        {
            return _allObjects.Contains(obj);
        }

        public async Task CheckIncreaseMapSizeAsync(int shipCount)
        {
            if (shipCount >= MAX_SHIPS)
            {
                IncreaseSize();
                _game.Configuration.mapConfig.WIDTH = WIDTH;
                _game.Configuration.mapConfig.HEIGHT = HEIGHT;
                await _gameHub.Clients.All.SendAsync("mapSizeIncreased", new { Width = WIDTH, Height = HEIGHT });
            }
        }

        public void IncreaseSize()
        {
            WIDTH *= 2;
            HEIGHT *= 2;
            MAX_SHIPS *= 4;
            MIN_PARTITION_WIDTH *= 2;
            MIN_PARTITION_HEIGHT *= 2;

            _boundary = new MapBoundary(WIDTH, HEIGHT);

            _space.ExpandTo(WIDTH, HEIGHT, MIN_PARTITION_WIDTH, MIN_PARTITION_HEIGHT);
        }

        public void Remove(Collidable obj)
        {
            _allObjects.Remove(obj);
            _space.Remove(obj);
        }

        public void Clear()
        {
            _space.Clear();
        }

        public void Clean()
        {
            for (int i = 0; i < _allObjects.Count; i++)
            {
                if (_allObjects[i].Disposed)
                {
                    Remove(_allObjects[i--]);
                }
                else
                {
                    _allObjects[i].ResetFlags();
                }
            }
        }

        public void Update()
        {
            _space.Update();
        }

        public static bool OnMap(Collidable obj)
        {
            return _boundary.OnMap(obj);
        }

        public void HandleOutOfBounds(Collidable obj)
        {

        }
    }
}