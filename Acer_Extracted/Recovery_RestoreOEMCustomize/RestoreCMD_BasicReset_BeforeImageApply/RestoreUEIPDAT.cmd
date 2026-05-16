
ECHO.
ECHO %DATE% %TIME%[Log START]  ============ [SUB]%~dpnx0 ============
pushd "%~dp0"

if exist %OSDrive%\Recovery\OEM\KeepEnabledUEIPDAT.tag del /f /q %OSDrive%\Recovery\OEM\KeepEnabledUEIPDAT.tag

if exist "%OSDrive%\ProgramData\OEM\User Experience Improvement Program\Config\EnabledUEIP.dat" (
	ECHO %DATE% %TIME%[Log TRACE]  EnabledUEIP.dat found, leave tag for keep EnabledUEIP.dat after keep my file
	ECHO %DATE% %TIME%[Log TRACE]  EnabledUEIP.dat found, leave tag for keep EnabledUEIP.dat after keep my file>%OSDrive%\Recovery\OEM\KeepEnabledUEIPDAT.tag
) else (
	ECHO %DATE% %TIME%[Log TRACE]  EnabledUEIP.dat not found, do nothing.
)

popd
ECHO %DATE% %TIME%[Log LEAVE]  ============ [SUB]%~dpnx0 ============
ECHO.
