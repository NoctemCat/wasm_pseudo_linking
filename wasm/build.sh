#!/bin/bash
[ -r ./setup_pwd.sh ] && cd wasm/

mkdir -p ./obj

$EMSDK/upstream/bin/wasm-as ./accessor.wat -o ./obj/accessor.wasm
$EMSDK/upstream/bin/wasm-as ./bridge_fb.wat -o ./obj/bridge_fb.wasm
$EMSDK/upstream/bin/wasm-as ./bridge_mm.wat --enable-bulk-memory-opt --enable-multimemory -o ./obj/bridge_mm.wasm

$EMSDK/upstream/bin/wasm-opt ./obj/accessor.wasm -O3 -o ./../output/accessor.wasm
$EMSDK/upstream/bin/wasm-opt ./obj/bridge_fb.wasm -O3 -o ./../output/bridge_fb.wasm
$EMSDK/upstream/bin/wasm-opt ./obj/bridge_mm.wasm -O3 --enable-bulk-memory-opt --enable-multimemory -o ./../output/bridge_mm.wasm

[ -r ./setup_pwd.sh ] && cd ..; exit 0