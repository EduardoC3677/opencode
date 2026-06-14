:: # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # #
:: #                                                               #
:: # This script sets up the client machine with OEM settings      #
:: # for Office 2016. This script is meant to be run on the client #
:: # machine after installation.                                   #
:: #                                                               #
:: # For more information, please read the documentation.          #
:: #                                                               #
:: # Date created: Feb 19th, 2016                                  #
:: #                                                               #
:: # Copyright Microsoft Corporation 2016.                         #
:: #                                                               #
:: # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # # #

@echo off

setLocal
cls

set str=%~n0
for /f "tokens=1,2,3 delims=." %%a in ("%str%") do set oobeMode=%%b& set referral=%%c


for /f "skip=2 tokens=2,*" %%a in ('reg.exe query "HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\RecoveryEnvironment" /v "TargetOS"') do set "TargetOS=%%b"
for %%A in (%TargetOS%) do set "TargetOSDrive=%%~dA"

set TARGETOSSYSTEMFOLDER=%TargetOS%\system32
set TARGETOSREGISTRY=%TARGETOSSYSTEMFOLDER%\config\software
set TEMPREGISTRY=HKEY_LOCAL_MACHINE\TEMPHIVE

REG LOAD %TEMPREGISTRY% %TARGETOSREGISTRY%

set wow=true

::
:: Taking care of arguments
::

:startArgs
if "%1"=="" goto endArgs
set key=%1
set value=%2
set setVal=0
if /I "%key%"=="Mode" (set oobeMode=%value%&set setVal=2
) else (
if /I "%key%"=="Referral" (set referral=%value%&set setVal=2
) else (
if NOT "%value%"=="1" (exit /B 1)
))
if %setVal%==0 (exit /B 1)
if %setVal%==2 shift
shift
goto startArgs
:endArgs

::
:: Validate the Mode parameter is supported
::

if /I "%oobeMode%"  NEQ "" (
	if /I "%oobeMode%" NEQ "OEM" (
		if /I "%oobeMode%"  NEQ "OEMTA" (
			if /I "%oobeMode%"  NEQ "OEMPIPC" (
				if /I "%oobeMode%" NEQ "OEMPIPCHYBRID" (
					if /I "%oobeMode%" NEQ "OEMSMB" (
						if /I "%oobeMode%" NEQ "CLOUDBOOK" (
							echo Invalid Mode parameter passed in: %oobemode%&echo Invalid Mode parameter passed in: %oobemode% >>%TEMP%\oobe.log&exit /B 1
						)
					)
				)
			)
		)
	)
)

::
:: Display selected options
::

if /I "%oobeMode%" NEQ "" (
	echo Mode = %oobeMode%&echo Mode = %oobeMode% >>%TEMP%\oobe.log
) else (
	echo Mode = OEM&echo Mode = OEM >>%TEMP%\oobe.log
)

if /I "%oobeMode%" NEQ "OEMPIPC" (
	if /I "%oobeMode%" NEQ "OEMPIPCHYBRID" (
		if /I "%oobeMode%" NEQ "OEMSMB" (
			if NOT "%referral%"=="" (echo Referral = %referral%& echo echo Referral = %referral% >>%TEMP%\oobe.log) else (echo No Referral code given&echo No Referral code given >>%TEMP%\oobe.log)
)))

::
:: Add the HardwareId
::

call :generateGUID GUID

::Add to the virtual registry location
reg add %TEMPREGISTRY%\Microsoft\Office\16.0\Common\OEM /v OOBE /t REG_SZ /d %GUID%  /f >>%TEMP%\oobe.log

if "%PROCESSOR_ARCHITECTURE%" NEQ "x86" (
	reg add %TEMPREGISTRY%\Wow6432Node\Microsoft\Office\16.0\Common\OEM /v OOBE /t REG_SZ /d %GUID% /f >>%TEMP%\oobe.log
)

::
:: Add the OOBEMode - default is OEM
::

::Add to the virtual registry location
if /I "%oobeMode%"=="" (
	reg add %TEMPREGISTRY%\Microsoft\Office\16.0\Common\OEM /v OOBEMode /t REG_SZ /d OEM /f >>%TEMP%\oobe.log

	if /I "%PROCESSOR_ARCHITECTURE%" NEQ "x86" (
		reg add %TEMPREGISTRY%\Wow6432Node\Microsoft\Office\16.0\Common\OEM /v OOBEMode /t REG_SZ /d OEM /f >>%TEMP%\oobe.log
	)
) else (
	reg add %TEMPREGISTRY%\Microsoft\Office\16.0\Common\OEM /v OOBEMode /t REG_SZ /d %oobeMode% /f >>%TEMP%\oobe.log

	if /I "%PROCESSOR_ARCHITECTURE%" NEQ "x86" (
		reg add %TEMPREGISTRY%\Wow6432Node\Microsoft\Office\16.0\Common\OEM /v OOBEMode /t REG_SZ /d %oobeMode% /f >>%TEMP%\oobe.log
	)
)

::
:: Add Cloudbook key
::

::Add to the virtual registry location
reg add %TEMPREGISTRY%\Microsoft\Windows\CurrentVersion\C20CDB00F /t REG_SZ /f >>%TEMP%\oobe.log

if /I "%PROCESSOR_ARCHITECTURE%" NEQ "x86" (
	reg add %TEMPREGISTRY%\WOW6432Node\Microsoft\Windows\CurrentVersion\C20CDB00F /t REG_SZ /f >>%TEMP%\oobe.log
)

::
:: Add the ReferralData
::

::Add to the virtual registry location
if /I "%oobeMode%" NEQ "OEMPIPC" (
	if /I "%oobeMode%" NEQ "OEMPIPCHYBRID" (
		if /I "%oobeMode%" NEQ "OEMSMB" (
			if "%referral%" NEQ "" (
				reg add %TEMPREGISTRY%\Microsoft\OfficeSoftwareProtectionPlatform /v ReferralData /t REG_DWORD /d "%referral%" /f >>%TEMP%\oobe.log

				if /I "%PROCESSOR_ARCHITECTURE%" NEQ "x86" (
					reg add %TEMPREGISTRY%\Wow6432Node\Microsoft\OfficeSoftwareProtectionPlatform /v ReferralData /t REG_DWORD /d "%referral%" /f >>%TEMP%\oobe.log
				)
			)
		)
	)
)

::
:: Add the Teams Installer
::

::Add to the virtual registry location
::Add to the virtual registry location
if /I "%PROCESSOR_ARCHITECTURE%" EQU "x86" (
reg add %TEMPREGISTRY%\Microsoft\Windows\CurrentVersion\Run /v TeamsMachineInstaller /t REG_SZ /d "%TargetOSDrive%\Program Files (x86)\Teams Installer\Teams.exe --checkInstall --source=default" /f >>%TEMP%\oobe.log
)

if /I "%PROCESSOR_ARCHITECTURE%" NEQ "x86" (
              reg add %TEMPREGISTRY%\WOW6432Node\Microsoft\Windows\CurrentVersion\Run /v TeamsMachineInstaller /t REG_SZ /d "%TargetOSDrive%\Program Files (x86)\Teams Installer\Teams.exe --checkInstall --source=default" /f >>%TEMP%\oobe.log
)

::
:: Write OOBE Express Lane Update keys
::

set uoRegPath=%TEMPREGISTRY%\Microsoft\WindowsUpdate\Orchestrator\UScheduler_Oobe\OfficeClickToRun 
set oobeUpdateCmdKey="CmdLine"
set oobeUpdateCmd="%TargetOSDrive%\Program Files\Common Files\Microsoft Shared\ClickToRun\OfficeC2RClient.exe"
set oobeUpdateArgsKey="startArg"
set oobeUpdateArgs="/update SCHEDULEDTASK triggeringUI=AprilForkExpressLane skipMoratorium=true displayLevel=none"
set oobeUpdateMetadataKey="updaterClientMetadata"
set oobeUpdateMetadata="{\"NdupProperties\":{}, \"AllowedFailureCount\":4, \" UpdaterOobeTimeout\":30, \"updaterMinsToWaitAfterOobeFailure\":30, \"BusinessCritical\":true}"

reg add "%uoRegPath%" /f /t REG_EXPAND_SZ /v %oobeUpdateCmdKey% /d %oobeUpdateCmd% >>%TEMP%\oobe.log
reg add "%uoRegPath%" /f /t REG_SZ /v %oobeUpdateArgsKey% /d %oobeUpdateArgs% >>%TEMP%\oobe.log
reg add "%uoRegPath%" /f /t REG_SZ /v %oobeUpdateMetadataKey% /d %oobeUpdateMetadata% >>%TEMP%\oobe.log

set c2rRegPath=%TEMPREGISTRY%\Microsoft\Office\ClickToRun
set c2rOobeKey="OOBEExpress"
set c2rOobe="true"

reg add "%c2rRegPath%" /f /t REG_SZ /v %c2rOobeKey% /d %c2rOobe% >>%TEMP%\oobe.log

::
:: Update Registry Permissions
::

echo %TEMPREGISTRY%\Microsoft\Office\16.0\Common\OEM [1 5 7 11 17] >>%TEMP%\regsettings.txt
echo %TEMPREGISTRY%\Microsoft\OfficeSoftwareProtectionPlatform [1 5 7 11 17] >>%TEMP%\regsettings.txt
echo %TEMPREGISTRY%\Microsoft\Windows\CurrentVersion\C20CDB00F [1 5 7 11 17] >>%TEMP%\regsettings.txt
echo %TEMPREGISTRY%\Microsoft\Windows\CurrentVersion\Run [1 5 7 11 17] >>%TEMP%\regsettings.txt

if /I "%PROCESSOR_ARCHITECTURE%" NEQ "x86" (
	echo %TEMPREGISTRY%\WOW6432Node\Microsoft\Office\16.0\Common\OEM [1 5 7 11 17] >>%TEMP%\regsettings.txt
	echo %TEMPREGISTRY%\WOW6432Node\Microsoft\OfficeSoftwareProtectionPlatform [1 5 7 11 17] >>%TEMP%\regsettings.txt
	echo %TEMPREGISTRY%\WOW6432Node\Microsoft\Windows\CurrentVersion\C20CDB00F [1 5 7 11 17] >>%TEMP%\regsettings.txt
	echo %TEMPREGISTRY%\WOW6432Node\Microsoft\Windows\CurrentVersion\Run [1 5 7 11 17] >>%TEMP%\regsettings.txt
)

%TARGETOSSYSTEMFOLDER%\regini %TEMP%\regsettings.txt
del %TEMP%\regsettings.txt

REG UNLOAD %TEMPREGISTRY%

goto :eof

:GenerateGUID <return>
setlocal enabledelayedexpansion
set "fullGUID=" 
for /L %%n in (1,1,32) do ( 
	if "%%~n"=="9"  set "fullGUID=!fullGUID!-" 
	if "%%~n"=="13" set "fullGUID=!fullGUID!-" 
	if "%%~n"=="17" set "fullGUID=!fullGUID!-" 
	if "%%~n"=="21" set "fullGUID=!fullGUID!-" 

	set /a "currentDigit=!random! %% 16" 

	if "!currentDigit!"=="10" set "currentDigit=A" 
	if "!currentDigit!"=="11" set "currentDigit=B" 
	if "!currentDigit!"=="12" set "currentDigit=C" 
	if "!currentDigit!"=="13" set "currentDigit=D" 
	if "!currentDigit!"=="14" set "currentDigit=E" 
	if "!currentDigit!"=="15" set "currentDigit=F" 

	set "fullGUID=!fullGUID!!currentDigit!" 
) 
set "fullGUID={!fullGUID!}
endlocal & if not "%~1"=="" set "%~1=%fullGUID%" 
goto :eof

