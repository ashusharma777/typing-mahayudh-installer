using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace TypingMahayudh
{
    static class Theme
    {
        public static readonly Color Bg = Color.FromArgb(13, 19, 32);
        public static readonly Color Panel = Color.FromArgb(25, 34, 53);
        public static readonly Color Panel2 = Color.FromArgb(32, 44, 67);
        public static readonly Color Text = Color.FromArgb(238, 243, 255);
        public static readonly Color Muted = Color.FromArgb(170, 182, 206);
        public static readonly Color Teal = Color.FromArgb(105, 214, 197);
        public static readonly Color Gold = Color.FromArgb(255, 209, 102);
        public static readonly Color Red = Color.FromArgb(255, 113, 133);
        public static readonly Color Line = Color.FromArgb(52, 65, 90);
        public static Font F(float size, bool bold = false)
        {
            return new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular);
        }
    }

    static class Ui
    {
        public static Label Lbl(string t, float size = 10f, Color? c = null, bool bold = false)
        {
            return new Label { Text = t, AutoSize = true, ForeColor = c ?? Theme.Text, Font = Theme.F(size, bold), BackColor = Color.Transparent, Margin = new Padding(4) };
        }
        public static Button Btn(string t, EventHandler h, bool gold = false)
        {
            var b = new Button
            {
                Text = t, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, FlatStyle = FlatStyle.Flat,
                BackColor = gold ? Theme.Gold : Theme.Teal, ForeColor = Color.FromArgb(16, 32, 42), Font = Theme.F(10, true),
                Padding = new Padding(8, 3, 8, 3), Margin = new Padding(4), Cursor = Cursors.Hand
            };
            b.FlatAppearance.BorderSize = 0;
            if (h != null) b.Click += h;
            return b;
        }
        public static TextBox Txt(int w)
        {
            return new TextBox { Width = w, BackColor = Theme.Panel2, ForeColor = Theme.Text, BorderStyle = BorderStyle.FixedSingle, Font = Theme.F(10) };
        }
        public static ComboBox Combo(int w, params string[] items)
        {
            var c = new ComboBox { Width = w, DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat, BackColor = Theme.Panel2, ForeColor = Theme.Text, Font = Theme.F(10) };
            c.Items.AddRange(items);
            if (items.Length > 0) c.SelectedIndex = 0;
            return c;
        }
        public static NumericUpDown Num(decimal min, decimal max, decimal val, int dec, int w)
        {
            return new NumericUpDown { Minimum = min, Maximum = max, Value = val, DecimalPlaces = dec, Width = w, BackColor = Theme.Panel2, ForeColor = Theme.Text, Font = Theme.F(10) };
        }
        public static DateTimePicker Date(DateTime v)
        {
            return new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 110, Value = v, Font = Theme.F(10) };
        }
        public static FlowLayoutPanel Field(string label, Control c)
        {
            var p = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, WrapContents = false, Margin = new Padding(4), BackColor = Color.Transparent };
            var l = Lbl(label, 9f, Theme.Muted); l.Margin = new Padding(0, 0, 0, 2);
            p.Controls.Add(l); p.Controls.Add(c);
            return p;
        }
        public static FlowLayoutPanel Row()
        {
            return new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true, BackColor = Color.Transparent };
        }
        public static DataGridView Grid()
        {
            var g = new DataGridView
            {
                Dock = DockStyle.Fill, BackgroundColor = Theme.Panel, GridColor = Theme.Line, BorderStyle = BorderStyle.None,
                RowHeadersVisible = false, AllowUserToAddRows = false, AllowUserToDeleteRows = false, EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                Font = Theme.F(9.5f), ReadOnly = true
            };
            g.DefaultCellStyle.BackColor = Theme.Panel2; g.DefaultCellStyle.ForeColor = Theme.Text;
            g.DefaultCellStyle.SelectionBackColor = Theme.Teal; g.DefaultCellStyle.SelectionForeColor = Color.FromArgb(16, 32, 42);
            g.ColumnHeadersDefaultCellStyle.BackColor = Theme.Bg; g.ColumnHeadersDefaultCellStyle.ForeColor = Theme.Teal;
            g.ColumnHeadersDefaultCellStyle.SelectionBackColor = Theme.Bg;
            return g;
        }
        public static void Info(string m) { MessageBox.Show(m, "Typing Mahayudh", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        public static void Warn(string m) { MessageBox.Show(m, "Typing Mahayudh", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        public static bool Ask(string m) { return MessageBox.Show(m, "Typing Mahayudh", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes; }
    }

    abstract class Page : UserControl
    {
        protected Page() { BackColor = Theme.Bg; Dock = DockStyle.Fill; Padding = new Padding(14); }
        public virtual void OnShow() { }
    }

    static class Db
    {
        public static string Dir, FilePath, BackupDir;
        static string ConnStr { get { return "Data Source=" + FilePath + ";Version=3;Pooling=False;"; } }
        public static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public static void Setup()
        {
            Dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TypingMahayudh");
            Directory.CreateDirectory(Dir);
            BackupDir = Path.Combine(Dir, "Backups");
            Directory.CreateDirectory(BackupDir);
            FilePath = Path.Combine(Dir, "mahayudh.db");
        }
        public static SQLiteConnection Open() { var c = new SQLiteConnection(ConnStr); c.Open(); return c; }
        static void Bind(SQLiteCommand m, object[] p)
        {
            for (int i = 0; i < p.Length; i++) m.Parameters.AddWithValue("@" + i, p[i] ?? DBNull.Value);
        }
        public static int Exec(string sql, params object[] p)
        {
            using (var c = Open()) using (var m = new SQLiteCommand(sql, c)) { Bind(m, p); return m.ExecuteNonQuery(); }
        }
        public static long Insert(string sql, params object[] p)
        {
            using (var c = Open())
            {
                using (var m = new SQLiteCommand(sql, c)) { Bind(m, p); m.ExecuteNonQuery(); }
                using (var m2 = new SQLiteCommand("SELECT last_insert_rowid()", c)) return Convert.ToInt64(m2.ExecuteScalar());
            }
        }
        public static object Scalar(string sql, params object[] p)
        {
            using (var c = Open()) using (var m = new SQLiteCommand(sql, c)) { Bind(m, p); return m.ExecuteScalar(); }
        }
        public static DataTable Query(string sql, params object[] p)
        {
            using (var c = Open()) using (var m = new SQLiteCommand(sql, c))
            {
                Bind(m, p);
                var t = new DataTable();
                using (var a = new SQLiteDataAdapter(m)) a.Fill(t);
                return t;
            }
        }
        public static double D(object o) { return (o == null || o == DBNull.Value) ? 0 : Convert.ToDouble(o, Inv); }
        public static string S(object o) { return (o == null || o == DBNull.Value) ? "" : Convert.ToString(o, Inv); }

        public static bool IntegrityOk(string path)
        {
            try
            {
                using (var c = new SQLiteConnection("Data Source=" + path + ";Version=3;Pooling=False;"))
                {
                    c.Open();
                    using (var m = new SQLiteCommand("PRAGMA integrity_check", c)) return Convert.ToString(m.ExecuteScalar()) == "ok";
                }
            }
            catch { return false; }
        }

        public static void Init()
        {
            using (var c = Open())
            {
                string[] ddl = {
                    "CREATE TABLE IF NOT EXISTS sessions(id INTEGER PRIMARY KEY AUTOINCREMENT, ts TEXT, day TEXT, language TEXT, kind TEXT, method TEXT, source TEXT, duration_min REAL, gross_wpm REAL, net_wpm REAL, accuracy REAL, errors INTEGER, backspaces INTEGER, words REAL, looked INTEGER, mistakes TEXT, notes TEXT, completed INTEGER)",
                    "CREATE INDEX IF NOT EXISTS ix_sessions_day ON sessions(day)",
                    "CREATE TABLE IF NOT EXISTS settings(k TEXT PRIMARY KEY, v TEXT)",
                    "CREATE TABLE IF NOT EXISTS achievements(words INTEGER PRIMARY KEY, title TEXT, unlocked TEXT)",
                    "CREATE TABLE IF NOT EXISTS efficiency(subject TEXT, session_no INTEGER, activity INTEGER, done INTEGER DEFAULT 0, minutes INTEGER DEFAULT 0, day TEXT DEFAULT '', topic TEXT DEFAULT '', assign_status TEXT DEFAULT 'Not started', mistakes TEXT DEFAULT '', notes TEXT DEFAULT '', PRIMARY KEY(subject, session_no, activity))",
                    "CREATE TABLE IF NOT EXISTS tasks(id INTEGER PRIMARY KEY AUTOINCREMENT, day TEXT, category TEXT, title TEXT, done INTEGER DEFAULT 0, custom INTEGER DEFAULT 0)",
                    "CREATE TABLE IF NOT EXISTS mistakes(id INTEGER PRIMARY KEY AUTOINCREMENT, day TEXT, language TEXT, kind TEXT, item TEXT, note TEXT)"
                };
                foreach (var s in ddl) using (var m = new SQLiteCommand(s, c)) m.ExecuteNonQuery();
                using (var tx = c.BeginTransaction())
                {
                    foreach (var subj in new[] { "Word", "Excel" })
                        for (int n = 1; n <= 15; n++)
                            for (int a = 1; a <= 3; a++)
                                using (var m = new SQLiteCommand("INSERT OR IGNORE INTO efficiency(subject,session_no,activity) VALUES(@0,@1,@2)", c, tx))
                                {
                                    m.Parameters.AddWithValue("@0", subj); m.Parameters.AddWithValue("@1", n); m.Parameters.AddWithValue("@2", a);
                                    m.ExecuteNonQuery();
                                }
                    tx.Commit();
                }
            }
            if (Cfg.Get("eff_start") == "") Cfg.Set("eff_start", DateTime.Today.ToString("yyyy-MM-dd", Inv));
        }
    }

    static class Cfg
    {
        static readonly Dictionary<string, string> Defaults = new Dictionary<string, string>
        {
            {"exam_date","2026-12-21"},{"eng_target","55"},{"hindi_target","44"},{"eng_wl","6.5"},{"hindi_wl","5.75"},
            {"acc_thresh","85"},{"bs_factor","1.5"},{"speed_drop","20"},{"improve_pts","3"},
            {"popups","1"},{"sound","1"},{"autobackup","1"},{"eff_start",""}
        };
        public static string Get(string k)
        {
            var o = Db.Scalar("SELECT v FROM settings WHERE k=@0", k);
            if (o != null && o != DBNull.Value) return Convert.ToString(o);
            string d; return Defaults.TryGetValue(k, out d) ? d : "";
        }
        public static double GetD(string k)
        {
            double v; return double.TryParse(Get(k), NumberStyles.Float, Db.Inv, out v) ? v : 0;
        }
        public static bool GetB(string k) { return Get(k) == "1"; }
        public static void Set(string k, string v) { Db.Exec("INSERT OR REPLACE INTO settings(k,v) VALUES(@0,@1)", k, v); }
        public static DateTime ExamDate
        {
            get
            {
                DateTime d; return DateTime.TryParseExact(Get("exam_date"), "yyyy-MM-dd", Db.Inv, DateTimeStyles.None, out d) ? d : new DateTime(2026, 12, 21);
            }
        }
    }

    class SessionRec
    {
        public long Id;
        public DateTime Ts = DateTime.Now;
        public string Language = "English", Kind = "Practice", Method = "", Source = "", Mistakes = "", Notes = "";
        public double Duration, Gross, Net, Accuracy, Words;
        public int Errors, Backspaces;
        public bool Looked, Completed = true;
        public long Save()
        {
            return Db.Insert(
                "INSERT INTO sessions(ts,day,language,kind,method,source,duration_min,gross_wpm,net_wpm,accuracy,errors,backspaces,words,looked,mistakes,notes,completed) VALUES(@0,@1,@2,@3,@4,@5,@6,@7,@8,@9,@10,@11,@12,@13,@14,@15,@16)",
                Ts.ToString("yyyy-MM-dd HH:mm:ss", Db.Inv), Ts.ToString("yyyy-MM-dd", Db.Inv), Language, Kind, Method, Source,
                Duration, Gross, Net, Accuracy, Errors, Backspaces, Words, Looked ? 1 : 0, Mistakes ?? "", Notes ?? "", Completed ? 1 : 0);
        }
    }

    static class Stats
    {
        public static double TotalWords() { return Db.D(Db.Scalar("SELECT IFNULL(SUM(words),0) FROM sessions")); }
        public static DataRow Latest(string lang)
        {
            var t = Db.Query("SELECT * FROM sessions WHERE language=@0 AND kind<>'Drill' AND words>0 ORDER BY ts DESC, id DESC LIMIT 1", lang);
            return t.Rows.Count > 0 ? t.Rows[0] : null;
        }
        public static double BestNet(string lang) { return Db.D(Db.Scalar("SELECT MAX(net_wpm) FROM sessions WHERE language=@0 AND kind<>'Drill'", lang)); }
        public static string LastDay() { return Db.S(Db.Scalar("SELECT MAX(day) FROM sessions")); }
        public static int CountOn(string day) { return Convert.ToInt32(Db.Scalar("SELECT COUNT(*) FROM sessions WHERE day=@0", day)); }
        public static double EfficiencyFraction(string subject)
        {
            string sql = subject == null ? "SELECT IFNULL(SUM(done),0)*1.0/COUNT(*) FROM efficiency" : "SELECT IFNULL(SUM(done),0)*1.0/COUNT(*) FROM efficiency WHERE subject=@0";
            return subject == null ? Db.D(Db.Scalar(sql)) : Db.D(Db.Scalar(sql, subject));
        }
        public static double OverallPct()
        {
            double eng = 0, hin = 0;
            var e = Latest("English"); if (e != null && Cfg.GetD("eng_target") > 0) eng = Math.Min(1, Db.D(e["net_wpm"]) / Cfg.GetD("eng_target"));
            var h = Latest("Hindi"); if (h != null && Cfg.GetD("hindi_target") > 0) hin = Math.Min(1, Db.D(h["net_wpm"]) / Cfg.GetD("hindi_target"));
            return (eng + hin + EfficiencyFraction(null)) / 3.0 * 100.0;
        }
    }

    static class BackupMgr
    {
        public static string Make(string dest) { File.Copy(Db.FilePath, dest, true); return dest; }
        public static string Auto()
        {
            string p = Path.Combine(Db.BackupDir, "auto_" + DateTime.Now.ToString("yyyyMMdd_HHmmss", Db.Inv) + ".db");
            File.Copy(Db.FilePath, p, true);
            var old = new DirectoryInfo(Db.BackupDir).GetFiles("auto_*.db").OrderByDescending(f => f.CreationTime).Skip(20).ToList();
            foreach (var f in old) { try { f.Delete(); } catch { } }
            return p;
        }
        public static DateTime LastAuto()
        {
            var f = new DirectoryInfo(Db.BackupDir).GetFiles("auto_*.db").OrderByDescending(x => x.CreationTime).FirstOrDefault();
            return f == null ? DateTime.MinValue : f.CreationTime;
        }
        public static string LatestFile()
        {
            var f = new DirectoryInfo(Db.BackupDir).GetFiles("*.db").Where(x => x.Name.StartsWith("auto_") || x.Name.StartsWith("manual_")).OrderByDescending(x => x.CreationTime).FirstOrDefault();
            return f == null ? null : f.FullName;
        }
        public static bool Restore(string src, out string err)
        {
            err = null;
            if (!File.Exists(src)) { err = "File not found."; return false; }
            if (!Db.IntegrityOk(src)) { err = "This file is not a healthy database (integrity check failed)."; return false; }
            try
            {
                using (var c = new SQLiteConnection("Data Source=" + src + ";Version=3;Pooling=False;"))
                {
                    c.Open();
                    using (var m = new SQLiteCommand("SELECT COUNT(*) FROM sqlite_master WHERE name IN ('sessions','settings','efficiency')", c))
                        if (Convert.ToInt32(m.ExecuteScalar()) < 3) { err = "This is not a Typing Mahayudh backup."; return false; }
                }
                if (File.Exists(Db.FilePath)) File.Copy(Db.FilePath, Path.Combine(Db.BackupDir, "before_restore_" + DateTime.Now.ToString("yyyyMMdd_HHmmss", Db.Inv) + ".db"), true);
                File.Copy(src, Db.FilePath, true);
                return true;
            }
            catch (Exception ex) { err = ex.Message; return false; }
        }
    }

    static class Csv
    {
        public static void Write(DataTable t, string path)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine(string.Join(",", t.Columns.Cast<DataColumn>().Select(c => Esc(c.ColumnName))));
            foreach (DataRow r in t.Rows)
                sb.AppendLine(string.Join(",", r.ItemArray.Select(o => Esc(Db.S(o)))));
            File.WriteAllText(path, sb.ToString(), new System.Text.UTF8Encoding(true));
        }
        static string Esc(string s)
        {
            if (s.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0) return "\"" + s.Replace("\"", "\"\"") + "\"";
            return s;
        }
    }
}
