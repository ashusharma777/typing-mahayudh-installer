# Testing status - READ THIS

**Nothing in this project has been compiled or run yet.** It was written in an environment with no C# compiler, no .NET/Mono, no internet and no Windows. So:

- Windows 7 compatibility is **designed for** (net48, WinForms, System.Data.SQLite 1.0.118 with x86/x64 native DLLs, Inno Setup `MinVersion=6.1sp1`) but **not verified**.
- Expect possibly a few compile errors on first build. Send the error text and they get fixed quickly.

## Checklist to run on the Windows 7 PC after the first successful build
1. Setup installs; shortcuts appear; app starts; `%APPDATA%\TypingMahayudh\mahayudh.db` is created.
2. English Typing: load sample, 5-minute Built-in session -> history row appears, Dashboard updates.
3. External timer mode -> result dialog -> Net > Gross is rejected.
4. Drills (min 5) and Tests manual entry save; English/Hindi 10-minute exam saves an Exam record.
5. Add sessions until >= 5,000 total words: achievement popup shows once only; Achievements page shows it unlocked.
6. Accuracy < 85% session shows a popup with real numbers; two such sessions same day cap at 2 nudges.
7. Efficiency: tick activities in any order; percentages and 15-day calendar update after restart.
8. Planner: defaults appear for a new date, custom task add/delete, percentage.
9. Progress: all 9 charts and filters; delete a session.
10. Mistake Analysis top-10 and trend; Reports text + 3 CSV exports open in Excel.
11. Settings: change thresholds, backup, restore (app restarts), corrupt the .db on purpose and confirm recovery prompt.
12. Hindi typing box with Hindi keyboard layout; fonts render (Mangal).
