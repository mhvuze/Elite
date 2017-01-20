param (
    [string]$user = "$env:userdomain\$env:username"
 )
# Acquire admin
if (!([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) { Start-Process powershell.exe "-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`" -ArgumentList -user $user" -Verb RunAs; exit }

# Set listener URI Reservation
netsh http delete urlacl url=http://+:8642/EliteService
netsh http add urlacl url=http://+:8642/EliteService user=$user
Read-Host -Prompt "Added URI Reservation for $user.`nPress any key to exit"