set BATDIR=%~dp0
cd %BATDIR%

set target="Amoeba\bin\Debug\Core"

md "Amoeba\bin\Debug 2"
rd /s /q "Amoeba\bin\Debug 2\Core"
xcopy %target% "Amoeba\bin\Debug 2\Core" /c /s /e /q /h /i /k /r /y

md "Amoeba\bin\Debug 3"
rd /s /q "Amoeba\bin\Debug 3\Core"
xcopy %target% "Amoeba\bin\Debug 3\Core" /c /s /e /q /h /i /k /r /y

md "Amoeba\bin\Debug 4"
rd /s /q "Amoeba\bin\Debug 4\Core"
xcopy %target% "Amoeba\bin\Debug 4\Core" /c /s /e /q /h /i /k /r /y

call "..\Library\Library.Tool\bin\Debug\Library.Tool.exe" "run" "Amoeba\bin\Debug 2\Core\Amoeba.exe"  "Amoeba\bin\Debug 2\Core"
call "..\Library\Library.Tool\bin\Debug\Library.Tool.exe" "run" "Amoeba\bin\Debug 3\Core\Amoeba.exe"  "Amoeba\bin\Debug 3\Core"
call "..\Library\Library.Tool\bin\Debug\Library.Tool.exe" "run" "Amoeba\bin\Debug 4\Core\Amoeba.exe"  "Amoeba\bin\Debug 4\Core"
