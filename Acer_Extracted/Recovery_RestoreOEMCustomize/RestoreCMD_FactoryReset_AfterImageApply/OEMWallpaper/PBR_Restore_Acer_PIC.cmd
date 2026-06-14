
ECHO.
ECHO %DATE% %TIME%[Log START]  ============ [SUB]%~dpnx0 ============ 
pushd "%~dp0"

set str=%~n0
for /f "tokens=1,2,3,4 delims=_" %%a in ("%str%") do set PicturePath=%%c
ECHO %DATE% %TIME%[Log TRACE]  xcopy .\%PicturePath%\*.* %OSDrive%\Users\Default\Pictures\%PicturePath%\*.* /vesy
xcopy .\%PicturePath%\*.* %OSDrive%\Users\Default\Pictures\%PicturePath%\*.* /vesy

popd
ECHO %DATE% %TIME%[Log LEAVE]  ============ [SUB]%~dpnx0 ============ 
ECHO.