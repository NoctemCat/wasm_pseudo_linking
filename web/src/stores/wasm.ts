import { dynamicImport, onceAsync } from '@/utility';
import { defineStore } from 'pinia';
import { computed, ref, type Ref } from 'vue';

interface RbModule {
    id: number;
    name: string;
    module: object;
    bridgeUsesMultiMemory?: boolean;
    accessor: WebAssembly.Exports;
    bridge?: WebAssembly.Exports;
    bridgeFb?: WebAssembly.Exports;
    __rbModules: {
        accessor: WebAssembly.WebAssemblyInstantiatedSource;
        bridge?: WebAssembly.WebAssemblyInstantiatedSource;
        bridgeFb?: WebAssembly.WebAssemblyInstantiatedSource;
    };
}

export const sortTypes = [
    '_sort',
    '_sort_rb',
    '_sort_rb_fallback',
    '_sort_rb_import',
    '_sort_rb_import_fallback',
    '_sort_rb_direct',
    '_sort_rb_direct_import',
    '_sort_net',
    '_sort_net_fallback',
    '_sort_net_import',
    '_sort_net_import_fallback',
    '_sort_net_direct',
    '_sort_net_direct_import',
] as const;

export type SortType = (typeof sortTypes)[number];
export type SortFunc = (src: number, dest: number, arrSize: number) => number;
type WasmModuleFuncs = {
    [key in SortType]: SortFunc;
};
interface WasmModule extends WasmModuleFuncs {
    rbGetLibrary: (libIdOrName: number | string) => RbModule | undefined;
    _allocate_random_array: (arrSize: number) => number;
    _allocate_array: (arrSize: number) => number;
    _free_array: (ptr: number) => void;
    _validate_sort_equality: (arrSize: number, variation: number, silent: boolean) => boolean;
}

export const useWasmStore = defineStore('wasm', () => {
    const initImpl = async (): Promise<WasmModule> => {
        const WasmLink = await dynamicImport('/main_exe.js');
        const myWasmLink = await WasmLink.default({
            pageBasePath: '/wasm_pseudo_linking/',
            rbLogEnabled: true,
        });
        return myWasmLink as WasmModule;
    };

    const mainModule: Ref<WasmModule | null> = ref(null);
    const assign = async () => {
        const mModule = await initImpl();
        mainModule.value = mModule;
        return mModule;
    };
    const isReady = computed(() => mainModule.value !== null);
    const isLoading = computed(() => mainModule.value === null);
    const ensureInit = onceAsync(assign);
    return { ensureInit, mainModule, isReady, isLoading };
});
