<template>
  <div class="timer-overlay">
    <div class="timer-card card">
      <div class="timer-label">ОТДЫХ</div>
      <div class="timer-ring">
        <svg class="ring-svg" viewBox="0 0 120 120">
          <circle class="ring-bg" cx="60" cy="60" r="52" />
          <circle
            class="ring-progress"
            cx="60"
            cy="60"
            r="52"
            :stroke-dashoffset="dashOffset"
          />
        </svg>
        <div class="timer-time">{{ displayTime }}</div>
      </div>
      <div class="timer-sub">Следующий подход через</div>
      <div class="timer-actions">
        <button class="btn-skip" @click="$emit('skip')">Пропустить</button>
        <button class="btn-done" @click="$emit('done')">Готов!</button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'

const props = defineProps<{ seconds: number }>()
const emit = defineEmits<{ done: []; skip: [] }>()

const remaining = ref(props.seconds)
const circumference = 2 * Math.PI * 52 // r=52

const dashOffset = computed(() => {
  const progress = remaining.value / props.seconds
  return circumference * (1 - progress)
})

const displayTime = computed(() => {
  const m = Math.floor(remaining.value / 60)
  const s = remaining.value % 60
  return `${m}:${String(s).padStart(2, '0')}`
})

let timer: ReturnType<typeof setInterval> | null = null

onMounted(() => {
  timer = setInterval(() => {
    remaining.value -= 1
    if (remaining.value <= 0) {
      clearInterval(timer!)
      emit('done')
    }
  }, 1000)
})

onUnmounted(() => {
  if (timer) clearInterval(timer)
})
</script>

<style scoped>
.timer-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.85);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 200;
  padding: 20px;
}

.card {
  background: var(--bg-card);
  border: 1px solid var(--border);
  border-radius: 20px;
}

.timer-card {
  width: 100%;
  max-width: 300px;
  padding: 32px 24px;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 16px;
}

.timer-label {
  font-size: 11px;
  font-weight: 700;
  letter-spacing: 2px;
  color: var(--text-muted);
}

.timer-ring {
  position: relative;
  width: 140px;
  height: 140px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.ring-svg {
  position: absolute;
  inset: 0;
  width: 100%;
  height: 100%;
  transform: rotate(-90deg);
}

.ring-bg {
  fill: none;
  stroke: var(--bg-input);
  stroke-width: 6;
}

.ring-progress {
  fill: none;
  stroke: var(--accent);
  stroke-width: 6;
  stroke-linecap: round;
  stroke-dasharray: v-bind('circumference + "px"');
  transition: stroke-dashoffset 1s linear;
}

.timer-time {
  font-size: 40px;
  font-weight: 700;
  font-family: Georgia, serif;
  color: var(--text-primary);
  position: relative;
  z-index: 1;
}

.timer-sub {
  font-size: 12px;
  color: var(--text-muted);
  text-align: center;
}

.timer-actions {
  display: flex;
  gap: 10px;
  width: 100%;
  margin-top: 8px;
}

.btn-skip {
  flex: 1;
  background: var(--bg-input);
  border: 1px solid var(--border);
  border-radius: 10px;
  color: var(--text-secondary);
  padding: 11px;
  font-size: 13px;
  font-weight: 600;
  font-family: inherit;
  cursor: pointer;
  transition: border-color 0.2s;
}

.btn-skip:hover { border-color: var(--text-muted); }

.btn-done {
  flex: 1;
  background: var(--accent);
  border: none;
  border-radius: 10px;
  color: #0d0d0f;
  padding: 11px;
  font-size: 13px;
  font-weight: 700;
  font-family: inherit;
  cursor: pointer;
  transition: opacity 0.2s;
}

.btn-done:hover { opacity: 0.88; }
</style>
