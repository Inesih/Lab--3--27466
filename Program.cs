using System;
using System.Windows.Forms;

namespace IncidentCommand
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += delegate (object s, System.Threading.ThreadExceptionEventArgs e)
            {
                MessageBox.Show("Something went wrong: " + e.Exception.Message, "Incident Command", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            };

            Store.Load();
            while (true)
            {
                Session.Me = null;
                using (LoginForm login = new LoginForm())
                {
                    if (login.ShowDialog() != DialogResult.OK) break;
                }
                MainForm.LoggedOut = false;
                Application.Run(new MainForm());
                if (!MainForm.LoggedOut) break;
            }
        }
    }
}
