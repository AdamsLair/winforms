:: search for msbuild and make it available
set bb.build.msbuild.exe=
for /D %%D in (%SYSTEMROOT%\Microsoft.NET\Framework\v4*) do set msbuildPath=%%D
set PATH=%PATH%;%msbuildPath%

:: make sure we have a clean release build
msbuild /t:Rebuild /p:Configuration=Release /p:Platform="Any CPU" AdamsLair.WinForms.sln

:: remove any existing nupkg files
del *.nupkg

:: build the nuget packages
.nuget\nuget pack WinForms\AdamsLair.WinForms.csproj -Properties Configuration=Release;Platform=AnyCPU

:: upload the nuget packages
.nuget\nuget push *.nupkg -Source "https://nuget.org"

:: remove nupkg files after uploading them
del *.nupkg
