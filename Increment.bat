set BATDIR=%~dp0
cd %BATDIR%

set TOOL="..\Library\Library.Tool\bin\Debug\Library.Tool.exe"
IF EXIST TOOL call TOOL "increment" %1 %2
