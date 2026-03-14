import { Router } from 'express'
import { db } from '../db/index.js'
import { authenticate } from '../middleware/auth.js'
import type { AuthRequest } from '../middleware/auth.js'
import { httpError } from '../middleware/errorHandler.js'
import type { CreateSetDTO } from '../types/index.js'

const router = Router()

// All workout routes require authentication
router.use(authenticate)

function todayISO(): string {
  return new Date().toISOString().split('T')[0]
}

// ── GET /api/workouts ──────────────────────────────────────────────────────
// Returns all sessions for the current user
router.get('/', async (req: AuthRequest, res, next) => {
  try {
    const sessions = await db.getSessions(req.userId!)
    res.json(sessions)
  } catch (err) {
    next(err)
  }
})

// ── GET /api/workouts/today ────────────────────────────────────────────────
// Returns today's session (creates it if it doesn't exist)
router.get('/today', async (req: AuthRequest, res, next) => {
  try {
    const session = await db.getOrCreateSessionByDate(req.userId!, todayISO())
    res.json(session)
  } catch (err) {
    next(err)
  }
})

// ── GET /api/workouts/history/:exerciseId ─────────────────────────────────
// Returns past sessions containing sets for the given exercise
router.get('/history/:exerciseId', async (req: AuthRequest, res, next) => {
  try {
    const limit = Math.min(Number(req.query.limit) || 3, 20)
    const sessions = await db.getHistoryForExercise(
      req.userId!,
      req.params.exerciseId,
      limit,
    )
    res.json(sessions)
  } catch (err) {
    next(err)
  }
})

// ── POST /api/workouts/sets ────────────────────────────────────────────────
// Adds a set to today's session
router.post('/sets', async (req: AuthRequest, res, next) => {
  try {
    const body = req.body as Partial<CreateSetDTO>

    if (!body.exerciseId) throw httpError(400, 'exerciseId обязателен')

    // Ensure today's session exists
    const session = await db.getOrCreateSessionByDate(req.userId!, todayISO())

    const setData: CreateSetDTO = {
      exerciseId: body.exerciseId,
      timestamp: body.timestamp ?? Date.now(),
      ...(body.weight !== undefined && { weight: body.weight }),
      ...(body.reps !== undefined && { reps: body.reps }),
      ...(body.tut !== undefined && { tut: body.tut }),
      ...(body.rpe !== undefined && { rpe: body.rpe }),
      ...(body.duration !== undefined && { duration: body.duration }),
      ...(body.pace !== undefined && { pace: body.pace }),
      ...(body.distance !== undefined && { distance: body.distance }),
      ...(body.incline !== undefined && { incline: body.incline }),
      ...(body.heartRate !== undefined && { heartRate: body.heartRate }),
      ...(body.side !== undefined && { side: body.side }),
      ...(body.breathingCycles !== undefined && { breathingCycles: body.breathingCycles }),
    }

    const set = await db.addSet(session.id, req.userId!, setData)
    res.status(201).json(set)
  } catch (err) {
    next(err)
  }
})

// ── DELETE /api/workouts/sets/:id ─────────────────────────────────────────
router.delete('/sets/:id', async (req: AuthRequest, res, next) => {
  try {
    const deleted = await db.removeSet(req.params.id, req.userId!)
    if (!deleted) throw httpError(404, 'Подход не найден')
    res.status(204).send()
  } catch (err) {
    next(err)
  }
})

export default router
