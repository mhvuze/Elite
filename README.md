# Elite

This is a workaround for mapping the Elite Controller paddles to keys. This should help when using tools like Auto Hotkey or simply avoiding the "quick save reach". To read about saqneo's - the original author - experience creating the app, [visit his blog](http://shawnquereshi.com/2016/02/binding-the-elite-controller-paddles-to-the-keyboard/).

helifax simplified the deployment process and added support for [Windows 10 Anniversary Update building](https://github.com/helifax/Elite-Enhanced/commit/f22f51abc60b0f1d67b8f8048d1aa1343a193760) but I ran into some issues using his fork so I merged the necessary changes here and built on top of that to create a version that works flawless for me.

### Changes to original
* Includes support for modifier / second keys (very useful for Shadowplay etc.)
* Includes support for trigger vibration (implementation by helifax)
* Includes full profile management support for multiple configurations
* Builds and works on Windows 10 Anniversary Update
* Updated look of UI and icons to the Xbox Elite vibe
* Service Host starts minimized

![alt tag](http://i.imgur.com/DSkl0If.png)

### Installation for end users
* Make sure you have the latst version of the Xbox Device app installed from the Windows Store
* Download and unpack the [latest release](https://github.com/mhvuze/Elite/releases) of this utility
* Run AddListenerURI.ps1 with PowerShell (confirm all relevant prompts you might run into)
* Run Add-AppDevPackage.ps1 found in the EliteUI folder (confirm all relevant prompts you might run into)

### Usage
* Run ElitePaddlesServiceHost.exe
* Run ElitePaddles app
* Configure buttons to your liking
* Click on "Apply Modifiers" if you modified the second key / modifier column
* Click on "Toggle Vibration" to turn trigger vibration on or off
* Save the config as default or seperate profile for easy access in the future
* Enjoy

### Usage Notes
* Paddle configs of the Xbox Device App will overwrite EliteUI settings
* Multiple Xbox One Elite Controllers are not supported

=========================================================================

### Sharing configurations
* The configuration files are tiny and stored in %userprofile%\AppData\Local\Packages\ElitePaddles_dnafrf5v1hgqa\LocalState
* You are free to manually delete them or copy-paste your friends profiles.

=========================================================================

This segment is solely intended for experienced users who want to customize the project or build it on their own.

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
7. Rebuild project in debug mode (!) for your target platform
8. Right click on EliteUI project, Store -> Create App Package
9. "No" to Windows Store upload and generate app bundle
10. Run Add-AppDevPackage.ps1 to install

=========================================================================

### Planned features and known issues
* Feature: Service Host Autostart
* Feature: Update profile list after saving automatically (currently: right-click on combobox to refresh)
* Feature: Apply modifier keys automatically after change
* UI: Toggle Switch for trigger vibration
* UI: Clean up stat output
* Default drive for Windows Store Apps has to be Windows installation drive, otherwise install will fail (good job M$)
* Multiple gamepads unsupported
* Some keys cannot be mapped by the app because they select the buttons instead (e.g. Enter and gamepad "A")
* Poor error handling/diagnostic ability, for instance when service is not running
* VS bug: Will throw exitcode -1073740791 during EliteUi compilation if using German language, switching to English fixes this (MS is aware, might be fixed soon-ish). Download ENU pack from [here](https://www.microsoft.com/en-US/download/details.aspx?id=48157)
