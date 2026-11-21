@echo off
REM دابل‌کلیک کن. خودش Visual Studio (یا MinGW) رو پیدا می‌کنه و DLL رو می‌سازه.
REM خروجی:  unity\Assets\Plugins\x86_64\RobotHeistCore.dll
setlocal
cd /d "%~dp0"
set "OUT=..\unity\Assets\Plugins\x86_64"
if not exist "%OUT%" mkdir "%OUT%"

REM --- 1) Visual Studio با C++ ---
set "VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
if exist "%VSWHERE%" (
  for /f "usebackq tokens=*" %%i in (`"%VSWHERE%" -latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath`) do set "VSPATH=%%i"
)
if defined VSPATH (
  echo Using Visual Studio at: %VSPATH%
  call "%VSPATH%\VC\Auxiliary\Build\vcvars64.bat" >nul
  cl /nologo /std:c++17 /O2 /EHsc /LD core.cpp /Fe:RobotHeistCore.dll
  if errorlevel 1 goto :fail
  copy /Y RobotHeistCore.dll "%OUT%\RobotHeistCore.dll" >nul
  del core.obj RobotHeistCore.lib RobotHeistCore.exp RobotHeistCore.dll 2>nul
  goto :ok
)

REM --- 2) MinGW-w64 ---
where g++ >nul 2>nul
if not errorlevel 1 (
  echo Using MinGW g++
  g++ -std=c++17 -O2 -shared core.cpp -o "%OUT%\RobotHeistCore.dll" -static -static-libgcc -static-libstdc++
  if errorlevel 1 goto :fail
  goto :ok
)

echo.
echo C++ compiler not found.
echo Install "Visual Studio Build Tools" and select the workload "Desktop development with C++",
echo then run this file again.
pause
exit /b 1

:fail
echo BUILD FAILED
pause
exit /b 1

:ok
echo.
echo OK - DLL created in unity\Assets\Plugins\x86_64
pause
