@echo off
REM Build on any modern Windows PC that has the .NET SDK (6 or newer) and internet access for the first NuGet restore.
REM The BUILT program runs offline on Windows 7 SP1 with .NET Framework 4.8.
setlocal
cd /d "%~dp0"
if exist publish rmdir /s /q publish
dotnet publish src\TypingMahayudh.csproj -c Release -o publish
if errorlevel 1 goto fail
echo.
echo --- Check that publish\ contains: TypingMahayudh.exe, System.Data.SQLite.dll, x86\SQLite.Interop.dll, x64\SQLite.Interop.dll
dir /s /b publish\*.dll
echo.
where iscc >nul 2>nul
if errorlevel 1 (
  echo Inno Setup compiler [iscc] not found. Install Inno Setup 6 and run:  iscc installer\TypingMahayudh.iss
) else (
  iscc installer\TypingMahayudh.iss
)
goto end
:fail
echo BUILD FAILED - send me the error text and I will fix it.
:end
endlocal
