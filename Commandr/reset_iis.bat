@echo off
setlocal

rem Reinicia IIS y deja activo solo el sitio de FyO (Default Web Site),
rem frenando el/los sitio(s) de BCR (cualquier sitio cuyo nombre contenga "BCR").

rem Sin comillas a proposito: la ruta no tiene espacios, y las comillas al principio del
rem comando de un "for /f" hacen que cmd las recorte mal y el comando falle.
set APPCMD=%windir%\system32\inetsrv\appcmd.exe
set FYO_SITE=Default Web Site

rem iisreset y appcmd requieren permisos de administrador.
net session >nul 2>&1
if errorlevel 1 (
    echo ERROR: hace falta ejecutar el Commander como administrador para reiniciar IIS.
    exit /b 1
)

echo Sitios antes del reinicio:
%APPCMD% list site
echo.

echo Reiniciando IIS...
iisreset
if errorlevel 1 exit /b 1

echo.
echo Frenando sitios de BCR...
set BCR_FOUND=
for /f "delims=" %%s in ('%APPCMD% list site /text:name ^| findstr /i bcr') do (
    set BCR_FOUND=1
    %APPCMD% stop site /site.name:"%%s"
)
if not defined BCR_FOUND echo No se encontro ningun sitio con "BCR" en el nombre.

echo.
echo Activando "%FYO_SITE%"...
set FYO_STATE=
for /f "delims=" %%t in ('%APPCMD% list site "%FYO_SITE%" /text:state') do set FYO_STATE=%%t

if not defined FYO_STATE (
    echo ERROR: no existe el sitio "%FYO_SITE%".
    exit /b 1
)

if /i "%FYO_STATE%"=="Started" (
    echo "%FYO_SITE%" ya estaba iniciado.
) else (
    %APPCMD% start site /site.name:"%FYO_SITE%"
    if errorlevel 1 exit /b 1
)

echo.
echo Sitios despues del reinicio:
%APPCMD% list site
exit /b 0
