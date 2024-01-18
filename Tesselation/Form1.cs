using System.Diagnostics;
using System.Security.Policy;
using System.Windows.Forms;

namespace Tesselation
{
    public partial class MainForm : Form
    {
        public static MainForm instance;
        public static SplitContainer menusplit;

        const int topoffset = 20;
        const int bottomoffset = 20;
        const int leftoffset = 20;
        const int rightoffset = 20;
        const int heightoffset = topoffset + bottomoffset;

        public MainForm()
        {
            instance = this;
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;

            InitializeComponent();
            menusplit = splitContainer1;

            canvas.Refresh();
            Random r = new Random();
            for (int y = 0; y < 6; ++y)
            {
                for (int x = 0; x < 2; ++x)
                {
                    int shapesize = 3;

                    Shape shape;
                    do
                    {
                        shape = new Shape(x+4, shapesize, shapesize);
                    } while (tilePlacers.Select(t => t.shape).Any(s => shape.rotations.Any(rs => rs == s)));

                    int rectx = 20 + x * 160;
                    int recty = 20 + y * 160;

                    tilePlacers.Add(new TilePlacer(shape, new Rectangle(rectx, recty, 150, 150)));
                }
            }
            mapFiller = new MapFiller(horizontalsquares, verticalsquares, tilePlacers.Select(t=>t.shape).ToList());

            AIMoveDelay.AutoReset = false;
            AIMoveDelay.Elapsed += AIMove;
            AIMoveDelay.Start();
        }
        System.Timers.Timer AIMoveDelay = new System.Timers.Timer(1000);
        MapFiller mapFiller;
        bool paintfinished = false;
        public void AIMove(object sender, EventArgs e)
        {
            int iterations = 0;
            Stopwatch s = new Stopwatch();
            s.Start();
            while (true) 
            {
                ++iterations;
                var moves = mapFiller.GenerateMoves();
                if (!(moves is null))
                {
                    if (moves.Count == 1 && !moves[0].isplacing)
                    {
                        placedshapes.Remove(moves[0].shape);

                        foreach (var tile in moves[0].shape.tiles)
                        {
                            mapFiller.board[tile.x + moves[0].shape.placedposition.X + (tile.y + moves[0].shape.placedposition.Y)*mapFiller.width] = 0;
                        }
                        mapFiller.placedshapes.Remove(moves[0].shape);
                    }
                    else
                    {
                        var bestmove = moves.OrderByDescending(t => t.touchingborders).FirstOrDefault();
                        int squares = bestmove.touchingborders;
                        bestmove = moves.Where(m=>m.touchingborders >= squares).ToList().Shuffle().FirstOrDefault();
                        placedshapes.Add(bestmove.shape);


                        foreach (var tile in bestmove.shape.tiles)
                        {
                            mapFiller.board[tile.x + bestmove.shape.placedposition.X + (tile.y + bestmove.shape.placedposition.Y) * mapFiller.width] = 1;
                        }
                        mapFiller.placedshapes.Add(bestmove.shape);
                    }
                }
                paintfinished = false;
                if (iterations % 100 == 0)
                {
                    s.Stop();
                    UpdateAILabel((int)s.ElapsedMilliseconds, 100);
                    s.Restart();
                    canvas.Invalidate();

                    while (!paintfinished)
                    {
                        //wait for paint to finish
                    }
                }
                //Thread.Sleep(100);
                if (mapFiller.board.All(i=>i==1))
                {
                    canvas.Invalidate();
                    return; //issolved
                }
            }
        }
        private void UpdateAILabel(int milliseconds, int moves)
        {
            if (label1.InvokeRequired)
            {
                label1.BeginInvoke(new Action(() => UpdateAILabel(milliseconds, moves)));
            }
            else
            {
                label1.Text = (milliseconds / ((float)moves)).ToString() + " miliseconds per move";
            }
        }

        public static int horizontalsquares = 21;
        public static int verticalsquares = 15;

        public List<TilePlacer> tilePlacers = new List<TilePlacer>();
        public List<Shape> placedshapes = new List<Shape>();
        public Shape placingshape;
        public int squaresize = 45;
        bool cantplace = true;

        public void CanvasPaint(object sender, PaintEventArgs e)
        {
            squaresize = Math.Min((menusplit.Panel1.Width - (leftoffset + rightoffset)) / horizontalsquares, (Height - heightoffset) / verticalsquares);

            for (int x = 0; x < horizontalsquares; ++x)
            {
                for (int y = 0; y < verticalsquares; ++y)
                {
                    e.Graphics.DrawRectangle(new Pen(Color.Black, 2), x * squaresize + leftoffset, y * squaresize + topoffset, squaresize, squaresize);
                }
            }

            //Copy incase of multithread issue
            foreach (var shape in placedshapes)
            {
                List<Point> points = new List<Point>();
                foreach (var tile in shape.tiles)
                {
                    bool tileright = false;
                    bool tileleft = false;
                    bool tileup = false;
                    bool tiledown = false;

                    if (shape.tiles.Any(t => t.x == tile.x && t.y == tile.y - 1))
                    {
                        //Tile above, dont shrink
                        tileup = true;
                    }
                    if (shape.tiles.Any(t => t.x == tile.x && t.y == tile.y + 1))
                    {
                        //Tile below, dont shrink
                        tiledown = true;
                    }
                    if (shape.tiles.Any(t => t.x == tile.x - 1 && t.y == tile.y))
                    {
                        //Tile left, dont shrink
                        tileleft = true;
                    }
                    if (shape.tiles.Any(t => t.x == tile.x + 1 && t.y == tile.y))
                    {
                        //Tile right, dont shrink
                        tileright = true;
                    }

                    if (!tileleft)
                    {
                        points.Add(new Point(tile.x, tile.y));
                        points.Add(new Point(tile.x, tile.y + 1));
                    }
                    if (!tileright)
                    {
                        points.Add(new Point(tile.x + 1, tile.y));
                        points.Add(new Point(tile.x + 1, tile.y + 1));
                    }
                    if (!tileup)
                    {
                        points.Add(new Point(tile.x, tile.y));
                        points.Add(new Point(tile.x + 1, tile.y));
                    }
                    if (!tiledown)
                    {
                        points.Add(new Point(tile.x, tile.y + 1));
                        points.Add(new Point(tile.x + 1, tile.y + 1));
                    }

                    Color drawcolor = shape.color;
                    if (deletingshape == shape.placedposition)
                    {
                        drawcolor = Color.Red;
                    }

                    e.Graphics.FillRectangle(new Pen(drawcolor).Brush, (shape.placedposition.X + tile.x) * squaresize + leftoffset, (shape.placedposition.Y + tile.y) * squaresize + topoffset, squaresize, squaresize);
                }
                points = points.Distinct().ToList();
                points = OrderPoints(points, shape.tiles);
                for (int i = 0; i < points.Count(); ++i)
                {
                    int newx = (points[i].X + shape.placedposition.X) * squaresize + leftoffset;
                    int newy = (points[i].Y + shape.placedposition.Y) * squaresize + topoffset;
                    points[i] = new Point(newx, newy);
                    //e.Graphics.FillEllipse(new Pen(Color.Purple).Brush, new Rectangle(newx-4, newy-4, 8, 8));
                }
                e.Graphics.DrawPolygon(new Pen(Color.Black, 2), points.ToArray());
            }


            if (placingtile.X != -1 && !(placingshape is null))
            {
                Color previewcolor = Color.LightGreen;
                if (placingshape.tiles.Any(t => t.x + placingtile.X >= horizontalsquares || t.y + placingtile.Y >= verticalsquares))
                {
                    previewcolor = Color.Red;
                }
                foreach (var shape in placedshapes)
                {
                    if (shape.tiles.Any(t2 => placingshape.tiles.Any(
                        t => t2.x + shape.placedposition.X == t.x + placingtile.X &&
                        t2.y + shape.placedposition.Y == t.y + placingtile.Y //Check for overwriting

                        || (shape.color == placingshape.color && ((Math.Abs(t.x + placingtile.X - (t2.x + shape.placedposition.X)) == 1 && t2.y + shape.placedposition.Y == t.y + placingtile.Y)
                        || (Math.Abs(t.y + placingtile.Y - (t2.y + shape.placedposition.Y)) == 1 && t2.x + shape.placedposition.X == t.x + placingtile.X)))
                    )))
                    {
                        previewcolor = Color.Red;
                        break;
                    }
                }
                cantplace = previewcolor == Color.Red;
                previewcolor = Color.FromArgb(200, previewcolor.R, previewcolor.G, previewcolor.B);
                foreach (var tile in placingshape.tiles)
                {
                    if (tile.x + placingtile.X >= horizontalsquares || tile.y + placingtile.Y >= verticalsquares)
                    {
                        continue;
                    }
                    e.Graphics.FillRectangle(new Pen(previewcolor).Brush, (placingtile.X + tile.x) * squaresize + leftoffset, (placingtile.Y + tile.y) * squaresize + topoffset, squaresize, squaresize);
                }
            }

            paintfinished = true;
        }

        private List<Point> OrderPoints(List<Point> points, List<Tile> tiles)
        {
            List<Point> result = new List<Point>();
            Point last = points.OrderByDescending(p => p.Y).OrderByDescending(p => p.X).FirstOrDefault();
            result.Add(last);
            points.Remove(last);

            while (true)
            {
                Point up = new Point(-1, -1);
                Point down = new Point(-1, -1);
                Point left = new Point(-1, -1);
                Point right = new Point(-1, -1);

                foreach (var p in points)
                {
                    if (p.X == last.X && p.Y == last.Y - 1)
                    {
                        up = p;
                    }
                    if (p.X == last.X && p.Y == last.Y + 1)
                    {
                        down = p;
                    }
                    if (p.X == last.X + 1 && p.Y == last.Y)
                    {
                        right = p;
                    }
                    if (p.X == last.X - 1 && p.Y == last.Y)
                    {
                        left = p;
                    }
                }

                if (up.X != -1 && tiles.Count(t => (t.x == up.X || t.x == up.X - 1) && t.y == up.Y) == 1)
                //There must be A tile up left or up right
                {
                    last = up;
                }
                else if (left.X != -1 && tiles.Count(t => (t.y == left.Y || t.y == left.Y - 1) && t.x == left.X) == 1)
                //There must be A tile up left of down left
                {
                    last = left;
                }

                else if (down.X != -1 && tiles.Count(t => (t.x == down.X || t.x == down.X - 1) && t.y == down.Y - 1) == 1)
                //There must be A tile down left or down right
                {
                    last = down;
                }
                else if (right.X != -1 && tiles.Count(t => (t.y == right.Y || t.y == right.Y - 1) && t.x == right.X - 1) == 1)
                //There must be A tile up right of down right
                {
                    last = right;
                }
                else
                {
                    //Finished
                    break;
                }

                result.Add(last);
                points.Remove(last);
            }
            return result;
        }

        private void splitContainer1_Panel2_Paint(object sender, PaintEventArgs e)
        {

        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            canvas.Invalidate();
        }

        private void canvas_Resize(object sender, EventArgs e)
        {
            canvas.Invalidate(true);
        }
        Point placingtile = new Point(-1, -1);
        Point deletingshape;
        private void canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (!(placingshape is null))
            {
                int tilex = (canvas.PointToClient(Cursor.Position).X - leftoffset) / squaresize;
                int tiley = (canvas.PointToClient(Cursor.Position).Y - topoffset) / squaresize;
                placingtile = new Point(tilex, tiley);
                canvas.Invalidate();
            }
            if (deleting)
            {
                int localx = (canvas.PointToClient(Cursor.Position).X - leftoffset) / squaresize;
                int localy = (canvas.PointToClient(Cursor.Position).Y - topoffset) / squaresize;

                Shape hover = placedshapes.FirstOrDefault(s => s.tiles.Any(t => t.x + s.placedposition.X == localx && t.y + s.placedposition.Y == localy));

                if (!(hover is null))
                {
                    deletingshape = hover.placedposition;
                    canvas.Invalidate();
                }
            }
        }

        private void canvas_Click(object sender, EventArgs e)
        {
            if (!(placingshape is null) && placingtile.X != -1 && !cantplace)
            {
                foreach (var tile in placingshape.tiles)
                {
                    if (tile.x + placingtile.X >= horizontalsquares || tile.y + placingtile.Y >= verticalsquares)
                    {
                        return;
                    }
                }
                placedshapes.Add(placingshape.Place(placingtile));
                canvas.Invalidate();
            }
            if (deleting && deletingshape.X != -1)
            {
                placedshapes.Remove(placedshapes.FirstOrDefault(s => s.placedposition.X == deletingshape.X && s.placedposition.Y == deletingshape.Y));
                canvas.Invalidate();
            }
        }

        public bool deleting;
        public void DeleteClick(object sender, EventArgs e)
        {
            placingshape = null;
            foreach (var tileplacer in tilePlacers)
            {
                tileplacer.background.BackColor = Color.White;
            }
            deletingshape = new Point(-1, -1);
            deleting = !deleting;
            if (deleting)
            {
                pictureBox1.BackColor = Color.LightGray;
            }
            else
            {
                pictureBox1.BackColor = Color.White;
            }
        }

        private void MainForm_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 'r' && !(placingshape is null)) //rotate
            {
                placingshape = placingshape.Rotate(90);
                placingshape.LeftCornerAdjust();
                canvas.Refresh();
            }
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            placedshapes.Clear();
            canvas.Invalidate();
            AIMoveDelay.Start();
        }
    }
    public class TilePlacer
    {
        public Shape shape;
        public Rectangle bounds;
        public Panel background;
        bool selected;

        public TilePlacer(Shape shape, Rectangle bounds)
        {
            this.shape = shape;
            this.bounds = bounds;

            background = new Panel() { BackColor = Color.White, Location = bounds.Location, Width = bounds.Width, Height = bounds.Height };
            MainForm.menusplit.Panel2.Controls.Add(background);
            background.Click += OnClick;
            background.Paint += Draw;
            background.Invalidate();
        }

        public void Draw(object sender, PaintEventArgs e)
        {
            float shapescreensize = ((float)bounds.Width / shape.width);
            background.BackColor = Color.Black;
            
            for (int x = 0; x<shape.width;++x)
            {
                for (int y = 0; y < shape.width; ++y)
                {
                    Color drawingcolor = selected ? Color.LightGreen : Color.White;
                    if (shape.tiles.Any(t=>t.x == x && t.y == y))
                    {
                        drawingcolor = shape.color;
                    }
                    e.Graphics.FillRectangle(new Pen(drawingcolor).Brush, new RectangleF(x * shapescreensize+1, y * shapescreensize+1, shapescreensize-2, shapescreensize-2));

                }
            }
        }
        public void OnClick(object sender, EventArgs e)
        {            
            if (MainForm.instance.placingshape != shape) // reverse
            {
                if (MainForm.instance.deleting)
                {
                    MainForm.instance.DeleteClick(null, null);
                }

                foreach (var tile in MainForm.instance.tilePlacers)
                {
                    tile.selected = false;
                    tile.background.Invalidate();
                }

                selected = true;
                MainForm.instance.placingshape = shape;
            }
            else
            {
                selected = false;
                MainForm.instance.placingshape = null;
            }
            background.Invalidate();
        }
    }
}
