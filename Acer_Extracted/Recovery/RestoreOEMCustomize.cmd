
@ECHO OFF
ECHO.
ECHO %DATE% %TIME%[Log START]  ============ [SUB]%~dpnx0 ============
pushd "%~dp0"
pushd .\RestoreOEMCustomize

:Restore
ECHO %DATE% %TIME%[Log TRACE]  Got OSDrive is [%OSDrive%]
ECHO %DATE% %TIME%[Log TRACE]  XCOPY .\OOBE\*.* %OSDrive%\Windows\System32\OOBE\*.* /vesyf
XCOPY .\OOBE\*.* %OSDrive%\Windows\System32\OOBE\*.* /vesyf
if exist .\layoutmodification.xml (
	ECHO %DATE% %TIME%[Log TRACE]  Copy /y .\layoutmodification.xml %OSDrive%\Users\Default\AppData\Local\Microsoft\Windows\Shell\
	Copy /y .\layoutmodification.xml %OSDrive%\Users\Default\AppData\Local\Microsoft\Windows\Shell\
)
if exist .\layoutmodification.json (
	ECHO %DATE% %TIME%[Log TRACE]  Copy /y .\layoutmodification.json %OSDrive%\Users\Default\AppData\Local\Microsoft\Windows\Shell\
	Copy /y .\layoutmodification.json %OSDrive%\Users\Default\AppData\Local\Microsoft\Windows\Shell\
) 
if exist .\TaskbarLayoutModification.xml (
	ECHO %DATE% %TIME%[Log TRACE]  Copy /y .\TaskbarLayoutModification.xml %OSDrive%\Users\Default\AppData\Local\Microsoft\Windows\Shell\
	Copy /y .\TaskbarLayoutModification.xml %OSDrive%\Users\Default\AppData\Local\Microsoft\Windows\Shell\
)
ECHO %DATE% %TIME%[Log TRACE]  Copy /y .\unattend.xml %OSDrive%\Windows\Panther\
Copy /y .\unattend.xml %OSDrive%\Windows\Panther\


:: 2015/8/11
::		HiddenFolderList.txt was come from Cleanup module
ECHO %DATE% %TIME%[Log TRACE]  Hide folder that list in the HiddenFolderList.txt
FOR /f "delims=" %%F IN (HiddenFolderList.txt) DO (
	if exist "%OSDrive%\%%~nxF" (
		ECHO !DATE! !TIME![Log TRACE]  attrib +h "%OSDrive%\%%~nxF"
		attrib +h "%OSDrive%\%%~nxF"
	)
)
:: 2017/6/20
::		Add runonce to hide folders again
::		due to in W10 S image, the %OSDrive%\OEM did not exist when call FactoryReset_AfterImageApply.cmd
::		[HiddenFolderList_PBR.txt] was come from Cleanup module
ECHO %DATE% %TIME%[Log TRACE]  reg load HKLM\TempHive %OSDrive%\Windows\System32\Config\SOFTWARE
reg load HKLM\TempHive %OSDrive%\Windows\System32\Config\SOFTWARE
FOR /f "delims=" %%F IN (HiddenFolderList_PBR.txt) DO (
	ECHO !DATE! !TIME![Log TRACE]  reg add HKLM\TempHive\Microsoft\Windows\CurrentVersion\RunOnce /v "Hide%%~nxFDir" /t REG_EXPAND_SZ /d "attrib +h \"%%~F\"" /f
	reg add HKLM\TempHive\Microsoft\Windows\CurrentVersion\RunOnce /v "Hide%%~nxFDir" /t REG_EXPAND_SZ /d "attrib +h \"%%~F\"" /f
)
ECHO %DATE% %TIME%[Log TRACE]  reg unload HKLM\TempHive
reg unload HKLM\TempHive

ECHO %DATE% %TIME%[Log TRACE]  DIR /S /B .\RestoreCMD\*.cmd
DIR /S /B .\RestoreCMD\*.cmd
FOR /F "DELIMS=" %%C in ('DIR /S /B .\RestoreCMD\*.cmd') DO (
	ECHO !DATE! !TIME![Log TRACE]  call %%C
	call %%C
)
popd

:END
popd

ECHO %DATE% %TIME%[Log LEAVE]  ============ [SUB]%~dpnx0 ============
ECHO.