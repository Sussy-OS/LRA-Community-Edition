# IMPORTANT
This is mainly maintained for Pi-Apps, the Debian open sourced app store. As well as being the more stable way to install, it also contains More Ram, which makes this run faster.
If you would like to use this, please install it on Pi-Apps.


[![badge](https://github.com/Botspot/pi-apps/blob/master/icons/badge.png?raw=true)](https://github.com/Botspot/pi-apps)  

# About Line Rider Advanced: Community Edition
Line Rider Advanced: Community Edition, abbreviated as LRA:CE, is a fork of [Line Rider Advanced](https://github.com/jealouscloud/linerider-advanced); "An open source spiritual successor to the flash game Line Rider 6.2 with lots of new features."

This project's goal is to unify the best features from several existing forks of the original Line Rider Advanced all into one version. Once this version is caught up with all others, we, the contributors, will aim to continue development of new features in this repository as opposed to branching off into new forks again.

## Install & Uninstall
Please, install it from Pi-Apps:

[![badge](https://github.com/Botspot/pi-apps/blob/master/icons/badge.png?raw=true)](https://github.com/Botspot/pi-apps) 

<details>
<summary><b>To uninstall LineRider</b></summary>
This will not uninstall any user data. To uninstall user data, remove the ~/Documents/LRA/ folder.

```
wget -qO- https://raw.githubusercontent.com/Sussy-OS/LRA-Community-Edition/linux/uninstall | bash
```
</details>

This project requires .NET 4.6 and C# 7 support.

# LRA:CE Features
* Start recording from current frame. (Not on any other LRA-based programs.)
* Discord integration (At this point only [rich presence](https://i.ibb.co/8s4NC1X/image.png))
* Customisable scarf ([Custom scarf colors](https://github.com/RatherBeLunar/LRA-Community-Edition/tree/master/Examples/Scarves/README.md) and custom scarf length)
* [Custom riders](https://github.com/RatherBeLunar/LRA-Community-Edition/tree/master/Examples/Riders/README.md)
* [Custom scarves on a custom rider](https://github.com/RatherBeLunar/LRA-Community-Edition/tree/master/Examples/Riders/Bosh-Custom-Scarf-On-Png-Example/README.md)

# Issues
Please, if you have an issue, make an issue here! (Or on Pi-Apps)

# Suggestions
If you think you know a way to make this better, let me know! Just fork this repo, edit it, and make a pull request.

# Build
### Step 0
Download and extract this and [gwen-lra](https://github.com/RatherBeLunar/gwen-lra)'s source code. Put the extracted gwen-lra source in the */lib/gwen-lra/* folder of this repository.

### Step 1
Open the *src/linerider.sln* file in Visual Studio. (Do not use Visual Studio Code). Click **Project > Manage NuGet Packages**. It should ask you to restore. Click yes.

### Step 2
Still in Visual Studio, click **Build > Build solution** or use the keyboard shortcut Ctrl+Shift+B. If you did everything correctly, you will find a new *build* folder in the root directory. This will contain the .exe file for the application itself.

# Libraries
This project uses binaries, sources, or modified sources from the following libraries:

* [ffmpeg](https://ffmpeg.org/)
* [NVorbis](https://github.com/ioctlLR/NVorbis)
* [gwen-dotnet](https://code.google.com/archive/p/gwen-dotnet/)
* [OpenTK](https://github.com/opentk/opentk)
* [Discord Game SDK](https://discord.com/)

You can find their license info in [LICENSES.txt](https://github.com/RatherBeLunar/LRA-Community-Edition/blob/master/LICENSES.txt).

The UI in LRA:CE is a modified version of gwen-dotnet by jealouscloud.

# License
LRA:CE is licensed under GPL3.
