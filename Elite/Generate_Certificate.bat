"C:\Program Files (x86)\Windows Kits\10\bin\x86\makecert.exe" ^
/n "cn=ElitePaddlesPublisher" /r /h 0 /eku "1.3.6.1.5.5.7.3.3,1.3.6.1.4.1.311.10.3.13" /e 10/10/2040 /sv Certificate.pvk Certificate.cer
 
"C:\Program Files (x86)\Windows Kits\10\bin\x86\pvk2pfx.exe" -pvk Certificate.pvk -pi "passWD" -spc Certificate.cer -pfx Certificate.pfx -po "passWD"