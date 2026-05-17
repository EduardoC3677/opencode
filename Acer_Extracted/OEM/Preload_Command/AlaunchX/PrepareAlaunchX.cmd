
@ECHO OFF
SET LogPath=C:\OEM\AcerLogs\%1.log
ECHO.>>%LogPath%
ECHO %DATE% %TIME%[Log START]  ============ %~dpnx0 ============ >> %LogPath%
pushd "%~dp0"

SET SysPath=c:\windows\system32
rem ECHO %DATE% %TIME%[Log TRACE]  FIND /I "CPU=ARM64" %~d0\OEM\Preload\Command\POP*.INI>>%LogPath% 2>&1
rem FIND /I "CPU=ARM64" %~d0\OEM\Preload\Command\POP*.INI>>%LogPath% 2>&1 && SET SysPath=c:\windows\SySWOW64

ECHO %DATE% %TIME%[Log TRACE]  copy /y acerReboot.exe %SysPath% >>%LogPath% 2>&1
if exist acerReboot.exe (
	copy /y acerReboot.exe %SysPath% >>%LogPath% 2>&1
)
rem call :%1
ECHO %DATE% %TIME%[Log TRACE]  del /f /q acerReboot.exe >>%LogPath%
del /f /q acerReboot.exe >>%LogPath%
popd
ECHO %DATE% %TIME%[Log LEAVE]  ============ %~dpnx0 ============ >> %LogPath%
ECHO.>>%LogPath%
exit /b 0

