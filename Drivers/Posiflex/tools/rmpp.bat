
del /q %windir%\system32\pfxapi.dll
del /q %windir%\system32\pp7klm.dll
del /q %windir%\system32\pp9klm.dll
del /q %windir%\system32\pp9kpm.dll
del /q %windir%\system32\pp9kpui.dll
del /q %windir%\syswow64\pp7klm.dll
del /q %windir%\syswow64\pp9klm.dll

for /f "usebackq tokens=4 delims=\:" %%i in (`find /c /i "pp9k" %windir%\inf\oem*.inf ^| find /v ".INF: 0" ^| find ".INF:"`) do pnputil -d %%i
for /f "usebackq tokens=4 delims=\:" %%i in (`find /c /i "pp7k" %windir%\inf\oem*.inf ^| find /v ".INF: 0" ^| find ".INF:"`) do pnputil -d %%i

del /q %windir%\system32\spool\drivers\x64\3\pp7*
