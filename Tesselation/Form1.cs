using System.Security.Policy;

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

            for (int y = 0; y < 6; ++y)
            {
                for (int x = 0; x < 2; ++x)
                {
                    int shapesize = 3;

                    Shape shape;
                    do
                    {
                        shape = new Shape(x + 4, shapesize, shapesize);
                    } while (tilePlacers.Select(t => t.shape).Any(s => s == shape));

                    int rectx = 20 + x * 160;
                    int recty = 20 + y * 160;

                    tilePlacers.Add(new TilePlacer(shape, new Rectangle(rectx, recty, 150, 150)));
                }
            }

        }

        public int horizontalsquares = 25;
        public int verticalsquares = 18;

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
                        t2.y + shape.placedposition.Y == t.y + placingtile.Y)))
                    {
                        previewcolor = Color.Red;
                        break;
                    }
                }
                cantplace = previewcolor == Color.Red;
                foreach (var tile in placingshape.tiles)
                {
                    if (tile.x + placingtile.X >= horizontalsquares || tile.y + placingtile.Y >= verticalsquares)
                    {
                        continue;
                    }
                    e.Graphics.FillRectangle(new Pen(previewcolor).Brush, (placingtile.X + tile.x) * squaresize + leftoffset, (placingtile.Y + tile.y) * squaresize + topoffset, squaresize, squaresize);
                }
            }
            foreach (var shape in placedshapes)
            {
                foreach (var tile in shape.tiles) 
                {
                    bool tileright = false;
                    bool tileleft = false;
                    bool tileup = false;
                    bool tiledown = false;

                    if (shape.tiles.Any(t=>t.x  == tile.x && t.y == tile.y-1))
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

                    int leftadd       = tileleft  ? 0 : 1;
                    int rightsubtract = tileright ? 0 : 1;
                    int topadd = tileup ? 0 : 1;
                    int downsubtract = tiledown ? 0 : 1;

                    e.Graphics.FillRectangle(new Pen(Color.DarkGreen).Brush, (shape.placedposition.X + tile.x) * squaresize + leftoffset + leftadd, (shape.placedposition.Y + tile.y) * squaresize + topoffset + topadd, squaresize-rightsubtract, squaresize-downsubtract);
                }
            }
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
        private void canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (!(placingshape is null))
            {
                int tilex = (canvas.PointToClient(Cursor.Position).X - 20) / squaresize;
                int tiley = (canvas.PointToClient(Cursor.Position).Y - 20) / squaresize;
                placingtile = new Point(tilex, tiley);
                canvas.Invalidate();
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
        }
    }
    public class TilePlacer
    {
        public Shape shape;
        public Rectangle bounds;
        public Panel background;

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

            foreach (var tile in shape.tiles)
            {
                e.Graphics.FillRectangle(new Pen(Color.Green).Brush, new RectangleF(tile.x * shapescreensize, tile.y * shapescreensize, shapescreensize, shapescreensize));
            }
        }
        public void OnClick(object sender, EventArgs e)
        {
            if (background.BackColor == Color.White)
            {
                foreach (var tile in MainForm.instance.tilePlacers)
                {
                    tile.background.BackColor = Color.White;
                }

                background.BackColor = Color.LightGreen;
                MainForm.instance.placingshape = shape;
            }
            else
            {
                background.BackColor = Color.White;
                MainForm.instance.placingshape = null;
            }
        }
    }
}
