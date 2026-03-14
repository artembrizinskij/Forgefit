<template>
  <div class="app-shell">
    <!-- Top bar (hidden on auth screen) -->
    <header v-if="!isAuthRoute" class="top-bar">
      <span class="logo">FORGEFIT</span>
      <div class="top-right">
        <span class="top-date">{{ todayFormatted }}</span>
        <button v-if="authStore.isAuthenticated" class="btn-logout" @click="logout" title="Выйти">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"/>
            <polyline points="16 17 21 12 16 7"/>
            <line x1="21" y1="12" x2="9" y2="12"/>
          </svg>
        </button>
      </div>
    </header>

    <!-- Main content -->
    <main class="main-content">
      <!-- Loading overlay while restoring session -->
      <div v-if="initializing" class="init-overlay">
        <span class="init-spinner" />
      </div>
      <RouterView v-else />
    </main>

    <!-- Bottom nav (hidden on auth/edit screens) -->
    <nav v-if="showNav" class="bottom-nav">
      <RouterLink to="/" class="nav-item" :class="{ active: route.name === 'workout' }">
        <span class="nav-icon">🏋️</span>
        <span class="nav-label">ТРЕНИРОВКА</span>
      </RouterLink>
      <RouterLink to="/exercises" class="nav-item" :class="{ active: route.name === 'exercises' }">
        <span class="nav-icon">📋</span>
        <span class="nav-label">УПРАЖНЕНИЯ</span>
      </RouterLink>
    </nav>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter, RouterView, RouterLink } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()

const initializing = ref(true)

const isAuthRoute = computed(() => route.name === 'auth')
const showNav = computed(() =>
  !isAuthRoute.value &&
  route.name !== 'exercise-new' &&
  route.name !== 'exercise-edit'
)

const todayFormatted = computed(() =>
  new Date().toLocaleDateString('ru-RU', {
    weekday: 'short', day: 'numeric', month: 'short',
  })
)

async function logout() {
  authStore.logout()
  await router.push('/auth')
}

// Restore session from saved token on startup
onMounted(async () => {
  await authStore.restoreSession()
  initializing.value = false
})
</script>

<style>
/* Global layout */
.app-shell {
  display: flex;
  flex-direction: column;
  height: 100vh;
  height: 100dvh;
  max-width: 480px;
  margin: 0 auto;
  position: relative;
  background: var(--bg-main);
}

/* Top bar */
.top-bar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 18px;
  height: 52px;
  background: var(--bg-card);
  border-bottom: 1px solid var(--border);
  flex-shrink: 0;
}

.logo {
  font-family: Georgia, 'Times New Roman', serif;
  font-size: 16px;
  font-weight: 700;
  letter-spacing: 3px;
  color: var(--accent);
}

.top-right {
  display: flex;
  align-items: center;
  gap: 12px;
}

.top-date {
  font-size: 11px;
  font-weight: 600;
  letter-spacing: 0.5px;
  color: var(--text-muted);
  text-transform: capitalize;
}

.btn-logout {
  background: none;
  border: 1px solid var(--border);
  border-radius: 7px;
  color: var(--text-muted);
  width: 30px;
  height: 30px;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-logout:hover {
  border-color: var(--error);
  color: var(--error);
}

/* Main content area */
.main-content {
  flex: 1;
  overflow-y: auto;
  overflow-x: hidden;
  min-height: 0;
  position: relative;
}

/* Initializing overlay */
.init-overlay {
  position: absolute;
  inset: 0;
  display: flex;
  align-items: center;
  justify-content: center;
}

.init-spinner {
  width: 32px;
  height: 32px;
  border: 3px solid var(--border);
  border-top-color: var(--accent);
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}

@keyframes spin { to { transform: rotate(360deg); } }

/* Bottom navigation */
.bottom-nav {
  display: flex;
  background: var(--bg-card);
  border-top: 1px solid var(--border);
  height: var(--nav-height);
  flex-shrink: 0;
}

.nav-item {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 4px;
  text-decoration: none;
  color: var(--text-muted);
  transition: color 0.2s;
  position: relative;
}

.nav-item.active { color: var(--accent); }

.nav-item.active::after {
  content: '';
  position: absolute;
  top: 0;
  left: 50%;
  transform: translateX(-50%);
  width: 40px;
  height: 2px;
  background: var(--accent);
  border-radius: 0 0 2px 2px;
}

.nav-icon { font-size: 20px; line-height: 1; }
.nav-label { font-size: 9px; font-weight: 700; letter-spacing: 1px; }

/* Desktop: center and add border */
@media (min-width: 481px) {
  .app-shell {
    border-left: 1px solid var(--border);
    border-right: 1px solid var(--border);
    box-shadow: 0 0 60px rgba(0, 0, 0, 0.5);
  }
}
</style>
