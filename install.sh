#!/bin/bash

version=2022.3.18
# Version

sudo apt install apt-transport-https dirmngr gnupg ffmpeg mono-devel -y || exit 1

mkdir -p ~/LineRider
cd ~/LineRider || error "Could not move to folder"

#Download LRA-Community-Edition build from repo
wget -O linerider.zip https://github.com/Sussy-OS/LRA-Community-Edition/releases/download/${version}/LineRider.game.auto-release.${version}-LR.zip || error "Could not download linerider"

#Install
unzip linerider.zip || error "Could not unzip the files"
rm linerider.zip

#Add system ffmpeg to expected location
mkdir -p ~/Documents/LRA/ffmpeg/linux
cp $(command -v ffmpeg) ~/Documents/LRA/ffmpeg/linux/ffmpeg

#Menu shortcut
echo "[Desktop Entry]
Name=LineRider
Comment=An Open Source spiritual successor to the flash game Line Rider
Icon=$(dirname "$0")/icon-64.png
Exec=mono $HOME/LineRider/linerider.exe
Path=$HOME/LineRider/
Type=Application
Terminal=false
Categories=Game;" > ~/.local/share/applications/LineRider.desktop
