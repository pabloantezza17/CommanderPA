@echo off

taskkill /im commandr* > nul
taskkill /im mailspy* > nul
taskkill /im helper* > nul
taskkill /im testrunner* > nul

robocopy /MIR %TEMP%\Commandr %1

del %1\install.bat

start %1\Commandr.exe

pause > nul