
@ECHO OFF
SET LogPath=C:\OEM\AcerLogs\UserAlaunch.log
Echo. >>%LogPath%
ECHO %DATE% %TIME%[Log START]  ============ %~dpnx0 ============ >> %LogPath%
pushd "%~dp0"
ECHO %DATE% %TIME%[Log TRACE]  Enable Delayed environment variable >> %LogPath%
SETLOCAL ENABLEDELAYEDEXPANSION

::: 2015/7/23, add OSDs:on-screen displays to remind user that "the Camera is ON or OFF"
::: 2018/12/3, OSDs:on-screen move to [DPOP] SET Camera Indicator
:::	ECHO %DATE% %TIME%[Log TRACE]  reg add "HKLM\SOFTWARE\Microsoft\OEM\Device\Capture" /v NoPhysicalCameraLED /t REG_DWORD /d 1 /f >>%LogPath% 2>&1
::: reg add "HKLM\SOFTWARE\Microsoft\OEM\Device\Capture" /v NoPhysicalCameraLED /t REG_DWORD /d 1 /f >>%LogPath% 2>&1

REM "PowerlineFrequency"=dword:00000002
REM 2 = 60Hz (US, CA, LATAM, KO, TW regions to 60hz)
REM 1 = 50Hz (Else)
REM ------------------------- HWID Example ----------------------------------
REM USB\VID_04FC&PID_2801&MI_00\7&170768C1&0&0000               : 1.3M HD WebCam
REM [With CCD]	 	1 matching device(s) found.
REM [Without CCD]	No matching devices found.
SET Count=0
:SET_Frequency
SET CCDHz=00000001
IF /I EXIST C:\OEM\Preload\Command\POP???????X0*.INI SET CCDHz=00000002
IF /I EXIST C:\OEM\Preload\Command\POP???????X5*.INI SET CCDHz=00000002
IF /I EXIST c:\OEM\Preload\Region\Taiwan.tag SET CCDHz=00000002
ECHO %DATE% %TIME%[Log TRACE]  Frequency as %CCDHz% >>%LogPath%
ECHO.>>%LogPath%
ECHO %DATE% %TIME%[Log TRACE]  call :Get_HWIDs Image >>%LogPath%
call :Get_HWIDs Image
ECHO.>>%LogPath%
ECHO %DATE% %TIME%[Log TRACE]  call :Get_HWIDs Camera >>%LogPath%
call :Get_HWIDs Camera


:END
ECHO %DATE% %TIME%[Log TRACE]  Disable Delayed environment variable >> %LogPath%
SETLOCAL DISABLEDELAYEDEXPANSION
popd
ECHO %DATE% %TIME%[Log LEAVE]  ============ %~dpnx0 ============ >> %LogPath%
Echo. >>%LogPath%
exit /b 0



:Get_HWIDs
SET CameraClass=%1
ECHO %DATE% %TIME%[Log TRACE]  ^|---- Get Camera HWID start ------- >>%LogPath%
C:\OEM\preload\utility\devcon_%Processor_Architecture%.exe find =%CameraClass% > C:\OEM\Camera_%CameraClass%.txt
type C:\OEM\Camera_%CameraClass%.txt >>%LogPath%
ECHO %DATE% %TIME%[Log TRACE]  ^|---- Get Camera HWID  end ------- >>%LogPath%
ECHO %DATE% %TIME%[Log TRACE]  FIND /I "matching device" C:\OEM\Camera_%CameraClass%.txt for get HWIDs amount. >>%LogPath% 2>&1
FOR /F "skip=2 tokens=1 delims= " %%C in ('FIND /I "matching device" C:\OEM\Camera_%CameraClass%.txt') do SET CCDAmount=%%C
ECHO %DATE% %TIME%[Log TRACE]  [CCDAmount] is %CCDAmount% >>%LogPath%
IF /I "%CCDAmount%" EQU "No" (
	ECHO %DATE% %TIME%[Log TRACE]  CCD not found, exit /b 0 >>%LogPath%
	exit /b 0
)
:Parse_HWIDs
if exist RestoreCameraFrequency.cmd del /f /q RestoreCameraFrequency.cmd
ECHO.>>RestoreCameraFrequency.cmd
ECHO ECHO %%DATE%% %%TIME%%[Log START]  ============ %%~dpnx0 ============^>^>C:\OEM\AcerLogs\RestoreCameraFrequencyByPBR.log 2^>^&1 >>RestoreCameraFrequency.cmd
ECHO.>>RestoreCameraFrequency.cmd
FOR /F "tokens=1" %%I in (C:\OEM\Camera_%CameraClass%.txt) do (	
	Set /A Count+=1
	ECHO !DATE! !TIME![Log TRACE]  ^|---- %%I start ------- >>%LogPath%
	ECHO !DATE! !TIME![Log TRACE]  REG ADD "HKLM\SYSTEM\CurrentControlSet\Enum\%%I\Device Parameters" /v PowerlineFrequency /t REG_DWORD /d "%CCDHz%" /f >>%LogPath%
	REG ADD "HKLM\SYSTEM\CurrentControlSet\Enum\%%I\Device Parameters" /v PowerlineFrequency /t REG_DWORD /d "%CCDHz%" /f >>%LogPath% 2>&1
	ECHO ECHO %%DATE%% %%TIME%%[Log TRACE]  REG ADD "HKLM\SYSTEM\CurrentControlSet\Enum\%%I\Device Parameters" /v PowerlineFrequency /t REG_DWORD /d "%CCDHz%" /f^>^>C:\OEM\AcerLogs\RestoreCameraFrequencyByPBR.log 2^>^&1 >>RestoreCameraFrequency.cmd
	ECHO REG ADD "HKLM\SYSTEM\CurrentControlSet\Enum\%%I\Device Parameters" /v PowerlineFrequency /t REG_DWORD /d "%CCDHz%" /f^>^>C:\OEM\AcerLogs\RestoreCameraFrequencyByPBR.log 2^>^&1 >>RestoreCameraFrequency.cmd
	ECHO.>>RestoreCameraFrequency.cmd
	ECHO !DATE! !TIME![Log TRACE]  ^|---- %%I END ------- >>%LogPath%	
	IF /I "!Count!" EQU "%CCDAmount%" (
		ECHO !DATE! !TIME![Log TRACE]  All HWIDs have been processed. exit /b 0 >>%LogPath%
		ECHO ECHO %%DATE%% %%TIME%%[Log LEAVE]  ============ %%~dpnx0 ============^>^>C:\OEM\AcerLogs\RestoreCameraFrequencyByPBR.log 2^>^&1 >>RestoreCameraFrequency.cmd
		ECHO !DATE! !TIME![Log TRACE]  copy /y PBR_RestoreCameraFrequency.cmd C:\Recovery\OEM\RestoreOEMCustomize\RestoreCMD >>%LogPath% 2>&1
		copy /y PBR_RestoreCameraFrequency.cmd C:\Recovery\OEM\RestoreOEMCustomize\RestoreCMD >>%LogPath% 2>&1
		exit /b 0
	)
)
exit /b 0
