
ECHO %DATE% %TIME%[Log START]  ============ %~dpnx0 ============>>C:\OEM\AcerLogs\RestoreCameraFrequencyByPBR.log 2>&1 

ECHO %DATE% %TIME%[Log TRACE]  REG ADD "HKLM\SYSTEM\CurrentControlSet\Enum\USB\VID_0408&PID_4033&MI_00\6&E6C6236&0&0000\Device Parameters" /v PowerlineFrequency /t REG_DWORD /d "00000002" /f>>C:\OEM\AcerLogs\RestoreCameraFrequencyByPBR.log 2>&1 
REG ADD "HKLM\SYSTEM\CurrentControlSet\Enum\USB\VID_0408&PID_4033&MI_00\6&E6C6236&0&0000\Device Parameters" /v PowerlineFrequency /t REG_DWORD /d "00000002" /f>>C:\OEM\AcerLogs\RestoreCameraFrequencyByPBR.log 2>&1 

ECHO %DATE% %TIME%[Log LEAVE]  ============ %~dpnx0 ============>>C:\OEM\AcerLogs\RestoreCameraFrequencyByPBR.log 2>&1 
