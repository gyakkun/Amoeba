set BATDIR=%~dp0
cd %BATDIR%

set TARGET="Amoeba.Interface\bin\Debug\Core"

md "Amoeba.Interface\bin\Debug_1"
rd /s /q "Amoeba.Interface\bin\Debug_1\Core"
xcopy %TARGET% "Amoeba.Interface\bin\Debug_1\Core" /c /s /e /q /h /i /k /r /y

md "Amoeba.Interface\bin\Debug_2"
rd /s /q "Amoeba.Interface\bin\Debug_2\Core"
xcopy %TARGET% "Amoeba.Interface\bin\Debug_2\Core" /c /s /e /q /h /i /k /r /y

md "Amoeba.Interface\bin\Debug_3"
rd /s /q "Amoeba.Interface\bin\Debug_3\Core"
xcopy %TARGET% "Amoeba.Interface\bin\Debug_3\Core" /c /s /e /q /h /i /k /r /y
