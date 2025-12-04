// needs -sIMPORTED_MEMORY to work, also accepts "Module.wasmMemory"
// if you want to be sure it will work
const RoundaboutSide = {
    $rbAddToPreInit: (fn) => {
        if (Module["preInit"] === undefined) {
            Module["preInit"] = [];
        }
        if (typeof Module["preInit"] === "function") {
            Module["preInit"] = [Module["preInit"]];
        }
        Module["preInit"].push(fn);
    },

    $rbSetImports: (inPreInit, inMemory) => {
        if (inPreInit && wasmExports !== undefined) {
            // Can happen in C# wasm, but in emscripten proper "preInit" works fine
            console.error([
                `RoundaboutSide: setting wasm imports failed, "preInit" `,
                `was called after wasm was created. You can pass external `,
                `memory to emscripten with "Module.wasmMemory" and it `,
                `will use it`,
            ].join(''));
            return;
        }

        const roundabout = Module.Roundabout;
        if (roundabout !== undefined) {
            const mainAcc = roundabout.create(inMemory ?? wasmMemory);
            const bridge = roundabout.rbLib.bridge;
            const bridgeFb = roundabout.rbLib.bridgeFb;
            (typeof _main_getI32 !== 'undefined') && (_main_getI32 = mainAcc.getI32);
            (typeof _main_setI32 !== 'undefined') && (_main_setI32 = mainAcc.setI32);
            (typeof _mts_memcpy !== 'undefined') && (_mts_memcpy = bridge.mts_memcpy);
            (typeof _stm_memcpy !== 'undefined') && (_stm_memcpy = bridge.stm_memcpy);
            (typeof _mts_memcpy_fb !== 'undefined') && (_mts_memcpy_fb = bridgeFb.mts_memcpy);
            (typeof _stm_memcpy_fb !== 'undefined') && (_stm_memcpy_fb = bridgeFb.stm_memcpy);
            (typeof _mts_addFnPtr !== 'undefined') && (_mts_addFnPtr = bridge.mts_addFnPtr);
            (typeof _stm_addFnPtr !== 'undefined') && (_stm_addFnPtr = bridge.stm_addFnPtr);
        }
    },

    $rbSideInit__deps: ["$rbAddToPreInit", "$rbSetImports"],
    $rbSideInit__postset: `rbSideInit();`,
    $rbSideInit: () => {
        if (Module.wasmMemory !== undefined || wasmMemory !== undefined) {
            rbSetImports(false, Module.wasmMemory ?? wasmMemory);
        } else {
            rbAddToPreInit(rbSetImports.bind(null, true, null));
        }
    },

    main_getI32__sig: 'ip',
    main_getI32: (_addr) => 0,

    main_setI32__sig: 'vii',
    main_setI32: (_addr, _v) => { },

    mts_memcpy__sig: 'vppi',
    mts_memcpy: (_dest, _src, _size) => { },

    stm_memcpy__sig: 'vppi',
    stm_memcpy: (_dest, _src, _size) => { },

    mts_memcpy_fb__sig: 'vppi',
    mts_memcpy_fb: (_dest, _src, _size) => { },

    stm_memcpy_fb__sig: 'vppi',
    stm_memcpy_fb: (_dest, _src, _size) => { },

    mts_addFnPtr__sig: 'pp',
    mts_addFnPtr: (_fnPtr) => 0,

    stm_addFnPtr__sig: 'pp',
    stm_addFnPtr: (_fnPtr) => 0,
};

autoAddDeps(RoundaboutSide, "$rbSideInit");
addToLibrary(RoundaboutSide);