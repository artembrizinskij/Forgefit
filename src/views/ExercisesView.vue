<template>
  <div class="exercises-view">
    <!-- Search -->
    <div class="search-wrapper">
      <span class="search-icon">⌕</span>
      <input
        v-model="searchQuery"
        class="search-input"
        placeholder="Поиск упражнения..."
      />
    </div>

    <!-- Filter by type -->
    <div class="filter-row">
      <button
        v-for="f in typeFilters"
        :key="f.value"
        class="filter-btn"
        :class="{ active: activeFilter === f.value }"
        @click="activeFilter = f.value"
      >
        {{ f.label }}
      </button>
    </div>

    <!-- Empty state -->
    <div v-if="filtered.length === 0" class="empty-state">
      <div class="empty-icon">🏋️</div>
      <p>Упражнений не найдено</p>
      <button class="btn-create-empty" @click="goCreate">Создать упражнение</button>
    </div>

    <!-- Exercises list -->
    <div class="exercises-list">
      <div
        v-for="ex in filtered"
        :key="ex.id"
        class="exercise-card"
      >
        <div class="ex-main">
          <div class="ex-info">
            <div class="ex-name">{{ ex.name }}</div>
            <div class="ex-meta-row">
              <span class="ex-type-badge" :class="ex.type">
                {{ typeLabel(ex.type) }}
              </span>
              <span class="ex-group-text">{{ ex.muscleGroup }}</span>
            </div>
            <div v-if="ex.muscles.length" class="ex-muscles">
              <span v-for="m in ex.muscles" :key="m" class="muscle-tag">{{ m }}</span>
            </div>
          </div>

          <div class="ex-actions">
            <button
              class="action-btn"
              title="Редактировать"
              @click="goEdit(ex.id)"
            >✎</button>
            <button
              class="action-btn danger"
              title="Удалить"
              @click="confirmDelete(ex)"
            >✕</button>
          </div>
        </div>

        <!-- Active params summary -->
        <div v-if="activeParamLabels(ex).length" class="ex-params">
          <span
            v-for="p in activeParamLabels(ex)"
            :key="p"
            class="param-chip"
          >{{ p }}</span>
        </div>

        <p v-if="ex.description" class="ex-description">{{ ex.description }}</p>
      </div>
    </div>

    <!-- FAB -->
    <button class="fab" title="Создать упражнение" @click="goCreate">+</button>

    <!-- Delete Confirm -->
    <div v-if="deletingExercise" class="modal-overlay" @click.self="deletingExercise = null">
      <div class="confirm-modal">
        <div class="confirm-title">Удалить упражнение?</div>
        <p class="confirm-text">
          «{{ deletingExercise.name }}» будет удалено безвозвратно.
        </p>
        <div class="confirm-actions">
          <button class="btn-secondary" @click="deletingExercise = null">Отмена</button>
          <button class="btn-danger" @click="doDelete">Удалить</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useExercisesStore } from '@/stores/exercises'
import type { Exercise } from '@/types'

const router = useRouter()
const store = useExercisesStore()
const exercises = computed(() => store.exercises)

// Load from server on mount
onMounted(() => {
  if (exercises.value.length === 0) store.fetchAll()
})

const searchQuery = ref('')
const activeFilter = ref<'' | 'strength' | 'cardio' | 'stretch'>('')

const typeFilters = [
  { value: '' as const,         label: 'Все' },
  { value: 'strength' as const, label: 'Силовые' },
  { value: 'cardio' as const,   label: 'Кардио' },
  { value: 'stretch' as const,  label: 'Растяжка' },
]

const filtered = computed(() => {
  let list = exercises.value
  if (activeFilter.value) list = list.filter(e => e.type === activeFilter.value)
  if (searchQuery.value.trim()) {
    const q = searchQuery.value.toLowerCase()
    list = list.filter(e =>
      e.name.toLowerCase().includes(q) ||
      e.muscleGroup.toLowerCase().includes(q) ||
      e.muscles.some(m => m.toLowerCase().includes(q))
    )
  }
  return list
})

// Navigation
function goCreate() {
  router.push('/exercises/new')
}

function goEdit(id: string) {
  router.push(`/exercises/${id}/edit`)
}

// Delete
const deletingExercise = ref<Exercise | null>(null)

function confirmDelete(ex: Exercise) {
  deletingExercise.value = ex
}

async function doDelete() {
  if (deletingExercise.value) {
    await store.remove(deletingExercise.value.id)
    deletingExercise.value = null
  }
}

// Helpers
function typeLabel(type: string) {
  const map: Record<string, string> = { strength: 'Силовое', cardio: 'Кардио', stretch: 'Растяжка' }
  return map[type] ?? type
}

function activeParamLabels(ex: Exercise): string[] {
  const p = ex.params
  const labels: string[] = []
  if (ex.type === 'strength') {
    if (p.weight)    labels.push('Вес')
    if (p.reps)      labels.push('Повторения')
    if (p.tut)       labels.push('TUT')
    if (p.rest)      labels.push(`Отдых ${p.restDuration}с`)
    if (p.rpe)       labels.push('РПЕ')
  } else if (ex.type === 'cardio') {
    if (p.duration)  labels.push('Длительность')
    if (p.pace)      labels.push(p.units === 'kmh' ? 'Темп км/ч' : 'Темп мин/км')
    if (p.distance)  labels.push('Расстояние')
    if (p.incline)   labels.push('Наклон')
    if (p.heartRate) labels.push('Пульс')
  } else {
    if (p.duration)  labels.push('Длительность')
    if (p.side)      labels.push('Сторона')
    if (p.breathing) labels.push('Дыхание')
  }
  return labels
}
</script>

<style scoped>
.exercises-view {
  padding: 16px;
  display: flex;
  flex-direction: column;
  gap: 12px;
  padding-bottom: 90px;
}

/* Search */
.search-wrapper { position: relative; }

.search-icon {
  position: absolute;
  left: 13px;
  top: 50%;
  transform: translateY(-50%);
  color: var(--text-muted);
  font-size: 16px;
  pointer-events: none;
}

.search-input {
  width: 100%;
  box-sizing: border-box;
  background: var(--bg-card);
  border: 1px solid var(--border);
  border-radius: 10px;
  color: var(--text-primary);
  padding: 11px 14px 11px 36px;
  font-size: 14px;
  font-family: inherit;
  transition: border-color 0.2s;
}

.search-input::placeholder { color: var(--text-muted); }
.search-input:focus { outline: none; border-color: var(--accent); }

/* Filters */
.filter-row { display: flex; gap: 8px; flex-wrap: wrap; }

.filter-btn {
  background: var(--bg-card);
  border: 1px solid var(--border);
  border-radius: 20px;
  color: var(--text-muted);
  padding: 6px 14px;
  font-size: 12px;
  font-weight: 600;
  font-family: inherit;
  cursor: pointer;
  transition: all 0.2s;
}

.filter-btn.active {
  background: var(--accent);
  border-color: var(--accent);
  color: #0d0d0f;
}

/* Empty state */
.empty-state {
  text-align: center;
  padding: 48px 16px;
  color: var(--text-muted);
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 10px;
}

.empty-icon { font-size: 40px; }

.btn-create-empty {
  background: var(--accent);
  border: none;
  border-radius: 10px;
  color: #0d0d0f;
  padding: 10px 20px;
  font-size: 13px;
  font-weight: 700;
  font-family: inherit;
  cursor: pointer;
  margin-top: 8px;
  transition: opacity 0.2s;
}

.btn-create-empty:hover { opacity: 0.88; }

/* Cards */
.exercises-list { display: flex; flex-direction: column; gap: 10px; }

.exercise-card {
  background: var(--bg-card);
  border: 1px solid var(--border);
  border-radius: 14px;
  padding: 14px;
  transition: border-color 0.2s;
}

.exercise-card:hover { border-color: rgba(232,255,71,0.25); }

.ex-main {
  display: flex;
  align-items: flex-start;
  gap: 10px;
}

.ex-info { flex: 1; min-width: 0; }

.ex-name {
  font-size: 15px;
  font-weight: 600;
  color: var(--text-primary);
  margin-bottom: 6px;
}

.ex-meta-row {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 8px;
}

.ex-type-badge {
  font-size: 9px;
  font-weight: 700;
  letter-spacing: 0.8px;
  padding: 2px 8px;
  border-radius: 20px;
  text-transform: uppercase;
}

.ex-type-badge.strength { background: rgba(232,255,71,0.12); color: var(--accent); }
.ex-type-badge.cardio   { background: rgba(255,120,71,0.15); color: #ff7847; }
.ex-type-badge.stretch  { background: rgba(71,200,255,0.12); color: #47c8ff; }

.ex-group-text { font-size: 12px; color: var(--text-muted); }

.ex-muscles { display: flex; flex-wrap: wrap; gap: 5px; }

.muscle-tag {
  font-size: 10px;
  font-weight: 500;
  color: var(--text-secondary);
  background: var(--bg-input);
  border: 1px solid var(--border);
  padding: 2px 7px;
  border-radius: 6px;
}

.ex-actions {
  display: flex;
  gap: 6px;
  flex-shrink: 0;
}

.action-btn {
  background: var(--bg-input);
  border: 1px solid var(--border);
  border-radius: 7px;
  color: var(--text-muted);
  width: 32px;
  height: 32px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 14px;
  cursor: pointer;
  transition: all 0.2s;
}

.action-btn:hover { color: var(--accent); border-color: var(--accent); background: rgba(232,255,71,0.07); }
.action-btn.danger:hover { color: var(--error); border-color: var(--error); background: rgba(255,82,82,0.1); }

/* Params chips */
.ex-params {
  display: flex;
  flex-wrap: wrap;
  gap: 5px;
  margin-top: 10px;
}

.param-chip {
  font-size: 10px;
  font-weight: 600;
  letter-spacing: 0.3px;
  color: var(--accent);
  background: rgba(232,255,71,0.08);
  border: 1px solid rgba(232,255,71,0.2);
  padding: 3px 8px;
  border-radius: 6px;
}

.ex-description {
  font-size: 12px;
  color: var(--text-muted);
  margin: 10px 0 0;
  line-height: 1.5;
  border-top: 1px solid var(--border);
  padding-top: 10px;
}

/* FAB */
.fab {
  position: fixed;
  bottom: 76px;
  right: 20px;
  width: 54px;
  height: 54px;
  border-radius: 50%;
  background: var(--accent);
  color: #0d0d0f;
  font-size: 28px;
  font-weight: 300;
  border: none;
  cursor: pointer;
  box-shadow: 0 4px 20px rgba(232,255,71,0.4);
  display: flex;
  align-items: center;
  justify-content: center;
  transition: transform 0.2s, box-shadow 0.2s;
  z-index: 10;
}

.fab:hover { transform: scale(1.08); box-shadow: 0 6px 28px rgba(232,255,71,0.55); }
.fab:active { transform: scale(0.95); }

@media (min-width: 481px) {
  .fab { right: calc(50% - 240px + 20px); }
}

/* Delete confirm */
.modal-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0,0,0,0.7);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 100;
  padding: 20px;
}

.confirm-modal {
  background: var(--bg-card);
  border: 1px solid var(--border);
  border-radius: 16px;
  padding: 24px;
  max-width: 320px;
  width: 100%;
}

.confirm-title {
  font-size: 17px;
  font-weight: 700;
  color: var(--text-primary);
  margin-bottom: 10px;
}

.confirm-text {
  font-size: 13px;
  color: var(--text-muted);
  margin: 0 0 20px;
  line-height: 1.5;
}

.confirm-actions { display: flex; gap: 10px; justify-content: flex-end; }

.btn-secondary {
  background: var(--bg-input);
  border: 1px solid var(--border);
  border-radius: 8px;
  color: var(--text-secondary);
  padding: 9px 16px;
  font-size: 13px;
  font-weight: 600;
  font-family: inherit;
  cursor: pointer;
  transition: border-color 0.2s;
}

.btn-secondary:hover { border-color: var(--text-muted); }

.btn-danger {
  background: var(--error);
  border: none;
  border-radius: 8px;
  color: #fff;
  padding: 9px 16px;
  font-size: 13px;
  font-weight: 700;
  font-family: inherit;
  cursor: pointer;
  transition: opacity 0.2s;
}

.btn-danger:hover { opacity: 0.85; }
</style>
