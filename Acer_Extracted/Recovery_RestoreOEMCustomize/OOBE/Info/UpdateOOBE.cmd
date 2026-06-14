
@Echo off

:MAIN
SET LogPath=%~d0\OEM\AcerLogs\%1.log
ECHO.>>%LogPath%
ECHO %DATE% %TIME%[Log START]  ============ %~dpnx0 ============ >> %LogPath%
pushd "%~dp0"
SETLOCAL ENABLEDELAYEDEXPANSION

ECHO %DATE% %TIME%[Log TRACE]  REG QUERY HKLM\Software\OEM\Metadata /v Brand /reg:64 ^| find /i "brand">>%LogPath% 2>&1
REG QUERY HKLM\Software\OEM\Metadata /v Brand /reg:64 | find /i "brand">>%LogPath% 2>&1
FOR /F "Tokens=3 Delims= " %%D in ('REG QUERY HKLM\Software\OEM\Metadata /v Brand /reg:64 ^| find /i "brand"') do SET BR=%%D

:::: 2017/2/3
::::	Updated public key for GREG V2,
::::	New version userdata format is changed, add [AcerGREG] registry for identify

:::: 2020/7/29
::::	Execute in C:\Windows\Sysnative failed, move to standalone cmd -- ImportAcerGREG.cmd
::::
REM ECHO %DATE% %TIME%[Log TRACE]  REG ADD HKLM\Software\OEM\Metadata /v "AcerGREG" /t REG_SZ /d "V2" /f /reg:64 >>%LogPath% 2>&1
REM REG ADD HKLM\Software\OEM\Metadata /v "AcerGREG" /t REG_SZ /d "V2" /f reg:64 >>%LogPath% 2>&1
REM ECHO %DATE% %TIME%[Log TRACE]  REG ADD HKLM\Software\OEM\Metadata /v "AcerGREG" /t REG_SZ /d "V2" /f /reg:32 >>%LogPath% 2>&1
REM REG ADD HKLM\Software\OEM\Metadata /v "AcerGREG" /t REG_SZ /d "V2" /f reg:32 >>%LogPath% 2>&1

call :%1

:END
SETLOCAL DISABLEDELAYEDEXPANSION
popd
ECHO %DATE% %TIME%[Log LEAVE]  ============ %~dpnx0 ============ >> %LogPath%
echo. >> %LogPath%
exit /b 0



:AuditAlaunch
::::	2016/7/6
::::	For Support OLD LPCD Script
::::		GENERATE_OOBE.CMD will modify Path C:\Windows\System32\OOBE\Info\Default\%@OOBE_LANG_Folder%\OOBE.XML
::::		Offline RCD won't execute AuditAlaunch module
::::		OOBE.xml should not be modified after NAPP2P
::::	New LPCD Script
::::		GENERATE_OOBE.CMD will modify Path C:\Windows\System32\OOBE\Info\OOBEXML\%BR%\%@OOBE_LANG_Folder%\OOBE.XML

IF EXIST C:\OEM\Preload\Command\PAP\*.* ECHO %DATE% %TIME%[Log TRACE]  This is UserAlaunch, do nothing. && exit /b 0 >>%LogPath% && exit /b 0
if /i "%BR%" EQU "Founder" (
	ECHO %DATE% %TIME%[Log TRACE]  RD /S /Q .\DEFAULT>>%LogPath% 2>&1
	RD /S /Q .\DEFAULT>>%LogPath% 2>&1
)
ECHO %DATE% %TIME%[Log TRACE]  XCOPY .\OOBEXML\%BR%\*.* .\DEFAULT\*.* /vesyf>>%LogPath% 2>&1
XCOPY .\OOBEXML\%BR%\*.* .\DEFAULT\*.* /vesyf>>%LogPath% 2>&1
ECHO %DATE% %TIME%[Log TRACE]  RD /S /Q .\OOBEXML >>%LogPath% 2>&1
RD /S /Q .\OOBEXML >>%LogPath% 2>&1
exit /b 0


:UserAlaunch
ECHO %DATE% %TIME%[Log TRACE]  call :CheckAV >>%LogPath% 2>&1
call :CheckAV
if /i "!AVCase!" equ "McAfee" (
	ECHO !DATE! !TIME![Log TRACE]  This is McAfee case. >>%LogPath% 2>&1
	ECHO !DATE! !TIME![Log TRACE]  XCOPY .\OOBEXML_McAfee\*.* .\OOBEXML\*.* /vesyf >>%LogPath% 2>&1
	XCOPY .\OOBEXML_McAfee\*.* .\OOBEXML\*.* /vesyf >>%LogPath% 2>&1
	ECHO !DATE! !TIME![Log TRACE]  XCOPY .\DEFAULT_McAfee\*.* .\DEFAULT\*.* /vesyf >>%LogPath% 2>&1
	XCOPY .\DEFAULT_McAfee\*.* .\DEFAULT\*.* /vesyf >>%LogPath% 2>&1
) else ( ECHO !DATE! !TIME![Log TRACE]  This is not McAfee case. Keep current OOBEXML/DEFAULT files. >>%LogPath% 2>&1 )

:::: 2017/3/1
::::	Updated HTML format as MS provided for RS2
::::	Updated Public key to AcerGREG V2

:::: 2016/9/6
::::	The OOBE.xml content is different between ZH and TC
::::	Replace 1028\OOBE.xml as 3076's OOBE.xml, due to the OS will load 1028\OOBE.xml for ZH start from RS.

:::: 2016/7/6
::::	Online + No Patch OOBE 	= OOBEXML folder will not exist
::::	Online + Patch OOBE		= OOBEXML folder will exist, if no exist Pending_GenerateOOBE.cmd(OLD LPCD) then skip update OOBE.xml
::::	Offline = OOBEXML folder will exist always, should exist Pending_GenerateOOBE.cmd always and can update OOBE.xml always


:::		--- 2021/9/17 update region start ---
:::
:::		first line command of Pending_GenerateOOBE.cmd, will return below error while call Pending_GenerateOOBE.cmd
:::		Add newline in fisrt line of log file can fix it.
:::			<--- error start --->
:::			'嚜盧script' is not recognized as an internal or external command,
:::			operable program or batch file.
:::			<--- error finish --->
:::
:::		--- 2021/9/17 update region finish ---

IF EXIST .\OOBEXML\*.* (
	IF EXIST Pending_GenerateOOBE.cmd (
		IF EXIST C:\OEM\Preload\Command\PAP\PXP?????????ZH*.INI (
			ECHO !DATE! !TIME![Log TRACE]  ZH LPCD found, copy /y .\OOBEXML\%BR%\3076\OOBE.XML .\OOBEXML\%BR%\1028\OOBE.XML >>%LogPath% 2>&1
			copy /y .\OOBEXML\%BR%\3076\OOBE.XML .\OOBEXML\%BR%\1028\OOBE.XML >>%LogPath% 2>&1
		) ELSE ( ECHO !DATE! !TIME![Log TRACE]  ZH LPCD not found. >>%LogPath% 2>&1 )
		ECHO !DATE! !TIME![Log TRACE]  found Pending_GenerateOOBE.cmd, type Pending_GenerateOOBE.cmd >>%LogPath% 2>&1
		type Pending_GenerateOOBE.cmd >>%LogPath% 2>&1
		ECHO !DATE! !TIME![Log TRACE]  call Pending_GenerateOOBE.cmd and record in Pending_GenerateOOBE.log >>%LogPath% 2>&1
		ECHO.>>C:\OEM\AcerLogs\Pending_GenerateOOBE.log 2>&1
		ECHO.>>C:\OEM\AcerLogs\Pending_GenerateOOBE.log 2>&1
		call Pending_GenerateOOBE.cmd >>C:\OEM\AcerLogs\Pending_GenerateOOBE.log 2>&1
		ECHO.>>%LogPath%
		if /i "%BR%" EQU "Founder" (
			ECHO !DATE! !TIME![Log TRACE]  RD /S /Q .\DEFAULT first for Founder Brand>>%LogPath% 2>&1
			RD /S /Q .\DEFAULT>>%LogPath% 2>&1
		)
		if /i "%BR%" EQU "Altos" (
			ECHO !DATE! !TIME![Log TRACE]  RD /S /Q .\DEFAULT first for Altos Brand>>%LogPath% 2>&1
			RD /S /Q .\DEFAULT>>%LogPath% 2>&1
		)
		ECHO !DATE! !TIME![Log TRACE]  Prepare OOBE.XML by brand >>%LogPath%
		ECHO !DATE! !TIME![Log TRACE]  XCOPY .\OOBEXML\%BR%\*.* .\DEFAULT\*.* /vesyf>>%LogPath% 2>&1
		XCOPY .\OOBEXML\%BR%\*.* .\DEFAULT\*.* /vesyf>>%LogPath% 2>&1
	) ELSE (
		ECHO !DATE! !TIME![Log TRACE]  Pending_GenerateOOBE.cmd not found, skip xcopy OOBEXML >>%LogPath% 2>&1
	)
) ELSE (
	ECHO !DATE! !TIME![Log TRACE]  .\OOBEXML\*.* not found, do nothing. >>%LogPath% 2>&1
)

ECHO %DATE% %TIME%[Log TRACE]  Prepare linkfile1.html by brand >>%LogPath%
ECHO %DATE% %TIME%[Log TRACE]  XCOPY .\%BR%\*.* .\DEFAULT\*.* /vesyf>>%LogPath% 2>&1
XCOPY .\%BR%\*.* .\DEFAULT\*.* /vesyf>>%LogPath% 2>&1
if /i "%BR%" EQU "Founder" (
	ECHO !DATE! !TIME![Log TRACE]  This is Founder Brand, goto :PrepareForPBR >>%LogPath%
	goto :PrepareForPBR
)
if /i "%BR%" EQU "Altos" (
	ECHO !DATE! !TIME![Log TRACE]  This is Altos Brand, goto :PrepareForPBR >>%LogPath%
	goto :PrepareForPBR
)

:::: 2021/5 
::::	--- Remark for RemoveCheckBox1_UEIP.ps1 start ---
::::	Skip to checking and remove UEIP checkbox, due to UEIP CheckBOX be moved to customerinfo, it must exist for OEM registration page

SET bUEIPInstalled=FALSE
ECHO %DATE% %TIME%[Log TRACE]  REG Query "HKLM\SOFTWARE\OEM\User Experience Improvement Program" /reg:64 >>%LogPath% 2>&1
REG Query "HKLM\SOFTWARE\OEM\User Experience Improvement Program" /reg:64 >>%LogPath% 2>&1
ECHO %DATE% %TIME%[Log TRACE]  REG Query "HKLM\SOFTWARE\OEM\User Experience Improvement Program\Version" /reg:64 >>%LogPath% 2>&1
REG Query "HKLM\SOFTWARE\OEM\User Experience Improvement Program\Version" /reg:64 >>%LogPath% 2>&1
if %errorlevel% equ 0 ECHO %DATE% %TIME%[Log TRACE]  UEIP Reg exist, SET bUEIPInstalled=TRUE>>%LogPath% && SET bUEIPInstalled=TRUE
if exist C:\OEM\Preload\UEIP_Installed.tag ECHO %DATE% %TIME%[Log TRACE]  UEIP_Installed.tag found, SET bUEIPInstalled=TRUE>>%LogPath% && SET bUEIPInstalled=TRUE
REM if /i "%bUEIPInstalled%" equ "FALSE" (
	REM ECHO %DATE% %TIME%[Log TRACE]  UEIP Reg or UEIP_Installed.tag not found, UEIP did not be installed.>>%LogPath%
	REM ECHO %DATE% %TIME%[Log TRACE]  powershell -ExecutionPolicy ByPass -command "%~dp0\RemoveCheckBox1_UEIP.ps1">>%LogPath% 2>&1
	REM powershell -ExecutionPolicy ByPass -command "%~dp0\RemoveCheckBox1_UEIP.ps1">>%LogPath% 2>&1
REM )
::::	--- Remark for RemoveCheckBox1_UEIP.ps1 finish ---


:RemoveJointEULA
if /i "!AVCase!" equ "None" (
	ECHO !DATE! !TIME![Log TRACE]  This is without AntiVirus SKU, Remove AntiVirus joint EULA >> %LogPath%
	ECHO !DATE! !TIME![Log TRACE]  XCOPY .\Without_NIS\*.* .\DEFAULT\*.* /vesyf>>%LogPath% 2>&1
	XCOPY .\Without_NIS\*.* .\DEFAULT\*.* /vesyf>>%LogPath% 2>&1
	ECHO !DATE! !TIME![Log TRACE]  powershell -ExecutionPolicy ByPass -command "%~dp0\RemoveCheckBox3_NIS.ps1">>%LogPath% 2>&1
	powershell -ExecutionPolicy ByPass -command "%~dp0\RemoveCheckBox3_NIS.ps1">>%LogPath% 2>&1
)

:CheckEULA_for_ENUS
ECHO %DATE% %TIME%[Log TRACE]  Using 1033 agreement files by region. >>%LogPath%
IF exist "%~d0\OEM\Preload\Command\POP???????X1*.INI" SET EULAPath=agreement_APJ
IF exist "%~d0\OEM\Preload\Command\POP???????X3*.INI" SET EULAPath=agreement_APJ
IF exist "%~d0\OEM\Preload\Command\POP???????X5*.INI" SET EULAPath=agreement_APJ
IF exist "%~d0\OEM\Preload\Command\POP???????X7*.INI" SET EULAPath=agreement_APJ
IF exist "%~d0\OEM\Preload\Command\POP???????X0*.INI" SET EULAPath=agreement_AMS
IF exist "%~d0\OEM\Preload\Command\PAP\PXP?????????E0*.INI" SET EULAPath=agreement_ENUS
if /i "%EULAPath%" neq "" (
	ECHO !DATE! !TIME![Log TRACE]  copy /y .\DEFAULT\1033\%EULAPath%.* .\DEFAULT\1033\agreement.* >> %LogPath% 2>&1
	copy /y .\DEFAULT\1033\%EULAPath%.* .\DEFAULT\1033\agreement.* >> %LogPath% 2>&1
	IF exist "%~d0\OEM\Preload\Command\PAP\PXP?????????AU*.INI" (
		ECHO !DATE! !TIME![Log TRACE]  ENAU LPCD found, copy /y .\DEFAULT\2057\%EULAPath%.* .\DEFAULT\2057\agreement.* >> %LogPath% 2>&1
		copy /y .\DEFAULT\2057\%EULAPath%.* .\DEFAULT\2057\agreement.* >> %LogPath% 2>&1
	) ELSE ( ECHO !DATE! !TIME![Log TRACE]  ENAU LPCD not found. >>%LogPath% )
) ELSE ( ECHO !DATE! !TIME![Log TRACE]  This is EMEA base. No need to revise EULA. >>%LogPath% )

IF EXIST "%~d0\OEM\Preload\Command\PAP\PXP?????????ZH*.INI" (
	ECHO !DATE! !TIME![Log TRACE]  ZH LPCD found, copy /y .\DEFAULT\3076\*.html .\DEFAULT\1028\*.html >>%LogPath% 2>&1
	copy /y .\DEFAULT\3076\*.html .\DEFAULT\1028\*.html >>%LogPath% 2>&1
	ECHO !DATE! !TIME![Log TRACE]  ZH LPCD found, copy /y .\DEFAULT\3076\*.rtf .\DEFAULT\1028\*.rtf >>%LogPath% 2>&1
	copy /y .\DEFAULT\3076\*.rtf .\DEFAULT\1028\*.rtf >>%LogPath% 2>&1
) ELSE ( ECHO !DATE! !TIME![Log TRACE]  ZH LPCD not found. >>%LogPath% 2>&1 )


:UpdateChkBox
if /i "%bUEIPInstalled%" equ "FALSE" (
	ECHO !DATE! !TIME![Log TRACE]  UEIP did not be installed, set customerinfo defaultvalue to false for all regions >>%LogPath%
	ECHO !DATE! !TIME![Log TRACE]  xcopy .\Wihtout_UEIP\*.* .\*.* /vesyf >>%LogPath%
	xcopy .\Wihtout_UEIP\*.* .\*.* /vesyf >>%LogPath% 2>&1
)
IF exist "%~d0\OEM\Preload\Command\POP???????X8*.INI" (
	ECHO !DATE! !TIME![Log TRACE]  This is EMEA Base, set Rest of EMEA checkbox ticked as NoNoNoNo >>%LogPath% 2>&1
	ECHO !DATE! !TIME![Log TRACE]  copy /y OOBE_RestOfEMEA.XML OOBE.XML >>%LogPath% 2>&1
	copy /y OOBE_RestOfEMEA.XML OOBE.XML >>%LogPath% 2>&1
)
FOR /F "Skip=1 Tokens=1,2 delims==" %%C in (CheckBOXSPEC.ini) do (
	ECHO !DATE! !TIME![Log TRACE]  MD %%C >>%LogPath% 2>&1
	MD %%C >>%LogPath% 2>&1
	ECHO !DATE! !TIME![Log TRACE]  Copy /y .\SwitchOOBEXMLCheckBox\OOBE_%%D.XML .\%%C\OOBE.XML >>%LogPath% 2>&1
	Copy /y .\SwitchOOBEXMLCheckBox\OOBE_%%D.XML .\%%C\OOBE.XML >>%LogPath% 2>&1
)

:DiscardOEMPage
if exist C:\OEM\Preload\ResourceCD\*.* (
	ECHO !DATE! !TIME![Log TRACE]  This is China Project, discard the Registration page. >>%LogPath%
	ECHO %DATE% %TIME%[Log TRACE]  powershell -ExecutionPolicy ByPass -command "%~dp0\RemoveRegistration.ps1">>%LogPath% 2>&1
	powershell -ExecutionPolicy ByPass -command "%~dp0\RemoveRegistration.ps1">>%LogPath% 2>&1
)
if exist RemoveRegistration.tag (
	ECHO !DATE! !TIME![Log TRACE]  RemoveRegistration.tag found, discard the Registration page. >>%LogPath%
	ECHO %DATE% %TIME%[Log TRACE]  powershell -ExecutionPolicy ByPass -command "%~dp0\RemoveRegistration.ps1">>%LogPath% 2>&1
	powershell -ExecutionPolicy ByPass -command "%~dp0\RemoveRegistration.ps1">>%LogPath% 2>&1
)

:PrepareForPBR
call :Clearfolder
:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
::
:: Install Priority is after [DPOP] PBR Reset Config, should copy to %~d0\Recovery\OEM\RestoreOEMCustomize
::
:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
ECHO %DATE% %TIME%[Log TRACE]  XCOPY *.* %~d0\Recovery\OEM\RestoreOEMCustomize\OOBE\Info\*.* /vesyf >>%LogPath% 2>&1
ECHO %DATE% %TIME%[Log TRACE]  XCOPY *.* %~d0\Recovery\OEM\RestoreOEMCustomize\OOBE\Info\*.* /vesyf >>C:\OEM\AcerLogs\PrepareForPBR.log 2>&1
XCOPY *.* %~d0\Recovery\OEM\RestoreOEMCustomize\OOBE\Info\*.* /vesyf >>%~d0\OEM\AcerLogs\PrepareForPBR.log 2>&1
exit /b 0


:Clearfolder
ECHO %DATE% %TIME%[Log TRACE]  DEL /F /Q Pending_GenerateOOBE.cmd >>%LogPath% 2>&1
DEL /F /Q Pending_GenerateOOBE.cmd >>%LogPath% 2>&1
ECHO %DATE% %TIME%[Log TRACE]  ECHO "%~dpnx0" to C:\OEM\Preload\DPOP\CLEANUP\DeleteFileList.txt >>%LogPath% 2>&1
ECHO "%~dpnx0">>C:\OEM\Preload\DPOP\CLEANUP\DeleteFileList.txt
ECHO %DATE% %TIME%[Log TRACE]  DEL /F /Q MergeOOBE.vbs >>%LogPath% 2>&1
DEL /F /Q MergeOOBE.vbs >>%LogPath% 2>&1
ECHO %DATE% %TIME%[Log TRACE]  DEL /F /Q *.ps1 >>%LogPath% 2>&1
DEL /F /Q *.ps1 >>%LogPath% 2>&1
ECHO %DATE% %TIME%[Log TRACE]  DEL /F /Q OOBE_RestOfEMEA.XML >>%LogPath% 2>&1
DEL /F /Q OOBE_RestOfEMEA.XML >>%LogPath% 2>&1
ECHO %DATE% %TIME%[Log TRACE]  DEL /F /Q CheckBOXSPEC.ini >>%LogPath% 2>&1
DEL /F /Q CheckBOXSPEC.ini >>%LogPath% 2>&1
ECHO %DATE% %TIME%[Log TRACE]  RD /S /Q .\OOBEXML>>%LogPath%
RD /S /Q .\OOBEXML>>%LogPath% 2>&1
ECHO %DATE% %TIME%[Log TRACE]  RD /S /Q .\OOBEXML_McAfee>>%LogPath%
RD /S /Q .\OOBEXML_McAfee>>%LogPath% 2>&1
ECHO %DATE% %TIME%[Log TRACE]  RD /S /Q .\DEFAULT_McAfee>>%LogPath%
RD /S /Q .\DEFAULT_McAfee>>%LogPath% 2>&1
ECHO %DATE% %TIME%[Log TRACE]  RD /S /Q .\Acer>>%LogPath%
RD /S /Q .\Acer>>%LogPath% 2>&1
ECHO %DATE% %TIME%[Log TRACE]  RD /S /Q .\Without_NIS>>%LogPath%
RD /S /Q .\Without_NIS>>%LogPath% 2>&1
ECHO %DATE% %TIME%[Log TRACE]  RD /S /Q .\Wihtout_UEIP>>%LogPath%
RD /S /Q .\Wihtout_UEIP>>%LogPath% 2>&1
ECHO %DATE% %TIME%[Log TRACE]  RD /S /Q .\Gateway>>%LogPath%
RD /S /Q .\Gateway>>%LogPath% 2>&1
ECHO %DATE% %TIME%[Log TRACE]  RD /S /Q .\Packard>>%LogPath%
RD /S /Q .\Packard>>%LogPath% 2>&1
ECHO %DATE% %TIME%[Log TRACE]  RD /S /Q .\Founder>>%LogPath%
RD /S /Q .\Founder>>%LogPath% 2>&1
ECHO %DATE% %TIME%[Log TRACE]  RD /S /Q .\Altos>>%LogPath%
RD /S /Q .\Altos>>%LogPath% 2>&1
ECHO %DATE% %TIME%[Log TRACE]  RD /S /Q .\SwitchOOBEXMLCheckBox>>%LogPath%
RD /S /Q .\SwitchOOBEXMLCheckBox>>%LogPath% 2>&1
ECHO %DATE% %TIME%[Log TRACE]  RD /S /Q .\Patch>>%LogPath%
RD /S /Q .\Patch>>%LogPath% 2>&1
ECHO %DATE% %TIME%[Log TRACE]  RD /S /Q .\ForGCBase>>%LogPath%
RD /S /Q .\ForGCBase>>%LogPath% 2>&1
exit /b 0


:CheckAV
:::: 2017/3/29
:::: 	- POP or PAP may list the McAfee/Norton, but no install for 1G16G sku
:::: 	detect the installed tag additional, make sure the Joint EULA remove from 1G16G sku.
::::	- MIS_Installed.tag/NIS_Installed.tag comes from McAfee/Norton APP module
::::	- But SWOD will install McAfee/Norton by End User, skip the tag checking.
::::	- Offline RCD wont exist SWOD=Y in the POP.ini, checking PAP for SWOD case as well
SET AVCase=None
if exist C:\OEM\Preload\NIS_Installed.tag (
	ECHO !DATE! !TIME![Log TRACE]  NIS_Installed.tag found, SET AVCase=Norton>>%LogPath%
	SET AVCase=Norton
) else if exist C:\OEM\Preload\MIS_Installed.tag (
	ECHO !DATE! !TIME![Log TRACE]  MIS_Installed.tag found, SET AVCase=McAfee>>%LogPath%
	SET AVCase=McAfee
) else (
	ECHO %DATE% %TIME%[Log TRACE]  NIS_Installed.tag and MIS_Installed.tag not found. >>%LogPath%
	ECHO %DATE% %TIME%[Log TRACE]  Checking if SWOD image... >>%LogPath%
	if exist C:\OEM\PRELOAD\SWOD\SWODList.ini (
		ECHO !DATE! !TIME![Log TRACE]  SWODList.ini found, this is SWOD image. >>%LogPath%
		
		REM check "McAfee Internet Security" module from SWOD list
		ECHO !DATE! !TIME![Log TRACE]  Find /i "MOD01A00R9" C:\OEM\PRELOAD\SWOD\SWODList.ini >>%LogPath% 2>&1
		Find /i "MOD01A00R9" C:\OEM\PRELOAD\SWOD\SWODList.ini >>%LogPath% 2>&1
		if !errorlevel! equ 0 (
			ECHO !DATE! !TIME![Log TRACE]  SWOD-McAfee case, SET AVCase=McAfee>>%LogPath%
			SET AVCase=McAfee
		)
		
		REM check "Norton Internet Security" module from SWOD list
		ECHO !DATE! !TIME![Log TRACE]  Find /i "MOD01APP5M" C:\OEM\PRELOAD\SWOD\SWODList.ini >>%LogPath% 2>&1
		Find /i "MOD01APP5M" C:\OEM\PRELOAD\SWOD\SWODList.ini >>%LogPath% 2>&1
		if !errorlevel! equ 0 (
			ECHO !DATE! !TIME![Log TRACE]  SWOD-Norton case, SET AVCase=Norton>>%LogPath%
			SET AVCase=Norton
		)
		
		REM check "Norton Internet Security L3" module from SWOD list
		ECHO !DATE! !TIME![Log TRACE]  Find /i "MOD01APPA3" C:\OEM\PRELOAD\SWOD\SWODList.ini >>%LogPath% 2>&1
		Find /i "MOD01APPA3" C:\OEM\PRELOAD\SWOD\SWODList.ini >>%LogPath% 2>&1
		if !errorlevel! equ 0 (
			ECHO !DATE! !TIME![Log TRACE]  SWOD-Norton L3 case, SET AVCase=Norton>>%LogPath%
			SET AVCase=Norton
		)
		
		REM check "McAfee LiveSafe P1" module from SWOD list
		ECHO !DATE! !TIME![Log TRACE]  Find /i "MOD01APPC2" C:\OEM\PRELOAD\SWOD\SWODList.ini >>%LogPath% 2>&1
		Find /i "MOD01APPC2" C:\OEM\PRELOAD\SWOD\SWODList.ini >>%LogPath% 2>&1
		if !errorlevel! equ 0 (
			ECHO !DATE! !TIME![Log TRACE]  SWOD-McAfee P1 case, SET AVCase=McAfee>>%LogPath%
			SET AVCase=McAfee
		)
		
		REM check "McAfee LiveSafe P2" module from SWOD list
		ECHO !DATE! !TIME![Log TRACE]  Find /i "MOD01APPC3" C:\OEM\PRELOAD\SWOD\SWODList.ini >>%LogPath% 2>&1
		Find /i "MOD01APPC3" C:\OEM\PRELOAD\SWOD\SWODList.ini >>%LogPath% 2>&1
		if !errorlevel! equ 0 (
			ECHO !DATE! !TIME![Log TRACE]  SWOD-McAfee P2 case, SET AVCase=McAfee>>%LogPath%
			SET AVCase=McAfee
		)
	) else (
		ECHO !DATE! !TIME![Log TRACE]  SWODList.ini not found, this is not SWOD image. >>%LogPath%
	)
)
exit /b 0