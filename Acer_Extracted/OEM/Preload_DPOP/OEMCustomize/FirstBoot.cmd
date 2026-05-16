
@ECHO OFF
md C:\OEM\AcerLogs
Set LogPath=C:\OEM\AcerLogs\FirstBoot.log
ECHO.>>%LogPath%
ECHO.>>%LogPath%
ECHO %DATE% %TIME%[Log START]  ============ %~dpnx0 ============ >> %LogPath%
setlocal enabledelayedexpansion
pushd "%~dp0"

ECHO %DATE% %TIME%[Log TRACE]  start /b c:\OEM\preload\command\alaunchx\AlaunchX.exe /FirstBoot >>%LogPath%
start /b c:\OEM\preload\command\alaunchx\AlaunchX.exe /FirstBoot

if exist .\FirstBootCMD\CMDList.txt (
	cd FirstBootCMD
	ECHO !DATE! !TIME![Log TRACE]  type .\FirstBootCMD\CMDList.txt >>%LogPath% 2>&1
	type .\CMDList.txt >>%LogPath% 2>&1
	for /f "delims=" %%C in (.\CMDList.txt) do (
		ECHO !DATE! !TIME![Log TRACE]  call %%C >>%LogPath% 2>&1
		call %%C >>%LogPath% 2>&1
		ECHO !DATE! !TIME![Log TRACE]  Clean %%C >>%LogPath% 2>&1
		ECHO.>%%C
		ECHO !DATE! !TIME![Log TRACE]  del /f /q %%C >>%LogPath% 2>&1
		del /f /q %%C
	)
	cd..
) else ( ECHO !DATE! !TIME![Log TRACE]  .\FirstBootCMD\CMDList.txt not found. >>%LogPath% 2>&1 )

ECHO %DATE% %TIME%[Log TRACE]  DIR /S /B .\FirstBootCMD\*.cmd >>%LogPath% 2>&1
DIR /S /B .\FirstBootCMD\*.cmd >>%LogPath% 2>&1
FOR /F "DELIMS=" %%C in ('DIR /S /B .\FirstBootCMD\*.cmd') DO (
	ECHO %DATE% %TIME%[Log TRACE]  call %%C >>%LogPath% 2>&1
	call %%C >>%LogPath% 2>&1
)

ECHO %DATE% %TIME%[Log TRACE]  RD %~dp0FirstBootCMD in C:\OEM\Preload\DPOP\CLEANUP\DeleteFolderList.txt >>%LogPath% 2>&1
ECHO %~dp0FirstBootCMD>> C:\OEM\Preload\DPOP\CLEANUP\DeleteFolderList.txt


popd
setlocal disabledelayedexpansion
ECHO %DATE% %TIME%[Log LEAVE]  ============ %~dpnx0 ============ >> %LogPath%
ECHO.>>%LogPath%
ECHO.>>%LogPath%

::::		Region Start: Comes from UEIP module for fix RS4 firstlogon process with exception issue
pushd C:\OEM\Preload\APP\UEIPFramework\5.00.3018\
setlocal

SET LogPath=C:\OEM\AcerLogs\DetectUserChoices.log
ECHO.>>%LogPath%
ECHO.>>%LogPath%
ECHO %DATE% %TIME%[Log TRACE]  call %CD%\DetectUserChoices_WN.cmd start>>%LogPath%

call DetectUserChoices_WN.cmd

ECHO %DATE% %TIME%[Log TRACE]  rd /s /q C:\OEM\Preload\APP\UEIPFramework in C:\OEM\Preload\DPOP\CLEANUP\DeleteFolderList.txt >>%LogPath%
ECHO C:\OEM\Preload\APP\UEIPFramework>>C:\OEM\Preload\DPOP\CLEANUP\DeleteFolderList.txt
ECHO %DATE% %TIME%[Log TRACE]  call %CD%\DetectUserChoices_WN.cmd finish>>%LogPath%
ECHO.>>%LogPath%
ECHO.>>%LogPath%

endlocal
popd
::::		Region Finish: Comes from UEIP module for fix RS4 firstlogon process with exception issue


::::		Region Start: Comes from Power Management Settings module for reduce first boot time 
setlocal

SET LogPath=C:\OEM\AcerLogs\PowerManagementSetting.log
ECHO.>>%LogPath%
ECHO.>>%LogPath%
ECHO %DATE% %TIME%[Log TRACE]  call [c:\OEM\Preload\APP\PowerSettings\PowerManagement.cmd FirstBoot] start >>%LogPath%

call c:\OEM\Preload\APP\PowerSettings\PowerManagement.cmd FirstBoot

ECHO %DATE% %TIME%[Log TRACE]  rd c:\OEM\Preload\APP\PowerSettings in C:\OEM\Preload\DPOP\CLEANUP\DeleteFolderList.txt>>%LogPath%
ECHO c:\OEM\Preload\APP\PowerSettings>>C:\OEM\Preload\DPOP\CLEANUP\DeleteFolderList.txt
ECHO %DATE% %TIME%[Log TRACE]  call [c:\OEM\Preload\APP\PowerSettings\PowerManagement.cmd FirstBoot] finish >>%LogPath%
ECHO.>>%LogPath%
ECHO.>>%LogPath%

endlocal
::::		Region Finish: Comes from Power Management Settings module for reduce first boot time 


::::		Region Start: Comes from [DPOP] Office Settings module for reduce first boot time 
setlocal


IF "%NewFirstBootLogPath%"=="" SET NewFirstBootLogPath=c:\OEM\AcerLogs\NewFirstBoot.log
ECHO.>>%NewFirstBootLogPath%
ECHO.>>%NewFirstBootLogPath%
ECHO %DATE% %TIME%[Log TRACE]  call C:\OEM\Preload\DPOP\OfficeSettings\__RemoveNoNecessarySource.cmd start >>%NewFirstBootLogPath%

call C:\OEM\Preload\DPOP\OfficeSettings\__RemoveNoNecessarySource.cmd 

ECHO %DATE% %TIME%[Log TRACE]  call C:\OEM\Preload\DPOP\OfficeSettings\__RemoveNoNecessarySource.cmd finish >>%NewFirstBootLogPath%
ECHO %DATE% %TIME%[Log TRACE]  rd C:\OEM\Preload\DPOP\OfficeSettings in C:\OEM\Preload\DPOP\CLEANUP\DeleteFolderList.txt >>%NewFirstBootLogPath% 2>&1
ECHO C:\OEM\Preload\DPOP\OfficeSettings>>C:\OEM\Preload\DPOP\CLEANUP\DeleteFolderList.txt
ECHO.>>%NewFirstBootLogPath%
ECHO.>>%NewFirstBootLogPath%

endlocal
::::		Region Finish: Comes from [DPOP] Office Settings module for reduce first boot time 


::::		Region Start: Comes from [DPOP] CLEANUP module for reduce first boot time 
setlocal
SET LogPath=C:\OEM\AcerLogs\NewFirstBoot.log
ECHO.>>%LogPath%
ECHO.>>%LogPath%
ECHO %DATE% %TIME%[Log TRACE]  call C:\OEM\Preload\DPOP\CLEANUP\CLEANUP.CMD FirstBoot start >>%LogPath%

call C:\OEM\Preload\DPOP\CLEANUP\CLEANUP.CMD FirstBoot

ECHO %DATE% %TIME%[Log TRACE]  call C:\OEM\Preload\DPOP\CLEANUP\CLEANUP.CMD FirstBoot finish >>%LogPath%
ECHO %DATE% %TIME%[Log TRACE]  rd /s /q C:\OEM\Preload\DPOP\CLEANUP >>%LogPath% 2>&1
rd /s /q C:\OEM\Preload\DPOP\CLEANUP >>%LogPath% 2>&1
ECHO.>>%LogPath%
ECHO.>>%LogPath%
endlocal
::::		Region Finish: Comes from [DPOP] CLEANUP module for reduce first boot time 
