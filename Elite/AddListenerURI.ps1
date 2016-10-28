# Acquire admin
if (!([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) { Start-Process powershell.exe "-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`"" -Verb RunAs; exit }

# Set listener URI Reservation
netsh http add urlacl url=http://+:8642/EliteService user=$env:userdomain\$env:username
Read-Host -Prompt "Added URI Reservation.`nPress any key to exit"