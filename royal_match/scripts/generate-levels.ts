import * as fs from 'node:fs';
import * as path from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { generateLevel } from '../src/levels/LevelGenerator';
import { TOTAL_LEVELS, getLevelFilename, getFolderRange } from '../src/levels/Difficulty';

const SCRIPT_DIR = path.dirname(fileURLToPath(import.meta.url));
const LEVELS_DIR = path.resolve(SCRIPT_DIR, '../src/levels/levels');

/** Generates all 1000 level JSON files and writes them to disk. */
async function main(watch: boolean = false): Promise<void> {
  const generateAll = () => {
    console.log('Generating 1000 levels...');
    const total = TOTAL_LEVELS;
    let count = 0;

    for (let i = 1; i <= total; i++) {
      const levelData = generateLevel(i);
      const folderRange = getFolderRange(i);
      const folderPath = path.join(LEVELS_DIR, folderRange);

      if (!fs.existsSync(folderPath)) {
        fs.mkdirSync(folderPath, { recursive: true });
      }

      const filename = getLevelFilename(i);
      const filePath = path.join(folderPath, filename);
      fs.writeFileSync(filePath, JSON.stringify(levelData, null, 2), 'utf-8');

      count++;
      if (count % 100 === 0) {
        console.log(`  ...${count}/${total} levels generated`);
      }
    }

    console.log(`✅ Generated ${count} levels in ${LEVELS_DIR}`);
  };

  generateAll();

  if (watch) {
    console.log('Watching for source changes...');
    let timeout: NodeJS.Timeout | null = null;
    fs.watch(path.resolve(__dirname, '../src'), { recursive: true }, () => {
      if (timeout) clearTimeout(timeout);
      timeout = setTimeout(() => {
        console.log('\n[watch] Changes detected, regenerating levels...');
        generateAll();
      }, 300);
    });
  }
}

// CLI entry
if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  const watch = process.argv.includes('--watch');
  main(watch).catch((err) => {
    console.error('❌ Level generation failed:', err);
    process.exit(1);
  });
}
