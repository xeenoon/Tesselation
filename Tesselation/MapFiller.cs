using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tesselation
{
    internal class MapFiller
    {
        public List<Shape> potentialplacements = new List<Shape>();
    }
    public class Board
    {
        public int width;
        public int height;
        public List<Shape> shapes = new List<Shape>();
        public int[,] board;

        public Board(int width, int height, List<Shape> shapes)
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

        public List<Board> GenerateMoves(List<Shape> potentialplacements)
        {
            List<Board> boards = new List<Board>();



            return boards;
        }
        public List<Tile> SmallestEmptyArea(Board board)
        {
            return null;
            int[,] boardcopy = new int[width, height];
            Array.Copy(board.board, boardcopy, width * height);

            for (int x = 0; x < width; ++x)
            {
                for (int y = 0; y < height; ++y)
                {
                    int isempty = boardcopy[x, y];
                    if (isempty == 0)
                    {
                        //Search from this position in the board
                    }
                }
            }
        }
        public void SearchIsland(int x, int y)
        {
            
        }
    }
}
