

ECHO.
ECHO %DATE% %TIME%[Log START]  ============ [SUB]%~dpnx0 ============

ECHO %DATE% %TIME%[Log TRACE]  DISM /image:%OSDrive%\ /enable-feature /featurename:VirtualMachinePlatform /all /norestart /limitaccess
DISM /image:%OSDrive%\ /enable-feature /featurename:VirtualMachinePlatform /all /norestart /limitaccess

ECHO %DATE% %TIME%[Log LEAVE]  ============ [SUB]%~dpnx0 ============
echo.

