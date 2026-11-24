using System;
using System.Drawing;
using System.Media;
using System.Windows.Forms;

namespace TypingMahayudh
{
    class PopupForm : Form
    {
        public PopupForm(Popup p)
        {
            Text = "Typing Mahayudh"; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent; BackColor = Theme.Bg; ClientSize = new Size(580, 320); ShowInTaskbar = false;
            var accent = p.Positive ? Theme.Gold : Theme.Teal;
            var title = Ui.Lbl(p.Title, 16, accent, true); title.Location = new Point(20, 16);
            var reason = Ui.Lbl(p.Reason, 12, Theme.Text); reason.MaximumSize = new Size(540, 0); reason.Location = new Point(20, 60);
            var line = Ui.Lbl(p.Line, 15, accent, true); line.MaximumSize = new Size(540, 0); line.Location = new Point(20, 130);
            var ok = Ui.Btn("ठीक है", (s, e) => Close(), p.Positive); ok.Location = new Point(20, 260);
            Controls.AddRange(new Control[] { title, reason, line, ok });
            AcceptButton = ok;
            if (Cfg.GetB("sound")) { if (p.Positive) SystemSounds.Asterisk.Play(); else SystemSounds.Beep.Play(); }
        }
    }

    class AchievementForm : Form
    {
        Timer tm = new Timer { Interval = 25 };
        ProgressBar pb;
        int target;

        public AchievementForm(int idx, double total)
        {
            Text = "Achievement Unlocked"; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent; BackColor = Color.FromArgb(24, 20, 8); ClientSize = new Size(700, 440); ShowInTaskbar = false;
            var head = Ui.Lbl("ACHIEVEMENT UNLOCKED", 13, Theme.Teal, true); head.Location = new Point(24, 20);
            var big = new Label { Text = Milestones.Titles[idx], ForeColor = Theme.Gold, Font = new Font("Segoe UI", 28, FontStyle.Bold), AutoSize = false, Size = new Size(650, 120), Location = new Point(24, 60), BackColor = Color.Transparent };
            var words = Ui.Lbl(string.Format("{0:N0} शब्द पूरे हुए", Milestones.Words[idx]), 16, Theme.Text, true); words.Location = new Point(24, 190);
            int next = Milestones.NextIndex(total);
            string nextText = next < 0 ? "सभी मील के पत्थर पूरे हो गए! जय हो!" :
                string.Format("कुल शब्द: {0:N0}  |  अगला लक्ष्य: {1:N0} शब्द  |  बाकी: {2:N0}", total, Milestones.Words[next], Milestones.Words[next] - total);
            var nl = Ui.Lbl(nextText, 12, Theme.Muted); nl.MaximumSize = new Size(650, 0); nl.Location = new Point(24, 235);
            var extra = Ui.Lbl(Dialogues.Pick("achievement"), 13, Theme.Gold); extra.MaximumSize = new Size(650, 0); extra.Location = new Point(24, 275);
            pb = new ProgressBar { Location = new Point(24, 340), Size = new Size(650, 22), Minimum = 0, Maximum = 100, Style = ProgressBarStyle.Continuous };
            if (next < 0) target = 100;
            else
            {
                double lo = next == 0 ? 0 : Milestones.Words[next - 1];
                target = (int)Math.Max(0, Math.Min(100, (total - lo) / (Milestones.Words[next] - lo) * 100));
            }
            var ok = Ui.Btn("शानदार!", (s, e) => Close(), true); ok.Location = new Point(24, 385);
            Controls.AddRange(new Control[] { head, big, words, nl, extra, pb, ok });
            tm.Tick += (s, e) => { if (pb.Value < target) pb.Value = Math.Min(target, pb.Value + 2); else tm.Stop(); };
            Shown += (s, e) => { tm.Start(); };
            FormClosed += (s, e) => tm.Dispose();
            if (Cfg.GetB("sound")) SystemSounds.Exclamation.Play();
        }
    }

    class EntryDialog : Form
    {
        EntryPanel panel; public SessionRec Result;
        public static SessionRec Ask(IWin32Window owner, SessionRec t)
        {
            using (var d = new EntryDialog(t)) return d.ShowDialog(owner) == DialogResult.OK ? d.Result : null;
        }
        EntryDialog(SessionRec t)
        {
            Text = "Enter session result"; StartPosition = FormStartPosition.CenterParent; BackColor = Theme.Bg; ClientSize = new Size(900, 330);
            panel = new EntryPanel("Practice", new[] { "MS Word", "Book", "Newspaper", "Custom passage" }, "Practice method", 1, "Built-in timer (manual entry): {0}") { Dock = DockStyle.Fill };
            panel.Load(t);
            panel.SaveRequested += s => { Result = s; DialogResult = DialogResult.OK; };
            Controls.Add(panel);
        }
    }

    class ExamForm : Form
    {
        string lang; TypingArea area; Panel setup; TextBox txt; bool finished;
        public ExamForm(string language)
        {
            lang = language;
            Text = "Final Exam Simulation - " + lang + " (10 minutes)";
            WindowState = FormWindowState.Maximized; BackColor = Theme.Bg;
            setup = new Panel { Dock = DockStyle.Fill, Padding = new Padding(30) };
            var h = Ui.Lbl("FINAL EXAM SIMULATION - " + lang.ToUpper(), 20, Theme.Gold, true); h.Dock = DockStyle.Top;
            var ins = Ui.Lbl("Instructions / निर्देश:\r\n1. Duration is exactly 10 minutes; the timer starts when you press Start.\r\n2. Paste the exam passage below (paper-style text of your choice). Do not look at the keyboard.\r\n3. Paste and shortcuts are disabled in the typing box. Backspace is counted.\r\n4. Results are saved as an Exam record when the time ends.\r\n\r\nसमय 10 मिनट है। कीबोर्ड मत देखिए, स्क्रीन पर ध्यान रखिए।", 12, Theme.Text);
            ins.Dock = DockStyle.Top; ins.MaximumSize = new Size(1000, 0);
            txt = new TextBox { Multiline = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Top, Height = 220, BackColor = Theme.Panel2, ForeColor = Theme.Text, Font = lang == "Hindi" ? new Font("Mangal", 14) : Theme.F(14) };
            var bar = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true };
            bar.Controls.Add(Ui.Btn("Load sample passage", (s, e) => txt.Text = Samples.For(lang)));
            bar.Controls.Add(Ui.Btn("START EXAM", (s, e) => Start(), true));
            bar.Controls.Add(Ui.Btn("Cancel", (s, e) => { finished = true; Close(); }));
            setup.Controls.Add(bar); setup.Controls.Add(txt); setup.Controls.Add(ins); setup.Controls.Add(h);
            area = new TypingArea { Visible = false };
            area.Finished += OnDone;
            Controls.Add(area); Controls.Add(setup);
        }

        void Start()
        {
            if (string.IsNullOrWhiteSpace(txt.Text)) { Ui.Warn("Paste the exam passage first."); return; }
            setup.Visible = false; area.Visible = true; area.Start(txt.Text, 600, lang, false);
        }

        void OnDone(TypingResult r, bool full)
        {
            finished = true;
            if (r.Chars == 0) { Ui.Info("Nothing was typed. No exam record was saved."); Close(); return; }
            Ui.Info(string.Format(Db.Inv, "EXAM RESULT ({0})\r\n\r\nGross speed: {1:0.0} WPM\r\nNet speed: {2:0.0} WPM\r\nAccuracy: {3:0.0}%\r\nErrors (characters): {4}\r\nBackspaces: {5}\r\nTotal words: {6:0}", lang, r.Gross, r.Net, r.Accuracy, r.Errors, r.Backspaces, r.Words));
            var s = new SessionRec { Ts = DateTime.Now, Language = lang, Kind = "Exam", Method = "Exam Simulation", Source = "Built-in exam simulation",
                Duration = Math.Round(r.Seconds / 60.0, 2), Gross = r.Gross, Net = r.Net, Accuracy = r.Accuracy, Errors = r.Errors, Backspaces = r.Backspaces, Words = r.Words, Completed = full };
            Flow.Complete(this, s);
            Close();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!finished && area.Running)
            {
                if (!Ui.Ask("Leave the exam? This attempt will NOT be saved.")) { e.Cancel = true; return; }
                area.Discard();
            }
            base.OnFormClosing(e);
        }
    }

    static class Samples
    {
        public static string For(string lang)
        {
            if (lang == "Hindi")
                return "सरकारी नौकरी की तैयारी में निरंतर अभ्यास सबसे बड़ा हथियार है। प्रतिदिन कुछ समय निकालकर शुद्ध और तेज टाइपिंग का अभ्यास करने से गति और शुद्धता दोनों बढ़ती हैं। स्क्रीन पर ध्यान रखें और कीबोर्ड को बार-बार न देखें।";
            return "Regular practice is the surest way to improve typing speed. A student who types a little every day, keeps his eyes on the screen and places his fingers correctly on the home row will soon notice that both speed and accuracy grow together. Accuracy should never be sacrificed for speed.";
        }
    }
}
