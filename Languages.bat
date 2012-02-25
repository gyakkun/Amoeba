set BATDIR=%~dp0
cd %BATDIR%
call "..\Library\Library.Tool\bin\Debug\Library.Tool.exe" "languages" %1 %2
