set BATDIR=%~dp0
cd %BATDIR%

set LIBRARY_INCREMENT="Omnius\Increment.bat"
IF EXIST %LIBRARY_INCREMENT% call %LIBRARY_INCREMENT%

set BATDIR=%~dp0
cd %BATDIR%

set TOOL="Omnius\Omnius.Tools\bin\Debug\Omnius.Tools.exe"
IF NOT EXIST %TOOL% exit

call %TOOL% "Increment" Amoeba.Core
call %TOOL% "Increment" Amoeba.Interface
call %TOOL% "Increment" Amoeba.Service
call %TOOL% "Increment" Amoeba.Test
