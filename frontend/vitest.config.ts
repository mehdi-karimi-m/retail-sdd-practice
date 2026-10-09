import { mergeConfig } from 'vite'
import { defineConfig } from 'vitest/config'
import viteConfig from './vite.config.ts'

export default mergeConfig(viteConfig, defineConfig({
  test: {
    globals: false,
    setupFiles: ['./src/test/setup.ts'],
    projects: [
      {
        extends: true,
        test: { name: 'node', environment: 'node', include: ['src/**/*.node.test.ts'] },
      },
      {
        extends: true,
        test: {
          name: 'dom',
          environment: 'jsdom',
          include: ['src/**/*.{test,spec}.{ts,tsx}'],
          exclude: ['src/**/*.node.test.ts', '**/node_modules/**'],
        },
      },
    ],
  },
}))
