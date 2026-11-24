using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace TypingMahayudh
{
    class DashboardPage : Page
    {
        Dictionary<string, Label> tiles = new Dictionary<string, Label>();
        Label lblQuote, lblToday, lblNext; ProgressBar pbNext, pbOverall; FlowLayoutPanel flow;

        public DashboardPage()
        {
            flow = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, WrapContents = true, BackColor = Theme.Bg };
            string[] keys = { "Days to exam", "English now (net WPM)", "Hindi now (net WPM)", "English best", "Hindi best", "Last accuracy", "Total words", "Words to next achievement", "Overall preparation" };
            foreach (var k in keys)
            {
                var c = new Panel { Size = new Size(215, 92), BackColor = Theme.Panel, Margin = new Padding(6), Padding = new Padding(10) };
                var v = Ui.Lbl("-", 22, Theme.Gold, true); v.Location = new Point(10, 34);
                var l = Ui.Lbl(k, 9, Theme.Muted); l.Location = new Point(10, 10);
                c.Controls.Add(l); c.Controls.Add(v); tiles[k] = v; flow.Controls.Add(c);
            }
            var q = new Panel { Size = new Size(670, 150), BackColor = Theme.Panel, Margin = new Padding(6), Padding = new Padding(12) };
            var qh = Ui.Lbl("आज का जोश", 11, Theme.Teal, true); qh.Location = new Point(12, 8);
            lblQuote = new Label { Location = new Point(12, 36), Size = new Size(640, 70), ForeColor = Theme.Text, Font = Theme.F(15, true), BackColor = Color.Transparent };
            var nb = Ui.Btn("नया डायलॉग", (s, e) => lblQuote.Text = Dialogues.DashboardPick()); nb.Location = new Point(12, 110);
            q.Controls.AddRange(new Control[] { qh, lblQuote, nb });
            var m = new Panel { Size = new Size(670, 150), BackColor = Theme.Panel, Margin = new Padding(6), Padding = new Padding(12) };
            var mh = Ui.Lbl("Next achievement", 11, Theme.Gold, true); mh.Location = new Point(12, 8);
            lblNext = Ui.Lbl("", 11, Theme.Text); lblNext.Location = new Point(12, 36); lblNext.MaximumSize = new Size(640, 0);
            pbNext = new ProgressBar { Location = new Point(12, 80), Size = new Size(640, 18) };
            pbOverall = new ProgressBar { Location = new Point(12, 118), Size = new Size(640, 18) };
            var oh = Ui.Lbl("Overall preparation (avg of English speed vs target, Hindi speed vs target, efficiency completion)", 8, Theme.Muted); oh.Location = new Point(12, 100);
            m.Controls.AddRange(new Control[] { mh, lblNext, pbNext, oh, pbOverall });
            var t = new Panel { Size = new Size(1346, 70), BackColor = Theme.Panel, Margin = new Padding(6) };
            lblToday = Ui.Lbl("", 12, Theme.Text); lblToday.Location = new Point(12, 20); t.Controls.Add(lblToday);
            flow.Controls.Add(q); flow.Controls.Add(m); flow.Controls.Add(t);
            Controls.Add(flow);
        }

        public override void OnShow()
        {
            tiles["Days to exam"].Text = Math.Max(0, (Cfg.ExamDate - DateTime.Today).Days).ToString();
            var e = Stats.Latest("English"); var h = Stats.Latest("Hindi");
            tiles["English now (net WPM)"].Text = e == null ? "-" : Db.D(e["net_wpm"]).ToString("0.0");
            tiles["Hindi now (net WPM)"].Text = h == null ? "-" : Db.D(h["net_wpm"]).ToString("0.0");
            tiles["English best"].Text = Stats.BestNet("English").ToString("0.0");
            tiles["Hindi best"].Text = Stats.BestNet("Hindi").ToString("0.0");
            var last = Db.Query("SELECT accuracy FROM sessions WHERE words>0 ORDER BY ts DESC, id DESC LIMIT 1");
            tiles["Last accuracy"].Text = last.Rows.Count == 0 ? "-" : Db.D(last.Rows[0][0]).ToString("0.0") + "%";
            double total = Stats.TotalWords(); int ni = Milestones.NextIndex(total);
            tiles["Total words"].Text = total.ToString("N0");
            tiles["Words to next achievement"].Text = ni < 0 ? "Done!" : (Milestones.Words[ni] - total).ToString("N0");
            double ov = Stats.OverallPct();
            tiles["Overall preparation"].Text = ov.ToString("0") + "%"; pbOverall.Value = (int)Math.Max(0, Math.Min(100, ov));
            if (ni < 0) { lblNext.Text = "All achievements unlocked. Jay ho!"; pbNext.Value = 100; }
            else
            {
                double lo = ni == 0 ? 0 : Milestones.Words[ni - 1];
                lblNext.Text = Milestones.Titles[ni] + "  -  " + Milestones.Words[ni].ToString("N0") + " words";
                pbNext.Value = (int)Math.Max(0, Math.Min(100, (total - lo) / (Milestones.Words[ni] - lo) * 100));
            }
            string today = DateTime.Today.ToString("yyyy-MM-dd", Db.Inv);
            int cnt = Stats.CountOn(today);
            var pl = Db.Query("SELECT COUNT(*), IFNULL(SUM(done),0) FROM tasks WHERE day=@0", today);
            double pt = Db.D(pl.Rows[0][0]), pd = Db.D(pl.Rows[0][1]);
            lblToday.Text = "Today: " + cnt + " session(s) saved   |   Planner: " + (pt > 0 ? (pd / pt * 100).ToString("0") + "% done" : "not opened yet");
            if (string.IsNullOrEmpty(lblQuote.Text) || cnt == 0 && lblQuote.Tag == null)
            {
                lblQuote.Text = cnt == 0 && Stats.LastDay() != "" ? Dialogues.Pick("nopractice") : Dialogues.DashboardPick();
                lblQuote.Tag = "set";
            }
        }
    }

    class PracticePage : Page
    {
        string lang; ComboBox cbMethod, cbDur, cbMode; TextBox txtPassage; CheckBox chkLooked; Button btnStart, btnStop; TypingArea area; DataGridView grid;

        public PracticePage(string lang)
        {
            this.lang = lang;
            var tl = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, BackColor = Theme.Bg };
            tl.RowStyles.Add(new RowStyle(SizeType.AutoSize)); tl.RowStyles.Add(new RowStyle(SizeType.Absolute, 95));
            tl.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); tl.RowStyles.Add(new RowStyle(SizeType.Absolute, 170));
            var cfg = Ui.Row();
            cbMethod = Ui.Combo(150, "MS Word", "Book", "Newspaper", "Custom passage");
            cbDur = Ui.Combo(70, "5", "10", "15", "20", "30", "40"); cbDur.SelectedIndex = 1;
            cbMode = Ui.Combo(260, "Built-in typing box (auto-scored)", "External timer (MS Word / book / newspaper)");
            chkLooked = new CheckBox { Text = "I looked at the keyboard", ForeColor = Theme.Text, AutoSize = true, Margin = new Padding(8, 22, 4, 0) };
            btnStart = Ui.Btn("START", (s, e) => StartClick(), true); btnStop = Ui.Btn("Stop", (s, e) => StopClick()); btnStop.Enabled = false;
            var title = Ui.Lbl(lang.ToUpper() + " TYPING PRACTICE  (target " + (lang == "Hindi" ? Cfg.GetD("hindi_target") : Cfg.GetD("eng_target")).ToString("0") + " WPM)", 13, Theme.Gold, true);
            title.Margin = new Padding(4, 8, 40, 4);
            cfg.Controls.Add(title); cfg.Controls.Add(Ui.Field("Method", cbMethod)); cfg.Controls.Add(Ui.Field("Minutes", cbDur)); cfg.Controls.Add(Ui.Field("Mode", cbMode));
            cfg.Controls.Add(chkLooked); cfg.Controls.Add(btnStart); cfg.Controls.Add(btnStop);
            cfg.Controls.Add(Ui.Btn("Load sample", (s, e) => txtPassage.Text = Samples.For(this.lang)));
            txtPassage = new TextBox { Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Vertical, BackColor = Theme.Panel2, ForeColor = Theme.Text, Font = lang == "Hindi" ? new Font("Mangal", 11) : Theme.F(11) };
            area = new TypingArea(); area.Finished += OnFinished;
            grid = Ui.Grid();
            tl.Controls.Add(cfg, 0, 0); tl.Controls.Add(txtPassage, 0, 1); tl.Controls.Add(area, 0, 2); tl.Controls.Add(grid, 0, 3);
            Controls.Add(tl);
        }

        public override void OnShow() { LoadHistory(); }

        void LoadHistory()
        {
            grid.DataSource = Db.Query("SELECT day AS Date, method AS Method, ROUND(duration_min,1) AS Min, ROUND(gross_wpm,1) AS Gross, ROUND(net_wpm,1) AS Net, ROUND(accuracy,1) AS Acc, errors AS Errors, backspaces AS Backspace, ROUND(words) AS Words, CASE completed WHEN 1 THEN 'Yes' ELSE 'No' END AS Done, source AS Source FROM sessions WHERE language=@0 AND kind='Practice' ORDER BY id DESC LIMIT 100", lang);
        }

        void StartClick()
        {
            bool ext = cbMode.SelectedIndex == 1;
            if (!ext && string.IsNullOrWhiteSpace(txtPassage.Text)) { Ui.Warn("Paste or type a passage in the box above first (or choose External timer mode)."); return; }
            btnStart.Enabled = false; btnStop.Enabled = true;
            area.Start(txtPassage.Text, int.Parse(cbDur.Text) * 60, lang, ext);
        }

        void StopClick()
        {
            if (!area.Running) return;
            if (Ui.Ask("Stop now? The partial session will be saved as 'not completed'.")) area.Abort();
        }

        void OnFinished(TypingResult r, bool full)
        {
            btnStart.Enabled = true; btnStop.Enabled = false;
            SessionRec s;
            if (r.External)
            {
                var t = new SessionRec { Language = lang, Kind = "Practice", Method = cbMethod.Text, Duration = Math.Max(1, Math.Round(r.Seconds / 60.0)), Completed = full, Looked = chkLooked.Checked };
                s = EntryDialog.Ask(FindForm(), t);
                if (s == null) return;
            }
            else
            {
                if (r.Chars == 0) { Ui.Info("Nothing was typed, so no session was saved."); return; }
                s = new SessionRec { Ts = DateTime.Now, Language = lang, Kind = "Practice", Method = cbMethod.Text, Source = "Built-in typing module",
                    Duration = Math.Round(r.Seconds / 60.0, 2), Gross = r.Gross, Net = r.Net, Accuracy = r.Accuracy, Errors = r.Errors,
                    Backspaces = r.Backspaces, Words = r.Words, Looked = chkLooked.Checked, Completed = full };
            }
            Flow.Complete(FindForm(), s);
            LoadHistory();
        }
    }

    // Typing Drills and Typing Tests: manual entry of results from Logic / Sonu / other tutors.
    class EntryPage : Page
    {
        string kind; EntryPanel panel; DataGridView grid;

        public EntryPage(string kind)
        {
            this.kind = kind;
            bool drill = kind == "Drill";
            var tl = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, BackColor = Theme.Bg };
            tl.RowStyles.Add(new RowStyle(SizeType.AutoSize)); tl.RowStyles.Add(new RowStyle(SizeType.AutoSize)); tl.RowStyles.Add(new RowStyle(SizeType.AutoSize)); tl.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            var h = Ui.Lbl(drill ? "TYPING DRILLS - accuracy, difficult words, finger position (minimum 5 minutes)" : "TYPING TESTS - results from Logic / Sonu / other tutors, and 10-minute exam simulation", 13, Theme.Gold, true);
            panel = drill
                ? new EntryPanel("Drill", new[] { "Logic Typing Tutor", "Sonu Typing Tutor" }, "Software", 5, "External: {0}")
                : new EntryPanel("Test", new[] { "Logic Typing Tutor", "Sonu Typing Tutor", "Other Typing Tutor" }, "Software", 1, "External: {0}");
            panel.SaveRequested += s => { Flow.Complete(FindForm(), s); panel.ClearFields(); LoadHistory(); };
            var bar = Ui.Row();
            bar.Controls.Add(Ui.Lbl(drill ? "Drills do not need a passage - enter what the tutor reports." : "Internal exam simulations:", 10, Theme.Muted));
            if (!drill)
            {
                bar.Controls.Add(Ui.Btn("10-min ENGLISH EXAM", (s, e) => { using (var f = new ExamForm("English")) f.ShowDialog(FindForm()); LoadHistory(); }, true));
                bar.Controls.Add(Ui.Btn("10-min HINDI EXAM", (s, e) => { using (var f = new ExamForm("Hindi")) f.ShowDialog(FindForm()); LoadHistory(); }, true));
            }
            grid = Ui.Grid();
            tl.Controls.Add(h, 0, 0); tl.Controls.Add(panel, 0, 1); tl.Controls.Add(bar, 0, 2); tl.Controls.Add(grid, 0, 3);
            Controls.Add(tl);
        }

        public override void OnShow() { LoadHistory(); }

        void LoadHistory()
        {
            string where = kind == "Drill" ? "kind='Drill'" : "kind IN ('Test','Exam')";
            grid.DataSource = Db.Query("SELECT day AS Date, language AS Language, kind AS Type, method AS Software, ROUND(duration_min,1) AS Min, ROUND(gross_wpm,1) AS Gross, ROUND(net_wpm,1) AS Net, ROUND(accuracy,1) AS Acc, errors AS Errors, backspaces AS Backspace, ROUND(words) AS Words, source AS Source FROM sessions WHERE " + where + " ORDER BY id DESC LIMIT 200");
        }
    }
}
