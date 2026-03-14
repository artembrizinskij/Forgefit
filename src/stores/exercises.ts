import { defineStore } from 'pinia'
import { ref } from 'vue'
import type { Exercise, ExerciseParams, MuscleGroup, ExerciseType } from '@/types'
import { exercisesApi } from '@/api/exercises'

// ─── Default params helpers (pure, type-only logic) ───────────────────────

const defaultParams = (): ExerciseParams => ({
  weight: true, reps: true, tut: false, rest: false, restDuration: 90, rpe: false,
  duration: false, pace: false, distance: false, incline: false, heartRate: false,
  units: 'kmh', side: false, breathing: false,
})

const defaultCardioParams = (): ExerciseParams => ({
  ...defaultParams(), weight: false, reps: false,
  duration: true, pace: true, distance: true,
})

const defaultStretchParams = (): ExerciseParams => ({
  ...defaultParams(), weight: false, reps: false,
  duration: true, side: true, breathing: true,
})

// ─── Store ────────────────────────────────────────────────────────────────

export const useExercisesStore = defineStore('exercises', () => {
  const exercises = ref<Exercise[]>([])
  const loading = ref(false)
  const error = ref<string | null>(null)

  /** Load all exercises from the server. Call on app init or on demand. */
  async function fetchAll(): Promise<void> {
    loading.value = true
    error.value = null
    try {
      exercises.value = await exercisesApi.getAll()
    } catch (e) {
      error.value = (e as Error).message
    } finally {
      loading.value = false
    }
  }

  /** Create a new exercise on the server and add it to the local list. */
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  async function add(data: Omit<Exercise, 'id'>): Promise<Exercise> {
    const created = await exercisesApi.create(data as any)
    exercises.value.push(created)
    return created
  }

  /** Update an exercise on the server and in the local list. */
  async function update(id: string, data: Partial<Omit<Exercise, 'id'>>): Promise<void> {
    const updated = await exercisesApi.update(id, data as any)
    const idx = exercises.value.findIndex(e => e.id === id)
    if (idx !== -1) exercises.value[idx] = updated
  }

  /** Delete an exercise on the server and from the local list. */
  async function remove(id: string): Promise<void> {
    await exercisesApi.remove(id)
    exercises.value = exercises.value.filter(e => e.id !== id)
  }

  function getById(id: string): Exercise | undefined {
    return exercises.value.find(e => e.id === id)
  }

  function makeDefaultParams(type: ExerciseType): ExerciseParams {
    if (type === 'cardio') return defaultCardioParams()
    if (type === 'stretch') return defaultStretchParams()
    return defaultParams()
  }

  return { exercises, loading, error, fetchAll, add, update, remove, getById, makeDefaultParams }
})

// ─── Constants ────────────────────────────────────────────────────────────

export const MUSCLE_GROUPS: MuscleGroup[] = [
  'Грудь', 'Спина', 'Плечи', 'Бицепс', 'Трицепс',
  'Ноги', 'Ягодицы', 'Пресс', 'Кардио', 'Растяжка',
]

export const MUSCLE_TAGS: Record<MuscleGroup, string[]> = {
  'Грудь': ['Большая грудная', 'Малая грудная', 'Передняя дельта', 'Трицепс'],
  'Спина': ['Широчайшая', 'Трапеция', 'Ромбовидная', 'Задняя дельта', 'Бицепс'],
  'Плечи': ['Передняя дельта', 'Средняя дельта', 'Задняя дельта', 'Трапеция'],
  'Бицепс': ['Бицепс', 'Плечевая', 'Брахиорадиальная'],
  'Трицепс': ['Трицепс', 'Локтевая'],
  'Ноги': ['Квадрицепс', 'Бицепс бедра', 'Икры', 'Камбаловидная'],
  'Ягодицы': ['Большая ягодичная', 'Средняя ягодичная', 'Малая ягодичная'],
  'Пресс': ['Прямая мышца живота', 'Косые мышцы', 'Поперечная мышца'],
  'Кардио': ['Квадрицепс', 'Икры', 'Ягодицы', 'Сердце'],
  'Растяжка': ['Сгибатели бедра', 'Квадрицепс', 'Широчайшая', 'Грушевидная'],
}
