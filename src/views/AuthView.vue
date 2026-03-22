<template>
  <div class="auth-page">
    <!-- Logo -->
    <div class="auth-logo">
      <span class="logo-icon">⚡</span>
      <span class="logo-text">FORGEFIT</span>
    </div>

    <div class="auth-card">
      <!-- Tabs -->
      <div class="auth-tabs">
        <button
          class="auth-tab"
          :class="{ active: mode === 'login' }"
          @click="switchMode('login')"
        >
          Вход
        </button>
        <button
          class="auth-tab"
          :class="{ active: mode === 'register' }"
          @click="switchMode('register')"
        >
          Регистрация
        </button>
      </div>

      <!-- Form -->
      <form class="auth-form" @submit.prevent="submit">
        <!-- Name (register only) -->
        <div v-if="mode === 'register'" class="field">
          <label class="field-label">Имя</label>
          <input
            v-model="form.name"
            type="text"
            class="field-input"
            placeholder="Ваше имя"
            autocomplete="name"
            required
          />
        </div>

        <div class="field">
          <label class="field-label">Email</label>
          <input
            v-model="form.email"
            type="email"
            class="field-input"
            placeholder="you@example.com"
            autocomplete="email"
            required
          />
        </div>

        <div class="field">
          <label class="field-label">Пароль</label>
          <div class="password-wrap">
            <input
              v-model="form.password"
              :type="showPwd ? 'text' : 'password'"
              class="field-input"
              placeholder="Минимум 6 символов"
              autocomplete="current-password"
              required
              minlength="6"
            />
            <button type="button" class="pwd-eye" @click="showPwd = !showPwd">
              {{ showPwd ? '🙈' : '👁' }}
            </button>
          </div>
        </div>

        <!-- Error message -->
        <div v-if="authStore.error" class="auth-error">
          {{ authStore.error }}
        </div>

        <button type="submit" class="btn-submit" :disabled="authStore.loading">
          <span v-if="authStore.loading" class="spinner" />
          <span v-else>{{ mode === 'login' ? 'Войти' : 'Создать аккаунт' }}</span>
        </button>
      </form>
    </div>
  </div>
</template>

<script setup lang="ts">
import { reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth.js'

const authStore = useAuthStore()
const router = useRouter()

type Mode = 'login' | 'register'
const mode = ref<Mode>('login')
const showPwd = ref(false)

const form = reactive({ name: '', email: '', password: '' })

function switchMode(m: Mode) {
  mode.value = m
  authStore.clearError()
  form.name = ''
  form.email = ''
  form.password = ''
}

async function submit() {
  try {
    if (mode.value === 'login') {
      await authStore.login(form.email, form.password)
    } else {
      await authStore.register(form.email, form.password, form.name)
    }
    router.push('/')
  } catch {
    // error is shown via authStore.error
  }
}
</script>

<style scoped>
.auth-page {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 24px 16px;
  background: var(--bg-main);
}

/* ── Logo ── */
.auth-logo {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-bottom: 32px;
}

.logo-icon {
  font-size: 28px;
}

.logo-text {
  font-size: 22px;
  font-weight: 800;
  letter-spacing: 3px;
  color: var(--accent);
}

/* ── Card ── */
.auth-card {
  width: 100%;
  max-width: 400px;
  background: var(--bg-card);
  border: 1px solid var(--border);
  border-radius: 20px;
  overflow: hidden;
}

/* ── Tabs ── */
.auth-tabs {
  display: flex;
  border-bottom: 1px solid var(--border);
}

.auth-tab {
  flex: 1;
  padding: 14px;
  background: none;
  border: none;
  cursor: pointer;
  font-size: 14px;
  font-weight: 600;
  color: var(--text-muted);
  transition: all 0.2s;
}

.auth-tab.active {
  color: var(--accent);
  border-bottom: 2px solid var(--accent);
}

/* ── Form ── */
.auth-form {
  padding: 24px;
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.field {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.field-label {
  font-size: 11px;
  font-weight: 700;
  letter-spacing: 0.8px;
  text-transform: uppercase;
  color: var(--text-muted);
}

.field-input {
  background: var(--bg-input);
  border: 1px solid var(--border);
  border-radius: 10px;
  padding: 12px 14px;
  color: var(--text-primary);
  font-size: 15px;
  font-family: inherit;
  transition: border-color 0.2s;
  width: 100%;
  box-sizing: border-box;
}

.field-input:focus {
  outline: none;
  border-color: var(--accent);
}

.password-wrap {
  position: relative;
}

.password-wrap .field-input {
  padding-right: 44px;
}

.pwd-eye {
  position: absolute;
  right: 12px;
  top: 50%;
  transform: translateY(-50%);
  background: none;
  border: none;
  cursor: pointer;
  font-size: 16px;
  line-height: 1;
}

/* ── Error ── */
.auth-error {
  background: rgba(255, 82, 82, 0.12);
  border: 1px solid rgba(255, 82, 82, 0.3);
  border-radius: 8px;
  padding: 10px 12px;
  font-size: 13px;
  color: var(--error);
}

/* ── Submit button ── */
.btn-submit {
  margin-top: 4px;
  background: var(--accent);
  color: #000;
  border: none;
  border-radius: 12px;
  padding: 14px;
  font-size: 15px;
  font-weight: 700;
  cursor: pointer;
  transition: opacity 0.2s;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
}

.btn-submit:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.spinner {
  width: 18px;
  height: 18px;
  border: 2px solid rgba(0, 0, 0, 0.3);
  border-top-color: #000;
  border-radius: 50%;
  animation: spin 0.7s linear infinite;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}
</style>
