#!/bin/bash
[ -r ./setup_pwd.sh ] && cd csharp/

rm -f -rf ./../output/_framework/
rm -f ./../output/pseudo_net.js
dotnet clean
rm -f -rf ./obj/Release
./build.sh

[ -r ./setup_pwd.sh ] && cd ..; exit 0
