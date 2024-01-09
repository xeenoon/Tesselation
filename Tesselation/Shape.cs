using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tesselation
{
    public class Tile
    {
        public int x, y;

        public Tile(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
    }
    [Flags]
    enum Direction
    {
        None = 0,
        Up=1,
        Right=2,
        Down=4,
        Left=8,
    }
    public class Shape
    {
        public List<Tile> tiles = new List<Tile>();
        Random r = new Random();
        public int width;
        public int height;

        public Shape(int tilecount, int width, int height)
        {
            this.width = width;
            this.height = height;
            GenerateShape(tilecount);
        }

        public void GenerateShape(int tilecount = 4)
        {
            //Place a random tile
            tiles.Add(new Tile(r.Next(0,width), r.Next(0,height)));
            for (int i = 1; i < tilecount; ++i)
            {
                Tile starttile;
                Direction potentialdirections = Direction.None;

                int tries = 0;
                do
                {
                    ++tries;
                    starttile = tiles[r.Next(0, tiles.Count())];
                    if (starttile.x >= 1 && !tiles.Any(t=>t.x == starttile.x-1 && t.y == starttile.y))
                    {
                        potentialdirections |= Direction.Left;
                    }
                    if (starttile.x <= width - 2 && !tiles.Any(t => t.x == starttile.x + 1 && t.y == starttile.y))
                    {
                        potentialdirections |= Direction.Right;
                    }
                    if (starttile.y >= 1 && !tiles.Any(t => t.y == starttile.y - 1 && t.x == starttile.x))
                    {
                        potentialdirections |= Direction.Up;
                    }
                    if (starttile.y <= height - 2 && !tiles.Any(t => t.y == starttile.y + 1 && t.x == starttile.x))
                    {
                        potentialdirections |= Direction.Down;
                    }
                } while (potentialdirections == Direction.None);
                tries = 0;

                //Pick a random direction
                var directionarray = Enum.GetValues(typeof(Direction))
                    .Cast<Direction>()
                    .Where(c => (potentialdirections & c) == c && c != Direction.None)    // or use HasFlag in .NET4
                    .ToArray();

                Direction nextsquare = directionarray[r.Next(0,directionarray.Count())];
                switch (nextsquare)
                {
                    case Direction.Up:
                        tiles.Add(new Tile(starttile.x, starttile.y - 1));
                        break;
                    case Direction.Right:
                        tiles.Add(new Tile(starttile.x + 1, starttile.y));
                        break;
                    case Direction.Down:
                        tiles.Add(new Tile(starttile.x, starttile.y + 1));
                        break;
                    case Direction.Left:
                        tiles.Add(new Tile(starttile.x - 1, starttile.y));
                        break;

                }
            }
        }
    }
}
