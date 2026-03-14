import { Router } from 'express'
import { db } from '../db/index.js'
import { authenticate } from '../middleware/auth.js'
import type { AuthRequest } from '../middleware/auth.js'
import { httpError } from '../middleware/errorHandler.js'
import type { CreateExerciseDTO } from '../types/index.js'

const router = Router()

// All exercise routes require authentication
router.use(authenticate)

// ── GET /api/exercises ─────────────────────────────────────────────────────
router.get('/', async (req: AuthRequest, res, next) => {
  try {
    const exercises = await db.getExercises(req.userId!)
    res.json(exercises)
  } catch (err) {
    next(err)
  }
})

// ── GET /api/exercises/:id ─────────────────────────────────────────────────
router.get('/:id', async (req: AuthRequest, res, next) => {
  try {
    const exercise = await db.getExerciseById(req.params.id, req.userId!)
    if (!exercise) throw httpError(404, 'Упражнение не найдено')
    res.json(exercise)
  } catch (err) {
    next(err)
  }
})

// ── POST /api/exercises ────────────────────────────────────────────────────
router.post('/', async (req: AuthRequest, res, next) => {
  try {
    const { name, muscleGroup, muscles, description, type, params } =
      req.body as Partial<CreateExerciseDTO>

    if (!name?.trim()) throw httpError(400, 'Название упражнения обязательно')
    if (!type) throw httpError(400, 'Тип упражнения обязателен')
    if (!muscleGroup) throw httpError(400, 'Группа мышц обязательна')
    if (!params) throw httpError(400, 'Параметры упражнения обязательны')

    const exercise = await db.createExercise(req.userId!, {
      name: name.trim(),
      muscleGroup,
      muscles: muscles ?? [],
      description: description ?? '',
      type,
      params,
    })

    res.status(201).json(exercise)
  } catch (err) {
    next(err)
  }
})

// ── PUT /api/exercises/:id ─────────────────────────────────────────────────
router.put('/:id', async (req: AuthRequest, res, next) => {
  try {
    const data = req.body as Partial<CreateExerciseDTO>
    if (data.name !== undefined && !data.name.trim()) {
      throw httpError(400, 'Название не может быть пустым')
    }

    const updated = await db.updateExercise(req.params.id, req.userId!, {
      ...data,
      name: data.name?.trim(),
    })

    if (!updated) throw httpError(404, 'Упражнение не найдено')
    res.json(updated)
  } catch (err) {
    next(err)
  }
})

// ── DELETE /api/exercises/:id ──────────────────────────────────────────────
router.delete('/:id', async (req: AuthRequest, res, next) => {
  try {
    const deleted = await db.deleteExercise(req.params.id, req.userId!)
    if (!deleted) throw httpError(404, 'Упражнение не найдено')
    res.status(204).send()
  } catch (err) {
    next(err)
  }
})

export default router
