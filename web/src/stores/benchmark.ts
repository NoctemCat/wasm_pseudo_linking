import { defineStore, storeToRefs } from 'pinia';
import { sortTypes, useWasmStore, type SortFunc, type SortType } from './wasm';
import { computed, reactive, ref } from 'vue';
import { useIntervalFn } from '@vueuse/core';

class Measure {
    totalTime: number = 0.0;
    iterationCount: number = 0;
    isWarmingUp: boolean = false;

    reset() {
        this.totalTime = 0.0;
        this.iterationCount = 0;
        this.isWarmingUp = false;
    }

    getStrRepr() {
        if (this.isWarmingUp) return 'warmup';
        if (this.iterationCount === 0) return '0.000 ms';
        return (this.totalTime / this.iterationCount).toFixed(3) + ' ms';
    }
}
type BenchmarkMapped = {
    [key in SortType]: Measure;
};
export class Benchmark implements BenchmarkMapped {
    _sort: Measure = new Measure();
    _sort_rb: Measure = new Measure();
    _sort_rb_import: Measure = new Measure();
    _sort_rb_fallback: Measure = new Measure();
    _sort_rb_import_fallback: Measure = new Measure();
    _sort_rb_direct: Measure = new Measure();
    _sort_rb_direct_import: Measure = new Measure();
    _sort_net: Measure = new Measure();
    _sort_net_import: Measure = new Measure();
    _sort_net_fallback: Measure = new Measure();
    _sort_net_import_fallback: Measure = new Measure();
    _sort_net_direct: Measure = new Measure();
    _sort_net_direct_import: Measure = new Measure();

    reset() {
        for (const t of sortTypes) {
            this[t].reset();
        }
    }
}

export type BenchmarkOptions = {
    arrSize: number;
    warmupSize: number;
    loopSize: number;
    batchSize: number;
    sortTypes: Array<SortType>;
};
type InputBenchmarkOptions = {
    arrSize?: number;
    warmupSize?: number;
    loopSize?: number;
    batchSize?: number;
    sortTypes?: Array<SortType>;
};

enum BenchmarkStates {
    Init,
    Running,
    Paused,
}
export const useBenchmarkStore = defineStore('benchmark', () => {
    const wasmStore = useWasmStore();
    const { mainModule } = storeToRefs(wasmStore);

    const defaultOpts: BenchmarkOptions = {
        arrSize: 50000,
        warmupSize: 64,
        loopSize: 256,
        batchSize: 1,
        sortTypes: [...sortTypes],
    };
    Object.freeze(defaultOpts);

    let opts: BenchmarkOptions = { ...defaultOpts };

    const benchmark = reactive(new Benchmark());
    const benchmarkState = ref(BenchmarkStates.Init);

    const benchmarkQueue: Array<{
        name: SortType;
        sort: SortFunc;
    }> = reactive([]);

    let src: null | number = null;
    let dest: null | number = null;

    let warmup = defaultOpts.warmupSize;
    let loop = defaultOpts.loopSize;

    let pause = () => {};
    let resume = () => {};

    const cleanup = () => {
        pause();
        benchmarkState.value = BenchmarkStates.Init;
        benchmarkQueue.length = 0;
        if (dest) {
            mainModule.value?._free_array(dest);
        }
        if (src) {
            mainModule.value?._free_array(src);
        }
        dest = null;
        src = null;
    };

    const resetValues = () => {
        warmup = opts.warmupSize;
        loop = opts.loopSize;
        benchmark.reset();
    };

    const benchProgressive = () => {
        if (src === null || dest === null) {
            cleanup();
            return;
        }

        for (let index = 0; index < opts.batchSize; index++) {
            const toSort = benchmarkQueue[0];
            if (toSort === undefined) break;

            const measure = benchmark[toSort.name];
            if (warmup > 0) {
                measure.isWarmingUp = true;
                toSort.sort(src, dest, opts.arrSize);
                warmup--;
                continue;
            }

            measure.isWarmingUp = false;
            if (loop > 0) {
                measure.iterationCount++;
                measure.totalTime += toSort.sort(src, dest, opts.arrSize);
                loop--;
                continue;
            }

            if (benchmarkQueue.length <= 0) break;
            benchmarkQueue.shift();
            warmup = opts.warmupSize;
            loop = opts.loopSize;
            console.log(`Benchmark: ${toSort.name.padEnd(6)} ${measure.getStrRepr()}`);
        }

        if (benchmarkQueue.length <= 0) {
            cleanup();
        }
    };
    const retUseInterval = useIntervalFn(benchProgressive, 1, { immediate: false });
    const { isActive } = retUseInterval;
    pause = retUseInterval.pause;
    resume = retUseInterval.resume;

    const start = async (benchmarkOptions?: InputBenchmarkOptions) => {
        if (isActive.value) return;
        await wasmStore.ensureInit();
        if (mainModule.value === null) throw new Error('mainModule must have value');

        opts = { ...defaultOpts, ...(benchmarkOptions ?? {}) };

        cleanup();
        resetValues();

        benchmarkState.value = BenchmarkStates.Running;
        for (const t of opts.sortTypes) {
            const item = { name: t, sort: mainModule.value[t] };
            benchmarkQueue.push(item);
        }

        src = mainModule.value._allocate_random_array(opts.arrSize);
        dest = mainModule.value._allocate_array(opts.arrSize);
        resume();
    };

    const stop = () => {
        cleanup();
    };
    const skip = () => {
        benchmarkQueue.shift();
        warmup = opts.warmupSize;
        loop = opts.loopSize;
    };

    const pauseBenchmark = () => {
        if (benchmarkState.value === BenchmarkStates.Running) {
            pause();
            benchmarkState.value = BenchmarkStates.Paused;
        }
    };
    const resumeBenchmark = () => {
        if (benchmarkState.value === BenchmarkStates.Paused) {
            resume();
            benchmarkState.value = BenchmarkStates.Running;
        }
    };

    const startDisabled = computed(() => benchmarkState.value === BenchmarkStates.Running);
    const stopDisabled = computed(() => benchmarkState.value === BenchmarkStates.Init);
    const resumeDisabled = computed(() => benchmarkState.value !== BenchmarkStates.Paused);
    const pauseDisabled = computed(
        () =>
            benchmarkState.value === BenchmarkStates.Paused ||
            benchmarkState.value === BenchmarkStates.Init,
    );
    const skipDisabled = computed(() => benchmarkQueue.length === 0);

    return {
        start,
        stop,
        resume: resumeBenchmark,
        pause: pauseBenchmark,
        skip,
        startDisabled,
        stopDisabled,
        resumeDisabled,
        pauseDisabled,
        skipDisabled,
        benchmark,
        defaultOpts,
    };
});
