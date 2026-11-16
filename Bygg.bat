@echo off
setlocal
cd /d "%~dp0"
set CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" (
  echo Fant ikke kompilatoren i Windows.
  pause
  exit /b 1
)
if not exist ikon.ico (
  echo Fant ikke ikon.ico i denne mappen.
  pause
  exit /b 1
)
if not exist lib\Microsoft.Web.WebView2.Core.dll (
  echo Laster ned WebView2-filer, dette skjer bare forste gang...
  powershell -NoProfile -ExecutionPolicy Bypass -Command "[Net.ServicePointManager]::SecurityProtocol=[Net.SecurityProtocolType]::Tls12; $ProgressPreference='SilentlyContinue'; Invoke-WebRequest 'https://www.nuget.org/api/v2/package/Microsoft.Web.WebView2/1.0.2592.51' -OutFile 'wv2.zip'; Expand-Archive wv2.zip -DestinationPath wv2tmp -Force; New-Item -ItemType Directory lib -Force | Out-Null; Copy-Item 'wv2tmp\lib\net462\Microsoft.Web.WebView2.Core.dll','wv2tmp\lib\net462\Microsoft.Web.WebView2.WinForms.dll','wv2tmp\runtimes\win-x64\native\WebView2Loader.dll' lib\; Remove-Item wv2.zip,wv2tmp -Recurse -Force"
)
if not exist lib\Microsoft.Web.WebView2.Core.dll (
  echo Klarte ikke a laste ned WebView2-filene. Sjekk internett og prov igjen.
  pause
  exit /b 1
)
if exist MatteHjelpern.exe del /f /q MatteHjelpern.exe
if exist MatteHjelpern.exe (
  echo Lukk MatteHjelpern forst, og kjor denne filen pa nytt.
  pause
  exit /b 1
)
"%CSC%" /nologo /target:winexe /platform:x64 /out:MatteHjelpern.exe /win32icon:ikon.ico ^
 /resource:index.html,index.html ^
 /resource:lib\Microsoft.Web.WebView2.Core.dll,Microsoft.Web.WebView2.Core.dll ^
 /resource:lib\Microsoft.Web.WebView2.WinForms.dll,Microsoft.Web.WebView2.WinForms.dll ^
 /resource:lib\WebView2Loader.dll,WebView2Loader.dll ^
 /reference:System.dll /reference:System.Windows.Forms.dll /reference:System.Drawing.dll ^
 /reference:lib\Microsoft.Web.WebView2.Core.dll /reference:lib\Microsoft.Web.WebView2.WinForms.dll ^
 Program.cs
if errorlevel 1 (
  echo Noe gikk galt under bygging.
  pause
  exit /b 1
)
echo.
echo Ferdig! MatteHjelpern.exe ligger na i denne mappen.
pause
