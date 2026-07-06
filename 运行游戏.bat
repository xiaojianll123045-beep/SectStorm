@echo off
chcp 65001 >nul
setlocal

set ENG=C:\Users\xijil\AppData\Local\Microsoft\WinGet\Packages\GodotEngine.GodotEngine.Mono_Microsoft.Winget.Source_8wekyb3d8bbwe\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64.exe

echo === Dev Run ===
echo.

echo [1/2] Clean and compile C#
if exist "%~dp0.godot\mono\temp" rmdir /S /Q "%~dp0.godot\mono\temp"
dotnet build "%~dp0SectStorm.csproj"
if errorlevel 1 goto fail

echo.
echo [2/2] Launching game (close game window to return here)
echo.
call :run_game
echo.
echo === Game closed ===
pause
exit /b 0

:fail
echo === Build FAILED ===
pause
exit /b 1

:run_game
set ENG_PATH=%ENG%
if not exist "%ENG_PATH%" (
    echo Godot engine not found: %ENG_PATH%
    exit /b 1
)
"%ENG_PATH%" --path "%~dp0." --rendering-driver d3d12
exit /b %ERRORLEVEL%
