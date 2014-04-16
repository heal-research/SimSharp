@ECHO OFF

SET CLEANBEFOREBUILD=

SET SELECTED=
SET CONFIGURATION=
SET PLATFORM=

IF "%~1"=="" GOTO :prompt_solution

SET SELECTED=%1
IF NOT EXIST %SELECTED% (
  ECHO Solution file %SELECTED% could not be found.
  GOTO :end
)
ECHO Building solution %SELECTED% ...
GOTO :config_selection

:prompt_solution
SET /A COUNT=0
FOR /F "tokens=*" %%A IN ('dir /B *.sln') DO (
  CALL :forloopbody "%%A"
)

IF "%COUNT%"=="1" (
  SET SELECTED=%SOLUTIONS.1%
  ECHO Building %SOLUTIONS.1% as it is the only solution that was found ...
  GOTO :config_selection
)

ECHO Found the following solutions:
FOR /F "tokens=2* delims=.=" %%A IN ('SET SOLUTIONS.') DO ECHO %%A = %%B
ECHO.
SET /P SOLUTIONINDEX=Which solution to build? Type the number: 

SET SELECTED=""
FOR /F "tokens=2* delims=.=" %%A IN ('SET SOLUTIONS.') DO (
  IF "%%A"=="%SOLUTIONINDEX%" SET SELECTED=%%B
)

IF %SELECTED%=="" GOTO :eof

:config_selection
IF "%~2"==""  GOTO :prompt_config

SET CONFIGURATION=%~2
ECHO Building configuration %CONFIGURATION% ...
GOTO :platform_selection

:prompt_config
SET /P CONFIGURATION=Which configuration to build [Release]: 
IF "%CONFIGURATION%"=="" SET CONFIGURATION=Release

:platform_selection
IF "%~3"=="" GOTO :prompt_platform
  
SET PLATFORM=%~3
ECHO Building platform %PLATFORM% ...
GOTO :clean

:prompt_platform
SET /P PLATFORM=Which platform to build [Any CPU]: 
IF "%PLATFORM%"=="" SET PLATFORM=Any CPU

:clean
IF "%~4"=="" GOTO :prompt_clean

SET CLEANBEFOREBUILD=%~4
GOTO :main

:prompt_clean
SET /P CLEANBEFOREBUILD=Would you like to clean before building [n]: 
IF "%CLEANBEFOREBUILD%"=="" SET CLEANBEFOREBUILD=n

:main
REM First find the path to the msbuild.exe by performing a registry query
FOR /F "tokens=1,3 delims=	 " %%A IN ('REG QUERY "HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\MSBuild\ToolsVersions\4.0"') DO (
  IF "%%A"=="MSBuildToolsPath" SET MSBUILDPATH=%%B)

REM Then execute msbuild to clean and build the solution
REM Disable that msbuild creates a cache file of the solution
SET MSBuildUseNoSolutionCache=1
REM Run msbuild to clean and then build
IF "%CLEANBEFOREBUILD%" NEQ "n" (
  ECHO Cleaning ...
  %MSBUILDPATH%msbuild.exe %SELECTED% /target:Clean /p:Configuration="%CONFIGURATION%",Platform="%PLATFORM%" /m:2 /nologo /verbosity:q /clp:ErrorsOnly
)
ECHO Building ...
%MSBUILDPATH%msbuild.exe %SELECTED% /target:Build /p:Configuration="%CONFIGURATION%",Platform="%PLATFORM%" /m:2 /nologo /verbosity:q /clp:ErrorsOnly

ECHO.
ECHO DONE.

:end

PAUSE

GOTO :eof

REM This workaround is necessary so that COUNT gets reevaluated
:forloopbody
SET /A COUNT+=1
SET SOLUTIONS.%COUNT%=%1
GOTO :eof
