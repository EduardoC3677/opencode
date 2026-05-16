
ECHO.
ECHO %DATE% %TIME%[Log START]  ============ [SUB]%~dpnx0 ============
pushd "%~dp0"

SET iesupporturl=http://www.acer.com/support
REM SET fURL=http://www.acer.com
REM SET favoritePath=%OSDrive%\Users\Default\Favorites\Acer
REM SET favoriteURL=%OSDrive%\Users\Default\Favorites\Acer\Acer.url
REM ECHO %DATE% %TIME%[Log TRACE]  md "%favoritePath%"
REM md "%favoritePath%"


REM ECHO %DATE% %TIME%[Log TRACE]  Creating %OSDrive%\Users\Default\Favorites\Acer\Acer.url
REM ECHO [InternetShortcut] > "%favoriteURL%"
REM ECHO URL=%fURL% >> "%favoriteURL%"
REM ECHO IDList= >> "%favoriteURL%"
REM ECHO %DATE% %TIME%[Log TRACE]  Type "%favoriteURL%"
REM Type "%favoriteURL%"


ECHO %DATE% %TIME%[Log TRACE]  reg load HKU\DEFAULTTEMP %OSDrive%\users\default\ntuser.dat
reg load HKU\DEFAULTTEMP %OSDrive%\users\default\ntuser.dat

ECHO %DATE% %TIME%[Log TRACE]  REG add "HKU\DEFAULTTEMP\Software\Microsoft\Internet Explorer\Help_Menu_URLs" /v Online_Support /t "REG_SZ" /d %iesupporturl% /f
REG add "HKU\DEFAULTTEMP\Software\Microsoft\Internet Explorer\Help_Menu_URLs" /v Online_Support /t "REG_SZ" /d %iesupporturl% /f

ECHO %DATE% %TIME%[Log TRACE]  reg unload HKU\DEFAULTTEMP
reg unload HKU\DEFAULTTEMP

popd
ECHO %DATE% %TIME%[Log LEAVE]  ============ [SUB]%~dpnx0 ============
ECHO.