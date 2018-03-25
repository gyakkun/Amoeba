setx DOTNET_CLI_TELEMETRY_OPTOUT 1
set BAT_DIR=%~dp0
set WIN_X64_DIR="Amoeba.Daemon\bin\Release\netcoreapp2.0\win-x64\publish"
set WIN_X86_DIR="Amoeba.Daemon\bin\Release\netcoreapp2.0\win-x86\publish"
set MS_BUILD_EXE="C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\MSBuild.exe"

cd %BAT_DIR%
rd /Q /S "Publish"
mkdir "Publish"

cd %BAT_DIR%
cd "Amoeba.Daemon"
dotnet publish --configuration Release --runtime win-x64

cd %BAT_DIR%
cd "Amoeba.Daemon"
dotnet publish --configuration Release --runtime win-x86

cd %BAT_DIR%
xcopy %WIN_X64_DIR% "Publish\Core\Daemon-x64" /D /S /R /Y /I /K
cd "Publish\Core\Daemon-x64"
del /Q /S *_x86.*

cd %BAT_DIR%
xcopy %WIN_X86_DIR% "Publish\Core\Daemon-x86" /D /S /R /Y /I /K
cd "Publish\Core\Daemon-x86"
del /Q /S *_x64.*

cd %BAT_DIR%
call Scripts\msbuildpath.bat
%MS_BUILD_EXE% "Amoeba.Interface\Wpf\Amoeba.Interface.csproj" /p:Configuration=Release

cd %BAT_DIR%
xcopy "Amoeba.Interface\Wpf\bin\Release\Interface" "Publish\Core\Interface" /D /S /R /Y /I /K

cd %BAT_DIR%
cd "Publish"
del /Q /S *.pdb
del /Q /S *.so
