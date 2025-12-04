import './assets/main.css';
import '@fontsource/inter/400.css';

import { createApp } from 'vue';
import { createPinia } from 'pinia';

import App from './App.vue';

const app = createApp(App);

app.use(createPinia());

app.mount('#app');
