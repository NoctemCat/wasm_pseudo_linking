Module = Module ?? {};
const basePath = Module["pageBasePath"] ?? "./";
delete Module["pageBasePath"];

Module["rbLog"] = (...args) => {
    if (Module.rbLogEnabled) {
        console.log(...args);
    }
};
Module.rbLog("base path", basePath);

const withDependency = (depId, fn) => {
    addRunDependency(depId);
    Promise.resolve(fn())
        .finally(() => removeRunDependency(depId))
};

Module["preRun"] = [
    () => withDependency("shared_direct_file", async () => {
        const response = await fetch(`${basePath}libshared_direct.wasm`);
        const data = await response.arrayBuffer();
        FS.writeFile("shared_direct.wasm", new Uint8Array(data));
    }),
    () => withDependency("load_pseudo_shared", async () => {
        await rbInit(basePath);
        await rbLoad(`${basePath}pseudo_shared.js`, "pseudo_shared");
    }),
    () => withDependency("load_pseudo_net", async () => {
        await rbInit(basePath);
        await rbLoad(`${basePath}pseudo_net.js`, "pseudo_net", {
            "importPath": `${basePath}_framework/dotnet.js`,
            "rbLogEnabled": Module.rbLogEnabled,
        });
    }),
];
