import { defineStore } from 'pinia'
import { ref } from 'vue'
import type { WorkoutSet, WorkoutSession } from '@/types'
import { workoutsApi } from '@/api/workouts'

export const useWorkoutsStore = defineStore('workouts', () => {
  /** Today's workout session (null until fetched). */
  const currentSession = ref<WorkoutSession | null>(null)

  /** History for the currently selected exercise. Populated by fetchHistory(). */
  const exerciseHistory = ref<WorkoutSession[]>([])

  const loading = ref(false)
  const error = ref<string | null>(null)

  // ── Data fetching ─────────────────────────────────────────────────────────

  /** Load today's session from the server. Creates it on the server if it doesn't exist. */
  async function fetchToday(): Promise<void> {
    loading.value = true
    error.value = null
    try {
      currentSession.value = await workoutsApi.getToday()
    } catch (e) {
      error.value = (e as Error).message
    } finally {
      loading.value = false
    }
  }

  /** Load exercise history for the given exercise into exerciseHistory. */
  async function fetchHistory(exerciseId: string, limit = 3): Promise<void> {
    if (!exerciseId) {
      exerciseHistory.value = []
      return
    }
    try {
      exerciseHistory.value = await workoutsApi.getHistory(exerciseId, limit)
    } catch {
      exerciseHistory.value = []
    }
  }

  // ── Mutations ─────────────────────────────────────────────────────────────

  /** Add a set to today's session (sends to server, updates local state). */
  async function addSet(set: Omit<WorkoutSet, 'id' | 'timestamp'>): Promise<void> {
    const payload = { ...set, timestamp: Date.now() }
    const created = await workoutsApi.addSet(payload)

    // Optimistically push into local session
    if (currentSession.value) {
      currentSession.value.sets.push(created)
    }
  }

  /** Remove a set from today's session. */
  async function removeSet(setId: string): Promise<void> {
    await workoutsApi.removeSet(setId)

    if (currentSession.value) {
      currentSession.value.sets = currentSession.value.sets.filter(s => s.id !== setId)
    }
  }

  // ── Helpers ───────────────────────────────────────────────────────────────

  /** Returns sets for the given exercise in today's session. Synchronous read from local state. */
  function getCurrentSets(exerciseId: string): WorkoutSet[] {
    return currentSession.value?.sets.filter(s => s.exerciseId === exerciseId) ?? []
  }

  /**
   * @deprecated Use exerciseHistory ref + fetchHistory() instead.
   * Kept for backward compatibility — returns already-fetched history synchronously.
   */
  function getHistoryForExercise(_exerciseId: string, _limit = 3): WorkoutSession[] {
    return exerciseHistory.value
  }

  return {
    currentSession,
    exerciseHistory,
    loading,
    error,
    fetchToday,
    fetchHistory,
    addSet,
    removeSet,
    getCurrentSets,
    getHistoryForExercise,
  }
})
