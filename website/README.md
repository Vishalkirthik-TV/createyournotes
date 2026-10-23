# CreateYourNotes — Marketing Website

A production-quality, static marketing site for **CreateYourNotes**, a Windows
desktop notes application. Built with **Astro**, **TypeScript**, and
**Tailwind CSS v4**, with self-hosted fonts and no client-side framework.

## Run it

```bash
cd website
npm install
npm run dev       # dev server at http://localhost:4321
npm run build     # static build to dist/
npm run preview   # preview the production build
```

## Regenerating brand assets

The logo, favicon, and Open Graph image are generated from the app icon
(`../Assets/HoldMyNotes.png`) with `sharp`:

```bash
node scripts/generate-images.mjs
```

This writes `public/images/logo.png`, `public/images/logo-512.png`,
`public/favicon.png`, and `public/images/og.png`.

## Configuration

- **Site content & links** — `src/config/site.ts` is the single source of
  truth for the product name, navigation, features, benefits, FAQ, and all
  outbound links.
- **Download destination** — set once in `src/config/site.ts` (`download.url`).
  It currently points at the GitHub releases page; swap it for a direct
  installer link once a release is published.
- **Design tokens** — brand colors, fonts, shadows, and animations live in
  `src/styles/global.css` under Tailwind's `@theme` block.

## Content rules

Every product claim is verified against the desktop app's source. Do **not**
add claims the app doesn't support (Markdown, cloud sync, encryption,
folders, tags, pinning, docking, or a default hotkey). Keep `site.ts` and the
copy in sync with the real application.

## Structure

```
src/
├── components/   Navbar, Hero, AppWindow, ProductShowcase, EdgeRailStory,
│                 FeatureRow, FeatureVisual, Benefits, DownloadCTA, FAQ,
│                 Footer, GithubIcon
├── config/       site.ts (content + links + download config)
├── layouts/      BaseLayout.astro (SEO, OG, fonts, reveal script)
├── pages/        index.astro, privacy.astro, 404.astro
└── styles/       global.css (Tailwind v4 @theme tokens)
```

## Deploy

`npm run build` produces a fully static `dist/` folder. Host it on any static
provider (Netlify, Vercel, GitHub Pages, Cloudflare Pages, an S3 bucket, …).
Update `site` in `astro.config.mjs` and the `url` in `site.ts` to your domain.
