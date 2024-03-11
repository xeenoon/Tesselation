using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Tesselation;

namespace Tesselation
{
    public class MapFiller
    {
        public int width;
        public int height;
        public List<Shape> potentialshapes = new List<Shape>();
        public List<Shape> placedshapes = new List<Shape>();
        public List<Shape> adjacentshapes = new List<Shape>();

        public int[] board;
        public List<int[]> blacklistedboards = new List<int[]>();
        public List<BoardMoves> visitedboards = new List<BoardMoves>();

        public MapFiller(int width, int height, List<Shape> potentialshapes)
        {
            this.width = width;
            this.height = height;
            this.potentialshapes = potentialshapes;
            board = new int[width * height];
            foreach (var tile in potentialshapes.SelectMany(s => s.data.tiles))
            {
                board[tile.x + tile.y*width] = 0;
            }
        }


        static bool CanSumToTarget(int[] numbers, int targetSum)
        {
            bool[] dp = new bool[targetSum + 1];
            dp[0] = true;

            for (int i = 1; i <= targetSum; i++)
            {
                for (int j = 0; j < numbers.Length; j++)
                {
                    if (i - numbers[j] >= 0 && dp[i - numbers[j]])
                    {
                        dp[i] = true;
                        break;
                    }
                }
            }

            return dp[targetSum];
        }
        Stopwatch debugtimer = new Stopwatch();
        public long emptyareatime;
        public long cansumtotargettime;
        public long blacklisttesttime;
        public long backtracetime;

        public List<MoveData> GenerateMoves()
        {
            var precalcmoves = visitedboards.FirstOrDefault(bm => bm.board.SequenceEqual(board));
            if (!(precalcmoves is null))
            {
                return precalcmoves.moves;
            }
            debugtimer.Restart();
            List<MoveData> potentialmoves = new List<MoveData>();
            var moves = FindEmptyArea(board, width, height);
            debugtimer.Stop();
            emptyareatime += debugtimer.ElapsedTicks;
            Random r = new Random();
            int[] boardcopy = new int[width * height];

            debugtimer.Restart();
            bool cansum = CanSumToTarget(potentialshapes.Select(s => s.data.tiles.Count).Distinct().ToArray(), moves.Count);
            debugtimer.Stop();
            cansumtotargettime += debugtimer.ElapsedTicks;

            if ((moves.Count > 50) || cansum)
            {
                //check if a possible combination could theoretically exist
                List<Shape> shaperotations = potentialshapes.SelectMany(s => s.rotations).ToList();

                foreach (var shape in shaperotations)
                {
                    foreach (var placedposition in moves)
                    {
                        debugtimer.Restart();
                        bool canplace = !shape.data.tiles.Any(t => t.x + placedposition.X >= width ||
                                                             t.y + placedposition.Y >= height ||
                                                             board[t.x + placedposition.X + (t.y + placedposition.Y) * width] == 1);
                        if (canplace)
                        {
                            //place the piece
                            var copy = shape.PlaceData(placedposition);

                            foreach (var tile in shape.data.tiles)
                            {
                                board[tile.x + placedposition.X + (tile.y + placedposition.Y) * width] = 1;
                            }
                            if (!blacklistedboards.Any(b => board.SequenceEqual(b)))
                            {
                                int touchingsquares = FindTouchingSquares(shape, placedposition);
                                potentialmoves.Add(new MoveData(copy, touchingsquares, true));
                            }
                            foreach (var tile in shape.data.tiles)
                            {
                                board[tile.x + placedposition.X + (tile.y + placedposition.Y) * width] = 0;
                            } //faster to operate on board rather than copying board

                        }

                        debugtimer.Stop();
                        blacklisttesttime += debugtimer.ElapsedTicks;
                    }
                }
                if (potentialmoves.Count >= 1)
                {
                    board.CopyTo(boardcopy, 0);
                    visitedboards.Add(new BoardMoves(boardcopy, potentialmoves));
                    return potentialmoves;
                }
            }
            debugtimer.Start();
            //Found no legal moves? Backtrace if there were many moves available, as it is likely that the problem was caused by the last piece placed
            Shape toremove;
            //Use normal backtracing to remove two areas if possible
            if (adjacentshapes.Count == 0 && (moves.Count > 30 || moves.Count <= 4 || AreaCount(board, width, height) >= 2))
            {
                //instead of backtracing, try to remove side pieces
                adjacentshapes = placedshapes.Where(shape => shape.data.touchingsquares.Any(touchingtile =>
                moves.Contains(new Point(shape.data.location.X + touchingtile.X, shape.data.location.Y + touchingtile.Y)))).ToList();

                foreach (var shape in adjacentshapes)
                {
                    placedshapes.Remove(shape);
                    placedshapes.Add(shape); //Push to end of list;
                }
            }
            toremove = placedshapes.Last();

            potentialmoves.Clear();
            potentialmoves.Add(new MoveData(toremove.data, 0, false));
            board.CopyTo(boardcopy, 0);
            blacklistedboards.Add(boardcopy);

            var boardsoftcopy = new int[width * height];
            board.CopyTo(boardsoftcopy, 0);

            foreach (var tile in toremove.data.tiles)
            {
                boardsoftcopy[tile.x + toremove.data.location.X + (tile.y + toremove.data.location.Y) * width] = 0;
            }
            visitedboards.RemoveAll(v => v.board.SequenceEqual(boardsoftcopy));
            debugtimer.Stop();
            backtracetime += debugtimer.ElapsedTicks;
            return potentialmoves;
        }

        private int FindTouchingSquares(Shape copy, Point position)
        {
            int result = 0;
            foreach (var point in copy.data.touchingsquares)
            {
                int x = point.X + position.X;
                int y = point.Y + position.Y;
                if (x >= width || y>=height || x < 0 || y < 0)
                {
                    ++result;
                }
                else if (board[x + y * width] == 1)
                {
                    result += 1;
                }
            }
            return result;
        }

        static List<Point> FindEmptyArea(int[] board, int width, int height)
        {
            List<List<Point>> emptyAreas = new List<List<Point>>();

            bool[] visited = new bool[width * height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (board[x + y*width] == 0 && !visited[x + y * width])
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
        static int AreaCount(int[] board, int width, int height)
        {
            int areacount=0;
            bool[] visited = new bool[width * height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (board[x + y * width] == 0 && !visited[x + y * width])
                    {
                        List<Point> emptyArea = new List<Point>();
                        DFS(board, x, y, width, height, visited, emptyArea);
                        ++areacount;
                    }
                }
            }

            return areacount;
        }
        static void DFS(int[] board, int x, int y, int width, int height, bool[] visited, List<Point> emptyArea)
        {
            if (x < 0 || x >= width || y < 0 || y >= height || visited[x + y*width] || board[x + y * width] != 0)
            {
                return;
            }

            visited[x + y*width] = true;
            emptyArea.Add(new Point(x, y));

            DFS(board, x + 1, y, width, height, visited, emptyArea);
            DFS(board, x - 1, y, width, height, visited, emptyArea);
            DFS(board, x, y + 1, width, height, visited, emptyArea);
            DFS(board, x, y - 1, width, height, visited, emptyArea);
        }
    }
    public class MoveData
    {
        public ShapeData shape;
        public int touchingborders;
        public bool isplacing;

        public MoveData(ShapeData shape, int touchingborders, bool isplacing)
        {
            this.shape = shape;
            this.touchingborders = touchingborders;
            this.isplacing = isplacing;
        }
    }
    public class BoardMoves
    {
        public int[] board;
        public List<MoveData> moves;

        public BoardMoves(int[] board, List<MoveData> moves)
        {
            this.board = board;
            this.moves = moves;
        }
    }
}
