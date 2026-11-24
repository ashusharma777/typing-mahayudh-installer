using System;
using System.IO;
using System.Windows.Forms;

namespace TypingMahayudh
{
    static class Program
    {
        public static void Log(Exception ex)
        {
            try { File.AppendAllText(Path.Combine(Db.Dir, "error.log"), DateTime.Now.ToString("s") + "  " + ex + "\r\n\r\n"); } catch { }
        }

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.ThreadException += (s, e) => { Log(e.Exception); MessageBox.Show("Something went wrong, but your saved data is safe.\r\n\r\n" + e.Exception.Message, "Typing Mahayudh"); };
            AppDomain.CurrentDomain.UnhandledException += (s, e) => Log(e.ExceptionObject as Exception);
            try
            {
                Db.Setup();
                if (File.Exists(Db.FilePath) && !Db.IntegrityOk(Db.FilePath)) Recover();
                Db.Init();
            }
            catch (Exception ex)
            {
                try { Log(ex); } catch { }
                MessageBox.Show("The database could not be opened:\r\n" + ex.Message + "\r\n\r\nIf SQLite DLLs are missing, reinstall the application.", "Typing Mahayudh", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            Application.Run(new MainForm());
        }

        static void Recover()
        {
            string bad = Path.Combine(Db.Dir, "corrupt_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".db");
            File.Move(Db.FilePath, bad);
            string latest = BackupMgr.LatestFile(), err;
            if (latest != null && MessageBox.Show("The progress database is damaged. The damaged file was kept as:\r\n" + bad + "\r\n\r\nRestore the latest backup?\r\n" + latest, "Typing Mahayudh", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                if (BackupMgr.Restore(latest, out err)) return;
                MessageBox.Show("Restore failed: " + err + "\r\nA fresh database will be created.");
            }
            else MessageBox.Show("A fresh database will be created. The damaged file was kept as:\r\n" + bad);
        }
    }
}
