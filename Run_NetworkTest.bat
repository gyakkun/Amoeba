set BATDIR=%~dp0
cd %BATDIR%

set target="Amoeba\bin\Debug\Core"

md "Amoeba\bin\Debug_1"
rd /s /q "Amoeba\bin\Debug_1\Core"
xcopy %target% "Amoeba\bin\Debug_1\Core" /c /s /e /q /h /i /k /r /y

md "Amoeba\bin\Debug_2"
rd /s /q "Amoeba\bin\Debug_2\Core"
xcopy %target% "Amoeba\bin\Debug_2\Core" /c /s /e /q /h /i /k /r /y

md "Amoeba\bin\Debug_3"
rd /s /q "Amoeba\bin\Debug_3\Core"
xcopy %target% "Amoeba\bin\Debug_3\Core" /c /s /e /q /h /i /k /r /y

md "Amoeba\bin\Debug_4"
rd /s /q "Amoeba\bin\Debug_4\Core"
xcopy %target% "Amoeba\bin\Debug_4\Core" /c /s /e /q /h /i /k /r /y

call "..\Library\Library.Tool\bin\Debug\Library.Tool.exe" "run" "Amoeba\bin\Debug_1\Core\Amoeba.exe"  "Amoeba\bin\Debug_1\Core"
call "..\Library\Library.Tool\bin\Debug\Library.Tool.exe" "run" "Amoeba\bin\Debug_2\Core\Amoeba.exe"  "Amoeba\bin\Debug_2\Core"
call "..\Library\Library.Tool\bin\Debug\Library.Tool.exe" "run" "Amoeba\bin\Debug_3\Core\Amoeba.exe"  "Amoeba\bin\Debug_3\Core"
call "..\Library\Library.Tool\bin\Debug\Library.Tool.exe" "run" "Amoeba\bin\Debug_4\Core\Amoeba.exe"  "Amoeba\bin\Debug_4\Core"
