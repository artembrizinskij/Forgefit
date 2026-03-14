import { v4 as uuidv4 } from 'uuid'
import type { IDatabase } from './interface.js'
import type {
  User,
  CreateUserDTO,
  Exercise,
  CreateExerciseDTO,
  UpdateExerciseDTO,
  WorkoutSession,
  WorkoutSet,
  CreateSetDTO,
} from '../types/index.js'

function todayISO(): string {
  return new Date().toISOString().split('T')[0]
}

/**
 * In-memory database implementation.
 * All data is stored in plain arrays/maps in process memory.
 * Data is lost on server restart — intended for development and testing.
 *
 * Replace this class with a real connector (PostgreSQL, MongoDB, SQLite, etc.)
 * by implementing the same IDatabase interface and updating db/index.ts.
 */
export class InMemoryDatabase implements IDatabase {
  private users: User[] = []
  private exercises: Exercise[] = []
  private sessions: WorkoutSession[] = []

  // ── Users ────────────────────────────────────────────────────────────────

  async findUserByEmail(email: string): Promise<User | null> {
    return this.users.find((u) => u.email.toLowerCase() === email.toLowerCase()) ?? null
  }

  async findUserById(id: string): Promise<User | null> {
    return this.users.find((u) => u.id === id) ?? null
  }

  async createUser(data: CreateUserDTO): Promise<User> {
    const user: User = {
      id: uuidv4(),
      ...data,
      createdAt: new Date().toISOString(),
    }
    this.users.push(user)
    return user
  }

  // ── Exercises ─────────────────────────────────────────────────────────────

  async getExercises(userId: string): Promise<Exercise[]> {
    return this.exercises.filter((e) => e.userId === userId)
  }

  async getExerciseById(id: string, userId: string): Promise<Exercise | null> {
    return this.exercises.find((e) => e.id === id && e.userId === userId) ?? null
  }

  async createExercise(userId: string, data: CreateExerciseDTO): Promise<Exercise> {
    const now = new Date().toISOString()
    const exercise: Exercise = {
      id: uuidv4(),
      userId,
      ...data,
      createdAt: now,
      updatedAt: now,
    }
    this.exercises.push(exercise)
    return exercise
  }

  async updateExercise(
    id: string,
    userId: string,
    data: UpdateExerciseDTO,
  ): Promise<Exercise | null> {
    const idx = this.exercises.findIndex((e) => e.id === id && e.userId === userId)
    if (idx === -1) return null
    this.exercises[idx] = {
      ...this.exercises[idx],
      ...data,
      updatedAt: new Date().toISOString(),
    }
    return this.exercises[idx]
  }

  async deleteExercise(id: string, userId: string): Promise<boolean> {
    const idx = this.exercises.findIndex((e) => e.id === id && e.userId === userId)
    if (idx === -1) return false
    this.exercises.splice(idx, 1)
    return true
  }

  // ── Workout sessions & sets ───────────────────────────────────────────────

  async getSessions(userId: string): Promise<WorkoutSession[]> {
    return this.sessions
      .filter((s) => s.userId === userId)
      .sort((a, b) => b.date.localeCompare(a.date))
  }

  async getSessionByDate(userId: string, date: string): Promise<WorkoutSession | null> {
    return this.sessions.find((s) => s.userId === userId && s.date === date) ?? null
  }

  async getOrCreateSessionByDate(userId: string, date: string): Promise<WorkoutSession> {
    const existing = await this.getSessionByDate(userId, date)
    if (existing) return existing
    const session: WorkoutSession = {
      id: uuidv4(),
      userId,
      date,
      sets: [],
      createdAt: new Date().toISOString(),
    }
    this.sessions.push(session)
    return session
  }

  async addSet(sessionId: string, userId: string, data: CreateSetDTO): Promise<WorkoutSet> {
    const session = this.sessions.find((s) => s.id === sessionId && s.userId === userId)
    if (!session) throw new Error('Session not found')
    const set: WorkoutSet = { id: uuidv4(), ...data }
    session.sets.push(set)
    return set
  }

  async removeSet(setId: string, userId: string): Promise<boolean> {
    // Find the session owned by this user that contains the set
    const session = this.sessions.find(
      (s) => s.userId === userId && s.sets.some((st) => st.id === setId),
    )
    if (!session) return false
    const idx = session.sets.findIndex((st) => st.id === setId)
    if (idx === -1) return false
    session.sets.splice(idx, 1)
    return true
  }

  async getHistoryForExercise(
    userId: string,
    exerciseId: string,
    limit: number,
  ): Promise<WorkoutSession[]> {
    const today = todayISO()
    return this.sessions
      .filter(
        (s) =>
          s.userId === userId &&
          s.date !== today &&
          s.sets.some((st) => st.exerciseId === exerciseId),
      )
      .sort((a, b) => b.date.localeCompare(a.date))
      .slice(0, limit)
      .map((s) => ({
        ...s,
        // Return only sets for the requested exercise
        sets: s.sets.filter((st) => st.exerciseId === exerciseId),
      }))
  }
}
