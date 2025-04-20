:: %1 - Release zp file
:: %2 - Destination
@echo off

if "%2" == "" (
    @echo installRelase ^<zipFile^> ^<Destination folder^>
) else (
    cd /d %2

    unzip %1
)

