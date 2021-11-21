# PrintheadMaintainer
<p align="center">
<img width="150" height="150" src="https://raw.githubusercontent.com/evilhawk00/PrintheadMaintainer/main/OtherResources/Icons/png/Blue_512x512.png">
</p>

Printhead Maintainer is a system service designed for printers with continuous ink systems. Aims to prevent the ink from drying out by printing automatically.

##### System requirements :
Windows 10 or higher
## Download
Newest version : [1.0.0.0](https://github.com/evilhawk00/PrintheadMaintainer/releases "1.0.0.0")

## Screenshots
<img width="550" src="https://github.com/evilhawk00/PrintheadMaintainer/blob/main/OtherResources/Screenshots/HomeScreen.jpg?raw=true">

<img width="275" src="https://github.com/evilhawk00/PrintheadMaintainer/blob/main/OtherResources/Screenshots/HomeScreen_Warning.jpg?raw=true"><img width="275" src="https://github.com/evilhawk00/PrintheadMaintainer/blob/main/OtherResources/Screenshots/HomeScreen_Error.jpg?raw=true">

<img width="550" src="https://github.com/evilhawk00/PrintheadMaintainer/blob/main/OtherResources/Screenshots/PrintNowScreen.jpg?raw=true">
<img width="550" src="https://github.com/evilhawk00/PrintheadMaintainer/blob/main/OtherResources/Screenshots/SettingsScreen.jpg?raw=true">
<img width="550" src="https://github.com/evilhawk00/PrintheadMaintainer/blob/main/OtherResources/Screenshots/LogsScreen.jpg?raw=true">

## Features
+ **Prints a user defined .Bmp image with the printer regularly.**
	+ This software does not use task scheduler, it is a background service running in the background. It checks the time span between last printing time and current time every 15 minutes. The advantage of not using task scheduler is that if the user does not have his or hers computer powered on at the scheduled time, on the next boot it will print a page immediately as fast as possible. And it can be deployed on a 24/7 server without an user logged-in. Printer status checking is also implemnted, it has more advantage than using task scheduler with an execute and print command-line printing tool.
	
	+ It checks the status of a printer, if the printer failed to print the page, the printing will not be counted as a successful printing. It raises a Windows 10 notification to the user in the following circumstances :
		+ One minute before the start of scheduled printing.<br/>
		  <img width="300" src="https://github.com/evilhawk00/PrintheadMaintainer/blob/main/OtherResources/Screenshots/Notification_Preparing.jpg?raw=true">
		+ Scheduled printing has failed.<br/>
		  <img width="300" src="https://github.com/evilhawk00/PrintheadMaintainer/blob/main/OtherResources/Screenshots/Notification_Failure.jpg?raw=true">
		<br/>
+ **Tray icon shows the current state of the software, user can know if scheduled printing has failed by a quick glance.**
	+ Tray icon has these different states:
		+ Functional<br/>
		  <img width="100" src="https://github.com/evilhawk00/PrintheadMaintainer/blob/main/OtherResources/Screenshots/Tray_OK.jpg?raw=true">
		+ Warning<br/>
		  <img width="100" src="https://github.com/evilhawk00/PrintheadMaintainer/blob/main/OtherResources/Screenshots/Tray_Warning.jpg?raw=true">
		+ Not functional<br/>
		  <img width="100" src="https://github.com/evilhawk00/PrintheadMaintainer/blob/main/OtherResources/Screenshots/Tray_Error.jpg?raw=true">
		  
+ **The software is seperated in two parts. The System Service part runs under LocalSystem account and the UI part runs under user account**
	+ The UI can be closed by right clicking the tray icon and select "exit". Please note closing the UI will not affect the scheduled printing function of this software because the background service is still running in the background. Closing the UI only disables the ability of showing a printing failure notification. The UI is just an bridge to communicate with the background service and display the current status to the user. With this kind of implementation, the software can do the printing job even if the PC is still at the user login screen.

## Image for printing
+ **Currently only .bmp image is supported.**
	+ Users can make their own printing image with photoshop.
	+ The provided image will be stretched to fit the paper size. If you do not want the image being stretched, try cropping or creating the image with the exact dimention ratios of your printer
	
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

## Credits of third-party
[Some vector assets](https://github.com/evilhawk00/PrintheadMaintainer/blob/main/PrintheadMaintainerUI/Assets/VectorIcons.xaml "VectorIcons.xaml") converted and used in this project are made by @UXWing. Thus, these vector assets are licensed under [The UXWing license](https://uxwing.com/license/ "The UXWing license").

## License
###### The GPLv3 License
Copyright (C) 2021  YAN-LIN, CHEN

This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License for more details.

You should have received a copy of the GNU General Public License along with this program.  If not, see <https://www.gnu.org/licenses/>.
