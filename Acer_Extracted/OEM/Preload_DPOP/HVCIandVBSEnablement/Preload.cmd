
@echo off
md c:\oem\acerlogs
SET LogPath=c:\oem\acerlogs\%~n0.log
Echo.>>%LogPath%
ECHO %DATE% %TIME%[Log START]  ============ %~dpnx0 ============ >> %LogPath%
pushd "%~dp0"

	
ECHO %DATE% %TIME%[Log TRACE]  [Before] reg query HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard >>%LogPath% 2>&1
reg query HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard >>%LogPath% 2>&1
ECHO. >>%LogPath% 2>&1
ECHO %DATE% %TIME%[Log TRACE]  According to [Windows 11 Security Levels v3.4_20211013.pdf], discard to import VBS registry. >>%LogPath%
REM ECHO %DATE% %TIME%[Log TRACE]  reg add "HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard" /v "EnableVirtualizationBasedSecurity" /t REG_DWORD /d 1 /f >>%LogPath% 2>&1
REM reg add "HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard" /v "EnableVirtualizationBasedSecurity" /t REG_DWORD /d 1 /f >>%LogPath% 2>&1
ECHO. >>%LogPath% 2>&1
ECHO %DATE% %TIME%[Log TRACE]  [After] reg query HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard >>%LogPath% 2>&1
reg query HKLM\SYSTEM\CurrentControlSet\Control\DeviceGuard >>%LogPath% 2>&1
ECHO. >>%LogPath% 2>&1

ECHO %DATE% %TIME%[Log TRACE]  RD C:\OEM\Preload\DPOP\HVCIandVBSEnablement in C:\OEM\Preload\DPOP\CLEANUP\DeleteFolderList.txt
ECHO C:\OEM\Preload\DPOP\HVCIandVBSEnablement>> C:\OEM\Preload\DPOP\CLEANUP\DeleteFolderList.txt

:END
popd
ECHO %DATE% %TIME%[Log LEAVE]  ============ %~dpnx0 ============ >> %LogPath%
Echo.>>%LogPath%
