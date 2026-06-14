
ECHO.
ECHO %DATE% %TIME%[Log START]  ============ [SUB]%~dpnx0 ============
pushd "%~dp0"

if exist %OSDrive%\Recovery\OEM\KeepEnabledUEIPDAT.tag (
	ECHO !DATE! !TIME![Log TRACE]  %OSDrive%\Recovery\OEM\KeepEnabledUEIPDAT.tag found, create EnabledUEIP.dat for Keep my file process
	ECHO !DATE! !TIME![Log TRACE]  md "%OSDrive%\ProgramData\OEM\User Experience Improvement Program\Config\"
	md "%OSDrive%\ProgramData\OEM\User Experience Improvement Program\Config\"
	ECHO !DATE! !TIME![Log TRACE]  %OSDrive%\Recovery\OEM\KeepEnabledUEIPDAT.tag found, create EnabledUEIP.dat for Keep my file process>"%OSDrive%\ProgramData\OEM\User Experience Improvement Program\Config\EnabledUEIP.dat"
	ECHO !DATE! !TIME![Log TRACE]  dir "%OSDrive%\ProgramData\OEM\User Experience Improvement Program\Config\EnabledUEIP.dat"
	dir "%OSDrive%\ProgramData\OEM\User Experience Improvement Program\Config\EnabledUEIP.dat"
) else (
	ECHO %DATE% %TIME%[Log TRACE]  EnabledUEIP.dat not found, do nothing.
)

popd
ECHO %DATE% %TIME%[Log LEAVE]  ============ [SUB]%~dpnx0 ============
ECHO.
