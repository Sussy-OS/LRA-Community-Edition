# IMPORTANT
This is mainly maintained for Pi-Apps, the Debian open sourced app store. If you would like to use this, please install it on Pi-Apps [![badge](https://github.com/Botspot/pi-apps/blob/master/icons/badge.png?raw=true)](https://github.com/Botspot/pi-apps) or run the install.sh below.


[![badge](https://github.com/Botspot/pi-apps/blob/master/icons/badge.png?raw=true)](https://github.com/Botspot/pi-apps)  

# About Line Rider Advanced: Community Edition
Line Rider Advanced: Community Edition, abbreviated as LRA:CE, is a fork of https://github.com/jealouscloud/linerider-advanced; "An open source spiritual successor to the flash game Line Rider 6.2 with lots of new features."

This project's goal is to unify the best features from several existing forks of the original Line Rider Advanced all into one version. Once this version is caught up with all others, we, the contributors, will aim to continue development of new features in this repo as opposed to branching off into new forks again.

## Install & Uninstall
You will need to run 
```
wget -qO- https://raw.githubusercontent.com/Sussy-OS/LRA-Community-Edition/linux/install | bash
``` 
This install ^ is now stable!

or install it from Pi-Apps:


[![badge](https://github.com/Botspot/pi-apps/blob/master/icons/badge.png?raw=true)](https://github.com/Botspot/pi-apps) 

<details>
<summary><b>To uninstall LineRider</b></summary>
This will not uninstall any user data. To uninstall user data, remove the ~/Documents/LRA/ folder.

```
wget -qO- https://raw.githubusercontent.com/Sussy-OS/LRA-Community-Edition/linux/uninstall | bash
```
</details>

# LRA:CE Features
* Start from current frame checkbox. (Not on any other LRA-based programs.)
* Discord activity support (Aka little stats when you click your user)
* Custom scarves from a .txt file in /LRA/Scarves -> [/Examples/Scarves/README.md](https://github.com/RatherBeLunar/LRA-Community-Edition/tree/master/Examples/Scarves/README.md)
* Custom amount of scarf segments
* Custom riders in /LRA/Riders -> [/Examples/Riders/README.md](https://github.com/RatherBeLunar/LRA-Community-Edition/tree/master/Examples/Riders/README.md)
* Custom scarves on a rider png -> [/Examples/Riders/Bosh-Custom-Scarf-On-Png-Example/README.md](https://github.com/RatherBeLunar/LRA-Community-Edition/tree/master/Examples/Riders/Bosh-Custom-Scarf-On-Png-Example/README.md)

# Issues
Please, if you have an issue, make an issue here! (Or on Pi-Apps)

# Suggestions
If you think you know a way to make this better, let me know! Just fork this repo, edit it, and make a pull request.

# Build
First extract the source code and download [gwen-lra](https://github.com/RatherBeLunar/gwen-lra)'s source code and extract it to the /lib/gwen-lra/ folder
Run nuget restore in src (Visual Studio (not VS Code) will do this for you)
Build src/linerider.sln with msbuild or Visual Studio


**Done**

This project requires .net 4.6 and C# 7 support.

# Libraries
This project uses binaries, sources, or modified sources from the following libraries:

* ffmpeg https://ffmpeg.org/
* NVorbis https://github.com/ioctlLR/NVorbis
* gwen-dotnet https://code.google.com/archive/p/gwen-dotnet/
* OpenTK https://github.com/opentk/opentk
* Discord Game SDK https://discord.com/

You can find their license info in LICENSES.txt

The UI is a modified modified version of gwen-dotnet by jealouscloud

# License
LRA:CE is licensed under GPL3.
