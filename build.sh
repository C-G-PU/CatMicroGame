#!/bin/bash
echo "Building Desktop Cat as a single executable for Windows..."
cd DesktopCat
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish
echo "Build complete! The executable is located in DesktopCat/publish/DesktopCat.exe"
