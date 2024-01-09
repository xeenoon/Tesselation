using System.Security.Policy;

namespace Tesselation
{
    public partial class MainForm : Form
    {
        public static MainForm instance;
        public static SplitContainer menusplit;

        const int topoffset = 20;
        const int bottomoffset = 10;
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
                    } while (tilePlacers.Select(t=>t.shape).Any(s=>s == shape));

                    int rectx = 20 + x * 160;
                    int recty = 20 + y * 160;

                    tilePlacers.Add(new TilePlacer(shape, new Rectangle(rectx, recty, 150, 150)));
                }
            }

        }

        public int horizontalsquares = 25;
        public int verticalsquares = 18;

        public List<TilePlacer> tilePlacers = new List<TilePlacer>();
        public Shape placingshape;

        public void CanvasPaint(object sender, PaintEventArgs e)
        {
            int squaresize = Math.Min((menusplit.Panel1.Width-(leftoffset+rightoffset)) / horizontalsquares, (Height - heightoffset) / verticalsquares);

            for (int x = 0; x < horizontalsquares; ++x)
            {
                for (int y = 0; y < verticalsquares; ++y)
                {
                    e.Graphics.DrawRectangle(new Pen(Color.Black), x * squaresize + leftoffset, y * squaresize + topoffset, squaresize, squaresize);
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
