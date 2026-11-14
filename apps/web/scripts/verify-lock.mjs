// YangSpace S0-02: read-only integrity and manifest alignment check.
// npm ci validates resolution reproducibility; this script rejects accidental
// external registry URLs and manifest drift before installing any package.
import { readFileSync } from 'node:fs';
import { createHash } from 'node:crypto';
import { resolve } from 'node:path';

const cwd = process.cwd();
const manifest = JSON.parse(readFileSync(resolve(cwd, 'package.json'), 'utf8'));
const lockBytes = readFileSync(resolve(cwd, 'package-lock.json'));
const lock = JSON.parse(lockBytes.toString('utf8'));

function fail(message) {
  process.stderr.write('LOCKFILE_VERIFY=FAIL: ' + message + '\n');
  process.exitCode = 1;
}

if (lock.lockfileVersion !== 3 || !lock.packages || !lock.packages['']) {
  fail('Expected npm lockfile v3 with root package metadata');
} else {
  const root = lock.packages[''];
  for (const key of ['name', 'version']) {
    if (root[key] !== manifest[key]) fail('Root ' + key + ' mismatch');
  }
  for (const key of ['dependencies', 'devDependencies', 'optionalDependencies', 'engines']) {
    const actual = root[key] ?? {};
    const expected = manifest[key] ?? {};
    if (JSON.stringify(Object.entries(actual).sort()) !== JSON.stringify(Object.entries(expected).sort())) {
      fail('Root ' + key + ' differs from package.json');
    }
  }
  let checked = 0;
  for (const [name, pkg] of Object.entries(lock.packages)) {
    if (!name) continue;
    if (pkg.resolved) {
      try {
        const url = new URL(pkg.resolved);
        if (url.protocol !== 'https:' || url.hostname !== 'registry.npmjs.org' || url.username || url.password) {
          fail('Unexpected package registry for ' + name);
        }
      } catch {
        fail('Invalid package URL for ' + name);
      }
      if (!pkg.integrity || !/^sha(?:256|384|512)-/.test(pkg.integrity)) {
        fail('Package integrity digest missing for ' + name);
      }
      checked++;
    }
  }
  if (process.exitCode !== 1) {
    const sha256 = createHash('sha256').update(lockBytes).digest('hex');
    console.log('LOCKFILE_VERIFY=PASS');
    console.log('LOCKFILE_REGISTRY_PACKAGES=' + checked);
    console.log('LOCKFILE_SHA256=' + sha256);
  }
}
