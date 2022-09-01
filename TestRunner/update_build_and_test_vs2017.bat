@echo off

REM
REM %1 = Branch BasePath
REM %2 = Current Directory
REM %3 = Current Branch
REM
REM Build

set VSCMD_START_DIR="C:\Projects\FyO\Corretaje\%3"
call "C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\Common7\Tools\VsDevCmd.bat
echo.
echo Getting latest version on all...
echo.

tf get $/Corretaje/Dev $/Corretaje/Main $/Corretaje/Next $/Corretaje/Live $/Corretaje/R4 /recursive
REM Compile & Update

cd "%2\TestRunner\"
call build_and_test_vs2017.bat %1 %2 %3
