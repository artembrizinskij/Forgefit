import type { Request, Response, NextFunction } from 'express'
import jwt from 'jsonwebtoken'
import { config } from '../config.js'
import { db } from '../db/index.js'
import type { PublicUser } from '../types/index.js'

export interface AuthRequest extends Request {
  userId?: string
  user?: PublicUser
}

interface JwtPayload {
  userId: string
  iat: number
  exp: number
}

export async function authenticate(
  req: AuthRequest,
  res: Response,
  next: NextFunction,
): Promise<void> {
  const authHeader = req.headers.authorization
  if (!authHeader?.startsWith('Bearer ')) {
    res.status(401).json({ error: 'Требуется авторизация' })
    return
  }

  const token = authHeader.slice(7)

  let payload: JwtPayload
  try {
    payload = jwt.verify(token, config.jwtSecret) as JwtPayload
  } catch {
    res.status(401).json({ error: 'Недействительный или истёкший токен' })
    return
  }

  const user = await db.findUserById(payload.userId)
  if (!user) {
    res.status(401).json({ error: 'Пользователь не найден' })
    return
  }

  // Attach user info (without passwordHash) to the request
  const { passwordHash: _ph, ...publicUser } = user
  req.userId = user.id
  req.user = publicUser
  next()
}
