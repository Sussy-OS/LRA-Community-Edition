#!/bin/bash

version=2022.3.18
# Version

# Copy the logo to LineRiderLogo
wget -O ~/LineRiderLogo/icon-64.png https://raw.githubusercontent.com/Sussy-OS/pi-apps/master/apps/LineRider/icon-64.png

# Install dependincies.
sudo apt install apt-transport-https dirmngr gnupg ffmpeg mono-devel -y

mkdir -p ~/LineRider
cd ~/LineRider

#Download LRA-Community-Edition build from repo
wget -O linerider.zip https://github.com/Sussy-OS/LRA-Community-Edition/releases/download/${version}/LineRider.game.auto-release.${version}-LR.zip

#Install
unzip linerider.zip
rm linerider.zip

#Add system ffmpeg to expected location
mkdir -p ~/Documents/LRA/ffmpeg/linux
cp $(command -v ffmpeg) ~/Documents/LRA/ffmpeg/linux/ffmpeg

#Menu shortcut
echo "[Desktop Entry]
Name=LineRider
Comment=An Open Source spiritual successor to the flash game Line Rider
Icon=$HOME/LineRiderLogo/icon-64.png
Exec=mono $HOME/LineRider/linerider.exe
Path=$HOME/LineRider/
Type=Application
Terminal=false
Categories=Game;" > ~/.local/share/applications/LineRider.desktop
