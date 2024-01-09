using System.Security.Policy;

namespace Tesselation
{
    public partial class MainForm : Form
    {
        public static MainForm instance;

        const int topoffset = 20;
        const int rightoffset = 400;
        const int bottomoffset = 10;
        const int heightoffset = topoffset + bottomoffset;

        public MainForm()
        {
            instance = this;
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;

            InitializeComponent();

            canvas.Refresh();

            for (int i = 0; i < 4; ++i)
            {
                const int shapesize = 4;

                Shape shape = new Shape(7, shapesize, shapesize);
                int rectx = Screen.PrimaryScreen.WorkingArea.Width -rightoffset + 20;
                int recty = 50 + i * 225;

                tilePlacers.Add(new TilePlacer(shape, new Rectangle(rectx, recty, 200, 200)));
            }

        }

        public int horizontalsquares = 25;
        public int verticalsquares = 18;

        public List<TilePlacer> tilePlacers = new List<TilePlacer>();
        public Shape placingshape;

        public void CanvasPaint(object sender, PaintEventArgs e)
        {
            //Draw side panel, size of at least 200 pixels
            e.Graphics.FillRectangle(new Pen(Color.LightGray).Brush, Width- rightoffset, 0, rightoffset, Height);

            //Generate some shapes to put in the menu
            foreach (var tileplacer in tilePlacers)
            {
                tileplacer.Draw(e.Graphics);
            }

            int squaresize = Math.Min((Width- rightoffset) / horizontalsquares, (Height- heightoffset) /verticalsquares);

            for (int x = 0; x < horizontalsquares; ++x)
            {
                for (int y = 0; y < verticalsquares; ++y)
                {
                    e.Graphics.DrawRectangle(new Pen(Color.Black) ,x * squaresize + topoffset, y * squaresize + topoffset, squaresize, squaresize);
                }
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

            background = new Panel() { BackColor = Color.DarkBlue, Location = bounds.Location, Width = bounds.Width, Height = bounds.Height };
            MainForm.instance.Controls.Add(background);
            background.Click += OnClick;
        }

        public void Draw(Graphics graphics)
        {
            //graphics.DrawRectangle(new Pen(Color.White), bounds.X, bounds.Y, 200, 200);

            int shapescreensize = (200 / shape.width);

            foreach (var tile in shape.tiles)
            {
                graphics.FillRectangle(new Pen(Color.Green).Brush, new Rectangle(tile.x * shapescreensize + bounds.X, tile.y * shapescreensize + bounds.Y, shapescreensize, shapescreensize));
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
