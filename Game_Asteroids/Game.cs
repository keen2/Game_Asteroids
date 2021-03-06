﻿using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game_Asteroids
{

    interface ICollision
    {
        bool CollisionWith(ICollision obj);
        Rectangle CollisionRectangle { get; }
    }

    static class Game
    {
        static BufferedGraphicsContext context;
        public static BufferedGraphics buffer;

        // lists of objects
        static List<BaseObject> objectsList = new List<BaseObject>();
        static List<Bullet> bulletsList=new List<Bullet>();
        static Ship ship;

        // game constants
        const int asteroidsOffset = 150; // to not damage the ship at start
        public const int directionMin = -5;    // min "speed" at 2D space
        public const int directionMax = 5;
        const string EnergyStringPrefix = "Energy: ";

        // game window resolution
        public static int WindowWidth { get; set; }
        public static int WindowHeight { get; set; }

        // timer for game loop
        static Timer timer = new Timer();

        // frames per second
        static int lastTickMilliseconds = 0;
        static int lastFPS = 0;
        static int frames = 0;

        // random generator
        public static Random rnd = new Random();

        // ship sprite
        public static Image shipSprite;

        // energy string (changes only on collision)
        static string energyString;

        // fonts
        static Font energyFont;
        static Font fpsFont;

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

            WindowWidth = form.Width - 2 * SystemInformation.BorderSize.Width
                                - 3 * SystemInformation.HorizontalResizeBorderThickness;
            WindowHeight = form.Height - SystemInformation.CaptionHeight
                                - 2 * SystemInformation.BorderSize.Height
                                - 3 * SystemInformation.VerticalResizeBorderThickness;

            // link buffer with graphical device to draw at buffer
            buffer = context.Allocate(graph, new Rectangle(0, 0, WindowWidth, WindowHeight));

            // load objects
            Load();

            // add Finish() method to event MessageDeath
            ship.MessageDeath += Finish;

            // catch Up and Down arrow keys
            form.KeyDown += Form_KeyDown;

            // start timer for game loop
            timer.Interval = 50;
            timer.Start();
            timer.Tick += Timer_Tick;
        }

        private static void Form_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space)
            {
                // spawn a bullet
                bulletsList.Add(new Bullet(
                                        new Point(ship.CollisionRectangle.Right,
                                                ship.CollisionRectangle.Top + ship.CollisionRectangle.Height / 2),
                                        new Point(25, 0),
                                        new Size(4, 1)));
            }
            else if (e.KeyCode == Keys.Up)
            {
                ship.Up();
            }
            else if (e.KeyCode == Keys.Down)
            {
                ship.Down();
            }
        }

        /// <summary>
        /// Realize game loop at every tick
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Timer_Tick(object sender, EventArgs e)
        {
            Update();
            // if the ship alive and timer isn't stopped
            if (timer.Enabled)
                Draw();
        }

        /// <summary>
        /// Create/load objects
        /// </summary>
        public static void Load()
        {
            int newObjSize;     // dimension of object
            int objectsCount = 30;

            // load objects
            for (int i = 0; i < objectsCount; i++)
            {
                newObjSize = rnd.Next(3, 8);

                // polymorphism: put Asteroid to List<BaseObject>
                objectsList.Add(new Asteroid(new Point(rnd.Next(asteroidsOffset, WindowWidth), i * 20),
                    new Point(rnd.Next(directionMin / 2, directionMax), rnd.Next(directionMin / 2, directionMax)),
                    new Size(newObjSize * 3, newObjSize * 3)));

                // polymorphism: put Star to List<BaseObject>
                objectsList.Add(new Star(new Point(rnd.Next(WindowWidth), i * 20),
                    new Point(rnd.Next(directionMin / 2, 0), 0),
                    new Size(newObjSize, newObjSize)));

                // set random magnitude (and radial) direction of Vector2(rndX, rndY)
                int rndX = -5 * rnd.Next(3, directionMax);
                /*int rndY = (int)(rndX / Math.Tan(i * 2 * Math.PI / objectsCount));*/

                // polymorphism: put Dot to List<BaseObject>
                objectsList.Add(new Dot(new Point(rnd.Next(WindowWidth), i * 20),
                    new Point(rndX, 0),
                    new Size(2, 2)));
            }

            // load a ship: create instance and load random resource image and initialize energyString
            ship = new Ship(new Point(10, Game.WindowHeight / 2), new Point(0, 10), new Size(50, 35));
            shipSprite = Properties.Resources.ResourceManager.GetObject("ship" + Game.rnd.Next(1, 6)) as Image;
            energyString = EnergyStringPrefix + ship.Energy;

            // load fonts
            fpsFont = new Font("Arial", 10);
            energyFont = new Font(FontFamily.GenericSansSerif, 12, FontStyle.Bold);
        }

        /// <summary>
        /// Draw graphics on Form area
        /// </summary>
        public static void Draw()
        {
            // background
            buffer.Graphics.Clear(Color.Black);

            // draw objects
            foreach (var obj in objectsList)
                obj.Draw();

            // draw bullets
            foreach (var bullet in bulletsList)
                bullet.Draw();

            // draw ship
            ship.Draw();

            // draw ship energy
            buffer.Graphics.DrawString(energyString, energyFont, Brushes.Yellow, 0, 0);

            // frames accumulating
            frames++;
            // until 1 second - draw old FPS, else - draw new FPS
            float textWidth = TextRenderer.MeasureText("FPS: " + lastFPS, fpsFont).Width;
            if (Environment.TickCount - lastTickMilliseconds < 1000)
                buffer.Graphics.DrawString("FPS: " + lastFPS, fpsFont, Brushes.Aqua, WindowWidth - textWidth, 5);
            else
            {
                buffer.Graphics.DrawString("FPS: " + frames, fpsFont, Brushes.Aqua, WindowWidth - textWidth, 5);
                lastFPS = frames;
                frames = 0;
                lastTickMilliseconds = Environment.TickCount;
            }

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

            // update bullets
            foreach (var bullet in bulletsList)
                bullet.Update();

            ship.Update();

            //collisions resolution
            foreach (var obj in objectsList)
            {
                if (obj is Asteroid)
                {
                    Asteroid asteroid = obj as Asteroid;

                    // collision resolution between Asteroid and bullet
                    foreach (Bullet bullet in bulletsList)
                    {
                        // pick up Asteroid from BaseObject list and if collision occurs...
                        if (bullet.CollisionWith(asteroid))
                        {
                            // ...set Asteroid's x value to right bound and bullet to inactive
                            asteroid.Position = new Point(WindowWidth - asteroid.Width, asteroid.Position.Y);
                            bullet.Active = false;

                            // play sound
                            System.Media.SystemSounds.Beep.Play();
                        }
                    }

                    //collision resolution between ship and Asteroid
                    if (ship.CollisionWith(asteroid))
                    {
                        System.Media.SystemSounds.Asterisk.Play();

                        // update ship energy and it's string
                        ship.EnergyLower(rnd.Next(1, 6));
                        energyString = EnergyStringPrefix + ship.Energy;
                        
                        if (ship.Energy <= 0)
                            ship.Death();   // call ship.MessageDeath
                    }
                }
            }

            // clean inactive bullets
            for (int i = bulletsList.Count - 1; i >= 0; i--)
            {
                if (!bulletsList[i].Active)
                    bulletsList.RemoveAt(i);
            }
        }

        /// <summary>
        /// Freeze screen and draw string "Game Over"
        /// </summary>
        public static void Finish()
        {
            timer.Stop();

            // clean ship energy value
            float energyValueWidth = TextRenderer.MeasureText("0", energyFont).Width;
            Size energyTextSize = TextRenderer.MeasureText(energyString, energyFont);
            buffer.Graphics.FillRectangle(Brushes.Black,
                                            energyTextSize.Width - energyValueWidth, 0,
                                            energyTextSize.Width, energyTextSize.Height);
            buffer.Render();

            // update screen at the end: draw energy value and string "Game Over" at the center
            buffer.Graphics.DrawString(energyString, energyFont, Brushes.Yellow, 0, 0);
            Size GameOverTextSize = TextRenderer.MeasureText("Game Over",
                                        new Font(FontFamily.GenericSansSerif, 60, FontStyle.Underline));
            buffer.Graphics.DrawString("Game Over",
                new Font(FontFamily.GenericSansSerif, 60, FontStyle.Underline),
                Brushes.Crimson,
                WindowWidth / 2 - GameOverTextSize.Width / 2,
                WindowHeight / 2 - GameOverTextSize.Height / 2);
            buffer.Render();
        }
    }

    /// <summary>
    /// Describes general behaviour of objects (for example, circles)
    /// </summary>
    abstract class BaseObject : ICollision
    {
        // delegate for messages
        public delegate void MyMessage();

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

        /// <summary>
        /// Returns true if collision detected
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public bool CollisionWith(ICollision obj)
        {
            return obj.CollisionRectangle.IntersectsWith(this.CollisionRectangle);
        }

        public Rectangle CollisionRectangle
        {
            get { return new Rectangle(position, size); }
        }
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
            if (position.X <= 0) position.X = Game.WindowWidth;
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

            if (position.X <= 0)
            {
                position.X = Game.WindowWidth;
                direction.X = -5 * Game.rnd.Next(3, Game.directionMax);
            }
        }
    }

    /// <summary>
    /// Class of asteroids inherited from BaseObject
    /// </summary>
    class Asteroid : BaseObject, IComparable<Asteroid>
    {
        /// <summary>
        /// Shots to destroy
        /// </summary>
        public int Power { get; set; } = 3;     // default value
        public int Width { get { return size.Width; } }

        /// <summary>
        /// Top left x and y
        /// </summary>
        public Point Position
        {
            get { return position; }
            set { position = value; }
        }
        
        public Asteroid(Point position, Point direction, Size size)
            : base(position, direction, size)
        {
            Power = Game.rnd.Next(1, 4);    // 1..3 shots to destroy
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

            if (position.X <= 0 || position.X >= Game.WindowWidth - size.Width) direction.X = -1 * direction.X;
            if (position.Y <= 0 || position.Y >= Game.WindowHeight - size.Height) direction.Y = -1 * direction.Y;

        }

        int IComparable<Asteroid>.CompareTo(Asteroid obj)
        {
            if (Power > obj.Power)
                return 1;
            else if (Power < obj.Power)
                return 1;
            else
                return 0;
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
        public bool Active { get; set; } = true;

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

            if (position.X > Game.WindowWidth) Active = false;
        }
    }

    /// <summary>
    /// Class of ship inherited from BaseObject
    /// </summary>
    class Ship : BaseObject
    {
        // event of delegate type BaseObject.Message
        public event MyMessage MessageDeath;

        int energy = 100;

        /// <summary>
        /// Health of the ship
        /// </summary>
        public int Energy { get { return energy; } }

        public Ship(Point position, Point direction, Size size)
            : base(position,direction,size)
        {
        }

        public void EnergyLower(int n)
        {
            energy -= n;
        }
        public void Up()
        {
            if (position.Y > 0 && (position.Y - direction.Y) >= 0) position.Y -= direction.Y;
        }
        public void Down()
        {
            if (position.Y < Game.WindowHeight
                                && (position.Y + direction.Y + size.Height) <= Game.WindowHeight)
                position.Y += direction.Y;
        }

        /// <summary>
        /// Destroying the ship
        /// </summary>
        public void Death()
        {
            energy = 0;
            // call event
            if (MessageDeath != null) MessageDeath();
        }

        /// <summary>
        /// Draw ship
        /// </summary>
        public override void Draw()
        {
            Game.buffer.Graphics.DrawImage(Game.shipSprite, new Rectangle(position.X, position.Y, size.Width, size.Height));
        }
        /// <summary>
        /// Update ship location
        /// </summary>
        public override void Update()
        {
        }
    }
}
