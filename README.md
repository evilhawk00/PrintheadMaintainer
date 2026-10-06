<p align="center">
<img src="https://raw.githubusercontent.com/evilhawk00/PrintheadMaintainer/main/OtherResources/Repository%20Social%20Preview/Repository%20Social%20Preview.jpg">
</p>

### System requirements :
64-bit Windows 10 or higher
## Download
Newest version : [1.0.0.1](https://github.com/evilhawk00/PrintheadMaintainer/releases "1.0.0.1")
Upcoming version (planned v1.1.0.0): Adds optional paper source / printer tray selection in Settings.

## Screenshots
<img width="550" src="https://github.com/evilhawk00/PrintheadMaintainer/blob/main/OtherResources/Screenshots/HomeScreen.jpg?raw=true">

<img width="275" src="https://github.com/evilhawk00/PrintheadMaintainer/blob/main/OtherResources/Screenshots/HomeScreen_Warning.jpg?raw=true"><img width="275" src="https://github.com/evilhawk00/PrintheadMaintainer/blob/main/OtherResources/Screenshots/HomeScreen_Error.jpg?raw=true">

<img width="550" src="https://github.com/evilhawk00/PrintheadMaintainer/blob/main/OtherResources/Screenshots/PrintNowScreen.jpg?raw=true">
<img width="550" src="./PHM_Settings_New.PNG?raw=true" alt="Settings screen showing paper source selection">
<img width="550" src="https://github.com/evilhawk00/PrintheadMaintainer/blob/main/OtherResources/Screenshots/LogsScreen.jpg?raw=true">

## Features
+ **Prints a user defined .Bmp image with the printer regularly.**
	+ This software does not use task scheduler, it is a background service running in the background. It checks the time span between last printing time and current time every 15 minutes. The advantage of not using task scheduler is that if the user does not have his or her computer powered on at the scheduled time, on the next boot it will print a page immediately as fast as possible. And it can be deployed on a 24/7 server without a user logged-in. Printer status checking is also implemented, it has more advantage than using task scheduler with an execute and print command-line printing tool.
	
	+ It checks the status of a printer, if the printer failed to print the page, the printing will not be counted as a successful printing. If the printer is offline, out of paper or has another problem before printing starts, it does not print and reports the problem instead. It raises a Windows notification to the user in the following circumstances :
		+ One minute before the start of scheduled printing.<br/>
		  <img width="300" src="https://github.com/evilhawk00/PrintheadMaintainer/blob/main/OtherResources/Screenshots/Notification_Preparing.jpg?raw=true">
		+ Scheduled printing has failed, with the reason (for example: the printer is offline, the paper is jammed).<br/>
		  <img width="300" src="https://github.com/evilhawk00/PrintheadMaintainer/blob/main/OtherResources/Screenshots/Notification_Failure.jpg?raw=true">
		  + Every failed attempt is notified. The notification stays on the screen until it is closed, and as long as the problem is not solved, it is shown again when the user comes back to the computer (unlocks it, or uses it again after a few minutes away) and when the UI starts. It is removed automatically once printing succeeds.
		<br/>
+ **Choose a specific paper source / printer tray for scheduled prints.**
	+ Settings now enumerates the printer's available paper sources so maintenance jobs can target a non-default tray.

+ **Tray icon shows the current state of the software, user can know if scheduled printing has failed by a quick glance.**
	+ Tray icon has these different states:
		+ Functional<br/>
		  <img width="100" src="https://github.com/evilhawk00/PrintheadMaintainer/blob/main/OtherResources/Screenshots/Tray_OK.jpg?raw=true">
		+ Warning<br/>
		  <img width="100" src="https://github.com/evilhawk00/PrintheadMaintainer/blob/main/OtherResources/Screenshots/Tray_Warning.jpg?raw=true">
		+ Not functional<br/>
		  <img width="100" src="https://github.com/evilhawk00/PrintheadMaintainer/blob/main/OtherResources/Screenshots/Tray_Error.jpg?raw=true">
		  
+ **The software is separated in two parts. The system service runs under its own restricted account (NT SERVICE\PrintheadMaintenanceSvc) and the UI runs under the user's account**
	+ The UI can be closed by right clicking the tray icon and select "exit". Please note closing the UI will not affect the scheduled printing function of this software because the background service is still running in the background. Closing the UI only disables the ability of showing a printing failure notification. The UI is just a bridge to communicate with the background service and display the current status to the user. With this kind of implementation, the software can do the printing job even if the PC is still at the user login screen.
	+ Because the service does not run under your user account, it can only use printers that are installed for all users of the computer. Printers connected only for your own account are not available to it.
	+ The settings are kept in the registry (HKEY_LOCAL_MACHINE\SOFTWARE\evilhawk00\Printhead Maintainer), the log and the custom image in the Data folder of the installation folder. They are kept when upgrading and removed when uninstalling.

## Image for printing
+ **Currently only .bmp image is supported.**
	+ Users can make their own printing image with photoshop.
	+ The software keeps its own copy of the selected image, so the original file can be moved or deleted. After editing the original, select it again in Settings.
	+ The provided image will be stretched to fit the paper size. If you do not want the image being stretched, try cropping or creating the image with the exact dimension ratios of your printer
	
		| Size      | Length  |  Width  |
		| --------  | -----:  | :----:  |
		|  A3       |  420mm  |  297mm  |
		|  A4       |  297mm  |  210mm  |
		|  B4       |  364mm  |  257mm  |

<br/>
For example, this is the use of custom image :
<br/>

1. Crop any image you want with photoshop, in this case, we crop for A4, ratio is 210 : 297<br/>
     <img width="600" src="https://github.com/evilhawk00/PrintheadMaintainer/blob/main/OtherResources/Screenshots/CustomImage_Example.jpg?raw=true">

2. Set the image with the software<br/>
   <img width="600" src="https://github.com/evilhawk00/PrintheadMaintainer/blob/main/OtherResources/Screenshots/PrintNowScreen_CustomBmp.jpg?raw=true">

## Building
The [build workflow](.github/workflows/build.yml) builds everything on every push. To build by hand:
1. Build `PrintheadMaintainerService/PrintheadMaintainer.sln` (Release, x64) with Visual Studio 2019 or later and the C++ workload.
2. Build `PrintheadMaintainerUI/PrintheadMaintainerUI.sln` (Release).
3. Build `PrintheadMaintainerInstaller/PrintheadMaintainerInstaller.sln` (Release, x64). It packages the files built in steps 1 and 2 with [WiX Toolset](https://wixtoolset.org/) v7, which NuGet restores during the build. WiX v7 requires accepting its [Open Source Maintenance Fee EULA](https://github.com/wixtoolset/wix/blob/main/OSMFEULA.txt), for example by adding `-p:AcceptEula=wix7` to the msbuild command line; the fee only applies to revenue-generating use by those with an annual gross revenue of US$10,000 or more. Visual Studio needs the HeatWave extension to open the installer project.

The version of the service, the UI and the installer is set in `Directory.Build.props`.

## Credits of third-party
[Some vector assets](https://github.com/evilhawk00/PrintheadMaintainer/blob/main/PrintheadMaintainerUI/Assets/VectorIcons.xaml "VectorIcons.xaml") converted and used in this project are made by @UXWing. Thus, these vector assets are licensed under [The UXWing license](https://uxwing.com/license/ "The UXWing license").

## License
###### The GPLv3 License
Copyright (C) 2021  YAN-LIN, CHEN

This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License for more details.

You should have received a copy of the GNU General Public License along with this program.  If not, see <https://www.gnu.org/licenses/>.
