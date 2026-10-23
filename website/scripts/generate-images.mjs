// Generates web-ready brand assets from the app's 3D folded-document icon.
// Source: ../Assets/HoldMyNotes.png (large transparent PNG).
// Run with: node scripts/generate-images.mjs
import sharp from 'sharp';
import { mkdir } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const SRC = path.join(__dirname, '..', '..', 'Assets', 'HoldMyNotes.png');
const PUB = path.join(__dirname, '..', 'public');

await mkdir(path.join(PUB, 'images'), { recursive: true });

// Transparent logo at a few useful sizes (trimmed, aspect preserved).
async function logo(width, out) {
  await sharp(SRC)
    .resize(width, width, { fit: 'contain', background: { r: 0, g: 0, b: 0, alpha: 0 } })
    .png()
    .toFile(path.join(PUB, out));
  console.log('✓', out);
}

await logo(192, 'images/logo.png');
await logo(512, 'images/logo-512.png');
await logo(64, 'favicon.png');

// Open Graph share card: branded gradient + soft glows + centered logo.
const bg = Buffer.from(`<svg width="1200" height="630" xmlns="http://www.w3.org/2000/svg">
  <defs>
    <linearGradient id="g" x1="0" y1="0" x2="1" y2="1">
      <stop offset="0" stop-color="#4A18AE"/>
      <stop offset="0.5" stop-color="#6525E8"/>
      <stop offset="1" stop-color="#E83EAD"/>
    </linearGradient>
    <radialGradient id="glow1" cx="0.12" cy="0.18" r="0.55">
      <stop offset="0" stop-color="#FFD83D" stop-opacity="0.45"/>
      <stop offset="1" stop-color="#FFD83D" stop-opacity="0"/>
    </radialGradient>
    <radialGradient id="glow2" cx="0.88" cy="0.92" r="0.55">
      <stop offset="0" stop-color="#FF7A24" stop-opacity="0.5"/>
      <stop offset="1" stop-color="#FF7A24" stop-opacity="0"/>
    </radialGradient>
  </defs>
  <rect width="1200" height="630" fill="url(#g)"/>
  <rect width="1200" height="630" fill="url(#glow1)"/>
  <rect width="1200" height="630" fill="url(#glow2)"/>
</svg>`);

const mark = await sharp(SRC)
  .resize(320, 320, { fit: 'contain', background: { r: 0, g: 0, b: 0, alpha: 0 } })
  .png()
  .toBuffer();

await sharp(bg)
  .composite([{ input: mark, gravity: 'center' }])
  .png()
  .toFile(path.join(PUB, 'images', 'og.png'));
console.log('✓ images/og.png');

console.log('Done.');
