@echo off
setlocal ENABLEDELAYEDEXPANSION

REM ============================================
REM  (ProxyCacheService + RoutingHost + ActiveMqProducer + Front)
REM  A lancer en tant qu'administrateur
REM ============================================

set ROOT=%~dp0

REM On remonte d’un dossier pour atteindre la racine
set BASE=%ROOT%..

REM
set CONFIG=Debug

echo.
echo ============================================
echo   LetsGoBiking - Lancement des services
echo ============================================
echo.

REM ProxyCacheService
echo [1/4] Lancement du Proxy / Cache...

set PROXY_PATH=%BASE%\ProxyCacheService\bin\%CONFIG%\ProxyCacheService.exe

echo    Chemin attendu : %PROXY_PATH%

if exist "%PROXY_PATH%" (
    echo    ✔ ProxyCacheService trouvé
    cd /d "%BASE%\ProxyCacheService\bin\%CONFIG%"
    start "ProxyCacheService" ProxyCacheService.exe
) else (
    echo    ❌ ERREUR : ProxyCacheService.exe introuvable.
)

timeout /t 2 /nobreak >nul

REM RoutingHost
echo [2/4] Lancement du RoutingHost...

set ROUTING_PATH=%BASE%\RoutingHost\bin\%CONFIG%\RoutingHost.exe

echo    Chemin attendu : %ROUTING_PATH%

if exist "%ROUTING_PATH%" (
    echo    ✔ RoutingHost trouvé
    cd /d "%BASE%\RoutingHost\bin\%CONFIG%"
    start "RoutingHost" RoutingHost.exe
) else (
    echo    ❌ ERREUR : RoutingHost.exe introuvable.
)

timeout /t 2 /nobreak >nul


REM ActiveMqProducer
echo [3/4] Lancement du ActiveMqProducer...

set MQ_EXE=

REM On teste plusieurs emplacements possibles
for %%D in (
    "%BASE%\ActiveMqProducer\bin\%CONFIG%"
    "%BASE%\ActiveMqProducer\bin\%CONFIG%\net8.0"
    "%BASE%\ActiveMqProducer\bin\%CONFIG%\net7.0"
    "%BASE%\ActiveMqProducer\bin\%CONFIG%\net6.0"
    "%BASE%\ActiveMqProducer\bin\%CONFIG%\net5.0"
    "%BASE%\ActiveMqProducer\bin\%CONFIG%\netcoreapp3.1"
    "%BASE%\ActiveMqProducer\bin"
) do (
    if exist "%%D\ActiveMqProducer.exe" (
        set MQ_EXE=%%D\ActiveMqProducer.exe
        goto found_mq
    )
)

echo    ❌ ERREUR : ActiveMqProducer.exe introuvable dans bin\%CONFIG% ni dans les sous-dossiers netX.
goto after_mq

:found_mq
echo    ✔ ActiveMqProducer trouvé : %MQ_EXE%
cd /d "%~dp0"
cd /d "%MQ_EXE:ActiveMqProducer.exe=%"
start "ActiveMqProducer" ActiveMqProducer.exe

:after_mq

timeout /t 2 /nobreak >nul



REM Front
echo [4/4] Lancement du Front...

REM 

set FRONT_DIR=%BASE%\Front\bin\%CONFIG%
set FRONT_DLL=%FRONT_DIR%\Front.dll

echo    Tentative 1 : %FRONT_DLL%

if exist "%FRONT_DLL%" (
    echo    ✔ Front.dll trouvé dans bin\%CONFIG%
    goto run_front
)

set FRONT_DIR=%BASE%\Front\bin
set FRONT_DLL=%FRONT_DIR%\Front.dll

echo    Tentative 2 : %FRONT_DLL%

if exist "%FRONT_DLL%" (
    echo    ✔ Front.dll trouvé dans bin\
    goto run_front
)

echo    ❌ ERREUR : Front.dll introuvable ni dans bin\%CONFIG% ni dans bin\
goto end_front

:run_front
cd /d "%FRONT_DIR%"
echo    Lancement du front via: dotnet Front.dll
start "Front" cmd /c "dotnet Front.dll"

REM 
timeout /t 3 /nobreak >nul

REM 
start "" "http://localhost:61701/Pages/Accueil/index.html"

goto end_front


:end_front


echo.
echo ============================================
echo   Services lancés (si pas d'erreur ci-dessus) :
echo        - ProxyCacheService
echo        - RoutingHost
echo        - ActiveMqProducer
echo        - Front
echo.
echo   A lancer manuellement si besoin :
echo        → HeavyClient (depuis VS ou .exe)
echo ============================================
echo.
pause

endlocal
