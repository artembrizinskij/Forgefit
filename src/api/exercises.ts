import { apiClient } from './client.js'
import type { Exercise, CreateExerciseDTO } from '../types/index.js'

export const exercisesApi = {
  getAll(): Promise<Exercise[]> {
    return apiClient.get<Exercise[]>('/exercises')
  },

  getById(id: string): Promise<Exercise> {
    return apiClient.get<Exercise>(`/exercises/${id}`)
  },

  create(data: CreateExerciseDTO): Promise<Exercise> {
    return apiClient.post<Exercise>('/exercises', data)
  },

  update(id: string, data: Partial<CreateExerciseDTO>): Promise<Exercise> {
    return apiClient.put<Exercise>(`/exercises/${id}`, data)
  },

  remove(id: string): Promise<void> {
    return apiClient.delete(`/exercises/${id}`)
  },
}
