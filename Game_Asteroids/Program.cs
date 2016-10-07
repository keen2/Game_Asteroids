using System;
using System.Windows.Forms;

namespace Game_Asteroids
{
    // Andrey Ermishin

    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Form form1 = new Form1();
            form1.Width = 800;
            form1.Height = 600;
            Game.Init(form1);
            form1.Show();   // display (graphics) Form area to user
            Game.Draw();

            Application.Run(form1);
        }
    }
}
