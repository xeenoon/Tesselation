using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tesselation;

namespace Tesselation
{
    public class MapFiller
    {
        public int width;
        public int height;
        public List<Shape> shapes = new List<Shape>();
        public int[,] board;

        public MapFiller(int width, int height, List<Shape> shapes)
        {
            this.width = width;
            this.height = height;
            this.shapes = shapes;
            board = new int[width, height];
            foreach (var tile in shapes.SelectMany(s => s.tiles))
            {
                board[tile.x, tile.y] = 0;
            }
        }


        public List<MoveData> GenerateMoves()
        {
            List<MoveData> potentialmoves = new List<MoveData>();
            var moves = FindEmptyArea(board, width, height);
            Random r = new Random();

            List<Shape> shuffledshapes = shapes.Shuffle().SelectMany(s=>s.rotations).ToList();
            foreach (var shape in shuffledshapes)
            {
                foreach (var placedposition in moves)
                {
                    if (!shape.tiles.Any(t => t.x + placedposition.X >= width || t.y + placedposition.Y >= height || board[t.x + placedposition.X, t.y + placedposition.Y] == 1))
                    {

                        //place the piece
                        Shape copy = shape.Place(placedposition);
                        shapes.Add(copy);
                        foreach (var tile in copy.tiles)
                        {
                            board[tile.x + placedposition.X, tile.y + placedposition.Y] = 1;
                        }
                        int touchingsquares = FindTouchingSquares(copy);
                        potentialmoves.Add(new MoveData(copy, touchingsquares, false));
                    }
                }
            }
            if (potentialmoves.Count >= 1)
            {
                return potentialmoves;
            }
            //Found no level moves? Backtrace
            Shape toremove = shapes.Last();
            foreach (var tile in toremove.tiles)
            {
                board[tile.x + toremove.placedposition.X, tile.y + toremove.placedposition.Y] = 0;
            }
            shapes.Remove(toremove);
            potentialmoves.Clear();
            potentialmoves.Add(new MoveData(toremove, 0, false));
            return potentialmoves;
        }

        private int FindTouchingSquares(Shape copy)
        {
            int result = 0;
            foreach (var point in copy.touchingsquares)
            {
                int x = point.X + copy.placedposition.X;
                int y = point.Y + copy.placedposition.Y;
                if (x >= width || y>=height || x < 0 || y < 0 || board[x, y] == 1)
                {
                    ++result;
                }
            }
            return result;
        }

        static List<Point> FindEmptyArea(int[,] board, int width, int height)
        {
            List<List<Point>> emptyAreas = new List<List<Point>>();

            bool[,] visited = new bool[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (board[x, y] == 0 && !visited[x, y])
                    {
                        List<Point> emptyArea = new List<Point>();
                        DFS(board, x, y, width, height, visited, emptyArea);
                        emptyAreas.Add(emptyArea);
                    }
                }
            }

            // Filter out only the smallest areas
            int minAreaSize = int.MaxValue;
            List<Point> smallestArea = new List<Point>();

            foreach (var area in emptyAreas)
            {
                if (area.Count < minAreaSize)
                {
                    smallestArea.Clear();
                    smallestArea = area;
                    minAreaSize = area.Count;
                }
                else if (area.Count == minAreaSize)
                {
                    smallestArea = area;
                }
            }

            return smallestArea;
        }

        static void DFS(int[,] board, int x, int y, int width, int height, bool[,] visited, List<Point> emptyArea)
        {
            if (x < 0 || x >= width || y < 0 || y >= height || visited[x, y] || board[x, y] != 0)
            {
                return;
            }

            visited[x, y] = true;
            emptyArea.Add(new Point(x, y));

            DFS(board, x + 1, y, width, height, visited, emptyArea);
            DFS(board, x - 1, y, width, height, visited, emptyArea);
            DFS(board, x, y + 1, width, height, visited, emptyArea);
            DFS(board, x, y - 1, width, height, visited, emptyArea);
        }
    }
    public class MoveData
    {
        public Shape shape;
        public int touchingborders;
        public bool isplacing;

        public MoveData(Shape shape, int touchingborders, bool isplacing)
        {
            this.shape = shape;
            this.touchingborders = touchingborders;
            this.isplacing = isplacing;
        }
    }
}
