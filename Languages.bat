set BATDIR=%~dp0
cd %BATDIR%

set TOOL="Library\Library.Tools\bin\Debug\Library.Tools.exe"

IF EXIST %TOOL% call %TOOL% "Languages" "Amoeba.Wpf\Properties\LanguagesManager.cs" "Amoeba.Wpf\Languages"
