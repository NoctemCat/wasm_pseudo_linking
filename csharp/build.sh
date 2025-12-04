#!/bin/bash
[ -r ./setup_pwd.sh ] && cd csharp/

dotnet publish -c Release
mkdir -p ./../output/_framework
cp -a ./bin/Release/net9.0/publish/wwwroot/_framework/. ./../output/_framework
cp ./bin/Release/net9.0/publish/wwwroot/pseudo_net.js ./../output/pseudo_net.js

[ -r ./setup_pwd.sh ] && cd ..; exit 0