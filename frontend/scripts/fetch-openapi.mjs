// Downloads the API's OpenAPI document into openapi.json; then `npm run api:types` regenerates src/api/schema.d.ts.
// The backend must be running with API docs on (Development, or ApiDocs:Enabled). URL: VITE_API_URL or :8080.
import { writeFile } from 'node:fs/promises';

const base = process.env.VITE_API_URL ?? 'http://localhost:8080';
const response = await fetch(`${base}/openapi/v1.json`);
if (!response.ok) {
  console.error(`Could not read ${base}/openapi/v1.json (${response.status}). Is the API running with docs enabled?`);
  process.exit(1);
}
await writeFile(new URL('../openapi.json', import.meta.url), JSON.stringify(await response.json(), null, 2) + '\n');
console.log('openapi.json updated; run `npm run api:types` next.');
