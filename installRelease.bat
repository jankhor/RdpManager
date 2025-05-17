:: %1 - Release zp file
:: %2 - Destination
@echo off

if "%2" == "" (
    @echo installRelease ^<zipFile^> ^<Destination folder^>
    echo.
    @echo Example: installRelease c:\temp\RdpManager-1.0.4.zip c:\tools
) else (
    set OLD_PWD=%cd%
    cd /d %2

    unzip %1

    cd /d %OLD_PWD%
)

