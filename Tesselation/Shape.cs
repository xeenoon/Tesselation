using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Tesselation
{
    public struct Tile
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
    public struct ShapeData
    {
        public List<Point> touchingsquares = new List<Point>();
        public List<Tile> tiles;
        public int width;
        public int height;
        public Point location;
        public Color color;

        public ShapeData(int width, int height, Point location)
        {
            this.tiles = new List<Tile>();
            this.width = width;
            this.height = height;
            this.location = location;
        }
    }
    public class Shape
    {
        public ShapeData data;

        public Color[] potentialcolors = [Color.Orange, Color.Yellow, Color.LightCoral, 
                                        Color.Blue, Color.Purple, Color.Green, Color.DeepPink, 
                                        Color.RebeccaPurple, Color.YellowGreen, Color.Wheat, Color.Gray, Color.Gold, Color.OrangeRed];
        Random r = new Random();

        public Shape(int tilecount, int width, int height)
        {
            data = new ShapeData(width, height, new Point(0,0));
            data.color = potentialcolors[r.Next(0, potentialcolors.Count())];
            GenerateShape(tilecount);
        }
        public Shape(int width, int height)
        {
            data = new ShapeData(width, height, new Point(0, 0));
            data.color = potentialcolors[r.Next(0, potentialcolors.Count())];
        }
        public Shape(ShapeData data)
        {
            this.data = data;

            for (int i = 0; i < 4; ++i)
            {
                rotations.Add(Rotate(i * 90));
            }
            data.touchingsquares = data.touchingsquares.Distinct().ToList();
            data.touchingsquares.RemoveAll(t => data.tiles.Any(tile => tile.x == t.X && tile.y == t.Y));
        }

        public void GenerateShape(int tilecount)
        {
            data.tiles.Add(new Tile(0, 0));

            AddSideTiles(0,0);

            for (int i = 1; i < tilecount; ++i)
            {
                Tile starttile;
                Direction potentialdirections = Direction.None;

                int tries = 0;
                do
                {
                    ++tries;
                    starttile = data.tiles[r.Next(0, data.tiles.Count())];
                    if (starttile.x >= 1 && !data.tiles.Any(t => t.x == starttile.x - 1 && t.y == starttile.y))
                    {
                        potentialdirections |= Direction.Left;
                    }
                    if (starttile.x <= data.width - 2 && !data.tiles.Any(t => t.x == starttile.x + 1 && t.y == starttile.y))
                    {
                        potentialdirections |= Direction.Right;
                    }
                    if (starttile.y >= 1 && !data.tiles.Any(t => t.y == starttile.y - 1 && t.x == starttile.x))
                    {
                        potentialdirections |= Direction.Up;
                    }
                    if (starttile.y <= data.height - 2 && !data.tiles.Any(t => t.y == starttile.y + 1 && t.x == starttile.x))
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
                        data.tiles.Add(new Tile(starttile.x, starttile.y - 1));
                        AddSideTiles(starttile.x, starttile.y - 1);
                        break;
                    case Direction.Right:
                        data.tiles.Add(new Tile(starttile.x + 1, starttile.y));
                        AddSideTiles(starttile.x + 1, starttile.y);
                        break;
                    case Direction.Down:
                        data.tiles.Add(new Tile(starttile.x, starttile.y + 1));
                        AddSideTiles(starttile.x, starttile.y + 1);
                        break;
                    case Direction.Left:
                        data.tiles.Add(new Tile(starttile.x - 1, starttile.y));
                        AddSideTiles(starttile.x - 1, starttile.y);
                        break;

                }
            }

            LeftCornerAdjust();
            for (int i = 0; i < 4; ++i)
            {
                rotations.Add(Rotate(i * 90));
            }
            data.touchingsquares = data.touchingsquares.Distinct().ToList();
            data.touchingsquares.RemoveAll(t => data.tiles.Any(tile => tile.x == t.X && tile.y == t.Y));
        }

        private void AddSideTiles(int x, int y)
        {
            data.touchingsquares.Add(new Point(x-1, y));
            data.touchingsquares.Add(new Point(x, y-1));
            data.touchingsquares.Add(new Point(x+1, y));
            data.touchingsquares.Add(new Point(x, y+1));
        }

        public List<Shape> rotations = new List<Shape>();

        public void LeftCornerAdjust()
        {
            //move to top left corner
            int lowestx = data.tiles.OrderBy(t => t.x).First().x;
            int lowesty = data.tiles.OrderBy(t => t.y).First().y;

            for (int i = 0; i < data.tiles.Count; ++i)
            {
                data.tiles[i] = new Tile(data.tiles[i].x - lowestx, data.tiles[i].y - lowesty);

            }
        }

        internal Shape Place(Point placingtile)
        {
            Shape duplicate = new Shape(data.width, data.height);
            foreach (var tile in data.tiles)
            {
                duplicate.data.tiles.Add(new Tile(tile.x, tile.y));
            }
            duplicate.data.color = this.data.color;
            duplicate.data.location = placingtile;
            return duplicate;
        }
        internal ShapeData PlaceData(Point placingtile)
        {
            ShapeData duplicate = new ShapeData(data.width, data.height, placingtile);
            foreach (var tile in data.tiles)
            {
                duplicate.tiles.Add(new Tile(tile.x, tile.y));
            }
            foreach (var tile in data.touchingsquares)
            {
                duplicate.touchingsquares.Add(new Point(tile.X, tile.Y));
            }
            duplicate.color = this.data.color;
            return duplicate;
        }

        public static bool operator ==(Shape a, Shape b)
        {
            if (a is null || b is null)
            {
                return false;
            }
            if (a.data.tiles.Count == b.data.tiles.Count)
            {
                int duplicatetiles = 0;
                for (int i = 0; i < a.data.tiles.Count; ++i)
                {
                    if (b.data.tiles.Any(t=>t.x == a.data.tiles[i].x && t.y == a.data.tiles[i].y))
                    {
                        ++duplicatetiles;
                    }
                }
                if (duplicatetiles == a.data.tiles.Count)
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
            Shape copy = new Shape(data.width, data.height);
            foreach (var tile in data.tiles)
            {
                Point newlocation = RotatePoint(new Point(tile.x, tile.y), new Point((data.width -1)/2, (data.height -1)/2), degrees);
                copy.data.tiles.Add(new Tile(newlocation.X, newlocation.Y));
            }
            copy.LeftCornerAdjust();
            foreach (var tile in copy.data.tiles)
            {
                copy.AddSideTiles(tile.x, tile.y);
            }

            copy.data.touchingsquares = copy.data.touchingsquares.Distinct().ToList();
            copy.data.touchingsquares.RemoveAll(t => copy.data.tiles.Any(tile => tile.x == t.X && tile.y == t.Y));

            copy.data.color = data.color;
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