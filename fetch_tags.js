#!/usr/bin/env node
import fs from 'fs/promises';

if (process.argv.length < 3) {
  console.error('Usage: node fetch_tags.js <urls.txt> [concurrency]');
  process.exit(1);
}

const file = process.argv[2];
const concurrency = Number(process.argv[3] || 5);
const urls = (await fs.readFile(file, 'utf-8')).split(/\r?\n/).filter(Boolean);

const results = [];
let index = 0;

async function worker() {
  while (true) {
    const i = index++;
    if (i >= urls.length) break;
    const itemUrl = urls[i];
    const m = itemUrl.match(/\/([^/]{2})?\/?items\/(\d+)/);
    if (!m) continue;
    const lang = m[1] || 'ja';
    const apiUrl = `https://booth.pm/${lang}/items/${m[2]}.json`;
    try {
      const res = await fetch(apiUrl, { credentials: 'omit' });
      const json = await res.json();
      const tags = Array.isArray(json.tags)
        ? json.tags.map(t => (t && t.name ? t.name.trim() : '')).filter(Boolean)
        : [];
      results.push({ itemUrl, tags });
    } catch (e) {
      console.warn('Failed to fetch', apiUrl, e);
    }
  }
}

await Promise.all(Array.from({ length: concurrency }, worker));
await fs.writeFile('tags.json', JSON.stringify(results, null, 2));
console.log('Wrote tags.json');
