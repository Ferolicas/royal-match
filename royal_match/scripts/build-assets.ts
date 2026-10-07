import * as fs from 'node:fs';
import * as path from 'node:path';

/**
 * Builds an asset manifest from files in `public/assets/`.
 * Scans for images, fonts, and audio, recording their paths and sizes.
 */
interface AssetEntry {
  name: string;
  path: string;
  size: number;
  type: 'image' | 'font' | 'audio' | 'data';
}

const ASSETS_DIR = path.resolve(__dirname, '../public/assets');
const OUTPUT_FILE = path.resolve(__dirname, '../public/assets-manifest.json');

const IMAGE_EXTENSIONS = ['.png', '.jpg', '.jpeg', '.gif', '.svg', '.webp'];
const FONT_EXTENSIONS = ['.woff', '.woff2', '.ttf', '.otf', '.eot'];
const AUDIO_EXTENSIONS = ['.mp3', '.wav', '.ogg', '.m4a'];
const DATA_EXTENSIONS = ['.json', '.csv', '.xml'];

function getAssetType(ext: string): AssetEntry['type'] {
  const e = ext.toLowerCase();
  if (IMAGE_EXTENSIONS.includes(e)) return 'image';
  if (FONT_EXTENSIONS.includes(e)) return 'font';
  if (AUDIO_EXTENSIONS.includes(e)) return 'audio';
  if (DATA_EXTENSIONS.includes(e)) return 'data';
  return 'data';
}

function scanDirectory(dir: string, base: string): AssetEntry[] {
  const entries: AssetEntry[] = [];
  if (!fs.existsSync(dir)) return entries;

  const items = fs.readdirSync(dir, { withFileTypes: true });
  for (const item of items) {
    const fullPath = path.join(dir, item.name);
    const relPath = path.relative(base, fullPath).replace(/\\/g, '/');

    if (item.isDirectory()) {
      entries.push(...scanDirectory(fullPath, base));
    } else {
      const ext = path.extname(item.name);
      const type = getAssetType(ext);
      if (type !== 'data') {
        const stat = fs.statSync(fullPath);
        entries.push({
          name: path.basename(item.name, ext),
          path: relPath,
          size: stat.size,
          type,
        });
      }
    }
  }

  return entries;
}

function main(): void {
  const assets = scanDirectory(ASSETS_DIR, ASSETS_DIR);
  const manifest = {
    generatedAt: new Date().toISOString(),
    count: assets.length,
    assets: assets.sort((a, b) => a.path.localeCompare(b.path)),
  };

  fs.writeFileSync(OUTPUT_FILE, JSON.stringify(manifest, null, 2), 'utf-8');
  console.log(`✅ Asset manifest built: ${assets.length} assets → ${OUTPUT_FILE}`);
}

if (require.main === module) {
  main();
}

export { scanDirectory, AssetEntry };
