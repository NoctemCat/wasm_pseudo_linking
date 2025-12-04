<script setup lang="ts">
import AppHeader from './components/AppHeader.vue';
import WasmProfile from './components/WasmBenchmark.vue';
</script>

<template>
    <header>
        <div class="wrapper">
            <AppHeader :msg="'Wasm Pseudo Linking'" />
        </div>
    </header>

    <WasmProfile />
</template>

<style scoped>
header {
    line-height: 1.5;
    max-height: 100vh;
}
header .wrapper {
    margin-top: 2rem;
}
.logo {
    display: block;
    margin: 0 auto 2rem;
}
</style>

<style>
body {
    font-size: 1.2rem;
}
</style>
