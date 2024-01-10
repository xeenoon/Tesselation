using System;
using System.Collections.Generic;
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

     /*   public List<Board> GenerateMoves(List<Shape> potentialplacements)
        {
            List<Board> boards = new List<Board>();


            foreach (var shape in placedshapes)
            {
                if (shape.tiles.Any(t2 => placingshape.tiles.Any(
                    t => t2.x + shape.placedposition.X == t.x + placingtile.X &&
                    t2.y + shape.placedposition.Y == t.y + placingtile.Y)))
                {
                    previewcolor = Color.Red;
                    break;
                }
            }
        }
     */
    }
}
