using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
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
        public List<Point> touchingsquares = new List<Point>();

        public Color[] potentialcolors = [Color.Orange, Color.Yellow, Color.LightCoral, 
                                        Color.Blue, Color.Purple, Color.Green, Color.DeepPink, 
                                        Color.RebeccaPurple, Color.YellowGreen, Color.Wheat, Color.Gray, Color.Gold, Color.OrangeRed];
        public Color color;
        Random r = new Random();
        public int width;
        public int height;
        public Point placedposition = new Point(-1, -1);

        public Shape(int tilecount, int width, int height)
        {
            this.width = width;
            this.height = height;
            this.color = potentialcolors[r.Next(0, potentialcolors.Count())];
            GenerateShape(tilecount);
        }
        public Shape(int width, int height)
        {
            this.width = width;
            this.height = height;
        }

        public void GenerateShape(int tilecount)
        {
            //Place a random tile
            tiles.Add(new Tile(0, 0));

            AddSideTiles(0,0);

            for (int i = 1; i < tilecount; ++i)
            {
                Tile starttile;
                Direction potentialdirections = Direction.None;

                int tries = 0;
                do
                {
                    ++tries;
                    starttile = tiles[r.Next(0, tiles.Count())];
                    if (starttile.x >= 1 && !tiles.Any(t => t.x == starttile.x - 1 && t.y == starttile.y))
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

                Direction nextsquare = directionarray[r.Next(0, directionarray.Count())];
                switch (nextsquare)
                {
                    case Direction.Up:
                        tiles.Add(new Tile(starttile.x, starttile.y - 1));
                        AddSideTiles(starttile.x, starttile.y - 1);
                        break;
                    case Direction.Right:
                        tiles.Add(new Tile(starttile.x + 1, starttile.y));
                        AddSideTiles(starttile.x + 1, starttile.y);
                        break;
                    case Direction.Down:
                        tiles.Add(new Tile(starttile.x, starttile.y + 1));
                        AddSideTiles(starttile.x, starttile.y + 1);
                        break;
                    case Direction.Left:
                        tiles.Add(new Tile(starttile.x - 1, starttile.y));
                        AddSideTiles(starttile.x - 1, starttile.y);
                        break;

                }
            }

            LeftCornerAdjust();
            for (int i = 0; i < 4; ++i)
            {
                rotations.Add(Rotate(i * 90));
            }
            touchingsquares = touchingsquares.Distinct().ToList();
            touchingsquares.RemoveAll(t => tiles.Any(tile => tile.x == t.X && tile.y == t.Y));
        }

        private void AddSideTiles(int x, int y)
        {
            touchingsquares.Add(new Point(x-1, y));
            touchingsquares.Add(new Point(x, y-1));
            touchingsquares.Add(new Point(x+1, y));
            touchingsquares.Add(new Point(x, y+1));
        }

        public List<Shape> rotations = new List<Shape>();

        public void LeftCornerAdjust()
        {
            //move to top left corner
            int lowestx = tiles.OrderBy(t => t.x).First().x;
            int lowesty = tiles.OrderBy(t => t.y).First().y;

            for (int i = 0; i < tiles.Count; ++i)
            {
                tiles[i].x -= lowestx;
                tiles[i].y -= lowesty;
            }
        }

        internal Shape Place(Point placingtile)
        {
            Shape duplicate = new Shape(width, height);
            foreach (var tile in tiles)
            {
                duplicate.tiles.Add(new Tile(tile.x, tile.y));
            }
            duplicate.color = this.color;
            duplicate.placedposition = placingtile;
            return duplicate;
        }

        public static bool operator ==(Shape a, Shape b)
        {
            if (a is null || b is null)
            {
                return false;
            }
            if (a.tiles.Count == b.tiles.Count)
            {
                int duplicatetiles = 0;
                for (int i = 0; i < a.tiles.Count; ++i)
                {
                    if (b.tiles.Any(t=>t.x == a.tiles[i].x && t.y == a.tiles[i].y))
                    {
                        ++duplicatetiles;
                    }
                }
                if (duplicatetiles == a.tiles.Count)
                {
                    return true;
                }
            }
            return false;
        }
        public static bool operator !=(Shape a, Shape b)
        {
            return !(a == b);
        }
        public Shape Rotate(int degrees)
        {
            Shape copy = new Shape(width,height);
            foreach (var tile in tiles)
            {
                Point newlocation = RotatePoint(new Point(tile.x, tile.y), new Point((width-1)/2, (height-1)/2), degrees);
                copy.tiles.Add(new Tile(newlocation.X, newlocation.Y));
            }
            copy.LeftCornerAdjust();
            foreach (var tile in copy.tiles)
            {
                copy.AddSideTiles(tile.x, tile.y);
            }

            copy.touchingsquares = copy.touchingsquares.Distinct().ToList();
            copy.touchingsquares.RemoveAll(t => copy.tiles.Any(tile => tile.x == t.X && tile.y == t.Y));

            copy.color = color;
            return copy;
        }
        static Point RotatePoint(Point pointToRotate, Point centerPoint, double angleInDegrees)
        {
            double angleInRadians = angleInDegrees * (Math.PI / 180);
            double cosTheta = Math.Cos(angleInRadians);
            double sinTheta = Math.Sin(angleInRadians);
            return new Point
            ((int)Math.Round(
                    (cosTheta * (pointToRotate.X - centerPoint.X) -
                    sinTheta * (pointToRotate.Y - centerPoint.Y) + centerPoint.X)),

             (int)Math.Round(
                    (sinTheta * (pointToRotate.X - centerPoint.X) +
                    cosTheta * (pointToRotate.Y - centerPoint.Y) + centerPoint.Y))
            );
        }
    }
}