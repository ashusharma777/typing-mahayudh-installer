using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace TypingMahayudh
{
    class Series { public string Name; public Color Color; public double?[] V; }

    class ChartControl : Control
    {
        public List<string> Labels = new List<string>();
        public List<Series> Data = new List<Series>();
        public bool Bars;
        public string Title = "";

        public ChartControl() { SetStyle(ControlStyles.AllPaintingFlags | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true); BackColor = Theme.Panel; }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; g.Clear(Theme.Panel);
            using (var tf = Theme.F(11, true)) using (var gb = new SolidBrush(Theme.Gold)) g.DrawString(Title, tf, gb, 10, 6);
            if (Labels.Count == 0 || Data.Count == 0)
            {
                using (var f = Theme.F(11)) using (var b = new SolidBrush(Theme.Muted))
                    g.DrawString("No saved sessions match this filter yet.", f, b, 20, Height / 2);
                return;
            }
            var plot = new Rectangle(55, 40, Math.Max(10, Width - 80), Math.Max(10, Height - 90));
            double max = 0;
            foreach (var s in Data) foreach (var v in s.V) if (v.HasValue && v.Value > max) max = v.Value;
            if (max <= 0) max = 1;
            max *= 1.1;
            using (var pen = new Pen(Theme.Line)) using (var f = Theme.F(8)) using (var mb = new SolidBrush(Theme.Muted))
            {
                for (int i = 0; i <= 5; i++)
                {
                    int y = plot.Bottom - i * plot.Height / 5;
                    g.DrawLine(pen, plot.Left, y, plot.Right, y);
                    g.DrawString((max * i / 5).ToString("0.#"), f, mb, 5, y - 8);
                }
                int n = Labels.Count, step = Math.Max(1, (int)Math.Ceiling(n / 12.0));
                for (int i = 0; i < n; i += step)
                {
                    float x = X(i, n, plot);
                    g.DrawString(Labels[i], f, mb, x - 18, plot.Bottom + 6);
                }
            }
            int ns = Data.Count;
            for (int si = 0; si < ns; si++)
            {
                var s = Data[si];
                using (var pen = new Pen(s.Color, 2.5f)) using (var br = new SolidBrush(s.Color))
                {
                    if (Bars)
                    {
                        float slot = plot.Width / (float)Labels.Count, w = slot * 0.8f / ns;
                        for (int i = 0; i < Labels.Count; i++)
                        {
                            if (!s.V[i].HasValue) continue;
                            float h = (float)(s.V[i].Value / max * plot.Height);
                            g.FillRectangle(br, plot.Left + slot * i + slot * 0.1f + w * si, plot.Bottom - h, Math.Max(2, w - 1), h);
                        }
                    }
                    else
                    {
                        PointF? last = null;
                        for (int i = 0; i < Labels.Count; i++)
                        {
                            if (!s.V[i].HasValue) continue;
                            var p = new PointF(X(i, Labels.Count, plot), (float)(plot.Bottom - s.V[i].Value / max * plot.Height));
                            if (last.HasValue) g.DrawLine(pen, last.Value, p);
                            g.FillEllipse(br, p.X - 3.5f, p.Y - 3.5f, 7, 7);
                            last = p;
                        }
                    }
                }
            }
            using (var f = Theme.F(9))
            {
                int lx = Width - 15;
                for (int si = Data.Count - 1; si >= 0; si--)
                {
                    var sz = g.MeasureString(Data[si].Name, f);
                    lx -= (int)sz.Width + 22;
                    using (var br = new SolidBrush(Data[si].Color)) g.FillRectangle(br, lx, 12, 12, 12);
                    using (var tb = new SolidBrush(Theme.Text)) g.DrawString(Data[si].Name, f, tb, lx + 16, 9);
                }
            }
        }

        float X(int i, int n, Rectangle plot)
        {
            if (Bars) return plot.Left + plot.Width / (float)n * (i + 0.5f);
            return n == 1 ? plot.Left + plot.Width / 2f : plot.Left + i * plot.Width / (float)(n - 1);
        }
    }

    class TypingResult { public double Seconds, Gross, Net, Accuracy, Words; public int Errors, Backspaces, Chars; public bool External; }

    class TypingArea : UserControl
    {
        public event Action<TypingResult, bool> Finished;
        Label lblTimer, lblStats; RichTextBox passageBox; TextBox box;
        Timer tm = new Timer { Interval = 250 };
        Stopwatch sw = new Stopwatch();
        string passage = "", lang = "English"; int totalSec, backs; bool running, external; double wl = 6.5;
        public bool Running { get { return running; } }

        public TypingArea()
        {
            BackColor = Theme.Bg; Dock = DockStyle.Fill;
            var top = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, BackColor = Theme.Bg };
            lblTimer = Ui.Lbl("00:00", 26, Theme.Gold, true);
            lblStats = Ui.Lbl("Press Start.", 11, Theme.Muted); lblStats.Margin = new Padding(20, 18, 0, 0);
            top.Controls.Add(lblTimer); top.Controls.Add(lblStats);
            passageBox = new RichTextBox { Dock = DockStyle.Fill, ReadOnly = true, BackColor = Theme.Panel, ForeColor = Theme.Text, BorderStyle = BorderStyle.None, Margin = new Padding(0, 6, 0, 6) };
            box = new TextBox { Dock = DockStyle.Fill, Multiline = true, BackColor = Theme.Panel2, ForeColor = Theme.Text, Enabled = false, ShortcutsEnabled = false, ContextMenu = new ContextMenu(), ScrollBars = ScrollBars.Vertical };
            var tl = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, BackColor = Theme.Bg };
            tl.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tl.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
            tl.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
            tl.Controls.Add(top, 0, 0); tl.Controls.Add(passageBox, 0, 1); tl.Controls.Add(box, 0, 2);
            Controls.Add(tl);

            box.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Back || e.KeyCode == Keys.Delete) backs++;
                if ((e.Control && (e.KeyCode == Keys.V || e.KeyCode == Keys.A || e.KeyCode == Keys.X)) || (e.Shift && e.KeyCode == Keys.Insert)) e.SuppressKeyPress = true;
            };
            box.KeyPress += (s, e) => { if (e.KeyChar == '\r' || e.KeyChar == '\n') e.Handled = true; };
            box.TextChanged += (s, e) => { if (running && !external && passage.Length > 0 && box.Text.Length >= passage.Length) Finish(true); };
            tm.Tick += (s, e) =>
            {
                double el = sw.Elapsed.TotalSeconds;
                int rem = Math.Max(0, (int)Math.Ceiling(totalSec - el));
                lblTimer.Text = string.Format("{0:00}:{1:00}", rem / 60, rem % 60);
                if (!external)
                {
                    var r = Calc();
                    lblStats.Text = string.Format(Db.Inv, "Gross {0:0.0} WPM | Net {1:0.0} WPM | Accuracy {2:0}% | Backspace {3}", r.Gross, r.Net, r.Accuracy, backs);
                }
                else lblStats.Text = "Timer only - type in MS Word / your book. Enter the result when the time ends.";
                if (el >= totalSec) Finish(true);
            };
        }

        public void Start(string text, int seconds, string language, bool ext)
        {
            external = ext; lang = language; totalSec = seconds; backs = 0;
            wl = Cfg.GetD(lang == "Hindi" ? "hindi_wl" : "eng_wl"); if (wl <= 0) wl = 5;
            passage = Regex.Replace((text ?? "").Trim(), @"\s+", " ");
            passageBox.Visible = box.Visible = !ext;
            if (!ext)
            {
                var f = lang == "Hindi" ? new Font("Mangal", 17f) : new Font("Segoe UI", 16f);
                passageBox.Font = f; box.Font = f; passageBox.Text = passage;
                box.MaxLength = passage.Length; box.Text = ""; box.Enabled = true; box.Focus();
            }
            sw.Reset(); sw.Start(); running = true; tm.Start();
        }

        public void Abort() { Finish(false); }
        public void Discard() { running = false; tm.Stop(); sw.Stop(); }

        TypingResult Calc()
        {
            string t = external ? "" : box.Text; int err = 0;
            for (int i = 0; i < t.Length; i++) if (i >= passage.Length || t[i] != passage[i]) err++;
            double min = Math.Max(sw.Elapsed.TotalSeconds, 1) / 60.0;
            double words = t.Length / wl;
            return new TypingResult
            {
                Seconds = sw.Elapsed.TotalSeconds, Chars = t.Length, Errors = err, Backspaces = backs, Words = words,
                Gross = words / min, Net = Math.Max(0, (t.Length - err) / wl / min), Accuracy = t.Length > 0 ? 100.0 * (t.Length - err) / t.Length : 0
            };
        }

        void Finish(bool full)
        {
            if (!running) return;
            running = false; tm.Stop(); sw.Stop(); box.Enabled = false;
            var r = Calc(); r.External = external;
            var h = Finished; if (h != null) h(r, full);
        }
    }

    class EntryPanel : UserControl
    {
        public event Action<SessionRec> SaveRequested;
        ComboBox cbLang, cbMethod; DateTimePicker dt;
        NumericUpDown nDur, nWords, nErr, nGross, nNet, nAcc, nBack;
        CheckBox chkLooked, chkDone; TextBox txtMist, txtNotes;
        string kind, srcFmt;

        public EntryPanel(string kind, string[] methods, string methodLabel, int minDur, string srcFmt)
        {
            this.kind = kind; this.srcFmt = srcFmt; BackColor = Theme.Bg; AutoSize = true;
            var fl = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = true, AutoSize = true, BackColor = Theme.Bg };
            dt = Ui.Date(DateTime.Today);
            cbLang = Ui.Combo(100, "English", "Hindi");
            cbMethod = Ui.Combo(180, methods);
            nDur = Ui.Num(minDur, 120, Math.Max(minDur, 10), 0, 70);
            nWords = Ui.Num(0, 20000, 0, 0, 80);
            nErr = Ui.Num(0, 20000, 0, 0, 70);
            nGross = Ui.Num(0, 300, 0, 1, 70);
            nNet = Ui.Num(0, 300, 0, 1, 70);
            nAcc = Ui.Num(0, 100, 0, 1, 70);
            nBack = Ui.Num(0, 50000, 0, 0, 70);
            chkLooked = new CheckBox { Text = "I looked at the keyboard", ForeColor = Theme.Text, AutoSize = true, Margin = new Padding(8, 22, 4, 0) };
            chkDone = new CheckBox { Text = "Session completed", ForeColor = Theme.Text, AutoSize = true, Checked = true, Margin = new Padding(8, 22, 4, 0) };
            txtMist = Ui.Txt(280); txtNotes = Ui.Txt(280);
            var tip = new ToolTip();
            tip.SetToolTip(nGross, "Gross WPM = all words typed per minute, errors NOT subtracted.");
            tip.SetToolTip(nNet, "Net WPM = (words typed - errors) per minute. Must not exceed Gross.");
            tip.SetToolTip(nAcc, "Accuracy % = correct words or characters / total typed x 100.");
            fl.Controls.Add(Ui.Field("Date", dt)); fl.Controls.Add(Ui.Field("Language", cbLang));
            fl.Controls.Add(Ui.Field(methodLabel, cbMethod)); fl.Controls.Add(Ui.Field("Duration (min)", nDur));
            fl.Controls.Add(Ui.Field("Total words typed", nWords)); fl.Controls.Add(Ui.Field("Errors", nErr));
            fl.Controls.Add(Ui.Field("Gross WPM", nGross)); fl.Controls.Add(Ui.Field("Net WPM", nNet));
            fl.Controls.Add(Ui.Field("Accuracy %", nAcc)); fl.Controls.Add(Ui.Field("Backspaces", nBack));
            fl.Controls.Add(chkLooked); fl.Controls.Add(chkDone);
            fl.Controls.Add(Ui.Field("Mistakes (comma separated words/keys)", txtMist));
            fl.Controls.Add(Ui.Field("Improvement notes", txtNotes));
            fl.Controls.Add(Ui.Btn("Auto-calculate from words + errors", (s, e) => AutoCalc()));
            fl.Controls.Add(Ui.Btn("SAVE RESULT", (s, e) => DoSave(), true));
            Controls.Add(fl);
        }

        public void Load(SessionRec t)
        {
            cbLang.SelectedItem = t.Language;
            int i = cbMethod.FindStringExact(t.Method); if (i >= 0) cbMethod.SelectedIndex = i;
            if (t.Duration > 0) nDur.Value = Math.Min(nDur.Maximum, Math.Max(nDur.Minimum, (decimal)Math.Round(t.Duration)));
            chkLooked.Checked = t.Looked; chkDone.Checked = t.Completed;
        }

        void AutoCalc()
        {
            decimal w = nWords.Value, d = nDur.Value;
            if (w <= 0 || d <= 0) { Ui.Warn("Enter total words typed and duration first."); return; }
            decimal err = Math.Min(nErr.Value, w);
            nGross.Value = Math.Min(nGross.Maximum, Math.Round(w / d, 1));
            nNet.Value = Math.Min(nNet.Maximum, Math.Max(0, Math.Round((w - err) / d, 1)));
            nAcc.Value = Math.Round((w - err) / w * 100m, 1);
        }

        void DoSave()
        {
            if (nGross.Value == 0 && nWords.Value == 0) { Ui.Warn("Enter total words typed or the WPM values."); return; }
            if (nNet.Value > nGross.Value) { Ui.Warn("Net WPM cannot be higher than Gross WPM.\r\nGross = all words per minute. Net = after subtracting errors."); return; }
            double dur = (double)nDur.Value;
            double words = nWords.Value > 0 ? (double)nWords.Value : (double)nGross.Value * dur;
            var s = new SessionRec
            {
                Ts = dt.Value.Date + DateTime.Now.TimeOfDay, Language = cbLang.Text, Kind = kind, Method = cbMethod.Text,
                Source = string.Format(srcFmt, cbMethod.Text), Duration = dur, Gross = (double)nGross.Value, Net = (double)nNet.Value,
                Accuracy = (double)nAcc.Value, Errors = (int)nErr.Value, Backspaces = (int)nBack.Value, Words = words,
                Looked = chkLooked.Checked, Mistakes = txtMist.Text.Trim(), Notes = txtNotes.Text.Trim(), Completed = chkDone.Checked
            };
            var h = SaveRequested; if (h != null) h(s);
        }

        public void ClearFields()
        {
            nWords.Value = 0; nErr.Value = 0; nGross.Value = 0; nNet.Value = 0; nAcc.Value = 0; nBack.Value = 0;
            txtMist.Text = ""; txtNotes.Text = ""; chkLooked.Checked = false; chkDone.Checked = true;
        }
    }
}
