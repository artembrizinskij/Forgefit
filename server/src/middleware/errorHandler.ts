import type { Request, Response, NextFunction } from 'express'

export interface AppError extends Error {
  statusCode?: number
}

export function errorHandler(
  err: AppError,
  _req: Request,
  res: Response,
  _next: NextFunction,
): void {
  const status = err.statusCode ?? 500
  const message = status === 500 ? 'Внутренняя ошибка сервера' : err.message
  if (status === 500) console.error('[Server Error]', err)
  res.status(status).json({ error: message })
}

/** Tiny helper to create errors with a status code. */
export function httpError(statusCode: number, message: string): AppError {
  const err = new Error(message) as AppError
  err.statusCode = statusCode
  return err
}
