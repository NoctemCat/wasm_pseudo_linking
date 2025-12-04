#!/bin/bash
[ -r ./setup_pwd.sh ] && cd cpp/

cmake -S . -B ./build \
-DCMAKE_TOOLCHAIN_FILE=$EMSDK/upstream/emscripten/cmake/Modules/Platform/Emscripten.cmake \
-DCMAKE_PROJECT_INCLUDE=override.cmake \
-DCMAKE_BUILD_TYPE=Release
cmake --build ./build/ --config Release
cp -a ./build/output/. ./../output/

[ -r ./setup_pwd.sh ] && cd ..; exit 0