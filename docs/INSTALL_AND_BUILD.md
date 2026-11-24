# Typing Mahayudh - build, install, backup

## 1. Build (once, on any Windows PC with internet)
1. Install **.NET SDK 6 or newer** (only needed to *build*) and **Inno Setup 6**.
2. Double-click `build.bat`. It restores NuGet packages (System.Data.SQLite.Core 1.0.118), publishes to `publish\`, then makes `dist\TypingMahayudh_Setup_1.0.0.exe`.
3. Optional: put `vcredist_x86.exe` and `vcredist_x64.exe` (Visual C++ 2013 Redistributable) into `installer\redist\` before step 2, so the installer bundles them.
   Verify what the SQLite native DLL needs: `dumpbin /dependents publish\x86\SQLite.Interop.dll` (look for msvcr120.dll = VC++ 2013).

## 2. Install on Windows 7 SP1 (primary target)
Prerequisites on the Win7 PC: **Service Pack 1**, **.NET Framework 4.8** (needs update KB4474419 / SHA-2 support first), and the VC++ runtime above if not bundled.
Run `TypingMahayudh_Setup_1.0.0.exe` -> desktop + Start Menu shortcuts -> Launch. Uninstall: Control Panel > Programs. No internet is needed after installation.

## 3. Install on Windows 10
Same setup file. .NET Framework 4.8 is built in from Windows 10 version 1903; older builds need the 4.8 installer.

## 4. Where data lives
`%APPDATA%\TypingMahayudh\mahayudh.db` (SQLite). Uninstalling does NOT delete it. Auto backups: `%APPDATA%\TypingMahayudh\Backups\` (last 20, made on exit and at startup if the last one is >20 h old).
- Manual backup / restore: Settings page. Restore makes a safety copy first, validates the file, and restarts the app.
- If the database is damaged at startup, the app keeps the damaged file as `corrupt_*.db` and offers the newest backup.
- Errors are logged to `%APPDATA%\TypingMahayudh\error.log`.
- Exports (Reports page): sessions / mistakes / efficiency as CSV with a UTF-8 BOM, which Excel opens correctly with Hindi text.

## 5. Known limits
- No PDF export (use the .txt report or CSV).
- Emoji in the 8 achievement titles may display as boxes on Windows 7 (no colour-emoji font); the Hindi text itself is unaffected.
- Hindi typing inside the built-in box needs a Windows Hindi keyboard layout (Settings > Region and Language) matching your exam layout.
- WPM uses your configured average word length (English 6.5, Hindi 5.75), net = (typed chars - mismatched chars) / word length / minutes. Your real exam board may score differently.
