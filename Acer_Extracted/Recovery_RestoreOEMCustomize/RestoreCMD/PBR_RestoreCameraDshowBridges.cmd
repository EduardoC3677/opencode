
ECHO.
ECHO %DATE% %TIME%[Log START]  ============ [SUB]%~dpnx0 ============
pushd "%~dp0"
ECHO %DATE% %TIME%[Log TRACE]  reg load HKLM\Temp %OSDrive%\Windows\System32\Config\SOFTWARE
reg load HKLM\Temp %OSDrive%\Windows\System32\Config\SOFTWARE
ECHO %DATE% %TIME%[Log TRACE]  reg add HKLM\Temp\Microsoft\Windows\CurrentVersion\RunOnce /v RestoreCameraDshowBridges /t REG_EXPAND_SZ /d "C:\OEM\PRELOAD\DPOP\SetDshowBridgesforUVC\RestoreCameraDshowBridges.cmd" /f
reg add HKLM\Temp\Microsoft\Windows\CurrentVersion\RunOnce /v RestoreCameraDshowBridges /t REG_EXPAND_SZ /d "C:\OEM\PRELOAD\DPOP\SetDshowBridgesforUVC\RestoreCameraDshowBridges.cmd" /f
ECHO %DATE% %TIME%[Log TRACE]  reg unload HKLM\Temp
reg unload HKLM\Temp
popd
ECHO %DATE% %TIME%[Log LEAVE]  ============ [SUB]%~dpnx0 ============
ECHO.