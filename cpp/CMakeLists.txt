cmake_minimum_required(VERSION 3.28)
project(wasm_pseudo_linking_cpp VERSION 1.0)

if(NOT DEFINED EMSCRIPTEN)
    message(FATAL_ERROR "Supports only emscripten")
endif()

set(CMAKE_EXPORT_COMPILE_COMMANDS ON)  # For IDE support
set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED True)

# ----- .clangd file -----
execute_process(COMMAND ${CMAKE_CXX_COMPILER} --cflags OUTPUT_VARIABLE CLANGD_FLAGS_TO_ADD)
separate_arguments(CLANGD_FLAGS_TO_ADD UNIX_COMMAND "${CLANGD_FLAGS_TO_ADD}")
list(JOIN CLANGD_FLAGS_TO_ADD ", " CLANGD_FLAGS_TO_ADD)
file(WRITE "${CMAKE_SOURCE_DIR}/.clangd" "\
CompileFlags:
  Remove: [-sSIDE_MODULE=2, -sMAIN_MODULE=2]
  Add: [${CLANGD_FLAGS_TO_ADD}]")
# -------------------------

set(OUTPUT_DIR "${CMAKE_SOURCE_DIR}/build/output")

add_library(shared_direct SHARED)
target_sources(
    shared_direct 
    PRIVATE
        src/shared_direct.cpp

    PUBLIC
    FILE_SET HEADERS
    FILES
        src/shared_direct.hpp
)
set_target_properties(shared_direct PROPERTIES
    SUFFIX ".wasm"
    RUNTIME_OUTPUT_DIRECTORY ${OUTPUT_DIR}
    LIBRARY_OUTPUT_DIRECTORY ${OUTPUT_DIR}
    COMPILE_FLAGS "-O3 -sSIDE_MODULE=2"
    LINK_FLAGS "-O3 -sSIDE_MODULE=2 -sSTANDALONE_WASM --no-entry -sEXPORTED_FUNCTIONS=_sub,_sort_array"
)

add_executable(pseudo_shared)
target_sources(
    pseudo_shared 
    PRIVATE
        src/pseudo_shared.cpp

    PUBLIC
    FILE_SET HEADERS
    FILES
        src/pseudo_shared.hpp
)
# _emscripten_stack_get_current, __emscripten_stack_alloc, __emscripten_stack_restore can be replaced by
# stackSave, stackAlloc, stackRestore. emscripten just for some reason only exports them as js wrapper
# which is not ideal
set_target_properties(pseudo_shared PROPERTIES
    RUNTIME_OUTPUT_DIRECTORY ${OUTPUT_DIR}
    LIBRARY_OUTPUT_DIRECTORY ${OUTPUT_DIR}
    COMPILE_FLAGS "-O3"
    LINK_FLAGS "-O3 -sALLOW_TABLE_GROWTH -sMODULARIZE -sEXPORT_NAME=PseudoShared -sEXPORT_ES6=1 -sEXPORT_KEEPALIVE -sIMPORTED_MEMORY \
-sEXPORTED_RUNTIME_METHODS=UTF8ToString,wasmMemory,addFunction,wasmTable --js-library ${CMAKE_SOURCE_DIR}/src/sidelib.js"
)

add_executable(main_exe)
target_sources(
    main_exe
    PRIVATE
        src/main.cpp
        src/main_sort.cpp

    PUBLIC
    FILE_SET HEADERS
    FILES
        src/timer.hpp
        src/main_sort.hpp
)
set_target_properties(main_exe PROPERTIES
    RUNTIME_OUTPUT_DIRECTORY ${OUTPUT_DIR}
    LIBRARY_OUTPUT_DIRECTORY ${OUTPUT_DIR}
    COMPILE_FLAGS "-O3 -sMAIN_MODULE=2"
    LINK_FLAGS "-O3 -sMAIN_MODULE=2 -sMODULARIZE -sEXPORT_NAME=WasmLink -sEXPORT_ES6=1 -sEXPORT_KEEPALIVE \
-sEXPORTED_RUNTIME_METHODS=UTF8ToString,addFunction,rbGetLibrary  \
--pre-js ${CMAKE_SOURCE_DIR}/src/mainpre.js --js-library ${CMAKE_SOURCE_DIR}/src/mainlib.js"
)
