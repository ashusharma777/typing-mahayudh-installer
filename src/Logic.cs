using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace TypingMahayudh
{
    class Popup { public string Title, Reason, Line; public bool Positive; }

    static class Milestones
    {
        public static readonly int[] Words = { 5000, 10000, 15000, 20000, 25000, 30000, 40000, 50000 };
        public static readonly string[] Titles = {
            "कुछू पुछू... वाह वाह! 🎉",
            "अच्छा कर रहे हो कुछू पुछू! 😎",
            "बालवीर! 🦸",
            "शक्तिमान! 💥",
            "अमर सिंह! ⚔️",
            "चंद्रगुप्त! 👑",
            "राजाधिराज! 🏰",
            "जय हो प्रभु सम्राट की! 👑🔥"
        };
        public static int NextIndex(double total)
        {
            for (int i = 0; i < Words.Length; i++) if (total < Words[i]) return i;
            return -1;
        }
    }

    static class Achv
    {
        // Unlocks every milestone reached and not yet stored. A milestone is stored once (PRIMARY KEY), so it is never awarded twice.
        public static List<int> Check(double total)
        {
            var res = new List<int>();
            for (int i = 0; i < Milestones.Words.Length; i++)
            {
                if (total < Milestones.Words[i]) continue;
                int n = Convert.ToInt32(Db.Scalar("SELECT COUNT(*) FROM achievements WHERE words=@0", Milestones.Words[i]));
                if (n == 0)
                {
                    Db.Exec("INSERT OR IGNORE INTO achievements(words,title,unlocked) VALUES(@0,@1,@2)", Milestones.Words[i], Milestones.Titles[i], DateTime.Now.ToString("yyyy-MM-dd HH:mm", Db.Inv));
                    res.Add(i);
                }
            }
            return res;
        }
    }

    static class Mist
    {
        public static void FromSession(SessionRec s)
        {
            if (string.IsNullOrWhiteSpace(s.Mistakes)) return;
            foreach (var part in s.Mistakes.Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string item = part.Trim();
                if (item.Length == 0) continue;
                Db.Exec("INSERT INTO mistakes(day,language,kind,item,note) VALUES(@0,@1,'Word',@2,'From session')", s.Ts.ToString("yyyy-MM-dd", Db.Inv), s.Language, item);
            }
        }
    }

    static class Flow
    {
        public static void Complete(IWin32Window owner, SessionRec s)
        {
            s.Id = s.Save();
            Mist.FromSession(s);
            AutoTick(s);
            double total = Stats.TotalWords();
            foreach (int i in Achv.Check(total))
                using (var f = new AchievementForm(i, total)) f.ShowDialog(owner);
            foreach (var p in MotivationEngine.Evaluate(s))
                using (var f = new PopupForm(p)) f.ShowDialog(owner);
            if (MainForm.Instance != null) MainForm.Instance.RefreshCurrent();
        }

        static void AutoTick(SessionRec s)
        {
            string title = null;
            if (s.Kind == "Practice")
            {
                if (s.Method.Contains("Word")) title = "MS Word practice";
                else if (s.Method.Contains("Book")) title = "Book practice";
                else if (s.Method.Contains("Newspaper")) title = "Newspaper practice";
            }
            else if (s.Kind == "Drill") title = "Accuracy drills";
            else if (s.Kind == "Test" || s.Kind == "Exam") title = "10-minute test";
            if (title == null) return;
            Db.Exec("UPDATE tasks SET done=1 WHERE day=@0 AND category=@1 AND title=@2", s.Ts.ToString("yyyy-MM-dd", Db.Inv), s.Language, title);
        }
    }

    static class MotivationEngine
    {
        static string Day() { return DateTime.Today.ToString("yyyy-MM-dd", Db.Inv); }

        public static List<Popup> Evaluate(SessionRec s)
        {
            var res = new List<Popup>();
            if (!Cfg.GetB("popups") || s.Words <= 0) return res;

            var prev = Db.Query("SELECT net_wpm,accuracy,backspaces,duration_min FROM sessions WHERE language=@0 AND kind=@1 AND id<>@2 AND words>0 ORDER BY ts DESC, id DESC LIMIT 5", s.Language, s.Kind, s.Id);
            int n = prev.Rows.Count;
            double avgNet = 0, avgBsRate = 0, lastAcc = 0;
            if (n > 0) lastAcc = Db.D(prev.Rows[0]["accuracy"]);
            foreach (DataRow r in prev.Rows)
            {
                avgNet += Db.D(r["net_wpm"]);
                avgBsRate += Db.D(r["backspaces"]) / Math.Max(1, Db.D(r["duration_min"]));
            }
            if (n > 0) { avgNet /= n; avgBsRate /= n; }

            double accT = Cfg.GetD("acc_thresh"), bsF = Cfg.GetD("bs_factor"), drop = Cfg.GetD("speed_drop") / 100.0, impr = Cfg.GetD("improve_pts");
            double target = s.Language == "Hindi" ? Cfg.GetD("hindi_target") : Cfg.GetD("eng_target");
            string L = s.Language, dur = s.Duration.ToString("0.#", Db.Inv);
            Func<string, object[], string> F = (fmt, a) => string.Format(Db.Inv, fmt, a);

            // ---- positive first: a celebration replaces any warning for this session
            if (n >= 1 && s.Accuracy - lastAcc >= impr)
            {
                res.Add(new Popup { Positive = true, Title = "शाबाश!", Line = Dialogues.Pick("improve"),
                    Reason = F("पिछले सेशन की {0:0.0}% के मुक़ाबले आज तुम्हारी Accuracy {1:0.0}% रही (+{2:0.0} अंक)।", new object[] { lastAcc, s.Accuracy, s.Accuracy - lastAcc }) });
                return res;
            }
            if (n >= 3 && s.Net >= avgNet * 1.10 && s.Net - avgNet >= 2)
            {
                res.Add(new Popup { Positive = true, Title = "शाबाश!", Line = Dialogues.Pick("improve"),
                    Reason = F("आज {0} speed {1:0.0} WPM रही, हाल के औसत {2:0.0} WPM से बेहतर!", new object[] { L, s.Net, avgNet }) });
                return res;
            }

            // ---- at most 2 friendly nudges per day
            string negKey = "neg_" + Day();
            int negCount; int.TryParse(Cfg.Get(negKey), out negCount);
            if (negCount >= 2) return res;

            Popup neg = null;
            double bsRate = s.Backspaces / Math.Max(1.0, s.Duration);
            if (s.Accuracy < accT)
                neg = new Popup { Title = "एक छोटी सलाह", Line = Dialogues.Pick("accuracy"),
                    Reason = F("आज तुम्हारी Accuracy {0:0}% रही है (लक्ष्य {1:0}%)।", new object[] { s.Accuracy, accT }) };
            else if (n >= 3 && avgBsRate > 0 && bsRate > avgBsRate * bsF && s.Backspaces >= 10)
                neg = new Popup { Title = "Backspace अलर्ट", Line = Dialogues.Pick("backspace"),
                    Reason = F("आज {0} मिनट में {1} बार Backspace दबाया! (सामान्य औसत लगभग {2:0} बार)", new object[] { dur, s.Backspaces, avgBsRate * s.Duration }) };
            else if (n >= 3 && s.Net < avgNet * (1 - drop))
                neg = new Popup { Title = "स्पीड चेक", Line = Dialogues.Pick("speed"),
                    Reason = F("आज {0} speed {1:0} WPM रही (हाल का औसत {2:0} WPM)।", new object[] { L, s.Net, avgNet }) };
            else if (s.Kind != "Drill" && target > 0 && s.Net < target && Cfg.Get("tgt_" + L) != Day())
            {
                Cfg.Set("tgt_" + L, Day());
                neg = new Popup { Title = L + " लक्ष्य", Line = Dialogues.Pick(s.Language == "Hindi" ? "hindi_target" : "eng_target"),
                    Reason = F("आज {0} speed {1:0} WPM रही; लक्ष्य {2:0} WPM है।", new object[] { L, s.Net, target }) };
            }
            if (neg != null)
            {
                Cfg.Set(negKey, (negCount + 1).ToString(Db.Inv));
                res.Add(neg);
            }
            return res;
        }

        public static Popup NoPractice(int daysAgo)
        {
            return new Popup { Title = "कीबोर्ड याद कर रहा है", Line = Dialogues.Pick("nopractice"),
                Reason = daysAgo > 1 ? "पिछला सेशन " + daysAgo + " दिन पहले हुआ था।" : "कल कोई सेशन पूरा नहीं हुआ।" };
        }
    }
}
