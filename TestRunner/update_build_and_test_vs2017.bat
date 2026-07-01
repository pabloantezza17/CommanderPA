@echo off

REM
REM %1 = Branch BasePath
REM %2 = Current Directory
REM %3 = Current Branch
REM
REM Build

set VSCMD_START_DIR="C:\Project\FyO\Corretaje\%3"
call "C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\Tools\VsDevCmd.bat
echo.
echo Getting latest version on all...
echo.

tf get $/Corretaje/Dev $/Corretaje/Live $/Corretaje/R18 /recursive

REM Compile & Update

cd "%2\TestRunner\"
call build_and_test_vs2017.bat %1 %2 %3