@echo off
REM Launcher - runs after extraction with UAC elevation

REM Launch ServerManager with admin rights
powershell -WindowStyle Hidden -Command "Start-Process '%TEMP%\ServerManager\ServerManager.exe' -ArgumentList '/silent','/autostart' -Verb RunAs"

exit
