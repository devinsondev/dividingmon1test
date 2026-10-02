@echo off
setlocal
cd /d "%~dp0"

where dotnet >nul 2>nul
if errorlevel 1 (
  echo ERROR: .NET 8 SDK was not found in PATH.
  exit /b 1
)

dotnet restore DividingMon1Test.sln --nologo
if errorlevel 1 exit /b 1

dotnet build DividingMon1Test.sln -c Release --no-restore --nologo
if errorlevel 1 exit /b 1

dotnet run --project tests\DividingMon1Test.SelfTest\DividingMon1Test.SelfTest.csproj -c Release --no-build --no-restore
if errorlevel 1 exit /b 1

dotnet publish src\DividingMon1Test\DividingMon1Test.csproj -c Release -r win-x64 --self-contained false --no-restore -p:PublishSingleFile=true -p:DebugType=None -p:DebugSymbols=false
if errorlevel 1 exit /b 1

echo.
echo PUBLISHED:
echo src\DividingMon1Test\bin\Release\net8.0-windows10.0.19041.0\win-x64\publish\DividingMon1Test.exe
exit /b 0
