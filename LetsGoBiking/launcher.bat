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
    echo    ERREUR : ProxyCacheService.exe introuvable.
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
    echo   ERREUR : RoutingHost.exe introuvable.
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

echo    ERREUR : ActiveMqProducer.exe introuvable dans bin\%CONFIG% ni dans les sous-dossiers netX.
goto after_mq

:found_mq
echo    ✔ ActiveMqProducer trouvé : %MQ_EXE%
cd /d "%~dp0"
cd /d "%MQ_EXE:ActiveMqProducer.exe=%"
start "ActiveMqProducer" ActiveMqProducer.exe

:after_mq

timeout /t 2 /nobreak >nul


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