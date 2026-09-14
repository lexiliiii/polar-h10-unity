import { build } from '../PolarH10Visualizer/node_modules/esbuild/lib/main.js';
import { cp, mkdir } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
const root = fileURLToPath(new URL('../PolarH10Visualizer/', import.meta.url));
await mkdir(root + 'build/styles', { recursive: true });
for (const f of ['index.html', 'favicon.ico', 'styles', 'coi-serviceworker.min.js']) await cp(root + 'src/' + f, root + 'build/' + f, { recursive: true });
for (const f of ['spectre.min.css', 'spectre-icons.min.css']) await cp(root + 'node_modules/@spectre-org/spectre-css/dist/' + f, root + 'build/styles/' + f);
await build({ absWorkingDir: root, entryPoints: [root + 'src/index.ts'], tsconfigRaw: {}, bundle: true, outfile: 'build/index.js', sourcemap: true, define: { 'window.IS_PRODUCTION': 'true' }, format: 'esm', minify: true, external: ['text-encoding'] });
console.log('Built original EXG visualizer with Windows-compatible asset copying.');
