@Echo off

ECHO.
ECHO %DATE% %TIME%[Log START]  ============ [SUB]%~dpnx0 ============
:START
pushd "%~dp0"

ECHO %DATE% %TIME%[Log TRACE]  REG Load HKLM\TempHive %OSDrive%\Windows\system32\config\software
REG Load HKLM\TempHive %OSDrive%\Windows\system32\config\software

ECHO %DATE% %TIME%[Log TRACE]  REG import TaskbarLayoutPath.reg
REG import TaskbarLayoutPath.reg

ECHO %DATE% %TIME%[Log TRACE]  REG Unload HKLM\TempHive
REG Unload HKLM\TempHive

:END
popd
ECHO %DATE% %TIME%[Log LEAVE]  ============ %~dpnx0 ============
ECHO.