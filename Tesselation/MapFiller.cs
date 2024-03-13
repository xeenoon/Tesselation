using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Tesselation;

namespace Tesselation
{
    public unsafe class MapFiller
    {
        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern int memcpy(byte* dest, byte* src, long count);
        public static extern IntPtr memcpy(IntPtr dest, IntPtr src, UIntPtr count);


        public int width;
        public int height;
        public List<Shape> potentialshapes = new List<Shape>();
        public List<Shape> placedshapes = new List<Shape>();
        public List<Shape> adjacentshapes = new List<Shape>();

        public Board board;
        public List<Board> blacklistedboards = new List<Board>();
        public List<BoardMoves> visitedboards = new List<BoardMoves>();

        public MapFiller(int width, int height, List<Shape> potentialshapes)
        {
            this.width = width;
            this.height = height;
            this.potentialshapes = potentialshapes;
            board = new Board(width, height);
            foreach (var tile in potentialshapes.SelectMany(s => s.data.tiles))
            {
                board.ClearBit(tile.x, tile.y);
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
        public long boardresettime;
        public long cansumtotargettime;
        public long blacklisttesttime;
        public long canplacetime;

        public List<MoveData> GenerateMoves()
        {
            var precalcmoves = visitedboards.FirstOrDefault(bm => bm.board.IsEqual(board));
            if (!(precalcmoves is null))
            {
                return precalcmoves.moves;
            }
            List<MoveData> potentialmoves = new List<MoveData>();
            var moves = FindEmptyArea(board, width, height);
            Random r = new Random();
            Board boardcopy = new Board(width, height);

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
                                                             board.GetData(t.x + placedposition.X, (t.y + placedposition.Y)) == true);
                        debugtimer.Stop();
                        canplacetime += debugtimer.ElapsedTicks;

                        if (canplace)
                        {
                            //place the piece
                            var copy = shape.PlaceData(placedposition);
                            debugtimer.Restart();
                            foreach (var tile in shape.data.tiles)
                            {
                                board.SetBit(tile.x + placedposition.X, (tile.y + placedposition.Y));
                            }
                            debugtimer.Stop();
                            boardresettime += debugtimer.ElapsedTicks;
                            debugtimer.Restart();

                            if (!blacklistedboards.Any(b => b.IsEqual(board)))
                            {
                                int touchingsquares = FindTouchingSquares(shape, placedposition);
                                if (touchingsquares >= 1)
                                {
                                    potentialmoves.Add(new MoveData(copy, touchingsquares, true));
                                }
                            }
                            debugtimer.Stop();
                            blacklisttesttime += debugtimer.ElapsedTicks;

                            debugtimer.Start();

                            foreach (var tile in shape.data.tiles)
                            {
                                board.ClearBit(tile.x + placedposition.X, (tile.y + placedposition.Y));
                            } //faster to operate on board rather than copying board
                            debugtimer.Stop();
                            boardresettime += debugtimer.ElapsedTicks;
                        }

                    }
                }
                if (potentialmoves.Count >= 1)
                {
                    memcpy(boardcopy.data, board.data, board.size);
                    if (visitedboards.Count >= 10)
                    {
                        visitedboards.RemoveAt(0); //Avoid memory issues
                    }
                    visitedboards.Add(new BoardMoves(boardcopy, potentialmoves));
                    return potentialmoves;
                }
            }
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
            memcpy(boardcopy.data, board.data, board.size);
            blacklistedboards.Add(boardcopy);

            Board boardsoftcopy = new Board(width, height);
            memcpy(boardsoftcopy.data, board.data, board.size);

            foreach (var tile in toremove.data.tiles)
            {
                boardsoftcopy.ClearBit(tile.x + toremove.data.location.X ,tile.y + toremove.data.location.Y);
            }
            visitedboards.RemoveAll(v => v.board.IsEqual(boardsoftcopy));
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
                else if (board.GetData(x,y) == true)
                {
                    result += 1;
                }
            }
            return result;
        }

        static List<Point> FindEmptyArea(Board board, int width, int height)
        {
            List<List<Point>> emptyAreas = new List<List<Point>>();

            bool[] visited = new bool[width * height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (board.GetData(x,y) == false && !visited[x + y * width])
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
        static int AreaCount(Board board, int width, int height)
        {
            int areacount=0;
            bool[] visited = new bool[width * height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (board.GetData(x, y) == false && !visited[x + y * width])
                    {
                        List<Point> emptyArea = new List<Point>();
                        DFS(board, x, y, width, height, visited, emptyArea);
                        ++areacount;
                    }
                }
            }

            return areacount;
        }
        static void DFS(Board board, int startX, int startY, int width, int height, bool[] visited, List<Point> emptyArea)
        {
            Stack<Point> stack = new Stack<Point>();
            stack.Push(new Point(startX, startY));

            while (stack.Count > 0)
            {
                Point current = stack.Pop();
                int x = current.X;
                int y = current.Y;

                if (x < 0 || x >= width || y < 0 || y >= height || visited[x + y * width] || board.GetData(x, y) == true)
                {
                    continue;
                }

                visited[x + y * width] = true;
                emptyArea.Add(new Point(x, y));

                // Push neighboring cells onto the stack
                stack.Push(new Point(x + 1, y));
                stack.Push(new Point(x - 1, y));
                stack.Push(new Point(x, y + 1));
                stack.Push(new Point(x, y - 1));
            }
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
        public Board board;
        public List<MoveData> moves;

        public BoardMoves(Board board, List<MoveData> moves)
        {
            this.board = board;
            this.moves = moves;
        }
    }
}
