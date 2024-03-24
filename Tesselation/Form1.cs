using System.Collections.Concurrent;
using System.Diagnostics;
using System.DirectoryServices.ActiveDirectory;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Windows.Forms;
using static System.Net.Mime.MediaTypeNames;

namespace Tesselation
{
    public unsafe partial class MainForm : Form
    {
        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        static extern int memcpy(byte* b1, byte* b2, long count);
        public static extern unsafe IntPtr memset(void* dest, int c, int count);
        public static extern int memcmp(byte[] b1, byte[] b2, long count);
        static extern IntPtr memcpy(IntPtr dest, IntPtr src, int count);


        public static int horizontalsquares = 10;
        public static int verticalsquares = 10;

        public static MainForm instance;
        public static SplitContainer menusplit;

        const int topoffset = 20;
        const int bottomoffset = 20;
        const int leftoffset = 20;
        const int rightoffset = 20;
        const int heightoffset = topoffset + bottomoffset;

        public MainForm()
        {
            File.WriteAllText(dumpfile, "");
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
                        shape = new Shape(x + 4, shapesize, shapesize);
                    } while (tilePlacers.Select(t => t.shape).Any(s => shape.rotations.Any(rs => rs == s)));

                    int rectx = 20 + x * (Height / 6 + 10);
                    int recty = 20 + y * (Height / 6 + 10);

                    tilePlacers.Add(new TilePlacer(shape, new Rectangle(rectx, recty, Height / 6, Height / 6)));
                }
            }
            InitializeBoard();
            AIAsyncMove.Elapsed += AIMove;
            mapFiller = new MapFiller(horizontalsquares, verticalsquares, tilePlacers.Select(t => t.shape).ToList());
            bitmapupdatetimer.Elapsed += BitmapUpdate;
            bitmapupdatetimer.Start();
        }

        private void InitializeBoard()
        {
            squaresize = Math.Min((menusplit.Panel1.Width - (leftoffset + rightoffset)) / (float)horizontalsquares, (Height - heightoffset) / (float)verticalsquares);
            canvasdata = new Bitmap(canvas.Width, canvas.Height);
            bitmapgraphics = Graphics.FromImage(canvasdata);
            for (int x = 0; x < horizontalsquares; ++x)
            {
                for (int y = 0; y < verticalsquares; ++y)
                {
                    bitmapgraphics.DrawRectangle(new Pen(Color.Black, 1), x * squaresize + leftoffset, y * squaresize + topoffset, squaresize, squaresize);
                }
            }
        }

        System.Timers.Timer AIAsyncMove = new System.Timers.Timer(20);
        MapFiller mapFiller;
        bool paintfinished = false;
        long totalmiliseconds = 0;
        long movegentime = 0;
        Random r = new Random();
        public void AIMove(object sender, EventArgs e)
        {
            int iterations = 0;
            Stopwatch totaltimer = new Stopwatch();
            totaltimer.Start();
            Stopwatch movegentimer = new Stopwatch();
            Stopwatch s = new Stopwatch();
            s.Start();
            int movesperrender = 0;
            while (true)
            {
                ++iterations;
                movegentimer.Restart();
                var moves = mapFiller.GenerateMoves();
                movegentimer.Stop();
                movegentime = movegentimer.ElapsedTicks;

                if (!(moves is null))
                {
                    if (moves.Count == 1 && !moves[0].isplacing)
                    {
                        Shape lookfor = placedshapes.FirstOrDefault(s => s.data.location.X == moves[0].shape.location.X &&
                                                             s.data.location.Y == moves[0].shape.location.Y);
                        queue.Enqueue(new PlacingData(new Shape(moves[0].shape), false));
                        placedshapes.TryTake(out lookfor);

                        foreach (var tile in moves[0].shape.tiles)
                        {
                            mapFiller.board.ClearBit(tile.x + moves[0].shape.location.X, (tile.y + moves[0].shape.location.Y));
                        }

                        mapFiller.RemoveShape(new Shape(moves[0].shape));
                    }
                    else
                    {
                        var bestmove = moves[r.Next(0,moves.Count)];
                        placedshapes.Add(new Shape(bestmove.shape));
                        queue.Enqueue(new PlacingData(new Shape(bestmove.shape), true));

                        foreach (var tile in bestmove.shape.tiles)
                        {
                            mapFiller.board.SetBit(tile.x + bestmove.shape.location.X, (tile.y + bestmove.shape.location.Y));
                        }

                        mapFiller.PlaceShape(new Shape(bestmove.shape));
                    }
                }

                const int rendermiliseconds = 50;
                movesperrender++;
                if (s.ElapsedMilliseconds > rendermiliseconds)
                {
                    totalmiliseconds += s.ElapsedMilliseconds;
                    s.Stop();
                    UpdateAILabel((int)s.ElapsedMilliseconds, movesperrender);
                    s.Restart();
                    canvas.Invalidate();
                    movesperrender = 0;
                }
                Board full = new Board(mapFiller.board.width, mapFiller.board.height);
                for (int x = 0; x < full.width; ++x)
                {
                    for (int y = 0; y < full.height; ++y)
                    {
                        full.SetBit(x, y);
                    }
                }
                if (mapFiller.board.IsEqual(full)) //Fast way to check if board is full
                {
                    totaltimer.Stop();
                    totaltime += totaltimer.ElapsedMilliseconds;
                    while (s.ElapsedMilliseconds < rendermiliseconds) { } //Wait to render
                    canvas.Invalidate();
                    return; //issolved
                }
            }
        }
        long totaltime = 0;
        const string dumpfile = @"C:\Users\ccw10\Downloads\debugdump.txt";

        private void DebugDump(MapFiller mapfiller)
        {
            var towrite = "{" + mapfiller.board.ToString() + ":[";
            foreach (var board in mapfiller.blacklistedboards)
            {
                towrite += board.ToString() + ",";
            }
            towrite += "]}\n";
            File.AppendAllText(dumpfile, towrite);
        }
        public string ArryStr(int[] board)
        {
            string result = "";
            for (int i = 0; i < board.Count(); i += 4)
            {
                string binary = "";
                for (int j = 0; j < 4; ++j)
                {
                    binary += board[i + j].ToString();
                }

                string hex = String.Format("{0:X2}", Convert.ToUInt64(binary, 2));

                result += hex;
            }
            return result;
        }
        private void UpdateAILabel(int milis, int moves)
        {
            if (label1.InvokeRequired)
            {
                label1.BeginInvoke(new Action(() => UpdateAILabel(milis, moves)));
            }
            else
            {
                double boardresettimems = mapFiller.boardresettime / (double)10000;
                double blacklisttestms = mapFiller.blacklisttesttime / (double)10000;
                double canplacems = mapFiller.canplacetime / (double)10000;
                double movegenms = movegentime / (double)10000;
                double preptimems = mapFiller.preptime / (double)10000;
                double wraptimems = mapFiller.wraptime / (double)10000;

                label1.Text = $"Totaltime:{milis}\nboardresettime:{boardresettimems}\nblacklisttesttime:{blacklisttestms}\ncanplacetime:{canplacems}\npreptime:{preptimems}\nwraptime:{wraptimems}\nlosttime:{movegenms - (canplacems+blacklisttestms+boardresettimems + preptimems + wraptimems)}";
                label1.Refresh();
                /*mapFiller.boardresettime = 0;
                mapFiller.blacklisttesttime = 0;
                mapFiller.canplacetime = 0;
                mapFiller.preptime = 0;
                mapFiller.wraptime = 0;*/
            }
        }

        public List<TilePlacer> tilePlacers = new List<TilePlacer>();
        public ConcurrentBag<Shape> placedshapes = new ConcurrentBag<Shape>();
        public Shape placingshape;
        public float squaresize = 45;
        bool cantplace = true;
        public Bitmap canvasdata;
        bool isupdatingbitmap;
        Queue<PlacingData> queue = new Queue<PlacingData>();
        public struct PlacingData
        {
            public Shape shape;
            public bool placing;

            public PlacingData(Shape shape, bool placing)
            {
                this.shape = shape;
                this.placing = placing;
            }
        }
        Graphics bitmapgraphics;
        System.Timers.Timer bitmapupdatetimer = new System.Timers.Timer(100);
        public void BitmapUpdate(object sender, EventArgs e)
        {
            while (queue.Count >= 1)
            {
                var data = queue.Dequeue();
                var shape = data.shape;
                var placing = data.placing;

                List<PointF> points = new List<PointF>();
                foreach (var tile in shape.data.tiles)
                {
                    bool tileright = false;
                    bool tileleft = false;
                    bool tileup = false;
                    bool tiledown = false;

                    if (shape.data.tiles.Any(t => t.x == tile.x && t.y == tile.y - 1))
                    {
                        //Tile above, dont shrink
                        tileup = true;
                    }
                    if (shape.data.tiles.Any(t => t.x == tile.x && t.y == tile.y + 1))
                    {
                        //Tile below, dont shrink
                        tiledown = true;
                    }
                    if (shape.data.tiles.Any(t => t.x == tile.x - 1 && t.y == tile.y))
                    {
                        //Tile left, dont shrink
                        tileleft = true;
                    }
                    if (shape.data.tiles.Any(t => t.x == tile.x + 1 && t.y == tile.y))
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

                    Color drawcolor = shape.data.color;
                    if (deletingshape == shape.data.location)
                    {
                        drawcolor = Color.Red;
                    }
                    if (placing)
                    {
                        bitmapgraphics.FillRectangle(new Pen(drawcolor).Brush, (shape.data.location.X + tile.x) * squaresize + leftoffset, (shape.data.location.Y + tile.y) * squaresize + topoffset, squaresize, squaresize);
                    }
                    else
                    {
                        bitmapgraphics.FillRectangle(new Pen(Color.White).Brush, (shape.data.location.X + tile.x) * squaresize + leftoffset, (shape.data.location.Y + tile.y) * squaresize + topoffset, squaresize, squaresize);
                        bitmapgraphics.DrawRectangle(new Pen(Color.Black), (shape.data.location.X + tile.x) * squaresize + leftoffset, (shape.data.location.Y + tile.y) * squaresize + topoffset, squaresize, squaresize);
                    }
                }
                points = points.Distinct().ToList();
                points = OrderPoints(points, shape.data.tiles.ToList());
                for (int i = 0; i < points.Count(); ++i)
                {
                    float newx = (points[i].X + shape.data.location.X) * squaresize + leftoffset;
                    float newy = (points[i].Y + shape.data.location.Y) * squaresize + topoffset;
                    points[i] = new PointF(newx, newy);
                    //e.Graphics.FillEllipse(new Pen(Color.Purple).Brush, new Rectangle(newx-4, newy-4, 8, 8));
                }
                bitmapgraphics.DrawPolygon(new Pen(Color.Black, 1), points.ToArray());
            }
            canvas.Invalidate();
            bitmapupdatetimer.Start();
        }
        bool drawingbitmap;
        public void CanvasPaint(object sender, PaintEventArgs e)
        {
            Stopwatch renderstopwatch = new Stopwatch();
            renderstopwatch.Restart();
            while (drawingbitmap || isupdatingbitmap || queue.Count>=1) { } //Wait for threads to finish operations
            drawingbitmap = true;
            e.Graphics.DrawImage(canvasdata,0,0,canvas.Width, canvas.Height);
            drawingbitmap = false;
            if (totaltime >= 1)
            {
                var cache = totaltime/1000f;
                new Thread(() => MessageBox.Show(string.Format("Completed board size of [{0},{1}] in {2} seconds", horizontalsquares, verticalsquares, cache))).Start();
                totaltime = 0;
            }
            renderstopwatch.Stop();
            renderms = (long)(renderstopwatch.ElapsedTicks/(double)10000);
        }
        long renderms;
        private List<PointF> OrderPoints(List<PointF> points, List<Tile> tiles)
        {
            List<PointF> result = new List<PointF>();
            PointF last = points.OrderByDescending(p => p.Y).OrderByDescending(p => p.X).FirstOrDefault();
            result.Add(last);
            points.Remove(last);

            while (true)
            {
                PointF up = new Point(-1, -1);
                PointF down = new Point(-1, -1);
                PointF left = new Point(-1, -1);
                PointF right = new Point(-1, -1);

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
                int tilex = (int)((canvas.PointToClient(Cursor.Position).X - leftoffset) / squaresize);
                int tiley = (int)((canvas.PointToClient(Cursor.Position).Y - topoffset) / squaresize);
                placingtile = new Point(tilex, tiley);
                canvas.Invalidate();
            }
            if (deleting)
            {
                int localx = (int)((canvas.PointToClient(Cursor.Position).X - leftoffset) / squaresize);
                int localy = (int)((canvas.PointToClient(Cursor.Position).Y - topoffset) / squaresize);

                Shape hover = placedshapes.FirstOrDefault(s => s.data.tiles.Any(t => t.x + s.data.location.X == localx && t.y + s.data.location.Y == localy));

                if (!(hover is null))
                {
                    deletingshape = hover.data.location;
                    canvas.Invalidate();
                }
            }
        }

        private void canvas_Click(object sender, EventArgs e)
        {
            if (!(placingshape is null) && placingtile.X != -1 && !cantplace)
            {
                foreach (var tile in placingshape.data.tiles)
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
                Shape? shape = placedshapes.FirstOrDefault(s => s.data.location.X == deletingshape.X && s.data.location.Y == deletingshape.Y);
                placedshapes.TryTake(out shape);
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
            mapFiller = new MapFiller(horizontalsquares, verticalsquares, tilePlacers.Select(t => t.shape).ToList());
            placedshapes.Clear();
            canvas.Invalidate();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            horizontalsquares = int.Parse(textBox1.Text);
            verticalsquares = int.Parse(textBox2.Text);
            InitializeBoard();
            pictureBox2_Click(sender, e);

            AIAsyncMove.AutoReset = false;
            AIAsyncMove.Start();
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
            float shapescreensize = ((float)bounds.Width / shape.data.width);
            background.BackColor = Color.Black;
            
            for (int x = 0; x<shape.data.width;++x)
            {
                for (int y = 0; y < shape.data.width; ++y)
                {
                    Color drawingcolor = selected ? Color.LightGreen : Color.White;
                    if (shape.data.tiles.Any(t=>t.x == x && t.y == y))
                    {
                        drawingcolor = shape.data.color;
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
