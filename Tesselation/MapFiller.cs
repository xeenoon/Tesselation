﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
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
        private List<Shape> placedshapes = new List<Shape>();
        public void PlaceShape(Shape shape) 
        { 
            placedshapes.Add(shape);
            foreach (var tile in shape.data.tiles)
            {
                emptysidetiles.Remove(new Point(tile.x + shape.data.location.X, tile.y + shape.data.location.Y));
            }
            foreach (var tile in shape.data.touchingsquares)
            {
                int x = tile.X + shape.data.location.X;
                int y = tile.Y + shape.data.location.Y;
                if (x >= -1 && x<width && y>= -1 && y < height)
                {
                    if (!board.GetData(x,y))
                    {
                        emptysidetiles.Add(new Point(x, y));
                    }
                }
            }
            emptysidetiles = emptysidetiles.Distinct().ToList();
        }
        public void RemoveShape(Shape shape)
        {
            placedshapes.RemoveAll(s=>s.data.location == shape.data.location); //Cant have two shapes at same location
            foreach (var tile in shape.data.tiles)
            {
                int x = tile.x + shape.data.location.X;
                int y = tile.y + shape.data.location.Y;
                if (x==0||x==width-1||y==0||y==width-1||
                    
                    board.GetData(x + 1, y) ||
                    board.GetData(x - 1, y) ||
                    board.GetData(x, y - 1) ||
                    board.GetData(x, y + 1))
                {
                    emptysidetiles.Add(new Point(x, y));
                }
            }
            emptysidetiles = emptysidetiles.Distinct().ToList();
            //Dont remove potential placements because it is difficult to determine if we would remove good moves
        }
        public List<Point> emptysidetiles = new List<Point>();
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

            for (int i = 0; i < board.width; ++i)
            {
                emptysidetiles.Add(new Point(i, 0));
                emptysidetiles.Add(new Point(i, height - 1));
            }
            for (int i = 0; i < board.height; ++i)
            {
                emptysidetiles.Add(new Point(0, i));
                emptysidetiles.Add(new Point(width - 1, i));
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
        //Stopwatch debugtimer = new Stopwatch();
        public long boardresettime;
        public long blacklisttesttime;
        public long canplacetime;
        public long preptime;
        public long wraptime;

        public List<MoveData> GenerateMoves()
        {
            //debugtimer.Restart();

            var precalcmoves = visitedboards.FirstOrDefault(bm => bm.board.IsEqual(board));
            if (!(precalcmoves is null))
            {
                return precalcmoves.moves;
            }

            List<MoveData> potentialmoves = new List<MoveData>();
            int[] movedata = FindEmptyAreas(board, width, height);
            var totalmoves = movedata[0];
            var areacount  = movedata[1];
            bool finishedsides = ScanSides();
            Random r = new Random();
            Board boardcopy = new Board(width, height);

            bool cansum = CanSumToTarget(potentialshapes.Select(s => s.data.tiles.Count).Distinct().ToArray(), totalmoves);
            int mosttouching = 2;
            if (emptysidetiles.Count() >= 1 && ((totalmoves > 50) || cansum) && areacount <= 1)
            {
                //check if a possible combination could theoretically exist
                List<Shape> shaperotations = potentialshapes.Shuffle().SelectMany(s => s.rotations).ToList();
                //debugtimer.Stop();
                //preptime += debugtimer.ElapsedTicks;

                foreach (var shape in shaperotations)
                {
                    foreach (var emptyspace in emptysidetiles)
                    {
                        foreach (var potentialanchor in shape.data.tiles)
                        {
                            //debugtimer.Restart();
                            var placedposition = new Point(emptyspace.X - potentialanchor.x, emptyspace.Y - potentialanchor.y);
                            bool canplace = true;
                            for (int i = 0; i < shape.data.tiles.Count; ++i)
                            {
                                var tile = shape.data.tiles[i];
                                int newx = tile.x + placedposition.X;
                                int newy = tile.y + placedposition.Y;
                                if (newx >= width || newy >= height || newx <= -1 || newy <= -1 || board.GetData(newx, newy))
                                {
                                    canplace = false;
                                }
                            }

                            //debugtimer.Stop();
                            //canplacetime += debugtimer.ElapsedTicks;

                            if (canplace)
                            {
                                //debugtimer.Restart();
                                //place the piece
                                var copy = shape.PlaceData(placedposition);
                                //var tempcopy = new Board(board);
                                foreach (var tile in copy.tiles)
                                {
                                    board.SetBit(tile.x + placedposition.X, (tile.y + placedposition.Y));
                                }
                                //debugtimer.Stop();
                                //boardresettime += debugtimer.ElapsedTicks;
                                //debugtimer.Restart();
                                int touchingsquares = FindTouchingSquares(shape, placedposition, board, finishedsides);

                                if (touchingsquares >= mosttouching && !blacklistedboards.Any(b => b.IsEqual(board)))
                                {
                                    if (NoNearbyAreas(copy, board) && (totalmoves >= 50 || AreaCount(board, width, height) <= 1))
                                    //Dont split up areas when solving at end
                                    {
                                        if (touchingsquares > mosttouching)
                                        {
                                            potentialmoves.Clear(); //Remove moves we aren't going to choose anyway
                                        }
                                        mosttouching = touchingsquares;
                                        if (touchingsquares >= 6 && totalmoves >= 50) //Dont remove potentially better moves at the end of search
                                        {
                                            return new List<MoveData>() {new MoveData(copy, touchingsquares, true, 0)};
                                        }
                                        potentialmoves.Add(new MoveData(copy, touchingsquares, true, 0));
                                    }
                                }

                                //debugtimer.Stop();
                                //blacklisttesttime += debugtimer.ElapsedTicks;

                                //debugtimer.Restart();
                                foreach (var tile in copy.tiles)
                                {
                                    board.ClearBit(tile.x + placedposition.X, (tile.y + placedposition.Y));
                                }

                                //debugtimer.Stop();
                                //boardresettime += debugtimer.ElapsedTicks;
                            }
                        }
                    }
                }
                //debugtimer.Restart();
                if (potentialmoves.Count >= 1)
                {
                    memcpy(boardcopy.data, board.data, board.size);
                    if (visitedboards.Count >= 10)
                    {
                        visitedboards.RemoveAt(0); //Avoid memory issues
                    }
                    visitedboards.Add(new BoardMoves(boardcopy, potentialmoves));
                    //debugtimer.Stop();
                    //wraptime += debugtimer.ElapsedTicks;
                    return potentialmoves;
                }
            }
            //Found no legal moves? Backtrace if there were many moves available, as it is likely that the problem was caused by the last piece placed
            Shape toremove;
            //Use normal backtracing to remove two areas if possible
            if (areacount == 1) 
            {
                if (totalmoves > 20)
                {
                    adjacentshapes.Clear();
                }
                else if (adjacentshapes.Count == 0)
                {
                    var smallestarea = FindEmptyArea(board, width, height);
                    //instead of backtracing, try to remove side pieces
                    adjacentshapes = placedshapes.Where(shape => 
                    shape.data.touchingsquares.Any(touchingtile =>
                    smallestarea.Contains(new Point(shape.data.location.X + touchingtile.X, shape.data.location.Y + touchingtile.Y)))).ToList();

                    foreach (var shape in adjacentshapes.OrderBy(a => a.data.location.X))
                    {
                        placedshapes.Remove(shape);
                        placedshapes.Add(shape); //Push to end of list;
                    }
                }
            }
            toremove = placedshapes.Last();

            potentialmoves.Clear();
            potentialmoves.Add(new MoveData(toremove.data, 0, false, 0));
            memcpy(boardcopy.data, board.data, board.size);
            blacklistedboards.Add(boardcopy);

            Board boardsoftcopy = new Board(width, height);
            memcpy(boardsoftcopy.data, board.data, board.size);

            foreach (var tile in toremove.data.tiles)
            {
                boardsoftcopy.ClearBit(tile.x + toremove.data.location.X ,tile.y + toremove.data.location.Y);
            }
            visitedboards.RemoveAll(v => v.board.IsEqual(boardsoftcopy));
            //debugtimer.Stop();
            //wraptime += debugtimer.ElapsedTicks;

            return potentialmoves;
        }

        private bool ScanSides()
        {
            for (int x = 0; x < width; ++x)
            {
                if (!board.GetData(x, 0) || !board.GetData(x, height-1))
                {
                    return false;
                }
            }
            for (int y = 0; y < height; ++y)
            {
                if (!board.GetData(0, y) || !board.GetData(width-1, y))
                {
                    return false;
                }
            }
            return true;
        }

        private bool NoNearbyAreas(ShapeData copy, Board pieceplaced)
        {
            bool[] visitedsquares = new bool[width*height];
            foreach (var tile in copy.tiles)
            {
                int rightcount = 0;
                int leftcount = 0;
                int upcount = 0;
                int downcount = 0;
                int x = tile.x + copy.location.X;
                int y = tile.y + copy.location.Y;

                DFS(pieceplaced, x + 1, y, width, height, visitedsquares, ref rightcount, 3);
                DFS(pieceplaced, x - 1, y, width, height, visitedsquares, ref leftcount, 3);
                DFS(pieceplaced, x, y + 1, width, height, visitedsquares, ref upcount, 3);
                DFS(pieceplaced, x, y - 1, width, height, visitedsquares, ref downcount, 3);

                visitedsquares[x + y * width] = false;
                if ((rightcount <= 3 && rightcount != 0) ||
                    (leftcount <= 3 && leftcount != 0) ||
                    (upcount <= 3 && upcount != 0) ||
                    (downcount <= 3 && downcount != 0))
                {
                    return false;
                }
            }
            return true;
        }

        private int FindTouchingSquares(Shape copy, Point position, Board b, bool finishedsides)
        {
            int result = 0;
            foreach (var point in copy.data.touchingsquares)
            {
                int persquareresard = 1; //Reward AI for having one tile connect to more other tiles
                int x = point.X + position.X;
                int y = point.Y + position.Y;
                if (x >= width || y >= height || x < 0 || y < 0)
                {
                    result += persquareresard*2;
                    persquareresard += 2;
                }
                else if (board.GetData(x, y) && finishedsides)
                {
                    result += persquareresard;
                    persquareresard += 2;
                }
            }
            return result;
        }

        static int[] FindEmptyAreas(Board board, int width, int height)
        {
            List<int> emptyAreas = new List<int>();

            bool[] visited = new bool[width * height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (board.GetData(x,y) == false && !visited[x + y * width])
                    {
                        int areasize = 0;
                        DFS(board, x, y, width, height, visited, ref areasize);
                        emptyAreas.Add(areasize);
                    }
                }
            }
            return new int[2] { emptyAreas.OrderBy(e => e).FirstOrDefault(), emptyAreas.Count};
        }
        static List<Point> FindEmptyArea(Board board, int width, int height)
        {
            List<List<Point>> emptyAreas = new List<List<Point>>();

            bool[] visited = new bool[width * height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (board.GetData(x, y) == false && !visited[x + y * width])
                    {
                        var list = new List<Point>();
                        emptyAreas.Add(list);
                        DFS(board, x, y, width, height, visited, list);
                    }
                }
            }
            return emptyAreas.OrderBy(a => a.Count()).FirstOrDefault();
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
                        int temp = 0;
                        DFS(board, x, y, width, height, visited, ref temp);
                        ++areacount;
                    }
                }
            }

            return areacount;
        }
        static void DFS(Board board, int startX, int startY, int width, int height, bool[] visited, List<Point> emptyArea, int distancelimit = int.MaxValue)
        {
            if (visited[startX + startY*width])
            {
                return;
            }
            Stack<Point> stack = new Stack<Point>();
            stack.Push(new Point(startX, startY));

            while (stack.Count > 0)
            {
                Point current = stack.Pop();
                int x = current.X;
                int y = current.Y;

                if (x < 0 || x >= width || y < 0 || y >= height || visited[x + y * width] || board.GetData(x, y) == true ||
                    Math.Abs(startX-x) >= distancelimit || Math.Abs(startY-y) >= distancelimit)
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
        static void DFS(Board board, int startX, int startY, int width, int height, bool[] visited, ref int result, int distancelimit = int.MaxValue)
        {
            if (startX >= width || startX <= -1 || startY >= height || startY <= -1 || visited[startX + startY * width])
            {
                return;
            }
            Stack<Point> stack = new Stack<Point>();
            stack.Push(new Point(startX, startY));

            while (stack.Count > 0)
            {
                Point current = stack.Pop();
                int x = current.X;
                int y = current.Y;

                if (x < 0 || x >= width || y < 0 || y >= height || visited[x + y * width] || (board.GetData(x, y)) ||
                    Math.Abs(startX - x) >= distancelimit || Math.Abs(startY - y) >= distancelimit)
                {
                    continue;
                }
                visited[x + y * width] = true;
                ++result;

                // Push neighboring cells onto the stack
                stack.Push(new Point(x + 1, y));
                stack.Push(new Point(x - 1, y));
                stack.Push(new Point(x, y + 1));
                stack.Push(new Point(x, y - 1));
            }
        }
        public double ThinnessMetric(Point[] tiles)
        {
            List<int> widths = new List<int>();
            List<int> heights = new List<int>();
            foreach (var tile in tiles)
            {
                if (tile.X == -1 || tile.Y ==  -1) //-1 is null flag
                {
                    continue;
                }
                int width = 0;
                int x = tile.X;
                while (tiles.Contains(new Point(x,tile.Y)))
                {
                    x++;
                    width++;
                }
                widths.Add(width);

                int height = 0;
                int y = tile.Y;
                while (tiles.Contains(new Point(tile.X, y)))
                {
                    y++;
                    height++;
                }
                heights.Add(height);
            }
            return Math.Min(widths.Average(), heights.Average());
        }
    }
    public class MoveData
    {
        public ShapeData shape;
        public int touchingborders;
        public bool isplacing;
        public double areathickness;

        public MoveData(ShapeData shape, int touchingborders, bool isplacing, double areathickness)
        {
            this.shape = shape;
            this.touchingborders = touchingborders;
            this.isplacing = isplacing;
            this.areathickness = areathickness;
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
