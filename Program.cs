using System;
using System.Windows.Forms;
using EclatEtSoin.Forms;

namespace EclatEtSoin
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new LoginForm());
        }
    }
}
