using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace ParserNamespace
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            if (true)
            {
                new DebugClass().GG(); return;
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
