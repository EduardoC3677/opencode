
ECHO.
ECHO %DATE% %TIME%[Log START]  ============ [SUB]%~dpnx0 ============
pushd "%~dp0"

md %OSDrive%\OEM\Preload\DPOP\HVCIandVBSEnablement
ECHO %DATE% %TIME%[Log TRACE]  copy /y EnableVBS.txt %OSDrive%\OEM\Preload\DPOP\HVCIandVBSEnablement\Preload.cmd
copy /y EnableVBS.txt %OSDrive%\OEM\Preload\DPOP\HVCIandVBSEnablement\Preload.cmd

popd
ECHO %DATE% %TIME%[Log LEAVE]  ============ [SUB]%~dpnx0 ============
ECHO.