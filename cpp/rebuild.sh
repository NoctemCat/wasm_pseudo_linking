#!/bin/bash
[ -r ./setup_pwd.sh ] && cd cpp/

rm -f -rf build
rm -f ./../output/libshared_direct.wasm ./../output/main_exe.js ./../output/main_exe.wasm ./../output/pseudo_shared.js ./../output/pseudo_shared.wasm
./build.sh

[ -r ./setup_pwd.sh ] && cd ..; exit 0