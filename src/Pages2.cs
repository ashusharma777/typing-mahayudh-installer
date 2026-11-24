using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace TypingMahayudh
{
    class EfficiencyPage : Page
    {
        static readonly string[] Act = { "Teacher Lesson (10 min)", "Telegram Assignment (10-15 min)", "Efficiency Test (10 min)" };
        static readonly string[] Status = { "Not started", "In progress", "Submitted", "Checked" };
        DataGridView gW, gE, gC; Label lblSum; ProgressBar pbW, pbE, pbA; DateTimePicker dtStart; bool loading;
        Control[] views;

        public EfficiencyPage()
        {
            var tl = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, BackColor = Theme.Bg };
            tl.RowStyles.Add(new RowStyle(SizeType.AutoSize)); tl.RowStyles.Add(new RowStyle(SizeType.AutoSize)); tl.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            var top = Ui.Row();
            top.Controls.Add(Ui.Lbl("EFFICIENCY TEST - 30 sessions / 15 days (any order)", 13, Theme.Gold, true));
            top.Controls.Add(Ui.Btn("MS Word (15)", (s, e) => Show(0)));
            top.Controls.Add(Ui.Btn("MS Excel (15)", (s, e) => Show(1)));
            top.Controls.Add(Ui.Btn("15-day calendar", (s, e) => Show(2)));
            dtStart = Ui.Date(DateTime.Today);
            top.Controls.Add(Ui.Field("Day 1 of the 15-day plan", dtStart));
            top.Controls.Add(Ui.Btn("Set start date", (s, e) => { Cfg.Set("eff_start", dtStart.Value.ToString("yyyy-MM-dd", Db.Inv)); LoadCal(); }));
            var sum = Ui.Row();
            lblSum = Ui.Lbl("", 10, Theme.Text);
            pbW = new ProgressBar { Width = 150 }; pbE = new ProgressBar { Width = 150 }; pbA = new ProgressBar { Width = 150 };
            sum.Controls.Add(lblSum); sum.Controls.Add(Ui.Lbl("Word", 9, Theme.Muted)); sum.Controls.Add(pbW);
            sum.Controls.Add(Ui.Lbl("Excel", 9, Theme.Muted)); sum.Controls.Add(pbE); sum.Controls.Add(Ui.Lbl("Total", 9, Theme.Muted)); sum.Controls.Add(pbA);
            top.Controls.Add(sum);
            gW = BuildGrid("Word"); gE = BuildGrid("Excel"); gC = Ui.Grid();
            var host = new Panel { Dock = DockStyle.Fill };
            host.Controls.Add(gW); host.Controls.Add(gE); host.Controls.Add(gC);
            views = new Control[] { gW, gE, gC };
            tl.Controls.Add(top, 0, 0); tl.Controls.Add(new Label { Height = 1 }, 0, 1); tl.Controls.Add(host, 0, 2);
            Controls.Add(tl);
            Show(0);
        }

        void Show(int i) { for (int k = 0; k < 3; k++) views[k].Visible = k == i; if (i == 2) LoadCal(); }

        DataGridView BuildGrid(string subject)
        {
            var g = Ui.Grid(); g.ReadOnly = false; g.Tag = subject;
            g.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Session", ReadOnly = true, FillWeight = 40 });
            g.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Activity", ReadOnly = true, FillWeight = 120 });
            g.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "Done", FillWeight = 40 });
            g.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Date (yyyy-mm-dd)", FillWeight = 70 });
            g.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Minutes", FillWeight = 45 });
            g.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Topic", FillWeight = 100 });
            var cb = new DataGridViewComboBoxColumn { HeaderText = "Assignment status", FillWeight = 70, FlatStyle = FlatStyle.Flat }; cb.Items.AddRange(Status); g.Columns.Add(cb);
            g.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Mistakes", FillWeight = 100 });
            g.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Revision notes", FillWeight = 100 });
            g.CurrentCellDirtyStateChanged += (s, e) => { if (g.IsCurrentCellDirty) g.CommitEdit(DataGridViewDataErrorContexts.Commit); };
            g.CellValueChanged += (s, e) => { if (!loading && e.RowIndex >= 0) SaveRow(g, e.RowIndex); };
            g.DataError += (s, e) => { e.ThrowException = false; };
            return g;
        }

        void SaveRow(DataGridView g, int r)
        {
            var row = g.Rows[r]; var tag = (int[])row.Tag;
            bool done = row.Cells[2].Value != null && (bool)row.Cells[2].Value;
            string day = Convert.ToString(row.Cells[3].Value) ?? "";
            DateTime tmp;
            if (day != "" && !DateTime.TryParseExact(day, "yyyy-MM-dd", Db.Inv, DateTimeStyles.None, out tmp)) { Ui.Warn("Use the date format yyyy-mm-dd, e.g. 2026-11-25."); loading = true; row.Cells[3].Value = ""; loading = false; day = ""; }
            if (done && day == "") { day = DateTime.Today.ToString("yyyy-MM-dd", Db.Inv); loading = true; row.Cells[3].Value = day; loading = false; }
            int mins; int.TryParse(Convert.ToString(row.Cells[4].Value), out mins);
            Db.Exec("UPDATE efficiency SET done=@0,minutes=@1,day=@2,topic=@3,assign_status=@4,mistakes=@5,notes=@6 WHERE subject=@7 AND session_no=@8 AND activity=@9",
                done ? 1 : 0, mins, day, Convert.ToString(row.Cells[5].Value) ?? "", Convert.ToString(row.Cells[6].Value) ?? "Not started",
                Convert.ToString(row.Cells[7].Value) ?? "", Convert.ToString(row.Cells[8].Value) ?? "", (string)g.Tag, tag[0], tag[1]);
            Summary();
        }

        void LoadGrid(DataGridView g)
        {
            var t = Db.Query("SELECT * FROM efficiency WHERE subject=@0 ORDER BY session_no, activity", (string)g.Tag);
            g.Rows.Clear();
            foreach (DataRow r in t.Rows)
            {
                int n = Convert.ToInt32(r["session_no"]), a = Convert.ToInt32(r["activity"]);
                int idx = g.Rows.Add("Session " + n, Act[a - 1], Convert.ToInt32(r["done"]) == 1, Db.S(r["day"]), Db.S(r["minutes"]), Db.S(r["topic"]),
                    Status.Contains(Db.S(r["assign_status"])) ? Db.S(r["assign_status"]) : "Not started", Db.S(r["mistakes"]), Db.S(r["notes"]));
                g.Rows[idx].Tag = new[] { n, a };
            }
        }

        void Summary()
        {
            double w = Stats.EfficiencyFraction("Word"), e = Stats.EfficiencyFraction("Excel"), a = Stats.EfficiencyFraction(null);
            int sw = Convert.ToInt32(Db.Scalar("SELECT COUNT(*) FROM (SELECT session_no FROM efficiency WHERE subject='Word' GROUP BY session_no HAVING MIN(done)=1)"));
            int se = Convert.ToInt32(Db.Scalar("SELECT COUNT(*) FROM (SELECT session_no FROM efficiency WHERE subject='Excel' GROUP BY session_no HAVING MIN(done)=1)"));
            pbW.Value = (int)(w * 100); pbE.Value = (int)(e * 100); pbA.Value = (int)(a * 100);
            lblSum.Text = string.Format("Completed sessions - Word {0}/15 ({1:0}%), Excel {2}/15 ({3:0}%), pending {4}/30. Total {5:0}%   ", sw, w * 100, se, e * 100, 30 - sw - se, a * 100);
        }

        void LoadCal()
        {
            DateTime st; if (!DateTime.TryParseExact(Cfg.Get("eff_start"), "yyyy-MM-dd", Db.Inv, DateTimeStyles.None, out st)) st = DateTime.Today;
            var t = new DataTable();
            t.Columns.Add("Day"); t.Columns.Add("Date"); t.Columns.Add("Word activities done"); t.Columns.Add("Excel activities done"); t.Columns.Add("Plan (about 6 activities = 2 sessions/day)");
            for (int i = 0; i < 15; i++)
            {
                string d = st.AddDays(i).ToString("yyyy-MM-dd", Db.Inv);
                int w = Convert.ToInt32(Db.Scalar("SELECT COUNT(*) FROM efficiency WHERE done=1 AND subject='Word' AND day=@0", d));
                int e = Convert.ToInt32(Db.Scalar("SELECT COUNT(*) FROM efficiency WHERE done=1 AND subject='Excel' AND day=@0", d));
                t.Rows.Add("Day " + (i + 1), d, w, e, (w + e) >= 6 ? "Target met" : (st.AddDays(i).Date < DateTime.Today ? "Behind" : (st.AddDays(i).Date == DateTime.Today ? "Today" : "Upcoming")));
            }
            gC.DataSource = t;
        }

        public override void OnShow()
        {
            DateTime st; if (DateTime.TryParseExact(Cfg.Get("eff_start"), "yyyy-MM-dd", Db.Inv, DateTimeStyles.None, out st)) dtStart.Value = st;
            loading = true; LoadGrid(gW); LoadGrid(gE); loading = false; Summary();
        }
    }

    class PlannerPage : Page
    {
        DateTimePicker dt; DataGridView grid; ProgressBar pb; Label lblPct; ComboBox cbCat; TextBox txtTask; bool loading;
        static readonly string[] Typing = { "MS Word practice", "Book practice", "Newspaper practice", "Accuracy drills", "10-minute test" };
        static readonly string[] Eff = { "MS Word teacher lesson", "MS Word Telegram assignment", "MS Word efficiency test", "MS Excel teacher lesson", "MS Excel Telegram assignment", "MS Excel efficiency test" };

        public PlannerPage()
        {
            var tl = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, BackColor = Theme.Bg };
            tl.RowStyles.Add(new RowStyle(SizeType.AutoSize)); tl.RowStyles.Add(new RowStyle(SizeType.AutoSize)); tl.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            var top = Ui.Row();
            dt = Ui.Date(DateTime.Today); dt.ValueChanged += (s, e) => LoadDay();
            pb = new ProgressBar { Width = 200 }; lblPct = Ui.Lbl("", 12, Theme.Gold, true);
            top.Controls.Add(Ui.Lbl("DAILY PLANNER", 13, Theme.Gold, true)); top.Controls.Add(Ui.Field("Date", dt));
            top.Controls.Add(Ui.Field("Completion", pb)); top.Controls.Add(lblPct);
            var add = Ui.Row();
            cbCat = Ui.Combo(120, "English", "Hindi", "Efficiency", "Custom"); txtTask = Ui.Txt(320);
            add.Controls.Add(Ui.Field("Category", cbCat)); add.Controls.Add(Ui.Field("Custom task", txtTask));
            add.Controls.Add(Ui.Btn("Add task", (s, e) => AddTask())); add.Controls.Add(Ui.Btn("Delete selected custom task", (s, e) => DelTask()));
            grid = Ui.Grid(); grid.ReadOnly = false;
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Category", ReadOnly = true, FillWeight = 30 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Task", ReadOnly = true, FillWeight = 100 });
            grid.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "Done", FillWeight = 20 });
            grid.CurrentCellDirtyStateChanged += (s, e) => { if (grid.IsCurrentCellDirty) grid.CommitEdit(DataGridViewDataErrorContexts.Commit); };
            grid.CellValueChanged += (s, e) =>
            {
                if (loading || e.RowIndex < 0 || e.ColumnIndex != 2) return;
                var row = grid.Rows[e.RowIndex];
                Db.Exec("UPDATE tasks SET done=@0 WHERE id=@1", (bool)row.Cells[2].Value ? 1 : 0, (long)row.Tag); Pct();
            };
            tl.Controls.Add(top, 0, 0); tl.Controls.Add(add, 0, 1); tl.Controls.Add(grid, 0, 2);
            Controls.Add(tl);
        }

        string Day() { return dt.Value.ToString("yyyy-MM-dd", Db.Inv); }

        void LoadDay()
        {
            string d = Day();
            if (Convert.ToInt32(Db.Scalar("SELECT COUNT(*) FROM tasks WHERE day=@0", d)) == 0)
            {
                foreach (var c in new[] { "English", "Hindi" }) foreach (var t in Typing) Db.Exec("INSERT INTO tasks(day,category,title) VALUES(@0,@1,@2)", d, c, t);
                foreach (var t in Eff) Db.Exec("INSERT INTO tasks(day,category,title) VALUES(@0,'Efficiency',@1)", d, t);
            }
            loading = true; grid.Rows.Clear();
            foreach (DataRow r in Db.Query("SELECT * FROM tasks WHERE day=@0 ORDER BY id", d).Rows)
            {
                int i = grid.Rows.Add(Db.S(r["category"]), Db.S(r["title"]), Convert.ToInt32(r["done"]) == 1);
                grid.Rows[i].Tag = Convert.ToInt64(r["id"]); grid.Rows[i].Cells[2].Tag = Convert.ToInt32(r["custom"]);
            }
            loading = false; Pct();
        }

        void Pct()
        {
            int n = grid.Rows.Count, d = 0;
            foreach (DataGridViewRow r in grid.Rows) if (r.Cells[2].Value != null && (bool)r.Cells[2].Value) d++;
            int p = n == 0 ? 0 : d * 100 / n; pb.Value = p; lblPct.Text = p + "%  (" + d + "/" + n + ")";
        }

        void AddTask()
        {
            if (txtTask.Text.Trim() == "") { Ui.Warn("Type the task first."); return; }
            Db.Exec("INSERT INTO tasks(day,category,title,custom) VALUES(@0,@1,@2,1)", Day(), cbCat.Text, txtTask.Text.Trim());
            txtTask.Text = ""; LoadDay();
        }

        void DelTask()
        {
            if (grid.CurrentRow == null) return;
            if (Convert.ToInt32(grid.CurrentRow.Cells[2].Tag) != 1) { Ui.Info("Only custom tasks can be deleted."); return; }
            Db.Exec("DELETE FROM tasks WHERE id=@0", (long)grid.CurrentRow.Tag); LoadDay();
        }

        public override void OnShow() { LoadDay(); }
    }

    class ProgressPage : Page
    {
        ChartControl chart; DataGridView grid; DateTimePicker dFrom, dTo; ComboBox cbLang, cbMethod, cbKind, cbChart; bool ready;
        static readonly string[] Charts = { "Daily WPM (net)", "Weekly WPM (net)", "Accuracy progression", "Daily word count", "Total cumulative words", "Error reduction (avg errors/session)", "Backspace usage (avg/session)", "Practice duration (min/day)", "English vs Hindi - best net WPM so far" };

        public ProgressPage()
        {
            var tl = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, BackColor = Theme.Bg };
            tl.RowStyles.Add(new RowStyle(SizeType.AutoSize)); tl.RowStyles.Add(new RowStyle(SizeType.Percent, 55)); tl.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
            var top = Ui.Row();
            dFrom = Ui.Date(DateTime.Today.AddDays(-60)); dTo = Ui.Date(DateTime.Today);
            cbLang = Ui.Combo(100, "All", "English", "Hindi");
            cbMethod = Ui.Combo(170, "All", "MS Word", "Book", "Newspaper", "Custom passage", "Logic Typing Tutor", "Sonu Typing Tutor", "Other Typing Tutor", "Exam Simulation");
            cbKind = Ui.Combo(100, "All", "Practice", "Drill", "Test", "Exam");
            cbChart = Ui.Combo(300, Charts);
            top.Controls.Add(Ui.Lbl("PROGRESS TRACKER", 13, Theme.Gold, true));
            top.Controls.Add(Ui.Field("Chart", cbChart)); top.Controls.Add(Ui.Field("From", dFrom)); top.Controls.Add(Ui.Field("To", dTo));
            top.Controls.Add(Ui.Field("Language", cbLang)); top.Controls.Add(Ui.Field("Method / software", cbMethod)); top.Controls.Add(Ui.Field("Type", cbKind));
            top.Controls.Add(Ui.Btn("Delete selected session", (s, e) => DelSel()));
            chart = new ChartControl { Dock = DockStyle.Fill }; grid = Ui.Grid();
            tl.Controls.Add(top, 0, 0); tl.Controls.Add(chart, 0, 1); tl.Controls.Add(grid, 0, 2);
            Controls.Add(tl);
            foreach (var c in new Control[] { dFrom, dTo }) ((DateTimePicker)c).ValueChanged += (s, e) => Reload();
            foreach (var c in new[] { cbLang, cbMethod, cbKind, cbChart }) c.SelectedIndexChanged += (s, e) => Reload();
            ready = true;
        }

        public override void OnShow() { Reload(); }

        DataTable Fetch()
        {
            var sql = new StringBuilder("SELECT * FROM sessions WHERE day>=@0 AND day<=@1"); var p = new List<object> { dFrom.Value.ToString("yyyy-MM-dd", Db.Inv), dTo.Value.ToString("yyyy-MM-dd", Db.Inv) };
            if (cbLang.Text != "All") { sql.Append(" AND language=@" + p.Count); p.Add(cbLang.Text); }
            if (cbMethod.Text != "All") { sql.Append(" AND method=@" + p.Count); p.Add(cbMethod.Text); }
            if (cbKind.Text != "All") { sql.Append(" AND kind=@" + p.Count); p.Add(cbKind.Text); }
            sql.Append(" ORDER BY ts, id");
            return Db.Query(sql.ToString(), p.ToArray());
        }

        static string WeekKey(string day)
        {
            var d = DateTime.ParseExact(day, "yyyy-MM-dd", Db.Inv);
            return d.AddDays(-(((int)d.DayOfWeek + 6) % 7)).ToString("yyyy-MM-dd", Db.Inv);
        }

        void Reload()
        {
            if (!ready) return;
            var t = Fetch(); var rows = t.Rows.Cast<DataRow>().ToList();
            grid.DataSource = Db.Query(string.Join("", new[] { "SELECT id AS ID, day AS Date, language AS Language, kind AS Type, method AS Method, ROUND(duration_min,1) AS Min, ROUND(gross_wpm,1) AS Gross, ROUND(net_wpm,1) AS Net, ROUND(accuracy,1) AS Acc, errors AS Errors, backspaces AS Backspace, ROUND(words) AS Words, CASE looked WHEN 1 THEN 'Yes' ELSE '' END AS Looked, mistakes AS Mistakes, notes AS Notes, source AS Source FROM sessions WHERE id IN (" + (rows.Count == 0 ? "-1" : string.Join(",", rows.Select(r => Convert.ToString(r["id"], Db.Inv)))) + ") ORDER BY id DESC" }));
            int ci = cbChart.SelectedIndex; chart.Title = Charts[ci]; chart.Bars = ci == 3 || ci == 7;
            Func<DataRow, string> dayOf = r => Db.S(r["day"]);
            Func<DataRow, string> keyOf = ci == 1 ? (Func<DataRow, string>)(r => WeekKey(dayOf(r))) : dayOf;
            var keys = rows.Select(keyOf).Distinct().OrderBy(x => x).ToList();
            chart.Labels = keys.Select(k => k.Length >= 10 ? k.Substring(5) : k).ToList();
            chart.Data = new List<Series>();
            if (keys.Count == 0) { chart.Invalidate(); return; }

            if (ci == 4)
            {
                double run = 0; var v = new double?[keys.Count];
                for (int i = 0; i < keys.Count; i++) { run += rows.Where(r => keyOf(r) == keys[i]).Sum(r => Db.D(r["words"])); v[i] = run; }
                chart.Data.Add(new Series { Name = "Total words", Color = Theme.Gold, V = v });
            }
            else
            {
                foreach (var lang in new[] { "English", "Hindi" })
                {
                    var lr = rows.Where(r => Db.S(r["language"]) == lang).ToList(); if (lr.Count == 0) continue;
                    var v = new double?[keys.Count]; double best = 0;
                    for (int i = 0; i < keys.Count; i++)
                    {
                        var g = lr.Where(r => keyOf(r) == keys[i]).ToList();
                        if (ci == 8) { if (g.Count > 0) best = Math.Max(best, g.Max(r => Db.D(r["net_wpm"]))); v[i] = best > 0 ? (double?)best : null; continue; }
                        if (g.Count == 0) continue;
                        switch (ci)
                        {
                            case 0: case 1: v[i] = g.Average(r => Db.D(r["net_wpm"])); break;
                            case 2: v[i] = g.Average(r => Db.D(r["accuracy"])); break;
                            case 3: v[i] = g.Sum(r => Db.D(r["words"])); break;
                            case 5: v[i] = g.Average(r => Db.D(r["errors"])); break;
                            case 6: v[i] = g.Average(r => Db.D(r["backspaces"])); break;
                            case 7: v[i] = g.Sum(r => Db.D(r["duration_min"])); break;
                        }
                    }
                    chart.Data.Add(new Series { Name = lang, Color = lang == "English" ? Theme.Teal : Theme.Gold, V = v });
                }
            }
            chart.Invalidate();
        }

        void DelSel()
        {
            if (grid.CurrentRow == null) return;
            long id = Convert.ToInt64(grid.CurrentRow.Cells["ID"].Value);
            if (!Ui.Ask("Delete session #" + id + " permanently? (Unlocked achievements stay unlocked.)")) return;
            Db.Exec("DELETE FROM sessions WHERE id=@0", id); Reload();
        }
    }

    class MistakePage : Page
    {
        DataGridView gTop, gLog; ComboBox cbLang, cbKind; TextBox txtItem, txtNote; DateTimePicker dt; Label lblIns;

        public MistakePage()
        {
            var tl = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, BackColor = Theme.Bg };
            tl.RowStyles.Add(new RowStyle(SizeType.AutoSize)); tl.RowStyles.Add(new RowStyle(SizeType.Percent, 40)); tl.RowStyles.Add(new RowStyle(SizeType.Percent, 35)); tl.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
            var top = Ui.Row();
            dt = Ui.Date(DateTime.Today); cbLang = Ui.Combo(90, "English", "Hindi");
            cbKind = Ui.Combo(220, "Key often mistyped", "Difficult word", "Letter repeatedly wrong", "Punctuation", "Accuracy drops here", "Looked at keyboard", "Speed hurting accuracy");
            txtItem = Ui.Txt(180); txtNote = Ui.Txt(260);
            top.Controls.Add(Ui.Lbl("MISTAKE ANALYSIS", 13, Theme.Gold, true)); top.Controls.Add(Ui.Field("Date", dt)); top.Controls.Add(Ui.Field("Language", cbLang));
            top.Controls.Add(Ui.Field("Kind", cbKind)); top.Controls.Add(Ui.Field("Key / word / letter", txtItem)); top.Controls.Add(Ui.Field("Note", txtNote));
            top.Controls.Add(Ui.Btn("Add mistake", (s, e) => Add(), true)); top.Controls.Add(Ui.Btn("Delete selected log entry", (s, e) => Del()));
            gTop = Ui.Grid(); gLog = Ui.Grid();
            lblIns = new Label { Dock = DockStyle.Fill, ForeColor = Theme.Text, Font = Theme.F(10), BackColor = Theme.Panel, Padding = new Padding(10) };
            tl.Controls.Add(top, 0, 0); tl.Controls.Add(gTop, 0, 1); tl.Controls.Add(gLog, 0, 2); tl.Controls.Add(lblIns, 0, 3);
            Controls.Add(tl);
        }

        void Add()
        {
            if (txtItem.Text.Trim() == "") { Ui.Warn("Enter the key, word or letter."); return; }
            Db.Exec("INSERT INTO mistakes(day,language,kind,item,note) VALUES(@0,@1,@2,@3,@4)", dt.Value.ToString("yyyy-MM-dd", Db.Inv), cbLang.Text, cbKind.Text, txtItem.Text.Trim(), txtNote.Text.Trim());
            txtItem.Text = ""; txtNote.Text = ""; OnShow();
        }

        void Del()
        {
            if (gLog.CurrentRow == null) return;
            Db.Exec("DELETE FROM mistakes WHERE id=@0", Convert.ToInt64(gLog.CurrentRow.Cells["ID"].Value)); OnShow();
        }

        public override void OnShow()
        {
            string recent = DateTime.Today.AddDays(-6).ToString("yyyy-MM-dd", Db.Inv), prior = DateTime.Today.AddDays(-13).ToString("yyyy-MM-dd", Db.Inv);
            var t = Db.Query("SELECT MIN(item) AS Item, kind AS Kind, language AS Language, COUNT(*) AS Total, SUM(CASE WHEN day>=@0 THEN 1 ELSE 0 END) AS Last7, SUM(CASE WHEN day<@0 AND day>=@1 THEN 1 ELSE 0 END) AS Prev7, MAX(day) AS LastSeen FROM mistakes GROUP BY LOWER(item), kind, language ORDER BY COUNT(*) DESC LIMIT 10", recent, prior);
            t.Columns.Add("Trend");
            foreach (DataRow r in t.Rows)
            {
                double a = Db.D(r["Last7"]), b = Db.D(r["Prev7"]);
                r["Trend"] = a < b ? "Improving" : (a > b ? "Increasing" : "Steady");
            }
            gTop.DataSource = t;
            gLog.DataSource = Db.Query("SELECT id AS ID, day AS Date, language AS Language, kind AS Kind, item AS Item, note AS Note FROM mistakes ORDER BY id DESC LIMIT 300");
            var sb = new StringBuilder("KEYBOARD DISCIPLINE & INSIGHTS\r\n");
            string d30 = DateTime.Today.AddDays(-30).ToString("yyyy-MM-dd", Db.Inv);
            var k = Db.Query("SELECT COUNT(*), IFNULL(SUM(looked),0) FROM sessions WHERE day>=@0", d30);
            double n = Db.D(k.Rows[0][0]), lk = Db.D(k.Rows[0][1]);
            sb.Append(n == 0 ? "No sessions in the last 30 days.\r\n" : string.Format("Looked at keyboard in {0:0} of {1:0} sessions in the last 30 days ({2:0}%).\r\n", lk, n, lk / n * 100));
            var a1 = Db.Query("SELECT AVG(accuracy) FROM sessions WHERE words>0 AND duration_min<=10"); var a2 = Db.Query("SELECT AVG(accuracy) FROM sessions WHERE words>0 AND duration_min>10");
            if (a1.Rows[0][0] != DBNull.Value && a2.Rows[0][0] != DBNull.Value)
                sb.Append(string.Format("Average accuracy: {0:0.0}% in sessions up to 10 min vs {1:0.0}% in longer sessions", Db.D(a1.Rows[0][0]), Db.D(a2.Rows[0][0])) + (Db.D(a2.Rows[0][0]) < Db.D(a1.Rows[0][0]) - 2 ? " - accuracy drops as sessions get longer.\r\n" : ".\r\n"));
            var sp = Db.Query("SELECT gross_wpm, accuracy FROM sessions WHERE words>0");
            if (sp.Rows.Count >= 5)
            {
                double mx = sp.AsEnumerable().Average(r => Db.D(r[0])), my = sp.AsEnumerable().Average(r => Db.D(r[1]));
                double sxy = sp.AsEnumerable().Sum(r => (Db.D(r[0]) - mx) * (Db.D(r[1]) - my)), sxx = sp.AsEnumerable().Sum(r => Math.Pow(Db.D(r[0]) - mx, 2)), syy = sp.AsEnumerable().Sum(r => Math.Pow(Db.D(r[1]) - my, 2));
                if (sxx > 0 && syy > 0)
                {
                    double c = sxy / Math.Sqrt(sxx * syy);
                    sb.Append(c < -0.3 ? "Speed vs accuracy: accuracy tends to FALL when your speed rises - slow down slightly.\r\n" : "Speed vs accuracy: no clear sign that speed is hurting accuracy.\r\n");
                }
            }
            int pun = Convert.ToInt32(Db.Scalar("SELECT COUNT(*) FROM mistakes WHERE kind='Punctuation'"));
            sb.Append("Punctuation mistakes logged: " + pun + ".\r\n\r\n");
            sb.Append("Reminders: do not look at the keyboard | eyes on screen | focus on words | correct finger position | avoid needless backspace | never trade accuracy for speed | comfortable rhythm.");
            lblIns.Text = sb.ToString();
        }
    }

    class AchievementsPage : Page
    {
        DataGridView grid; Label lblTot; ProgressBar pb;
        public AchievementsPage()
        {
            var tl = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, BackColor = Theme.Bg };
            tl.RowStyles.Add(new RowStyle(SizeType.AutoSize)); tl.RowStyles.Add(new RowStyle(SizeType.AutoSize)); tl.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            lblTot = Ui.Lbl("", 12, Theme.Gold, true); pb = new ProgressBar { Width = 500, Height = 22 };
            grid = Ui.Grid(); grid.DefaultCellStyle.Font = new Font("Segoe UI", 13);
            var top = Ui.Row(); top.Controls.Add(Ui.Lbl("ACHIEVEMENTS", 13, Theme.Gold, true));
            tl.Controls.Add(top, 0, 0);
            var mid = Ui.Row(); mid.Controls.Add(lblTot); mid.Controls.Add(pb); tl.Controls.Add(mid, 0, 1); tl.Controls.Add(grid, 0, 2);
            Controls.Add(tl);
        }
        public override void OnShow()
        {
            double total = Stats.TotalWords(); int ni = Milestones.NextIndex(total);
            lblTot.Text = ni < 0 ? string.Format("Total {0:N0} words - ALL UNLOCKED", total) : string.Format("Total {0:N0} words | next {1:N0} | remaining {2:N0}", total, Milestones.Words[ni], Milestones.Words[ni] - total);
            double lo = ni <= 0 ? 0 : Milestones.Words[ni - 1];
            pb.Value = ni < 0 ? 100 : (int)Math.Max(0, Math.Min(100, (total - lo) / (Milestones.Words[ni] - lo) * 100));
            var t = new DataTable(); t.Columns.Add("Words"); t.Columns.Add("Title"); t.Columns.Add("Status"); t.Columns.Add("Unlocked on");
            for (int i = 0; i < Milestones.Words.Length; i++)
            {
                var o = Db.Scalar("SELECT unlocked FROM achievements WHERE words=@0", Milestones.Words[i]);
                t.Rows.Add(Milestones.Words[i].ToString("N0"), Milestones.Titles[i], o != null ? "UNLOCKED" : "Locked", o == null ? "" : Db.S(o));
            }
            grid.DataSource = t;
        }
    }

    class ReportsPage : Page
    {
        ComboBox cbPeriod; TextBox txt;
        public ReportsPage()
        {
            var tl = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = Theme.Bg };
            tl.RowStyles.Add(new RowStyle(SizeType.AutoSize)); tl.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            var top = Ui.Row(); cbPeriod = Ui.Combo(170, "Last 7 days", "This week (Mon-Sun)", "This month", "Last 30 days", "All time");
            top.Controls.Add(Ui.Lbl("REPORTS", 13, Theme.Gold, true)); top.Controls.Add(Ui.Field("Period", cbPeriod));
            top.Controls.Add(Ui.Btn("Generate", (s, e) => Gen(), true));
            top.Controls.Add(Ui.Btn("Save report (.txt)", (s, e) => SaveTxt()));
            top.Controls.Add(Ui.Btn("Export sessions CSV", (s, e) => Exp("SELECT * FROM sessions ORDER BY id", "sessions")));
            top.Controls.Add(Ui.Btn("Export mistakes CSV", (s, e) => Exp("SELECT * FROM mistakes ORDER BY id", "mistakes")));
            top.Controls.Add(Ui.Btn("Export efficiency CSV", (s, e) => Exp("SELECT * FROM efficiency ORDER BY subject, session_no, activity", "efficiency")));
            txt = new TextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, BackColor = Theme.Panel, ForeColor = Theme.Text, Font = new Font("Consolas", 10.5f) };
            tl.Controls.Add(top, 0, 0); tl.Controls.Add(txt, 0, 1); Controls.Add(tl);
        }
        public override void OnShow() { Gen(); }

        void Exp(string sql, string name)
        {
            using (var d = new SaveFileDialog { Filter = "CSV (Excel compatible)|*.csv", FileName = "mahayudh_" + name + "_" + DateTime.Today.ToString("yyyyMMdd") + ".csv" })
                if (d.ShowDialog() == DialogResult.OK) { try { Csv.Write(Db.Query(sql), d.FileName); Ui.Info("Saved: " + d.FileName); } catch (Exception ex) { Ui.Warn("Could not write file: " + ex.Message); } }
        }
        void SaveTxt()
        {
            using (var d = new SaveFileDialog { Filter = "Text|*.txt", FileName = "mahayudh_report_" + DateTime.Today.ToString("yyyyMMdd") + ".txt" })
                if (d.ShowDialog() == DialogResult.OK) { try { File.WriteAllText(d.FileName, txt.Text, new UTF8Encoding(true)); } catch (Exception ex) { Ui.Warn(ex.Message); } }
        }

        void Gen()
        {
            DateTime to = DateTime.Today, from;
            switch (cbPeriod.SelectedIndex)
            {
                case 0: from = to.AddDays(-6); break;
                case 1: from = to.AddDays(-(((int)to.DayOfWeek + 6) % 7)); break;
                case 2: from = new DateTime(to.Year, to.Month, 1); break;
                case 3: from = to.AddDays(-29); break;
                default: from = new DateTime(2000, 1, 1); break;
            }
            string f = from.ToString("yyyy-MM-dd", Db.Inv), t = to.ToString("yyyy-MM-dd", Db.Inv);
            var sb = new StringBuilder();
            sb.AppendLine("TYPING MAHAYUDH - PROGRESS REPORT"); sb.AppendLine("Period: " + (cbPeriod.SelectedIndex == 4 ? "all time" : f + " to " + t)); sb.AppendLine("Days to exam: " + Math.Max(0, (Cfg.ExamDate - DateTime.Today).Days)); sb.AppendLine(new string('-', 60));
            var all = Db.Query("SELECT COUNT(*), IFNULL(SUM(words),0), IFNULL(SUM(duration_min),0), COUNT(DISTINCT day) FROM sessions WHERE day>=@0 AND day<=@1", f, t).Rows[0];
            sb.AppendLine(string.Format("Sessions: {0}   Words: {1:N0}   Minutes: {2:N0}   Active days: {3}", all[0], Db.D(all[1]), Db.D(all[2]), all[3]));
            foreach (var lang in new[] { "English", "Hindi" })
            {
                var r = Db.Query("SELECT COUNT(*), AVG(net_wpm), MAX(net_wpm), AVG(accuracy), IFNULL(SUM(words),0), AVG(backspaces), AVG(errors) FROM sessions WHERE language=@0 AND day>=@1 AND day<=@2 AND words>0", lang, f, t).Rows[0];
                sb.AppendLine(); sb.AppendLine(lang.ToUpper() + " (target " + (lang == "Hindi" ? Cfg.GetD("hindi_target") : Cfg.GetD("eng_target")).ToString("0") + " WPM)");
                if (Convert.ToInt32(r[0]) == 0) { sb.AppendLine("  no sessions in this period"); continue; }
                sb.AppendLine(string.Format("  sessions {0} | avg net {1:0.0} | best net {2:0.0} | avg accuracy {3:0.0}% | words {4:N0} | avg backspace {5:0.0} | avg errors {6:0.0}", r[0], Db.D(r[1]), Db.D(r[2]), Db.D(r[3]), Db.D(r[4]), Db.D(r[5]), Db.D(r[6])));
            }
            sb.AppendLine(); sb.AppendLine("EFFICIENCY (all time)");
            sb.AppendLine(string.Format("  Word {0:0}% | Excel {1:0}% | Total {2:0}% of activities completed", Stats.EfficiencyFraction("Word") * 100, Stats.EfficiencyFraction("Excel") * 100, Stats.EfficiencyFraction(null) * 100));
            sb.AppendLine(); sb.AppendLine("TOP RECURRING MISTAKES");
            var m = Db.Query("SELECT MIN(item), COUNT(*) FROM mistakes WHERE day>=@0 AND day<=@1 GROUP BY LOWER(item) ORDER BY COUNT(*) DESC LIMIT 5", f, t);
            if (m.Rows.Count == 0) sb.AppendLine("  none logged"); foreach (DataRow r in m.Rows) sb.AppendLine("  " + Db.S(r[0]) + "  x" + Db.S(r[1]));
            sb.AppendLine(); sb.AppendLine("Total words overall: " + Stats.TotalWords().ToString("N0"));
            txt.Text = sb.ToString().Replace("\r\n", "\n").Replace("\n", "\r\n");
        }
    }

    class SettingsPage : Page
    {
        Dictionary<string, Control> f = new Dictionary<string, Control>(); Label lblPath;
        public SettingsPage()
        {
            var fl = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, WrapContents = true, BackColor = Theme.Bg };
            fl.Controls.Add(new Label { Text = "SETTINGS", ForeColor = Theme.Gold, Font = Theme.F(13, true), AutoSize = false, Width = 1000, Height = 30 });
            Add(fl, "exam_date", "Exam date (yyyy-mm-dd)", Ui.Txt(110)); Add(fl, "eng_target", "English target WPM", Ui.Num(1, 200, 55, 0, 70)); Add(fl, "hindi_target", "Hindi target WPM", Ui.Num(1, 200, 44, 0, 70));
            Add(fl, "eng_wl", "English avg word length", Ui.Num(3, 12, 6.5m, 2, 70)); Add(fl, "hindi_wl", "Hindi avg word length", Ui.Num(3, 12, 5.75m, 2, 70));
            Add(fl, "acc_thresh", "Low-accuracy popup below (%)", Ui.Num(1, 100, 85, 0, 70)); Add(fl, "bs_factor", "Backspace popup when rate x times average", Ui.Num(1, 10, 1.5m, 1, 70));
            Add(fl, "speed_drop", "Speed popup when below recent avg by (%)", Ui.Num(1, 90, 20, 0, 70)); Add(fl, "improve_pts", "Celebrate accuracy gain of (points)", Ui.Num(1, 50, 3, 0, 70));
            var p = new CheckBox { Text = "Funny popups", ForeColor = Theme.Text, AutoSize = true, Margin = new Padding(10, 22, 6, 0) }; f["popups"] = p; fl.Controls.Add(p);
            var s = new CheckBox { Text = "Sounds", ForeColor = Theme.Text, AutoSize = true, Margin = new Padding(10, 22, 6, 0) }; f["sound"] = s; fl.Controls.Add(s);
            var a = new CheckBox { Text = "Auto backup on exit", ForeColor = Theme.Text, AutoSize = true, Margin = new Padding(10, 22, 6, 0) }; f["autobackup"] = a; fl.Controls.Add(a);
            var bar = Ui.Row(); bar.Dock = DockStyle.None; bar.Width = 1100;
            bar.Controls.Add(Ui.Btn("SAVE SETTINGS", (x, y) => SaveAll(), true));
            bar.Controls.Add(Ui.Btn("Backup now...", (x, y) => Backup()));
            bar.Controls.Add(Ui.Btn("Restore backup...", (x, y) => Restore()));
            bar.Controls.Add(Ui.Btn("Open backup folder", (x, y) => Process.Start("explorer.exe", Db.BackupDir)));
            fl.Controls.Add(bar);
            lblPath = Ui.Lbl("", 9, Theme.Muted); lblPath.MaximumSize = new Size(1000, 0); fl.Controls.Add(lblPath);
            Controls.Add(fl);
        }
        void Add(FlowLayoutPanel fl, string key, string label, Control c) { f[key] = c; fl.Controls.Add(Ui.Field(label, c)); }

        public override void OnShow()
        {
            foreach (var kv in f)
            {
                string v = Cfg.Get(kv.Key);
                if (kv.Value is CheckBox) ((CheckBox)kv.Value).Checked = v == "1";
                else if (kv.Value is NumericUpDown) { decimal d; if (decimal.TryParse(v, NumberStyles.Float, Db.Inv, out d)) ((NumericUpDown)kv.Value).Value = Math.Min(((NumericUpDown)kv.Value).Maximum, Math.Max(((NumericUpDown)kv.Value).Minimum, d)); }
                else kv.Value.Text = v;
            }
            lblPath.Text = "Database: " + Db.FilePath + "\r\nAuto backups (last 20 kept): " + Db.BackupDir + "\r\nProgress is saved to the database the moment each session, task or entry is saved.";
        }

        void SaveAll()
        {
            DateTime tmp;
            if (!DateTime.TryParseExact(f["exam_date"].Text, "yyyy-MM-dd", Db.Inv, DateTimeStyles.None, out tmp)) { Ui.Warn("Exam date must look like 2026-12-21."); return; }
            foreach (var kv in f)
            {
                if (kv.Value is CheckBox) Cfg.Set(kv.Key, ((CheckBox)kv.Value).Checked ? "1" : "0");
                else if (kv.Value is NumericUpDown) Cfg.Set(kv.Key, ((NumericUpDown)kv.Value).Value.ToString(Db.Inv));
                else Cfg.Set(kv.Key, kv.Value.Text.Trim());
            }
            Ui.Info("Settings saved.");
        }

        void Backup()
        {
            using (var d = new SaveFileDialog { Filter = "Typing Mahayudh backup|*.db", FileName = "manual_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".db" })
                if (d.ShowDialog() == DialogResult.OK) { try { BackupMgr.Make(d.FileName); Ui.Info("Backup saved:\r\n" + d.FileName); } catch (Exception ex) { Ui.Warn("Backup failed: " + ex.Message); } }
        }

        void Restore()
        {
            using (var d = new OpenFileDialog { Filter = "Typing Mahayudh backup|*.db", InitialDirectory = Db.BackupDir })
            {
                if (d.ShowDialog() != DialogResult.OK) return;
                if (!Ui.Ask("Replace ALL current data with this backup? (A safety copy of the current data is made first.) The app will restart.")) return;
                string err;
                if (BackupMgr.Restore(d.FileName, out err)) { Ui.Info("Restored. The app will restart now."); Application.Restart(); }
                else Ui.Warn("Restore failed: " + err);
            }
        }
    }
}
