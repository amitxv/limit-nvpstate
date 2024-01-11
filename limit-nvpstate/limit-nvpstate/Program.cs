using System;
using System.Reflection;
using System.Windows.Forms;

namespace LimitNvpstate {
    internal static class Program {
        public static Version Version = Assembly.GetExecutingAssembly().GetName().Version;

        [STAThread]
        private static void Main() {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new LimitNvpstate());
        }
    }
}
