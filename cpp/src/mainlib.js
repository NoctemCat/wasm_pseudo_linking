// "use strict";

const Roundabout = {
    $rbData: {
        initStatus: false,
        libs: [],
        libsMap: new Map(),
        bytes: {},
    },

    $genRbBridge: (rbLib, bridgeName, bridgeType) => {
        return (libId, namePtr) => {
            const libName = rbData.libs[libId];
            const lib = rbData.libsMap.get(libName);
            const fnName = rbLib.module.UTF8ToString(namePtr);
            Module.rbLog(`${bridgeName}: "${rbLib.name}" getting "${fnName}" from "${libName}"`);

            const expFunc = lib?.accessor?.[fnName] ?? lib?.[bridgeType]?.[fnName];
            if (typeof expFunc === "function") return rbLib.module.addFunction(expFunc);
            return 0;
        };
    },

    $createAccessor__deps: ["$genRbBridge"],
    $createAccessor: (rbLib, moduleMemory) => {
        const mod = new WebAssembly.Module(rbData.bytes["accessor"]);
        const instance = new WebAssembly.Instance(mod, {
            "env": {
                "memory": moduleMemory,
                // "stackSave": wasmModule._emscripten_stack_get_current ?? wasmModule.stackSave,
                // "stackAlloc": wasmModule.__emscripten_stack_alloc ?? wasmModule.stackAlloc,
                // "stackRestore": wasmModule.__emscripten_stack_restore ?? wasmModule.stackRestore,
                "rbBridge": genRbBridge(rbLib, "rbBridge", "bridge"),
                "rbBridgeFb": genRbBridge(rbLib, "rbBridgeFb", "bridgeFb"),
            }
        });
        rbLib.accessor = instance.exports;
        rbLib.__rbModules.accessor = instance;
    },

    $createBridgeImport__deps: ["$wasmMemory", "$wasmTable", "$addFunction"],
    $createBridgeImport: (rbSideLib, sideMemory) => {
        const accMain = rbData.libsMap.get("main").accessor;
        const importObj = {
            "main": { ...accMain, "memory": wasmMemory },
            "side": { ...rbSideLib.accessor, "memory": sideMemory },
            "mainToSide": {
                "addFnPtr": (fPtr) => {
                    if (fPtr === 0) return 0;
                    const entry = wasmTable.get(fPtr);
                    return rbSideLib.module.addFunction(entry);
                },
            },
            "sideToMain": {
                "addFnPtr": (fPtr) => {
                    if (fPtr === 0) return 0;
                    const entry = rbSideLib.module?.wasmTable?.get(fPtr) ?? rbSideLib.module.getWasmTableEntry(fPtr);
                    return addFunction(entry);
                }
            }
        };
        return importObj;
    },

    $createBridge__deps: ["$createBridgeImport"],
    $createBridge: (rbSideLib, sideMemory) => {
        const importObj = createBridgeImport(rbSideLib, sideMemory);
        const createSync = (bytes) => {
            const mod = new WebAssembly.Module(bytes);
            const instance = new WebAssembly.Instance(mod, importObj);
            return instance;
        }

        // always create js bridge for testing
        rbSideLib.__rbModules.bridgeFb = createSync(rbData.bytes["bridgeFb"]);
        rbSideLib.bridgeUsesMultiMemory = false;
        try {
            rbSideLib.__rbModules.bridge = createSync(rbData.bytes["bridgeMm"]);
            rbSideLib.bridgeUsesMultiMemory = true;
        } catch (error) {
            rbSideLib.__rbModules.bridge = rbSideLib.__rbModules.bridgeFb;
        }

        rbSideLib.bridge = rbSideLib.__rbModules.bridge.exports;
        rbSideLib.bridgeFb = rbSideLib.__rbModules.bridgeFb.exports;
    },

    $rbInitImpl__deps: [
        "$createAccessor",
        "$wasmMemory",
        "$addFunction",
        "$UTF8ToString",
        "emscripten_stack_get_current",
        "_emscripten_stack_alloc",
        "_emscripten_stack_restore",
    ],
    $rbInitImpl: async (basePath) => {
        if (rbData.initStatus === true) return;
        Module.rbLog("rbInit: start");

        const fetchWasm = async (url, as) => {
            Module.rbLog(`rbInit: fetching "${url}" wasm`)
            const response = await fetch(url);
            const data = await response.arrayBuffer();
            rbData.bytes[as] = data;
        }
        await Promise.all([
            fetchWasm(`${basePath}accessor.wasm`, "accessor"),
            fetchWasm(`${basePath}bridge_mm.wasm`, "bridgeMm"),
            fetchWasm(`${basePath}bridge_fb.wasm`, "bridgeFb"),
        ]);
        const rbLib = {
            id: 0,
            name: "main",
            module: Module,
            __rbModules: {},
        }
        rbData.libs.push("main");
        rbData.libsMap.set("main", rbLib);
        createAccessor(rbLib, wasmMemory);
        rbData.initStatus = true;
        Module.rbLog("rbInit: success", rbLib);
    },

    $rbInit__deps: ["$rbInitImpl"],
    $rbInit: async (basePath) => {
        if (rbData.initStatus === true) return;
        if (rbData.initStatus === false) {
            rbData.initStatus = rbInitImpl(basePath);
        }
        await rbData.initStatus;
    },

    $checkModule: (moduleName, wasmModule) => {
        let validModule = true;
        const hasPropsFunc = (...props) => {
            if (!props.some(Object.hasOwn.bind(null, wasmModule))) {
                let elemsStr = props.length > 1 ? "either " : "";
                elemsStr += props.map(el => `"${el}"`).join(" or ");
                console.error(`rbLoad: Pseudo side module "${moduleName}" must have ${elemsStr}"`);
                validModule = false;
            }
        };
        hasPropsFunc("wasmMemory");
        hasPropsFunc("getWasmTableEntry", "wasmTable");
        hasPropsFunc("addFunction");
        hasPropsFunc("UTF8ToString");
        // hasPropsFunc("_emscripten_stack_get_current", "stackSave");
        // hasPropsFunc("__emscripten_stack_alloc", "stackAlloc");
        // hasPropsFunc("__emscripten_stack_restore", "stackRestore");
        return validModule;
    },


    $rbLoad__deps: ["$rbInit", "$checkModule", "$createAccessor", "$createBridge"],
    $rbLoad: async (path, libName, opts) => {
        opts ??= {};
        Module.rbLog(`rbLoad: "${libName}" from "${path}" start`);
        const rbLib = {
            name: libName,
            __rbModules: {},
        };

        const create = (sideMemory) => {
            Module.rbLog(`rbLoad: "${libName}" "create" called`, sideMemory);
            createAccessor(rbLib, sideMemory);
            createBridge(rbLib, sideMemory);
            return rbData.libsMap.get("main").accessor;
        };
        // Optional pass to "sidelib.js", it gets called in $rbSideInit__postset
        opts["Roundabout"] = { rbLib, create };

        const loaded = await import(path);
        const loadedModule = await loaded.default(opts);
        Module.rbLog(`rbLoad: loaded "${libName}"`, loadedModule);

        if (checkModule(libName, loadedModule)) {
            if (rbLib["accessor"] === undefined) {
                create(loadedModule.wasmMemory);
            }
            rbLib.module = loadedModule;
            rbLib.id = rbData.libs.length;

            rbData.libs.push(libName);
            rbData.libsMap.set(libName, rbLib);
            Module.rbLog(`rbLoad: "${libName}" success`, rbLib);
            return loadedModule;
        } else {
            Module.rbLog(`rbLoad: "${libName}" failed`);
            return null;
        }
    },

    $rbGetLibrary: (libIdOrName) => {
        const libName = typeof libId === "number" ? rbData.libs[libIdOrName] : libIdOrName;
        return rbData.libsMap.get(libName);
    },

    rb_dlopen__deps: ["$rbLoad", "$UTF8ToString"],
    rb_dlopen__sig: "pp",
    rb_dlopen: (namePtr) => {
        const libName = UTF8ToString(namePtr);
        Module.rbLog(`rb_dlopen: "${libName}"`);
        return rbData.libsMap.get(libName)?.id ?? 0;
    },

    rb_dlsym__deps: ["$addFunction", "$UTF8ToString"],
    rb_dlsym__sig: "ppp",
    rb_dlsym: (libId, fnNamePtr) => {
        const libName = rbData.libs[libId];
        const lib = rbData.libsMap.get(libName);
        const fnName = UTF8ToString(fnNamePtr);
        Module.rbLog(`rb_dlsym: "${fnName}" from "${libName}"`);

        const fn = lib?.module[`_${fnName}`] ?? lib?.module[fnName];
        if (typeof fn === "function") return addFunction(fn);
        Module.rbLog(`rb_dlsym: "${fnName}" from "${libName}" failed`);
        return 0;
    },

    rb_bridge__sig: "ppp",

    rb_bridge: (libId, namePtr) => {
        // created in $rbInitImpl
        const accMain = rbData.libsMap.get("main").accessor;
        return accMain.rbBridge(libId, namePtr) ?? 0;
    },

    rb_log_enabled__sig: "i",
    rb_log_enabled: () => Module.rbLogEnabled === true,
};

autoAddDeps(Roundabout, '$rbData');
addToLibrary(Roundabout);
