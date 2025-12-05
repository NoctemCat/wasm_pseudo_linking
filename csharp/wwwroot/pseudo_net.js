// "use strict";

let enableLog = false;
const rbLog = (...args) => {
    if (enableLog) {
        console.log(...args);
    }
}

// Gets loaded by $rbLoad
async function PseudoNet(moduleArg = {}) {
    enableLog = moduleArg.rbLogEnabled === true;
    rbLog(`dotnet: Loading from ${moduleArg["importPath"]}`);
    const dotnet = (await import(moduleArg["importPath"])).dotnet;
    rbLog("dotnet: loaded", dotnet);
    delete moduleArg["importPath"];
    delete moduleArg["rbLogEnabled"];

    rbLog("dotnet: module", moduleArg);
    const created = await dotnet.withModuleConfig(moduleArg).create();
    rbLog("dotnet:", created);
    const { setModuleImports, getAssemblyExports, getConfig, runMain, Module } = created;
    const config = getConfig();
    rbLog("dotnet: config", config);
    const netExports = await getAssemblyExports(config.mainAssemblyName);
    rbLog("dotnet: exports", netExports);
    await runMain();

    // Robust workaround for [JSExport]
    // 'rb_dlsym' can't handle js functions, so this is a workaround to make it a wasm function
    Module[`_EntryPoint`] = Module.wasmTable.get(Module.addFunction(netExports.WasmPseudoLinking.Program.EntryPoint, "ppp"));
    return Module;
}
export default PseudoNet;