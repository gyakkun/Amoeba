set BATDIR=%~dp0
cd %BATDIR%

set TOOL="Library\Library.Tools\bin\Debug\Library.Tools.exe"

IF EXIST %TOOL% call %TOOL% "Settings" "Amoeba.Wpf\Properties\Settings.cs"
