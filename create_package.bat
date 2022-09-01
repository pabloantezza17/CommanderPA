@echo off
set BASEPATH=C:\buildtest\Commandr\Commandr
set VisualStudioVersion=2022
set VisualStudioEdition=Enterprise
set CONFIG=Release
set PROGRAMFILES(X86)=C:\Program Files
set Installer = "%PROGRAMFILES(X86)%\Microsoft Visual Studio\%VisualStudioVersion%\%VisualStudioEdition%\MSBuild\Current\Bin\MSBuild.exe"

rmdir /S /Q Commandr\bin
rmdir /S /Q Release

mkdir Release

echo -------------------------------------------
echo Compiling
echo -------------------------------------------
"%PROGRAMFILES(X86)%\Microsoft Visual Studio\%VisualStudioVersion%\%VisualStudioEdition%\MSBuild\Current\Bin\MSBuild.exe" "%BASEPATH%\FullSolution.sln" /v:q /m /nr:false /p:WarningLevel=0;Configuration=%CONFIG% /clp:ErrorsOnly

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