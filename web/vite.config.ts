import { fileURLToPath, URL } from 'node:url';

import { defineConfig, type Plugin, type HtmlTagDescriptor } from 'vite';
import vue from '@vitejs/plugin-vue';
import vueDevTools from 'vite-plugin-vue-devtools';
import VueI18nPlugin from '@intlify/unplugin-vue-i18n/vite';
import UnheadVite from '@unhead/addons/vite';

// https://vite.dev/config/
export default defineConfig({
    base: '/wasm_pseudo_linking/',
    plugins: [vue(), vueDevTools(), VueI18nPlugin(), UnheadVite(), addFontPreload()],
    resolve: {
        alias: {
            '@': fileURLToPath(new URL('./src', import.meta.url)),
        },
    },
});

function addFontPreload(): Plugin {
    const extractTags = (src: string): HtmlTagDescriptor[] => {
        const tags: HtmlTagDescriptor[] = [];
        // Only "woff2"
        const reg = /src:url\(['"]?(.+?)['"]?\)\s+format\(\s*['"]woff2['"]\s*\)/g;
        let match: RegExpExecArray | null;
        while ((match = reg.exec(src))) {
            const href = match[1];
            // If url includes inter
            if (href.includes('inter')) {
                tags.push({
                    injectTo: 'head-prepend',
                    tag: 'link',
                    attrs: {
                        rel: 'preload',
                        as: 'font',
                        type: 'font/woff2',
                        href: href,
                        crossorigin: true,
                    },
                });
            }
        }
        return tags;
    };

    return {
        name: 'vite-add-font-preload',
        transformIndexHtml: {
            order: 'post',
            handler: (_, ctx) => {
                if (ctx.bundle == null) {
                    return [];
                }
                const tags: HtmlTagDescriptor[] = [];
                for (const [k, v] of Object.entries(ctx.bundle)) {
                    if (v.type === 'asset' && typeof v.source === 'string' && k.endsWith('.css')) {
                        tags.push(...extractTags(v.source));
                    }
                }
                return tags;
            },
        },
    };
}
