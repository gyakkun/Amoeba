set BATDIR=%~dp0
cd %BATDIR%

set TOOL="C:\Local\Projects\Alliance-Network\Library\Library.Tools\bin\Debug\Library.Tools.exe"
set TARGET="Amoeba\bin\Debug\Core"

md "Amoeba\bin\Debug_1"
rd /s /q "Amoeba\bin\Debug_1\Core"
xcopy %TARGET% "Amoeba\bin\Debug_1\Core" /c /s /e /q /h /i /k /r /y

md "Amoeba\bin\Debug_2"
rd /s /q "Amoeba\bin\Debug_2\Core"
xcopy %TARGET% "Amoeba\bin\Debug_2\Core" /c /s /e /q /h /i /k /r /y

md "Amoeba\bin\Debug_3"
rd /s /q "Amoeba\bin\Debug_3\Core"
xcopy %TARGET% "Amoeba\bin\Debug_3\Core" /c /s /e /q /h /i /k /r /y

md "Amoeba\bin\Debug_4"
rd /s /q "Amoeba\bin\Debug_4\Core"
xcopy %TARGET% "Amoeba\bin\Debug_4\Core" /c /s /e /q /h /i /k /r /y

IF EXIST %TOOL% call %TOOL% "run" "Amoeba\bin\Debug_1\Core\Amoeba.exe" "Amoeba\bin\Debug_1\Core"
IF EXIST %TOOL% call %TOOL% "run" "Amoeba\bin\Debug_2\Core\Amoeba.exe" "Amoeba\bin\Debug_2\Core"
IF EXIST %TOOL% call %TOOL% "run" "Amoeba\bin\Debug_3\Core\Amoeba.exe" "Amoeba\bin\Debug_3\Core"
IF EXIST %TOOL% call %TOOL% "run" "Amoeba\bin\Debug_4\Core\Amoeba.exe" "Amoeba\bin\Debug_4\Core"
