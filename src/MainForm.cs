using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace TypingMahayudh
{
    class MainForm : Form
    {
        public static MainForm Instance;
        static readonly string[] Names = { "Dashboard", "English Typing", "Hindi Typing", "Typing Drills", "Typing Tests", "Efficiency Test", "Daily Planner", "Progress Tracker", "Mistake Analysis", "Achievements", "Reports", "Settings" };
        static readonly string[] Reminders = {
            "Do not look at the keyboard.", "Keep your eyes on the screen.", "Focus on the words.", "Maintain correct finger positioning.",
            "Avoid unnecessary backspace.", "Do not sacrifice accuracy for speed.", "Maintain a comfortable typing rhythm." };
        Panel content, side; Button[] nav = new Button[12]; Page[] pages = new Page[12]; int cur = -1; Label ticker; int rem; Timer tm = new Timer { Interval = 7000 };

        public MainForm()
        {
            Instance = this;
            Text = "Typing Mahayudh - LDC Exam Preparation System"; BackColor = Theme.Bg; MinimumSize = new Size(1000, 640);
            var wa = Screen.PrimaryScreen.WorkingArea; Size = new Size((int)(wa.Width * 0.92), (int)(wa.Height * 0.92)); StartPosition = FormStartPosition.CenterScreen;
            side = new Panel { Dock = DockStyle.Left, Width = 230, BackColor = Theme.Panel };
            var brand = new Label { Text = "TYPING\r\nMAHAYUDH", Dock = DockStyle.Top, Height = 80, ForeColor = Theme.Gold, Font = Theme.F(17, true), TextAlign = ContentAlignment.MiddleCenter };
            for (int i = Names.Length - 1; i >= 0; i--)
            {
                int idx = i;
                var b = new Button { Text = "   " + (i + 1) + "   " + Names[i], Dock = DockStyle.Top, Height = 44, FlatStyle = FlatStyle.Flat, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Theme.Text, BackColor = Theme.Panel, Font = Theme.F(10.5f), Cursor = Cursors.Hand };
                b.FlatAppearance.BorderSize = 0; b.Click += (s, e) => Go(idx); nav[i] = b; side.Controls.Add(b);
            }
            side.Controls.Add(brand);
            var foot = new Panel { Dock = DockStyle.Bottom, Height = 38, BackColor = Theme.Panel2 };
            ticker = new Label { Dock = DockStyle.Fill, ForeColor = Theme.Teal, Font = Theme.F(11, true), TextAlign = ContentAlignment.MiddleCenter, Text = Reminders[0] };
            foot.Controls.Add(ticker);
            content = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Bg };
            Controls.Add(content); Controls.Add(foot); Controls.Add(side);
            tm.Tick += (s, e) => { rem = (rem + 1) % Reminders.Length; ticker.Text = Reminders[rem]; };
            tm.Start();
            Go(0);
            Shown += (s, e) => Startup();
            FormClosing += (s, e) =>
            {
                try { if (Cfg.GetB("autobackup")) BackupMgr.Auto(); } catch { }
            };
        }

        Page Make(int i)
        {
            switch (i)
            {
                case 0: return new DashboardPage();
                case 1: return new PracticePage("English");
                case 2: return new PracticePage("Hindi");
                case 3: return new EntryPage("Drill");
                case 4: return new EntryPage("Test");
                case 5: return new EfficiencyPage();
                case 6: return new PlannerPage();
                case 7: return new ProgressPage();
                case 8: return new MistakePage();
                case 9: return new AchievementsPage();
                case 10: return new ReportsPage();
                default: return new SettingsPage();
            }
        }

        public void Go(int i)
        {
            if (cur >= 0) { pages[cur].Visible = false; nav[cur].BackColor = Theme.Panel; nav[cur].ForeColor = Theme.Text; }
            if (pages[i] == null) { pages[i] = Make(i); content.Controls.Add(pages[i]); }
            cur = i; pages[i].Visible = true; pages[i].BringToFront();
            nav[i].BackColor = Theme.Panel2; nav[i].ForeColor = Theme.Teal;
            pages[i].OnShow();
        }

        public void RefreshCurrent() { if (cur >= 0) pages[cur].OnShow(); }

        void Startup()
        {
            try
            {
                if (Cfg.GetB("autobackup") && (DateTime.Now - BackupMgr.LastAuto()).TotalHours > 20) BackupMgr.Auto();
                string last = Stats.LastDay(), today = DateTime.Today.ToString("yyyy-MM-dd", Db.Inv);
                if (Cfg.GetB("popups") && last != "" && Cfg.Get("nop_shown") != today)
                {
                    int ago = (DateTime.Today - DateTime.ParseExact(last, "yyyy-MM-dd", Db.Inv)).Days;
                    if (ago >= 2)
                    {
                        Cfg.Set("nop_shown", today);
                        using (var f = new PopupForm(MotivationEngine.NoPractice(ago))) f.ShowDialog(this);
                    }
                }
            }
            catch (Exception ex) { Program.Log(ex); }
        }
    }
}
