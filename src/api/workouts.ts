import { apiClient } from './client.js'
import type { WorkoutSession, WorkoutSet } from '../types/index.js'

export type CreateSetPayload = Omit<WorkoutSet, 'id'>

export const workoutsApi = {
  getAll(): Promise<WorkoutSession[]> {
    return apiClient.get<WorkoutSession[]>('/workouts')
  },

  getToday(): Promise<WorkoutSession> {
    return apiClient.get<WorkoutSession>('/workouts/today')
  },

  getHistory(exerciseId: string, limit = 3): Promise<WorkoutSession[]> {
    return apiClient.get<WorkoutSession[]>(
      `/workouts/history/${exerciseId}?limit=${limit}`,
    )
  },

  addSet(payload: CreateSetPayload): Promise<WorkoutSet> {
    return apiClient.post<WorkoutSet>('/workouts/sets', payload)
  },

  removeSet(id: string): Promise<void> {
    return apiClient.delete(`/workouts/sets/${id}`)
  },
}
