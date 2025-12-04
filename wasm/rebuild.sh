#!/bin/bash
[ -r ./setup_pwd.sh ] && cd wasm/

rm -f -rf ./obj
rm -f ./../output/accessor.wasm ./../output/bridge_fb.wasm ./../output/bridge_mm.wasm
./build.sh

[ -r ./setup_pwd.sh ] && cd ..; exit 0