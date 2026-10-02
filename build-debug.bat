@echo off
setlocal
cd /d "%~dp0"

where dotnet >nul 2>nul
if errorlevel 1 (
  echo ERROR: .NET 8 SDK was not found in PATH.
  exit /b 1
)

echo [1/3] Restore
dotnet restore DividingMon1Test.sln --nologo
if errorlevel 1 exit /b 1

echo [2/3] Build Debug
dotnet build DividingMon1Test.sln -c Debug --no-restore --nologo
if errorlevel 1 exit /b 1

echo [3/3] Geometry self-test
dotnet run --project tests\DividingMon1Test.SelfTest\DividingMon1Test.SelfTest.csproj -c Debug --no-build --no-restore
if errorlevel 1 exit /b 1

echo.
echo READY:
echo src\DividingMon1Test\bin\Debug\net8.0-windows10.0.19041.0\DividingMon1Test.exe
exit /b 0
