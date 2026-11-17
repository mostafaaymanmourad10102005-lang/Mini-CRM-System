import { defineConfig } from 'vitest/config';
import react from '@vitejs/plugin-react';
import { fileURLToPath } from 'node:url';
const projectRoot = fileURLToPath(new URL('.', import.meta.url));
export default defineConfig({ root: projectRoot, plugins: [react()], server: { fs: { allow: [projectRoot] } }, test: { environment: 'jsdom' } });
