export type ExerciseType = 'strength' | 'cardio' | 'stretch'

export type MuscleGroup =
  | 'Грудь'
  | 'Спина'
  | 'Плечи'
  | 'Бицепс'
  | 'Трицепс'
  | 'Ноги'
  | 'Ягодицы'
  | 'Пресс'
  | 'Кардио'
  | 'Растяжка'

export interface ExerciseParams {
  // Strength
  weight: boolean
  reps: boolean
  tut: boolean         // Time Under Tension
  rest: boolean
  restDuration: number // seconds
  rpe: boolean         // Rate of Perceived Exertion
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
  name: string
  muscleGroup: MuscleGroup
  muscles: string[]
  description: string
  type: ExerciseType
  params: ExerciseParams
}

export interface WorkoutSet {
  id: string
  exerciseId: string
  // Strength
  weight?: number
  reps?: number
  tut?: number
  rpe?: number
  // Cardio
  duration?: number
  pace?: number
  distance?: number
  incline?: number
  heartRate?: number
  // Stretch
  side?: 'left' | 'right' | 'both'
  breathingCycles?: number
  timestamp: number
}

export interface WorkoutSession {
  id: string
  date: string // ISO date
  sets: WorkoutSet[]
}
