using System;
using System.Windows.Forms;

namespace KutuphaneOtomasyon
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Programın Form1 ile başlamasını sağlar
            Application.Run(new Form1());
        }
    }
}