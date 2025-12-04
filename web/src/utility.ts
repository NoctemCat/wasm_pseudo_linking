export const dynamicImport = async (publicPath: string) => {
    return import(/* @vite-ignore */ `${location.origin}/wasm_pseudo_linking${publicPath}`);
};

export const onceAsync = <F extends (...args: Parameters<F>) => Promise<Awaited<ReturnType<F>>>, T>(
    fn: F,
) => {
    let localFunc: F | null = fn;
    let result: Promise<Awaited<ReturnType<F>>>;
    return async (thisArg?: T, ...params: Parameters<F>): Promise<Awaited<ReturnType<F>>> => {
        if (localFunc) {
            result = localFunc.apply(thisArg, params);
            localFunc = null;
        }
        return await result;
    };
};
