@echo off
set BASEPATH=C:\buildtest\Commandr\Commandr
set CONFIG=Release

rmdir /S /Q Commandr\bin
rmdir /S /Q Release

mkdir Release

echo -------------------------------------------
echo Compiling
echo -------------------------------------------
rem Compila con el SDK de .NET instalado (dotnet build); el proyecto apunta a net9.0-windows.
dotnet build "%BASEPATH%\FullSolution.sln" -c %CONFIG% -v:q -nologo -clp:ErrorsOnly

robocopy /E %BASEPATH%\Commandr\bin\Release Release

echo -------------------------------------------
echo Packaging
echo -------------------------------------------
SET Seven=%cd%\7za.exe

for /f %%a in ('powershell -Command "Get-Date -format yyyy_MM_dd__HH_mm_ss"') do set datetime=%%a


cd Release

%Seven% a \\COA043\DirdePaso\%datetime%.pkg.7z . -r -x!7za.dll -x!*.pdb -x!SevenZipSharp.dll -x!Updater.exe

cd ..

PAUSE

rmdir /S /Q Release