1. Use Studio 2015 to build the project in Debug mode. Release does NOT work properly at the moment.
2. Create a valid cert using Generate_Certificate.bat. Might have to specifically select it for the EliteUI project before step 3.
3. Go to "EliteUI (R_Click)->Store->Create App Packages" to generate the .appx
4. Use the PowerShell script generated to import the certificate + app. Make sure to have your default install drive for WinStore apps set to your Windows drives, because Win10 sucks and the installation will fail otherwise.
