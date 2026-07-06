@echo off
chcp 65001 >nul
setlocal

set VER=%~1
if "%VER%"=="" set VER=0.1
set ENG=C:\Users\xijil\AppData\Local\Microsoft\WinGet\Packages\GodotEngine.GodotEngine.Mono_Microsoft.Winget.Source_8wekyb3d8bbwe\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64.exe

echo === Building SectStorm v%VER% ===
echo.

echo [1/2] Clean and compile C# (Release)
if exist "%~dp0.godot\mono\temp" rmdir /S /Q "%~dp0.godot\mono\temp"
dotnet build "%~dp0SectStorm.csproj" --configuration Release
if errorlevel 1 goto fail

echo [2/2] Exporting
call :run_export
if errorlevel 1 goto fail

echo.
echo === Done === build\SectStorm.exe v%VER%
echo DLC and mods excluded from PCK - use user:// for external content
pause
exit /b 0

:fail
echo === Build FAILED ===
pause
exit /b 1

:run_export
set ENG_PATH=%ENG%
if not exist "%ENG_PATH%" (
    echo Godot engine not found: %ENG_PATH%
    exit /b 1
)
"%ENG_PATH%" --headless --path "%~dp0." --export-release "Windows Desktop" "%~dp0build\SectStorm.exe"
exit /b %ERRORLEVEL%
