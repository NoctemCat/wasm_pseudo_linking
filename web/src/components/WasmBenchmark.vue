<script setup lang="ts">
import { useBenchmarkStore, type BenchmarkOptions } from '@/stores/benchmark';
import { sortTypes, useWasmStore, type SortType } from '@/stores/wasm';
import { storeToRefs } from 'pinia';
import { computed, ref } from 'vue';

const wasmStore = useWasmStore();
wasmStore.ensureInit();
const { mainModule } = storeToRefs(wasmStore);

const benchmarkStore = useBenchmarkStore();
const { pauseDisabled, resumeDisabled, startDisabled, stopDisabled, skipDisabled, benchmark } =
    storeToRefs(benchmarkStore);

const opts: BenchmarkOptions = {
    ...benchmarkStore.defaultOpts,
};

const mmTypes = computed(() => sortTypes.filter((t) => !t.includes('fallback')));

const getNormalBench = (sortType: SortType) => {
    return benchmark.value[sortType].getStrRepr();
};
const getFallbackBench = (sortType: SortType) => {
    const fallbackKey = (sortType + '_fallback') as SortType;
    if (benchmark.value[fallbackKey] === undefined) {
        return '---';
    }
    return benchmark.value[fallbackKey].getStrRepr();
};

const equalArrSize = ref(10000);
const equalRes = ref('No result');
const beginCheck = () => {
    equalRes.value = 'Wait for result';
    setTimeout(() => {
        const res = mainModule.value?._validate_sort_equality(equalArrSize.value, 1000000, true);
        if (res) {
            equalRes.value = 'Success';
        } else {
            equalRes.value = 'Failure';
        }
    }, 0);
};
const cantCheck = computed(() => mainModule.value === null);
</script>

<template>
    <h2 class="benchmark-heading">Quicksort test</h2>
    <h3 class="bench-title">Equality test</h3>
    <div class="sort-check">
        <div class="size">
            <div>Array Size</div>
            <input type="number" v-model="equalArrSize" />
        </div>
        <button :disabled="cantCheck" type="button" @click="beginCheck">Check</button>
        <div class="result">{{ equalRes }}</div>
    </div>
    <h3 class="bench-title">Benchmark</h3>
    <div class="inputs">
        <div>
            <div>Array Size</div>
            <input name="arrSize" v-model="opts.arrSize" type="number" />
        </div>
        <div>
            <div>Warmup Size</div>
            <input name="warmupSize" v-model="opts.warmupSize" type="number" />
        </div>
        <div>
            <div>Loop Size</div>
            <input name="loopSize" v-model="opts.loopSize" type="number" />
        </div>
        <div>
            <div>Batch Size</div>
            <input name="batchSize" v-model="opts.batchSize" type="number" />
        </div>
    </div>
    <div class="controllers">
        <button type="button" :disabled="startDisabled" @click="() => benchmarkStore.start(opts)">
            Start
        </button>
        <button type="button" :disabled="stopDisabled" @click="benchmarkStore.stop">Stop</button>
        <button type="button" :disabled="pauseDisabled" @click="benchmarkStore.pause">Pause</button>
        <button type="button" :disabled="resumeDisabled" @click="benchmarkStore.resume">
            Resume
        </button>
        <button type="button" :disabled="skipDisabled" @click="benchmarkStore.skip">Skip</button>
    </div>

    <div class="bench-desc">
        <div>_sort: uses native dlsym and operates on the main memory</div>
        <div>_sort_rb,_sort_net: copies data with <b>memcpy</b> analog and then operates on it</div>
        <div>
            <b>*_direct</b>: doesn't copy data, but directly sets and gets values from the array
        </div>
        <div><b>*_import</b>: does the same, but uses imported to wasm functions instead</div>
        <div>Fallback uses maunally adjusted simple memcpy</div>
    </div>
    <div class="bench-grid">
        <div>Sort Function</div>
        <div></div>
        <div>Fallback</div>
        <span class="divider"></span>
        <template v-for="mmType in mmTypes" :key="mmType">
            <div>{{ mmType }}:</div>
            <div>{{ getNormalBench(mmType) }}</div>
            <div>{{ getFallbackBench(mmType) }}</div>
            <span class="divider"></span>
        </template>
    </div>
</template>

<style>
.benchmark-heading {
    font-size: 2rem;
}

.sort-check {
    margin-top: 1rem;
    display: grid;
    width: 40%;
    gap: 1rem;
}

.sort-check .size {
    display: grid;
    grid-template-columns: 1fr 1fr;
}
.sort-check .result {
    color: rgb(214, 214, 214);
    margin-inline: auto;
}
.bench-title {
    margin-top: 1rem;
    color: rgb(214, 214, 214);
}
.inputs {
    margin-top: 1rem;
    display: grid;
    grid-template-columns: 1fr 1fr 1fr 1fr;
    gap: 1rem;
}
.inputs > div {
    width: 100%;
}
.inputs input,
.sort-check input {
    width: 100%;
    font-size: 1.2rem;
    font-family: Inter, Roboto, 'Helvetica Neue', sans-serif;
    background-color: rgb(207, 207, 207);
    border: 1px solid rgb(224, 242, 237);
}

.controllers {
    margin-top: 1rem;
    display: grid;
    grid-template-columns: 1fr 1fr 1fr 1fr;
    gap: 1rem;
}
.controllers button,
.sort-check button {
    font-size: 1.2rem;
    padding: 0.2rem 0.2rem;
    border-radius: 0px;
}
.bench-desc {
    margin-top: 2rem;
}

.bench-grid {
    margin-top: 1rem;
    width: 100%;
    /* font-size: 1.2rem; */
    display: grid;
    grid-template-columns: 1fr 1fr 1fr;
    gap: 0.2rem;
    margin-bottom: 24rem;
}

.bench-grid > div {
    margin-top: 0.3rem;
    margin-inline: auto;
}
.bench-grid > div:nth-child(4n + 1) {
    /* font-size: 1.4rem; */
    /* margin-left: auto; */
    /* margin-right: 0px; */
    margin-inline: unset;
    /* color: rgb(196, 0, 0); */
}
.bench-grid > div:nth-child(-n + 3) {
    font-size: 1.4rem;
    margin-inline: auto;
    color: rgb(214, 214, 214);
}

.bench-grid .divider {
    grid-column: span 3;
    border-bottom: 1px solid rgb(70, 70, 70);
}

table {
    margin-top: 2rem;
    border-collapse: collapse;
    width: 100%;
    table-layout: fixed;
    word-wrap: break-word;
    font-size: 1.2rem;
}

td {
    padding: 0.2rem;
    text-align: center;
    border: 1px solid rgb(224, 242, 237);
}
td.label {
    text-align: left;
}
/* .table-rows:nth-child(odd) {
    background-color: rgb(250, 250, 250);
}

.table-rows:nth-child(n):hover {
    background-color: rgb(244, 246, 245);
} */
</style>
