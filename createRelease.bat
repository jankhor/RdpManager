@echo off
if "%1" == "" (
    echo "passing in zip filename to create."
    exit /b 1
)

echo **** Building ****
dotnet clean
dotnet publish -c Release -r win-x64 --self-contained true

echo.
echo **** Packaging ****
echo Creating bin\RdpManager
mkdir bin\RdpManager 2> nul

xcopy c:\projects\RdpManager\bin\Release\net8.0-windows\win-x64\publish\*.* bin\RdpManager\. /y/s/q

cd bin

if exist "%1" (
    echo Deleting %1
)

echo.
echo **** Creating %1 ****
zip -q -r %1 RdpManager

cd ..
