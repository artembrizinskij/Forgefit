import { createRouter, createWebHistory } from 'vue-router'
import { getToken } from '@/api/client'
import WorkoutView from '@/views/WorkoutView.vue'
import ExercisesView from '@/views/ExercisesView.vue'
import ExerciseEditView from '@/views/ExerciseEditView.vue'
import AuthView from '@/views/AuthView.vue'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    // ── Public routes ─────────────────────────────────────────────────────
    {
      path: '/auth',
      name: 'auth',
      component: AuthView,
      meta: { public: true },
    },

    // ── Protected routes ──────────────────────────────────────────────────
    {
      path: '/',
      name: 'workout',
      component: WorkoutView,
    },
    {
      path: '/exercises',
      name: 'exercises',
      component: ExercisesView,
    },
    {
      path: '/exercises/new',
      name: 'exercise-new',
      component: ExerciseEditView,
    },
    {
      path: '/exercises/:id/edit',
      name: 'exercise-edit',
      component: ExerciseEditView,
    },
  ],
})

// ── Auth guard ────────────────────────────────────────────────────────────

router.beforeEach((to) => {
  const isPublic = to.meta.public === true
  const hasToken = !!getToken()

  if (!isPublic && !hasToken) {
    return { name: 'auth' }
  }

  // If already logged in, redirect away from auth page
  if (isPublic && hasToken && to.name === 'auth') {
    return { name: 'workout' }
  }
})

export default router
