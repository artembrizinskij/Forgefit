// ─── Exercise ──────────────────────────────────────────────────────────────

export type ExerciseType = 'strength' | 'cardio' | 'stretch'

export interface ExerciseParams {
  // Strength
  weight: boolean
  reps: boolean
  tut: boolean
  rest: boolean
  restDuration: number
  rpe: boolean
  // Cardio
  duration: boolean
  pace: boolean
  distance: boolean
  incline: boolean
  heartRate: boolean
  units: 'kmh' | 'minpkm'
  // Stretch
  side: boolean
  breathing: boolean
}

export interface Exercise {
  id: string
  userId: string
  name: string
  muscleGroup: string
  muscles: string[]
  description: string
  type: ExerciseType
  params: ExerciseParams
  createdAt: string
  updatedAt: string
}

export type CreateExerciseDTO = Omit<Exercise, 'id' | 'userId' | 'createdAt' | 'updatedAt'>
export type UpdateExerciseDTO = Partial<CreateExerciseDTO>

// ─── Workout ───────────────────────────────────────────────────────────────

export interface WorkoutSet {
  id: string
  exerciseId: string
  weight?: number
  reps?: number
  tut?: number
  rpe?: number
  duration?: number
  pace?: number
  distance?: number
  incline?: number
  heartRate?: number
  side?: 'left' | 'right' | 'both'
  breathingCycles?: number
  timestamp: number
}

export interface WorkoutSession {
  id: string
  userId: string
  date: string // YYYY-MM-DD
  sets: WorkoutSet[]
  createdAt: string
}

export type CreateSetDTO = Omit<WorkoutSet, 'id'>

// ─── User ──────────────────────────────────────────────────────────────────

export interface User {
  id: string
  email: string
  passwordHash: string
  name: string
  createdAt: string
}

export type PublicUser = Omit<User, 'passwordHash'>

export interface CreateUserDTO {
  email: string
  passwordHash: string
  name: string
}
