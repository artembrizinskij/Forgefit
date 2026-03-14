import { apiClient } from './client.js'

export interface PublicUser {
  id: string
  email: string
  name: string
  createdAt: string
}

export interface AuthResponse {
  token: string
  user: PublicUser
}

export const authApi = {
  register(email: string, password: string, name: string): Promise<AuthResponse> {
    return apiClient.post<AuthResponse>('/auth/register', { email, password, name })
  },

  login(email: string, password: string): Promise<AuthResponse> {
    return apiClient.post<AuthResponse>('/auth/login', { email, password })
  },

  me(): Promise<{ user: PublicUser }> {
    return apiClient.get<{ user: PublicUser }>('/auth/me')
  },
}
