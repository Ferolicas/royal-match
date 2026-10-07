import * as fs from 'node:fs';
import * as path from 'node:path';
import { LevelDataSchema } from '../src/levels/LevelData';
import { TOTAL_LEVELS, getFolderRange, getLevelFilename } from '../src/levels/Difficulty';

const LEVELS_DIR = path.resolve(__dirname, '../src/levels/levels');

interface ValidationResult {
  levelNumber: number;
  valid: boolean;
  error?: string;
}

async function validateAll(): Promise<ValidationResult[]> {
  const results: ValidationResult[] = [];
  let passed = 0;
  let failed = 0;

  for (let i = 1; i <= TOTAL_LEVELS; i++) {
    const folderRange = getFolderRange(i);
    const filename = getLevelFilename(i);
    const filePath = path.join(LEVELS_DIR, folderRange, filename);

    if (!fs.existsSync(filePath)) {
      results.push({ levelNumber: i, valid: false, error: 'File not found' });
      failed++;
      continue;
    }

    const raw = fs.readFileSync(filePath, 'utf-8');
    const json = JSON.parse(raw);
    const result = LevelDataSchema.safeParse(json);

    if (result.success) {
      results.push({ levelNumber: i, valid: true });
      passed++;
    } else {
      const errorMsg = result.error.issues.map((i) => i.message).join(', ');
      results.push({ levelNumber: i, valid: false, error: errorMsg });
      failed++;
    }
  }

  console.log(`✅ ${passed} levels passed validation`);
  if (failed > 0) {
    console.log(`❌ ${failed} levels failed validation`);
  }

  // Print details for any failures
  const failures = results.filter((r) => !r.valid);
  if (failures.length > 0) {
    console.log('\nFailures:');
    for (const f of failures) {
      console.log(`  Level ${f.levelNumber}: ${f.error}`);
    }
  }

  return results;
}

if (require.main === module) {
  validateAll().catch((err) => {
    console.error('❌ Validation failed:', err);
    process.exit(1);
  });
}

export { validateAll, ValidationResult };
