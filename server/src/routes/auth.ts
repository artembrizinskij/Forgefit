import { Router } from 'express'
import bcrypt from 'bcryptjs'
import jwt from 'jsonwebtoken'
import { db } from '../db/index.js'
import { config } from '../config.js'
import { authenticate } from '../middleware/auth.js'
import type { AuthRequest } from '../middleware/auth.js'
import { httpError } from '../middleware/errorHandler.js'

const router = Router()

// ── POST /api/auth/register ────────────────────────────────────────────────
router.post('/register', async (req, res, next) => {
  try {
    const { email, password, name } = req.body as {
      email?: string
      password?: string
      name?: string
    }

    if (!email || !password || !name) {
      throw httpError(400, 'Поля email, password и name обязательны')
    }
    if (password.length < 6) {
      throw httpError(400, 'Пароль должен содержать минимум 6 символов')
    }

    const existing = await db.findUserByEmail(email)
    if (existing) {
      throw httpError(409, 'Пользователь с таким email уже существует')
    }

    const passwordHash = await bcrypt.hash(password, 10)
    const user = await db.createUser({ email: email.toLowerCase(), passwordHash, name })

    const token = jwt.sign({ userId: user.id }, config.jwtSecret, {
      expiresIn: config.jwtExpiresIn,
    })

    const { passwordHash: _ph, ...publicUser } = user
    res.status(201).json({ token, user: publicUser })
  } catch (err) {
    next(err)
  }
})

// ── POST /api/auth/login ───────────────────────────────────────────────────
router.post('/login', async (req, res, next) => {
  try {
    const { email, password } = req.body as { email?: string; password?: string }

    if (!email || !password) {
      throw httpError(400, 'Поля email и password обязательны')
    }

    const user = await db.findUserByEmail(email)
    if (!user) {
      throw httpError(401, 'Неверный email или пароль')
    }

    const valid = await bcrypt.compare(password, user.passwordHash)
    if (!valid) {
      throw httpError(401, 'Неверный email или пароль')
    }

    const token = jwt.sign({ userId: user.id }, config.jwtSecret, {
      expiresIn: config.jwtExpiresIn,
    })

    const { passwordHash: _ph, ...publicUser } = user
    res.json({ token, user: publicUser })
  } catch (err) {
    next(err)
  }
})

// ── GET /api/auth/me ───────────────────────────────────────────────────────
router.get('/me', authenticate, (req: AuthRequest, res) => {
  res.json({ user: req.user })
})

export default router
