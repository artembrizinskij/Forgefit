<template>
  <div class="workout-view">
    <!-- Exercise Selector -->
    <div class="section">
      <label class="section-label">УПРАЖНЕНИЕ</label>
      <div class="select-wrapper">
        <select v-model="selectedExerciseId" class="exercise-select">
          <option value="">— Выберите упражнение —</option>
          <option v-for="ex in exercises" :key="ex.id" :value="ex.id">
            {{ ex.name }}
          </option>
        </select>
        <span class="select-arrow">▼</span>
      </div>
    </div>

    <!-- Exercise Details -->
    <template v-if="selectedExercise">
      <!-- Settings hint -->
      <div class="exercise-meta">
        <span class="ex-type-badge" :class="selectedExercise.type">
          {{ typeLabel(selectedExercise.type) }}
        </span>
        <span class="ex-group">{{ selectedExercise.muscleGroup }}</span>
        <button class="btn-icon" title="Настройки упражнения" @click="openSettings">
          ⚙
        </button>
      </div>

      <!-- Set input form -->
      <div class="set-form card">
        <div class="set-form-header">НОВЫЙ ПОДХОД</div>
        <div class="set-form-fields">
          <!-- Strength params -->
          <template v-if="selectedExercise.type === 'strength'">
            <div v-if="params.weight" class="field-group">
              <label class="field-label">ВЕС (кг)</label>
              <input
                v-model.number="form.weight"
                type="number"
                min="0"
                step="0.5"
                class="field-input"
                placeholder="0"
              />
            </div>
            <div v-if="params.reps" class="field-group">
              <label class="field-label">ПОВТОРЕНИЯ</label>
              <input
                v-model.number="form.reps"
                type="number"
                min="1"
                step="1"
                class="field-input"
                placeholder="0"
              />
            </div>
            <div v-if="params.tut" class="field-group">
              <label class="field-label">TUT (сек)</label>
              <input
                v-model.number="form.tut"
                type="number"
                min="0"
                step="1"
                class="field-input"
                placeholder="0"
              />
            </div>
            <div v-if="params.rpe" class="field-group">
              <label class="field-label">РПЕ (1–10)</label>
              <input
                v-model.number="form.rpe"
                type="number"
                min="1"
                max="10"
                step="0.5"
                class="field-input"
                placeholder="0"
              />
            </div>
          </template>

          <!-- Cardio params -->
          <template v-if="selectedExercise.type === 'cardio'">
            <div v-if="params.duration" class="field-group">
              <label class="field-label">ДЛИТ. (мин)</label>
              <input
                v-model.number="form.duration"
                type="number"
                min="0"
                step="1"
                class="field-input"
                placeholder="0"
              />
            </div>
            <div v-if="params.distance" class="field-group">
              <label class="field-label">ДИСТАНЦИЯ (км)</label>
              <input
                v-model.number="form.distance"
                type="number"
                min="0"
                step="0.1"
                class="field-input"
                placeholder="0"
              />
            </div>
            <div v-if="params.pace" class="field-group">
              <label class="field-label">
                {{ params.units === 'kmh' ? 'ТЕМП (км/ч)' : 'ТЕМП (мин/км)' }}
              </label>
              <input
                v-model.number="form.pace"
                type="number"
                min="0"
                step="0.1"
                class="field-input"
                placeholder="0"
              />
            </div>
            <div v-if="params.incline" class="field-group">
              <label class="field-label">НАКЛОН (%)</label>
              <input
                v-model.number="form.incline"
                type="number"
                min="0"
                max="30"
                step="0.5"
                class="field-input"
                placeholder="0"
              />
            </div>
            <div v-if="params.heartRate" class="field-group">
              <label class="field-label">ПУЛЬС (уд/мин)</label>
              <input
                v-model.number="form.heartRate"
                type="number"
                min="0"
                step="1"
                class="field-input"
                placeholder="0"
              />
            </div>
          </template>

          <!-- Stretch params -->
          <template v-if="selectedExercise.type === 'stretch'">
            <div v-if="params.duration" class="field-group">
              <label class="field-label">ДЛИТ. (сек)</label>
              <input
                v-model.number="form.duration"
                type="number"
                min="0"
                step="5"
                class="field-input"
                placeholder="0"
              />
            </div>
            <div v-if="params.side" class="field-group">
              <label class="field-label">СТОРОНА</label>
              <select v-model="form.side" class="field-input field-select">
                <option value="both">Обе</option>
                <option value="left">Левая</option>
                <option value="right">Правая</option>
              </select>
            </div>
            <div v-if="params.breathing" class="field-group">
              <label class="field-label">ДЫХАНИЙ</label>
              <input
                v-model.number="form.breathingCycles"
                type="number"
                min="0"
                step="1"
                class="field-input"
                placeholder="0"
              />
            </div>
          </template>
        </div>

        <button class="btn-primary btn-add-set" @click="addSet">
          + ДОБАВИТЬ ПОДХОД
        </button>
      </div>

      <!-- Current Sets -->
      <div v-if="currentSets.length > 0" class="section">
        <label class="section-label">ТЕКУЩИЕ ПОДХОДЫ</label>
        <div class="sets-list">
          <div
            v-for="(set, idx) in currentSets"
            :key="set.id"
            class="set-row"
          >
            <span class="set-number">{{ idx + 1 }}</span>
            <span class="set-summary">{{ formatSet(set) }}</span>
            <button class="btn-remove" @click="removeSet(set.id)" title="Удалить">✕</button>
          </div>
        </div>
      </div>

      <!-- History -->
      <div class="section">
        <label class="section-label">ИСТОРИЯ (последние 3 занятия)</label>
        <div v-if="history.length === 0" class="empty-hint">
          Нет записей по этому упражнению
        </div>
        <div v-for="session in history" :key="session.id" class="history-card card">
          <div class="history-date">{{ formatDate(session.date) }}</div>
          <div class="history-sets">
            <div
              v-for="(set, i) in session.sets.filter(s => s.exerciseId === selectedExerciseId)"
              :key="set.id"
              class="history-set-row"
            >
              <span class="history-num">{{ i + 1 }}</span>
              <span class="history-data">{{ formatSet(set) }}</span>
              <span class="history-vol">
                {{ setVolume(set) }}
              </span>
            </div>
          </div>
        </div>
      </div>
    </template>

    <!-- Rest Timer -->
    <RestTimerModal
      v-if="showTimer"
      :seconds="params.restDuration"
      @done="showTimer = false"
      @skip="showTimer = false"
    />

  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, reactive, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useExercisesStore } from '@/stores/exercises'
import { useWorkoutsStore } from '@/stores/workouts'
import type { WorkoutSet, Exercise } from '@/types'
import RestTimerModal from '@/components/RestTimerModal.vue'

const router = useRouter()
const exercisesStore = useExercisesStore()
const workoutsStore = useWorkoutsStore()

// Load exercises and today's session on mount
onMounted(async () => {
  await Promise.all([
    exercisesStore.exercises.length === 0 ? exercisesStore.fetchAll() : Promise.resolve(),
    workoutsStore.currentSession === null ? workoutsStore.fetchToday() : Promise.resolve(),
  ])
})

const exercises = computed(() => exercisesStore.exercises)
const selectedExerciseId = ref('')
const showTimer = ref(false)

const selectedExercise = computed<Exercise | undefined>(() =>
  exercises.value.find(e => e.id === selectedExerciseId.value)
)

const params = computed(() => selectedExercise.value?.params ?? {} as Exercise['params'])

interface FormData {
  weight: number | null
  reps: number | null
  tut: number | null
  rpe: number | null
  duration: number | null
  pace: number | null
  distance: number | null
  incline: number | null
  heartRate: number | null
  side: 'both' | 'left' | 'right'
  breathingCycles: number | null
}

const form = reactive<FormData>({
  weight: null,
  reps: null,
  tut: null,
  rpe: null,
  duration: null,
  pace: null,
  distance: null,
  incline: null,
  heartRate: null,
  side: 'both',
  breathingCycles: null,
})

// Reset form and fetch history when exercise changes
watch(selectedExerciseId, (newId) => {
  Object.assign(form, {
    weight: null, reps: null, tut: null, rpe: null,
    duration: null, pace: null, distance: null, incline: null,
    heartRate: null, side: 'both', breathingCycles: null,
  })
  workoutsStore.fetchHistory(newId, 3)
})

const currentSets = computed(() =>
  workoutsStore.getCurrentSets(selectedExerciseId.value)
)

// History is fetched async and stored in the store ref
const history = computed(() => workoutsStore.exerciseHistory)

async function addSet() {
  if (!selectedExerciseId.value) return
  const ex = selectedExercise.value!
  const p = ex.params

  const setData: Omit<WorkoutSet, 'id' | 'timestamp'> = {
    exerciseId: selectedExerciseId.value,
    ...(p.weight && form.weight != null ? { weight: form.weight } : {}),
    ...(p.reps && form.reps != null ? { reps: form.reps } : {}),
    ...(p.tut && form.tut != null ? { tut: form.tut } : {}),
    ...(p.rpe && form.rpe != null ? { rpe: form.rpe } : {}),
    ...(p.duration && form.duration != null ? { duration: form.duration } : {}),
    ...(p.pace && form.pace != null ? { pace: form.pace } : {}),
    ...(p.distance && form.distance != null ? { distance: form.distance } : {}),
    ...(p.incline && form.incline != null ? { incline: form.incline } : {}),
    ...(p.heartRate && form.heartRate != null ? { heartRate: form.heartRate } : {}),
    ...(p.side ? { side: form.side } : {}),
    ...(p.breathing && form.breathingCycles != null ? { breathingCycles: form.breathingCycles } : {}),
  }

  await workoutsStore.addSet(setData)

  if (p.rest && p.restDuration > 0) {
    showTimer.value = true
  }
}

async function removeSet(id: string) {
  await workoutsStore.removeSet(id)
}

function openSettings() {
  if (selectedExerciseId.value) {
    router.push(`/exercises/${selectedExerciseId.value}/edit`)
  }
}

function typeLabel(type: string) {
  const map: Record<string, string> = { strength: 'Силовое', cardio: 'Кардио', stretch: 'Растяжка' }
  return map[type] ?? type
}

function formatSet(set: WorkoutSet): string {
  const parts: string[] = []
  if (set.weight != null) parts.push(`${set.weight} кг`)
  if (set.reps != null) parts.push(`${set.reps} повт.`)
  if (set.tut != null) parts.push(`TUT ${set.tut}с`)
  if (set.rpe != null) parts.push(`РПЕ ${set.rpe}`)
  if (set.duration != null) parts.push(`${set.duration} мин`)
  if (set.distance != null) parts.push(`${set.distance} км`)
  if (set.pace != null) parts.push(`${set.pace} км/ч`)
  if (set.incline != null) parts.push(`наклон ${set.incline}%`)
  if (set.heartRate != null) parts.push(`${set.heartRate} уд/мин`)
  if (set.side) {
    const sideMap = { both: 'обе', left: 'лев.', right: 'прав.' }
    parts.push(sideMap[set.side])
  }
  if (set.breathingCycles != null) parts.push(`${set.breathingCycles} дых.`)
  return parts.join(' · ') || '—'
}

function setVolume(set: WorkoutSet): string {
  if (set.weight != null && set.reps != null) {
    return `${(set.weight * set.reps).toFixed(0)} кг`
  }
  if (set.distance != null) return `${set.distance} км`
  if (set.duration != null) return `${set.duration} мин`
  return ''
}

function formatDate(iso: string): string {
  const d = new Date(iso)
  return d.toLocaleDateString('ru-RU', { day: 'numeric', month: 'long', year: 'numeric' })
}
</script>

<style scoped>
.workout-view {
  padding: 16px;
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.section {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.section-label {
  font-size: 10px;
  font-weight: 600;
  letter-spacing: 1.5px;
  color: var(--text-muted);
}

.select-wrapper {
  position: relative;
}

.exercise-select {
  width: 100%;
  background: var(--bg-card);
  border: 1px solid var(--border);
  border-radius: 10px;
  color: var(--text-primary);
  padding: 12px 40px 12px 14px;
  font-size: 14px;
  font-family: inherit;
  appearance: none;
  cursor: pointer;
  transition: border-color 0.2s;
}

.exercise-select:focus {
  outline: none;
  border-color: var(--accent);
}

.select-arrow {
  position: absolute;
  right: 14px;
  top: 50%;
  transform: translateY(-50%);
  color: var(--text-muted);
  pointer-events: none;
  font-size: 10px;
}

.exercise-meta {
  display: flex;
  align-items: center;
  gap: 8px;
}

.ex-type-badge {
  font-size: 10px;
  font-weight: 600;
  letter-spacing: 0.8px;
  padding: 3px 8px;
  border-radius: 20px;
  text-transform: uppercase;
}

.ex-type-badge.strength { background: rgba(232, 255, 71, 0.12); color: var(--accent); }
.ex-type-badge.cardio   { background: rgba(255, 120, 71, 0.15); color: #ff7847; }
.ex-type-badge.stretch  { background: rgba(71, 200, 255, 0.12); color: #47c8ff; }

.ex-group {
  font-size: 12px;
  color: var(--text-muted);
}

.btn-icon {
  margin-left: auto;
  background: none;
  border: none;
  color: var(--text-muted);
  font-size: 16px;
  cursor: pointer;
  padding: 4px;
  border-radius: 6px;
  transition: color 0.2s, background 0.2s;
}

.btn-icon:hover { color: var(--accent); background: rgba(232, 255, 71, 0.08); }

.card {
  background: var(--bg-card);
  border: 1px solid var(--border);
  border-radius: 14px;
  overflow: hidden;
}

.set-form-header {
  background: var(--bg-input);
  padding: 8px 14px;
  font-size: 10px;
  font-weight: 600;
  letter-spacing: 1.5px;
  color: var(--text-muted);
}

.set-form-fields {
  padding: 14px;
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(130px, 1fr));
  gap: 12px;
}

.field-group {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.field-label {
  font-size: 9px;
  font-weight: 600;
  letter-spacing: 1.2px;
  color: var(--text-muted);
}

.field-input {
  background: var(--bg-input);
  border: 1px solid var(--border);
  border-radius: 8px;
  color: var(--text-primary);
  padding: 10px 12px;
  font-size: 15px;
  font-weight: 600;
  font-family: Georgia, serif;
  text-align: center;
  width: 100%;
  box-sizing: border-box;
  transition: border-color 0.2s;
}

.field-input:focus {
  outline: none;
  border-color: var(--accent);
}

.field-select {
  appearance: none;
  cursor: pointer;
  font-family: inherit;
  font-size: 13px;
  font-weight: 600;
}

.btn-primary {
  background: var(--accent);
  color: #0d0d0f;
  border: none;
  font-size: 12px;
  font-weight: 700;
  letter-spacing: 1px;
  cursor: pointer;
  transition: opacity 0.2s;
  font-family: inherit;
}

.btn-primary:hover { opacity: 0.88; }
.btn-primary:active { opacity: 0.75; }

.btn-add-set {
  display: block;
  width: 100%;
  padding: 13px;
  border-radius: 0 0 14px 14px;
}

.sets-list {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.set-row {
  display: flex;
  align-items: center;
  gap: 10px;
  background: var(--bg-card);
  border: 1px solid var(--border);
  border-radius: 10px;
  padding: 10px 14px;
}

.set-number {
  font-size: 15px;
  font-weight: 700;
  font-family: Georgia, serif;
  color: var(--accent);
  min-width: 20px;
}

.set-summary {
  flex: 1;
  font-size: 13px;
  color: var(--text-primary);
}

.btn-remove {
  background: none;
  border: none;
  color: var(--text-muted);
  cursor: pointer;
  font-size: 12px;
  padding: 4px 6px;
  border-radius: 6px;
  transition: color 0.2s, background 0.2s;
}

.btn-remove:hover { color: var(--error); background: rgba(255, 82, 82, 0.1); }

.empty-hint {
  font-size: 13px;
  color: var(--text-muted);
  padding: 16px;
  text-align: center;
  background: var(--bg-card);
  border: 1px solid var(--border);
  border-radius: 10px;
}

.history-card {
  padding: 0;
}

.history-date {
  background: var(--bg-input);
  padding: 7px 14px;
  font-size: 10px;
  font-weight: 600;
  letter-spacing: 1.2px;
  color: var(--text-muted);
}

.history-sets {
  padding: 4px 0;
}

.history-set-row {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 8px 14px;
  border-bottom: 1px solid var(--border);
}

.history-set-row:last-child { border-bottom: none; }

.history-num {
  font-size: 13px;
  font-weight: 700;
  font-family: Georgia, serif;
  color: var(--accent);
  min-width: 18px;
}

.history-data {
  flex: 1;
  font-size: 12px;
  color: var(--text-secondary);
}

.history-vol {
  font-size: 11px;
  font-weight: 600;
  color: var(--accent);
  background: rgba(232, 255, 71, 0.1);
  padding: 2px 8px;
  border-radius: 20px;
}
</style>
