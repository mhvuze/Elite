# Elite

This is a hacky workaround for mapping the Elite Controller paddles to keys. This should help when using tools like Auto Hotkey or simply avoiding the "quick save reach". To read about saqneos experience creating the app, [visit his blog](http://shawnquereshi.com/2016/02/binding-the-elite-controller-paddles-to-the-keyboard/).

Since I ran into a bunch of issues during deployment (Exitcode, appx not building etc), I decided to push my minor changes and instructions to this fork. Maybe somebody will find it useful.

Thanks to added modifier support this also works for Shadowplay etc. now :)

### Changes to original
* Deployment script splitted due to .NET-sided issues
* Support for modifier keys (hardcoded right now; TODO: Add UI buttons)
* Configuration file with auto-save and auto-load (only writing right now; TODO: Fixup)
* Host now starts minimized to tray (TODO: Add to PowerShell script)

### Prerequisites
* Xbox Elite Controller
* Xbox Accessories App
* Microsoft Build Tools (or VS2015)
* Nuget Command Line tool (or VS2015)
* Windows 10 SDK for certificate creation tools (or VS2015)

### Deployment Steps
1. Download and extract the package
2. Download the nuget command-line utility (https://docs.nuget.org/consume/installing-nuget)
3. Run nuget restore, targetting Elite.sln (e.g. "nuget.exe restore Elite.sln")
4. Run Install-ElitePaddles-1.ps1 from the directory where it is packaged, alongside Elite.sln
5. Open the solution in Visual Studio, select your platform and rebuild everything
6. Right click on EliteUI project, Store -> Create App Package
7. "No" to Windows Store upload, select \Elite\EliteUi\Out\EliteUi\AppPackages\ as output directory, don't generate app bundle and select your platform package
8. Create the appx, then run Install-ElitePaddles-2.ps1

I splitted the original Install-ElitePaddles.ps1 script in two parts. In summary, they will perform the following tasks:

1. Modify the dependencies of the Elite.csproj file to point to the directory of the XboxDevices app
2. ACL the URL http://+:8642/EliteService to the active user so ElitePaddlesServiceHost can listen on it
3. Generate a certificate to sign the appx package. The user will be prompted for passwords to create the certs, and then again to use them
4. Add the certificate to the root store and sign the appx package.
5. Deploy the ElitePaddles appx package.
6. Enable loopback on the ElitePaddles app

### Running the Application

After deploying the app, do the following:

1. Start ElitePaddlesServiceHost.exe from the ElitePaddlesServiceHost\Out\ directory
2. Start ElitePaddles app

### Known or Potential Issues
* Multiple gamepads unsupported
* Large number of dependencies in deployment script
* Fiddling with deployment script may be required for some users as it was only lightly tested
* No autostart for the service host application
* Some keys cannot be mapped by the app because they select the buttons instead (e.g. Enter and gamepad "A")
* Poor error handling/diagnostic ability, for instance when service is not running
* VS bug: Will throw exitcode -1073740791 during EliteUi compilation if using German language, switching to English fixes this (MS is aware, might be fixed soon-ish). Download ENU pack from [here](https://www.microsoft.com/en-US/download/details.aspx?id=48157)

App icons based on [icon](http://www.flaticon.com/free-icon/xbox-one-wireless-control_37649) by Freepik.