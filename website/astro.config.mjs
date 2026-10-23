// @ts-check
import { defineConfig } from 'astro/config';
import sitemap from '@astrojs/sitemap';
import tailwindcss from '@tailwindcss/vite';

// https://astro.build/config
export default defineConfig({
  // Canonical domain — update if the site is deployed elsewhere.
  site: 'https://createyournotes.app',
  integrations: [sitemap()],
  vite: {
    plugins: [tailwindcss()],
  },
});
