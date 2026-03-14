import 'dotenv/config'
import app from './app.js'
import { config } from './config.js'

app.listen(config.port, () => {
  console.log(`✅  Forgefit API  →  http://localhost:${config.port}`)
  console.log(`   Health:        →  http://localhost:${config.port}/api/health`)
})
