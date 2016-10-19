using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game_Asteroids
{
    static class Game
    {
        static BufferedGraphicsContext context;
        public static BufferedGraphics buffer;

        // list of objects
        static List<BaseObject> objectsList = new List<BaseObject>();
        static Bullet bullet;

        // game window resolution
        public static int Width { get; set; }
        public static int Height { get; set; }

        /// <summary>
        /// Initialize graphics components
        /// </summary>
        /// <param name="form"></param>
        public static void Init(Form form)
        {
            // graphical device for output
            Graphics graph;

            context = BufferedGraphicsManager.Current;

            // create draw area and link it to the form
            graph = form.CreateGraphics();

            Width = form.Width - 2 * SystemInformation.BorderSize.Width
                                - SystemInformation.HorizontalResizeBorderThickness;
            Height = form.Height - SystemInformation.CaptionHeight
                                - 2 * SystemInformation.BorderSize.Height
                                - SystemInformation.VerticalResizeBorderThickness;

            // link buffer with graphical device to draw at buffer
            buffer = context.Allocate(graph, new Rectangle(0, 0, Width, Height));

            // load objects
            Load();

            // timer for game loop
            Timer timer = new Timer();
            timer.Interval = 100;
            timer.Start();
            timer.Tick += Timer_Tick;
        }

        private static void Timer_Tick(object sender, EventArgs e)
        {
            Update();
            Draw();
        }

        /// <summary>
        /// Create/load objects
        /// </summary>
        public static void Load()
        {
            Random rnd = new Random();
            int newObjSize;     // dimension of object
            const int directionMin = -5;    // min "speed" at 2D space
            const int directionMax = 5;
            int objectsCount = 30;

            // load objects
            for (int i = 0; i < objectsCount; i++)
            {
                newObjSize = rnd.Next(4, 8);

                // polymorphism: put Asteroid to List<BaseObject>
                objectsList.Add(new Asteroid(new Point(rnd.Next(Width), i * 20),
                    new Point(rnd.Next(directionMin, directionMax), rnd.Next(directionMin, directionMax)),
                    new Size(newObjSize * 2, newObjSize * 2)));

                // polymorphism: put Star to List<BaseObject>
                objectsList.Add(new Star(new Point(rnd.Next(Width), i * 20),
                    new Point(rnd.Next(directionMin, directionMax), 0),
                    new Size(newObjSize, newObjSize)));

                // set random magnitude and radial direction of Vector2(rndX, rndY)
                int rndX = 3 * rnd.Next(directionMin, directionMax);
                int rndY = (int)(rndX / Math.Tan(i * 2 * Math.PI / objectsCount));
                // polymorphism: put Dot to List<BaseObject>
                objectsList.Add(new Dot(new Point(Game.Width / 2, Game.Height / 2),
                    new Point(rndX, rndY),
                    new Size(2, 2)));
            }

            // load one bullet
            //objectsList.Add(new Bullet(new Point(0, 200), new Point(5, 0), new Size(4, 1)));
            bullet = new Bullet(new Point(0, 200), new Point(5, 0), new Size(4, 1));
        }

        /// <summary>
        /// Draw graphics on Form area
        /// </summary>
        public static void Draw()
        {
            // test graphics
            buffer.Graphics.Clear(Color.Black);

            // draw objects
            foreach (var obj in objectsList)
                obj.Draw();

            bullet.Draw();

            // write from buffer to form
            buffer.Render();
        }

        /// <summary>
        /// Update objects location
        /// </summary>
        public static void Update()
        {
            // update objects
            foreach (var obj in objectsList)
                obj.Update();

            bullet.Update();

            //collision resolution between bullet and Asteroid
            foreach (var obj in objectsList)
            {
                // pick up Asteroid from BaseObject list and if collision occurs...
                if (obj is Asteroid
                    && (obj as Asteroid).CollisionRectangle.Contains(bullet.Center))
                {
                    // ...set Asteroid's x value to right bound and bullet's x value to left bound
                    (obj as Asteroid).Position = new Point(Width - (obj as Asteroid).CollisionRectangle.Width,
                                                        (obj as Asteroid).Position.Y);
                    bullet.X = 0;
                }
            }
        }
    }

    /// <summary>
    /// Describes general behaviour of objects (for example, circles)
    /// </summary>
    abstract class BaseObject
    {
        protected Point position;
        protected Point direction;
        protected Size size;

        public BaseObject(Point position, Point direction, Size size)
        {
            this.position = position;
            this.direction = direction;
            this.size = size;
        }

        /// <summary>
        /// Draw object (abstract)
        /// </summary>
        abstract public void Draw();

        /// <summary>
        /// Update object and bounce it in window
        /// </summary>
        abstract public void Update();
    }

    /// <summary>
    /// Class of stars inherited from BaseObject
    /// </summary>
    class Star : BaseObject
    {
        public Star(Point position, Point direction, Size size)
            : base(position, direction, size)
        {
        }

        /// <summary>
        /// Draw 4-ray star (2 lines)
        /// </summary>
        public override void Draw()
        {
            Game.buffer.Graphics.DrawLine(Pens.White,
                position.X, position.Y, position.X + size.Width, position.Y + size.Height);
            Game.buffer.Graphics.DrawLine(Pens.White,
                position.X, position.Y + size.Height, position.X + size.Width, position.Y);
        }

        /// <summary>
        /// Update horizontal moving of a star
        /// </summary>
        public override void Update()
        {
            position.X += direction.X;

            // bouncing object
            if (position.X <= 0 || position.X >= Game.Width - size.Width) direction.X = -1 * direction.X;
        }
    }

    /// <summary>
    /// Class of dots inherited from BaseObject
    /// </summary>
    class Dot : BaseObject
    {
        public Dot(Point position, Point direction, Size size)
            : base(position, direction, size)
        {
        }

        /// <summary>
        /// Draw rectangle for a dot
        /// </summary>
        public override void Draw()
        {
            Game.buffer.Graphics.FillRectangle(Brushes.White,
                position.X, position.Y, size.Width, size.Height);
        }

        /// <summary>
        /// Update dot location
        /// </summary>
        public override void Update()
        {
            position.X += direction.X;
            position.Y += direction.Y;

            if (position.X <= 0 || position.X >= Game.Width - size.Width
                || position.Y <= 0 || position.Y >= Game.Height - size.Height)
            {
                position.X = Game.Width / 2;
                position.Y = Game.Height / 2;
            }
        }
    }

    /// <summary>
    /// Class of asteroids inherited from BaseObject
    /// </summary>
    class Asteroid : BaseObject
    {
        private Rectangle drawRectangle;

        public int Power { get; set; }

        /// <summary>
        /// Top left x and y
        /// </summary>
        public Point Position
        {
            get { return position; }
            set { position = value; }
        }
        
        public Rectangle CollisionRectangle
        {
            get { return drawRectangle; }
        }

        public Asteroid(Point position, Point direction, Size size)
            : base(position, direction, size)
        {
            Power = 1;
            drawRectangle = new Rectangle(position.X, position.Y,
                                            size.Width, size.Height);
        }

        /// <summary>
        /// Draw fillEllipse
        /// </summary>
        public override void Draw()
        {
            Game.buffer.Graphics.FillEllipse(Brushes.White,
                position.X, position.Y, size.Width, size.Height);
        }

        /// <summary>
        /// Update asteroid location
        /// </summary>
        public override void Update()
        {
            position.X += direction.X;
            position.Y += direction.Y;

            if (position.X <= 0 || position.X >= Game.Width - size.Width) direction.X = -1 * direction.X;
            if (position.Y <= 0 || position.Y >= Game.Height - size.Height) direction.Y = -1 * direction.Y;

            drawRectangle.X = position.X;
            drawRectangle.Y = position.Y;
        }
    }

    /// <summary>
    /// Class of bullets inherited from BaseObject
    /// </summary>
    class Bullet : BaseObject
    {
        /// <summary>
        /// Sets left edge position
        /// </summary>
        public int X
        {
            set { position.X = value; }
        }

        /// <summary>
        /// Gets the location of bullet (roughly)
        /// </summary>
        public Point Center
        {
            get { return position; }
        }

        public Bullet(Point position, Point direction, Size size)
            : base(position, direction, size)
        {
        }

        /// <summary>
        /// Draw bullets
        /// </summary>
        public override void Draw()
        {
            Game.buffer.Graphics.DrawRectangle(Pens.OrangeRed,
                position.X, position.Y, size.Width, size.Height);
        }

        /// <summary>
        /// Update bullet location
        /// </summary>
        public override void Update()
        {
            position.X += direction.X;

            if (position.X <= 0 || position.X >= Game.Width - size.Width) position.X = 0;
        }
    }
}
