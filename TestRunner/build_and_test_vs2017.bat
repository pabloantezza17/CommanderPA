@echo off
set BASEPATH=C:\Projects\FyO\Corretaje\%3
set CONFIG=Debug
set VisualStudioVersion=2019
set VisualStudioEdition=Enterprise

set PROGRAMFILES(X86)=C:\Program Files (x86)

echo %time%
echo.

color 21
echo -------------------------------------------
echo Deteniendo FyOCorSchedulerWinService
echo -------------------------------------------
net stop FyOCorSchedulerWinService 2>nul
echo.

echo -------------------------------------------
echo Compilando FWK                   ( 1 de 12)
echo -------------------------------------------
"%PROGRAMFILES(X86)%\Microsoft Visual Studio\%VisualStudioVersion%\%VisualStudioEdition%\MSBuild\Current\Bin\MSBuild.exe" "%BASEPATH%\src\Fwk\Neoris.FWK.sln" /v:q /m /nr:false /p:WarningLevel=0;Configuration=%CONFIG% /clp:ErrorsOnly

if not %ERRORLEVEL%==0 goto fail 

color 30
echo -------------------------------------------
echo Compilando AppCore               ( 2 de 12)
echo -------------------------------------------
"%PROGRAMFILES(X86)%\Microsoft Visual Studio\%VisualStudioVersion%\%VisualStudioEdition%\MSBuild\Current\Bin\MSBuild.exe" "%BASEPATH%\src\AppCore\FyO.AppCore.sln" /v:q /m /nr:false /p:WarningLevel=0;Configuration=%CONFIG% /clp:ErrorsOnly

if not %ERRORLEVEL%==0 goto fail 

color 72
echo -------------------------------------------
echo Compilando Mae                   ( 3 de 12)
echo -------------------------------------------
"%PROGRAMFILES(X86)%\Microsoft Visual Studio\%VisualStudioVersion%\%VisualStudioEdition%\MSBuild\Current\Bin\MSBuild.exe" "%BASEPATH%\src\FyO.Cor\FyO.Cor.Mae.sln" /v:q /m /nr:false /p:WarningLevel=0;Configuration=%CONFIG% /clp:ErrorsOnly

if not %ERRORLEVEL%==0 goto fail 

color 89
echo -------------------------------------------
echo Compilando Apc                   ( 4 de 12)
echo -------------------------------------------
"%PROGRAMFILES(X86)%\Microsoft Visual Studio\%VisualStudioVersion%\%VisualStudioEdition%\MSBuild\Current\Bin\MSBuild.exe" "%BASEPATH%\src\FyO.Cor\FyO.Cor.Apc.sln" /v:q /m /nr:false /p:WarningLevel=0;Configuration=%CONFIG% /clp:ErrorsOnly

if not %ERRORLEVEL%==0 goto fail 


color 4F
echo -------------------------------------------
echo Compilando Rie                   ( 5 de 12)
echo -------------------------------------------
"%PROGRAMFILES(X86)%\Microsoft Visual Studio\%VisualStudioVersion%\%VisualStudioEdition%\MSBuild\Current\Bin\MSBuild.exe" "%BASEPATH%\src\FyO.Cor\FyO.Cor.Rie.sln" /v:q /m /nr:false /p:WarningLevel=0;Configuration=%CONFIG% /clp:ErrorsOnly

if not %ERRORLEVEL%==0 goto fail  


color 70
echo -------------------------------------------
echo Compilando Con                   ( 6 de 12)
echo -------------------------------------------
"%PROGRAMFILES(X86)%\Microsoft Visual Studio\%VisualStudioVersion%\%VisualStudioEdition%\MSBuild\Current\Bin\MSBuild.exe" "%BASEPATH%\src\FyO.Cor\FyO.Cor.Con.sln" /v:q /m /nr:false /p:WarningLevel=0;Configuration=%CONFIG% /clp:ErrorsOnly

if not %ERRORLEVEL%==0 goto fail 


color 80
echo -------------------------------------------
echo Compilando Apl                   ( 7 de 12)
echo -------------------------------------------
"%PROGRAMFILES(X86)%\Microsoft Visual Studio\%VisualStudioVersion%\%VisualStudioEdition%\MSBuild\Current\Bin\MSBuild.exe" "%BASEPATH%\src\FyO.Cor\FyO.Cor.Apl.sln" /v:q /m /nr:false /p:WarningLevel=0;Configuration=%CONFIG% /clp:ErrorsOnly

if not %ERRORLEVEL%==0 goto fail 


color 8F
echo -------------------------------------------
echo Compilando Doc                   ( 8 de 12)
echo -------------------------------------------
"%PROGRAMFILES(X86)%\Microsoft Visual Studio\%VisualStudioVersion%\%VisualStudioEdition%\MSBuild\Current\Bin\MSBuild.exe" "%BASEPATH%\src\FyO.Cor\FyO.Cor.Doc.sln" /v:q /m /nr:false /p:WarningLevel=0;Configuration=%CONFIG% /clp:ErrorsOnly

if not %ERRORLEVEL%==0 goto fail 


color 20
echo -------------------------------------------
echo Compilando Fac                   ( 9 de 12)
echo -------------------------------------------
"%PROGRAMFILES(X86)%\Microsoft Visual Studio\%VisualStudioVersion%\%VisualStudioEdition%\MSBuild\Current\Bin\MSBuild.exe" "%BASEPATH%\src\FyO.Cor\FyO.Cor.Fac.sln" /v:q /m /nr:false /p:WarningLevel=0;Configuration=%CONFIG% /clp:ErrorsOnly

if not %ERRORLEVEL%==0 goto fail 


color 2F
echo -------------------------------------------
echo Compilando Int                   (10 de 12)
echo -------------------------------------------
"%PROGRAMFILES(X86)%\Microsoft Visual Studio\%VisualStudioVersion%\%VisualStudioEdition%\MSBuild\Current\Bin\MSBuild.exe" "%BASEPATH%\src\FyO.Cor\FyO.Cor.Int.sln" /v:q /m /nr:false /p:WarningLevel=0;Configuration=%CONFIG% /clp:ErrorsOnly

if not %ERRORLEVEL%==0 goto fail 


color 3F
echo -------------------------------------------
echo Compilando Eai                   (11 de 12)
echo -------------------------------------------
"%PROGRAMFILES(X86)%\Microsoft Visual Studio\%VisualStudioVersion%\%VisualStudioEdition%\MSBuild\Current\Bin\MSBuild.exe" "%BASEPATH%\src\FyO.Cor\FyO.Cor.Eai.sln" /v:q /m /nr:false /p:WarningLevel=0;Configuration=%CONFIG% /clp:ErrorsOnly

if not %ERRORLEVEL%==0 goto fail 


color 21
echo -------------------------------------------
echo Compilando Log                   (12 de 12)
echo -------------------------------------------
"%PROGRAMFILES(X86)%\Microsoft Visual Studio\%VisualStudioVersion%\%VisualStudioEdition%\MSBuild\Current\Bin\MSBuild.exe" "%BASEPATH%\src\FyO.Cor\FyO.Cor.Log.sln" /v:q /m /nr:false /p:WarningLevel=0;Configuration=%CONFIG% /clp:ErrorsOnly

if not %ERRORLEVEL%==0 goto fail 

:end

echo %time%
echo.
color
     
echo TEST RUN
START %2\TestRunner\TestRunner.exe %3
pause
exit /b 0

:fail
color 07

echo.
echo.
echo Failed
echo.
pause
exit /b 1