import express from 'express'
import cors from 'cors'
import helmet from 'helmet'
import { config } from './config.js'
import { errorHandler } from './middleware/errorHandler.js'
import authRoutes from './routes/auth.js'
import exercisesRoutes from './routes/exercises.js'
import workoutsRoutes from './routes/workouts.js'

const app = express()

// ── Security & middleware ──────────────────────────────────────────────────

app.use(helmet())

app.use(
  cors({
    origin: config.corsOrigin,
    credentials: true,
  }),
)

app.use(express.json({ limit: '1mb' }))

// ── Health check ───────────────────────────────────────────────────────────

app.get('/api/health', (_req, res) => {
  res.json({ status: 'ok', timestamp: new Date().toISOString() })
})

// ── Routes ─────────────────────────────────────────────────────────────────

app.use('/api/auth', authRoutes)
app.use('/api/exercises', exercisesRoutes)
app.use('/api/workouts', workoutsRoutes)

// ── 404 handler ───────────────────────────────────────────────────────────

app.use((_req, res) => {
  res.status(404).json({ error: 'Маршрут не найден' })
})

// ── Global error handler ───────────────────────────────────────────────────

app.use(errorHandler)

export default app
