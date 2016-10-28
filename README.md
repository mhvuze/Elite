# Elite

This is a hacky workaround for mapping the Elite Controller paddles to keys. This should help when using tools like Auto Hotkey or simply avoiding the "quick save reach". To read about saqneo's - the original author - experience creating the app, [visit his blog](http://shawnquereshi.com/2016/02/binding-the-elite-controller-paddles-to-the-keyboard/).

helifax simplified the deployment process and added support for [Windows 10 Anniversary Update building](https://github.com/helifax/Elite-Enhanced/commit/f22f51abc60b0f1d67b8f8048d1aa1343a193760) but I ran into some issues using his fork so I merged the necessary changes here and built on top of that.

### Changes to original
* Builds and works on Windows 10 Anniversary Update
* Includes support for modifier / second keys (very useful for Shadowplay etc.)
* Writes and reads a configuration file for extra comfort
* Loads last saved configuration at launch
* Updated look of UI and icons to the Xbox Elite vibe
* Service Host starts minimized

![alt tag](http://i.imgur.com/fl7Qb9q.png)

### Installation for end users
* Make sure you have the latst version of the Xbox Device app installed from the Windows Store
* Download and unpack the [latest release](https://github.com/mhvuze/Elite/releases) of this utility
* Run AddListenerURI.ps1 with PowerShell (confirm all relevant prompts you might run into)
* Run Add-AppDevPackage.ps1 found in the EliteUI folder (confirm all relevant prompts you might run into)

### Usage
* Run ElitePaddlesServiceHost.exe
* Run ElitePaddles app
* Configure buttons to your liking and save the config so it loads up automatically at launch
* Enjoy

### Usage Notes
* Paddle configs of the Xbox Device App will overwrite EliteUI settings
* Multiple Xbox One Elite Controllers are not supported

### Building Prerequisites
* Xbox Accessories App
* Microsoft Build Tools (or VS2015)
* Nuget Command Line tool (or VS2015)
* Windows 10 SDK for certificate creation tools (or VS2015)

### Deployment Steps
1. Clone the repo
2. Download the nuget command-line utility (https://docs.nuget.org/consume/installing-nuget)
3. Run nuget restore, targetting Elite.sln (e.g. "nuget.exe restore Elite.sln")
4. Run Generate_Certificate.bat (if you are asked for a password, it's 'passWD')
5. Open the solution in Visual Studio
6. Edit Package.appxmanifest and point to the certificate
7. Rebuild project for your target platform
8. Right click on EliteUI project, Store -> Create App Package
9. "No" to Windows Store upload and generate app bundle
10. Run Add-AppDevPackage.ps1 to install

### Planned features and known issues
* Feature: Service Host Autostart
* Feature: Trigger Vibration from helifax fork
* Multiple gamepads unsupported
* Some keys cannot be mapped by the app because they select the buttons instead (e.g. Enter and gamepad "A")
* Poor error handling/diagnostic ability, for instance when service is not running
* VS bug: Will throw exitcode -1073740791 during EliteUi compilation if using German language, switching to English fixes this (MS is aware, might be fixed soon-ish). Download ENU pack from [here](https://www.microsoft.com/en-US/download/details.aspx?id=48157)
