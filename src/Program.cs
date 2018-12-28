using System;
using System.Threading;
using System.Windows.Forms;

namespace GarboDev.WinForms
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();

            MainWindow mainWindow = new MainWindow();

            mainWindow.Show();

            while (mainWindow.Created)
            {
                mainWindow.CheckKeysHit();
                Application.DoEvents();

                Thread.Sleep(5);
            }
        }
    }
}