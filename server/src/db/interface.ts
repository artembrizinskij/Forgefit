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

/**
 * Abstract database interface.
 * Swap out the implementation in db/index.ts to use any database.
 * All methods are async to support both sync (in-memory) and async (SQL, Mongo, etc.) backends.
 */
export interface IDatabase {
  // ── Users ──────────────────────────────────────────────────────────────

  /** Find a user by their email address (case-insensitive). */
  findUserByEmail(email: string): Promise<User | null>

  /** Find a user by their unique ID. */
  findUserById(id: string): Promise<User | null>

  /** Create a new user record. */
  createUser(data: CreateUserDTO): Promise<User>

  // ── Exercises ──────────────────────────────────────────────────────────

  /** Return all exercises that belong to the given user. */
  getExercises(userId: string): Promise<Exercise[]>

  /** Return one exercise by ID, scoped to the owner. Returns null if not found or not owned. */
  getExerciseById(id: string, userId: string): Promise<Exercise | null>

  /** Create a new exercise for the given user. */
  createExercise(userId: string, data: CreateExerciseDTO): Promise<Exercise>

  /** Update an existing exercise. Returns null if not found or not owned. */
  updateExercise(id: string, userId: string, data: UpdateExerciseDTO): Promise<Exercise | null>

  /** Delete an exercise. Returns true on success, false if not found or not owned. */
  deleteExercise(id: string, userId: string): Promise<boolean>

  // ── Workout sessions & sets ────────────────────────────────────────────

  /** Return all sessions for the given user, sorted newest first. */
  getSessions(userId: string): Promise<WorkoutSession[]>

  /** Return the session for a specific date or null. */
  getSessionByDate(userId: string, date: string): Promise<WorkoutSession | null>

  /** Return the session for a specific date, creating it if it doesn't exist. */
  getOrCreateSessionByDate(userId: string, date: string): Promise<WorkoutSession>

  /** Append a set to the session. Returns the created WorkoutSet. */
  addSet(sessionId: string, userId: string, data: CreateSetDTO): Promise<WorkoutSet>

  /** Remove a set by ID. Returns true on success. */
  removeSet(setId: string, userId: string): Promise<boolean>

  /**
   * Return up to `limit` past sessions (excluding today) that contain
   * at least one set for the given exercise, newest first.
   */
  getHistoryForExercise(userId: string, exerciseId: string, limit: number): Promise<WorkoutSession[]>
}
