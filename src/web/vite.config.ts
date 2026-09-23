import { existsSync, readFileSync } from 'node:fs'
import { homedir } from 'node:os'
import { join } from 'node:path'
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// ASP.NET Core dev certificate exported by `make certs` (shared with the api/bff containers).
const certDir = join(homedir(), '.aspnet', 'https')
const certFile = join(certDir, 'invoicing.pem')
const keyFile = join(certDir, 'invoicing.key')

// https://vite.dev/config/
export default defineConfig(({ command }) => {
  if (command !== 'serve') {
    return { plugins: [react()] }
  }

  if (!existsSync(certFile) || !existsSync(keyFile)) {
    throw new Error(`Dev certificate not found in ${certDir}. Run \`make certs\` from the repo root.`)
  }
  const cert = readFileSync(certFile, 'utf8')

  return {
    plugins: [react()],
    server: {
      port: 5173,
      https: { cert, key: readFileSync(keyFile, 'utf8') },
      proxy: {
        // Same-origin from the browser's perspective, so the BFF's session
        // cookie is set for this origin and no CORS policy is needed.
        '/bff': {
          target: 'https://localhost:7180',
          changeOrigin: true,
          // Node doesn't read the OS trust store; trust the dev certificate explicitly.
          ca: cert,
        },
      },
    },
  }
})
