
@ECHO OFF
SET LogPath=C:\OEM\AcerLogs\UserAlaunch.log
Echo. >>%LogPath%
ECHO %DATE% %TIME%[Log START]  ============ %~dpnx0 ============ >> %LogPath%
pushd "%~dp0"
ECHO %DATE% %TIME%[Log TRACE]  Enable Delayed environment variable >> %LogPath%
SETLOCAL ENABLEDELAYEDEXPANSION

::: The OS descriptors allow devices to define registry properties for USB devices or composite devices.
::: To configure DShow Bridges using the USB OS Descriptors, the host software should create the following registry key for each USB device interface:
::: 	HKLM\SYSTEM\CurrentControlSet\Enum\USB\<DeviceVID&PID>\<DeviceInstance>\Device Parameters
::: 	DWORD: EnableDshowRedirection
::: The EnableDshowRedirection registry value is a bit mask value which can be used to configure DShow Bridge as described by the table below.
::: Bit mask	Description												Remarks
::: 0x00000001	Opt into DShow Bridge									0 – Opt-out
::: 																	1 – Opt-in
::: 0x00000002	Enable MJPEG decoding in Frame Server (see note below)	0 – MJPEG compressed media type exposed (no operation)
::: 																	1 – Expose the translated uncompressed media types from MJPEG (YUY2)


ECHO %DATE% %TIME%[Log TRACE]  find /i "Product=NB" C:\OEM\PRELOAD\command\PAP\PAP*.ini >>%LogPath%
find /i "Product=NB" C:\OEM\PRELOAD\command\PAP\PAP*.ini >>%LogPath% 2>&1
if %errorlevel% neq 0 ECHO %DATE% %TIME%[Log TRACE]  Not NB image, goto :END >>%LogPath% && goto :END
ECHO %DATE% %TIME%[Log TRACE]  This is NB image, continue... >>%LogPath%

SET Count=0
:SET_Frequency
SET EnableDshow=00000001
ECHO %DATE% %TIME%[Log TRACE]  Set EnableDshow as [%EnableDshow%] >>%LogPath%
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
if exist RestoreCameraDshowBridges.cmd del /f /q RestoreCameraDshowBridges.cmd
ECHO.>>RestoreCameraDshowBridges.cmd
ECHO ECHO %%DATE%% %%TIME%%[Log START]  ============ %%~dpnx0 ============^>^>C:\OEM\AcerLogs\RestoreCameraDshowBridgesByPBR.log 2^>^&1 >>RestoreCameraDshowBridges.cmd
ECHO.>>RestoreCameraDshowBridges.cmd
FOR /F "tokens=1" %%I in (C:\OEM\Camera_%CameraClass%.txt) do (	
	Set /A Count+=1
	ECHO !DATE! !TIME![Log TRACE]  ^|---- %%I start ------- >>%LogPath%
	ECHO !DATE! !TIME![Log TRACE]  REG ADD "HKLM\SYSTEM\CurrentControlSet\Enum\%%I\Device Parameters" /v EnableDshowRedirection /t REG_DWORD /d "%EnableDshow%" /f >>%LogPath%
	REG ADD "HKLM\SYSTEM\CurrentControlSet\Enum\%%I\Device Parameters" /v EnableDshowRedirection /t REG_DWORD /d "%EnableDshow%" /f >>%LogPath% 2>&1
	ECHO ECHO %%DATE%% %%TIME%%[Log TRACE]  REG ADD "HKLM\SYSTEM\CurrentControlSet\Enum\%%I\Device Parameters" /v EnableDshowRedirection /t REG_DWORD /d "%EnableDshow%" /f^>^>C:\OEM\AcerLogs\RestoreCameraDshowBridgesByPBR.log 2^>^&1 >>RestoreCameraDshowBridges.cmd
	ECHO REG ADD "HKLM\SYSTEM\CurrentControlSet\Enum\%%I\Device Parameters" /v EnableDshowRedirection /t REG_DWORD /d "%EnableDshow%" /f^>^>C:\OEM\AcerLogs\RestoreCameraDshowBridgesByPBR.log 2^>^&1 >>RestoreCameraDshowBridges.cmd
	ECHO.>>RestoreCameraDshowBridges.cmd
	ECHO !DATE! !TIME![Log TRACE]  ^|---- %%I END ------- >>%LogPath%	
	IF /I "!Count!" EQU "%CCDAmount%" (
		ECHO !DATE! !TIME![Log TRACE]  All HWIDs have been processed. exit /b 0 >>%LogPath%
		ECHO ECHO %%DATE%% %%TIME%%[Log LEAVE]  ============ %%~dpnx0 ============^>^>C:\OEM\AcerLogs\RestoreCameraDshowBridgesByPBR.log 2^>^&1 >>RestoreCameraDshowBridges.cmd
		ECHO !DATE! !TIME![Log TRACE]  copy /y PBR_RestoreCameraDshowBridges.cmd C:\Recovery\OEM\RestoreOEMCustomize\RestoreCMD >>%LogPath% 2>&1
		copy /y PBR_RestoreCameraDshowBridges.cmd C:\Recovery\OEM\RestoreOEMCustomize\RestoreCMD >>%LogPath% 2>&1
		exit /b 0
	)
)
exit /b 0
