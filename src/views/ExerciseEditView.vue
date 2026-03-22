<template>
  <div class="edit-view">
    <!-- Header -->
    <div class="edit-header">
      <button class="btn-back" @click="goBack">←</button>
      <span class="edit-title">{{ isEdit ? 'РЕДАКТИРОВАТЬ' : 'НОВОЕ УПРАЖНЕНИЕ' }}</span>
      <div style="width:34px" />
    </div>

    <div class="edit-body">
      <!-- ── Section: Основное ── -->
      <section class="card-section">
        <div class="section-heading">ОСНОВНОЕ</div>

        <div class="field-group">
          <label class="field-label">НАЗВАНИЕ</label>
          <input
            v-model="form.name"
            class="field-input"
            placeholder="Жим штанги лёжа"
            maxlength="80"
          />
        </div>

        <div class="field-group">
          <label class="field-label">ТИП УПРАЖНЕНИЯ</label>
          <div class="type-tabs">
            <button
              v-for="t in types"
              :key="t.value"
              class="type-tab"
              :class="{ active: form.type === t.value, [t.value]: true }"
              @click="setType(t.value)"
            >
              <span class="type-tab-icon">{{ t.icon }}</span>
              {{ t.label }}
            </button>
          </div>
        </div>

        <div class="field-group">
          <label class="field-label">ГРУППА МЫШЦ</label>
          <div class="select-wrapper">
            <select v-model="form.muscleGroup" class="field-input field-select">
              <option v-for="g in muscleGroups" :key="g" :value="g">{{ g }}</option>
            </select>
            <span class="select-arrow">▼</span>
          </div>
        </div>

        <div class="field-group">
          <label class="field-label">ЗАДЕЙСТВОВАННЫЕ МЫШЦЫ</label>
          <div class="muscle-tags-grid">
            <button
              v-for="tag in availableTags"
              :key="tag"
              class="muscle-tag-btn"
              :class="{ active: form.muscles.includes(tag) }"
              @click="toggleMuscle(tag)"
            >
              {{ tag }}
            </button>
          </div>
        </div>

        <div class="field-group">
          <label class="field-label">ОПИСАНИЕ (необязательно)</label>
          <textarea
            v-model="form.description"
            class="field-input field-textarea"
            placeholder="Базовое упражнение для развития грудных мышц..."
            rows="3"
          />
        </div>
      </section>

      <!-- ── Section: Параметры подхода ── -->
      <section class="card-section">
        <div class="section-heading">
          ПАРАМЕТРЫ ПОДХОДА
          <span class="section-hint">включите нужные поля</span>
        </div>
        <p class="section-desc">
          Только включённые параметры будут отображаться при добавлении подхода во время тренировки.
        </p>

        <!-- СИЛОВОЕ -->
        <template v-if="form.type === 'strength'">
          <div class="param-row">
            <div class="param-left">
              <span class="param-icon strength">⚖</span>
              <div>
                <div class="param-name">Вес</div>
                <div class="param-desc">Рабочий вес (кг)</div>
              </div>
            </div>
            <label class="toggle" :class="{ on: params.weight }">
              <input type="checkbox" v-model="params.weight" />
              <span class="toggle-track"><span class="toggle-thumb" /></span>
            </label>
          </div>

          <div v-if="params.weight" class="param-sub">
            <div class="unit-tabs">
              <button class="unit-tab" :class="{ active: weightUnit === 'kg' }" @click="weightUnit = 'kg'">кг</button>
              <button class="unit-tab" :class="{ active: weightUnit === 'lb' }" @click="weightUnit = 'lb'">фунты</button>
            </div>
          </div>

          <div class="param-row">
            <div class="param-left">
              <span class="param-icon strength">#</span>
              <div>
                <div class="param-name">Повторения</div>
                <div class="param-desc">Количество повторений в подходе</div>
              </div>
            </div>
            <label class="toggle" :class="{ on: params.reps }">
              <input type="checkbox" v-model="params.reps" />
              <span class="toggle-track"><span class="toggle-thumb" /></span>
            </label>
          </div>

          <div class="param-row">
            <div class="param-left">
              <span class="param-icon strength">⏱</span>
              <div>
                <div class="param-name">Время под нагрузкой (TUT)</div>
                <div class="param-desc">Темп движения — опускание / пауза / подъём</div>
              </div>
            </div>
            <label class="toggle" :class="{ on: params.tut }">
              <input type="checkbox" v-model="params.tut" />
              <span class="toggle-track"><span class="toggle-thumb" /></span>
            </label>
          </div>

          <div class="param-row">
            <div class="param-left">
              <span class="param-icon strength">☕</span>
              <div>
                <div class="param-name">Отдых между подходами</div>
                <div class="param-desc">Автоматический таймер после каждого подхода</div>
              </div>
            </div>
            <label class="toggle" :class="{ on: params.rest }">
              <input type="checkbox" v-model="params.rest" />
              <span class="toggle-track"><span class="toggle-thumb" /></span>
            </label>
          </div>

          <div v-if="params.rest" class="param-sub">
            <div class="field-label" style="margin-bottom:8px">ДЛИТЕЛЬНОСТЬ ОТДЫХА</div>
            <div class="rest-presets">
              <button
                v-for="s in restPresets"
                :key="s"
                class="preset-btn"
                :class="{ active: params.restDuration === s }"
                @click="params.restDuration = s"
              >
                {{ formatRest(s) }}
              </button>
            </div>
            <div class="rest-custom">
              <input
                v-model.number="params.restDuration"
                type="number"
                min="10"
                max="600"
                step="5"
                class="rest-input"
              />
              <span class="rest-unit">сек</span>
            </div>
          </div>

          <div class="param-row">
            <div class="param-left">
              <span class="param-icon strength" style="font-size:10px;font-weight:800">RPE</span>
              <div>
                <div class="param-name">Субъективная нагрузка (РПЕ)</div>
                <div class="param-desc">Шкала 1–10 — насколько тяжело был подход</div>
              </div>
            </div>
            <label class="toggle" :class="{ on: params.rpe }">
              <input type="checkbox" v-model="params.rpe" />
              <span class="toggle-track"><span class="toggle-thumb" /></span>
            </label>
          </div>
        </template>

        <!-- КАРДИО -->
        <template v-if="form.type === 'cardio'">
          <div class="param-row">
            <div class="param-left">
              <span class="param-icon cardio">⏱</span>
              <div>
                <div class="param-name">Длительность</div>
                <div class="param-desc">Общее время сессии (мин)</div>
              </div>
            </div>
            <label class="toggle" :class="{ on: params.duration }">
              <input type="checkbox" v-model="params.duration" />
              <span class="toggle-track"><span class="toggle-thumb" /></span>
            </label>
          </div>

          <div class="param-row">
            <div class="param-left">
              <span class="param-icon cardio">⚡</span>
              <div>
                <div class="param-name">Темп</div>
                <div class="param-desc">Скорость движения</div>
              </div>
            </div>
            <label class="toggle" :class="{ on: params.pace }">
              <input type="checkbox" v-model="params.pace" />
              <span class="toggle-track"><span class="toggle-thumb" /></span>
            </label>
          </div>

          <div v-if="params.pace" class="param-sub">
            <div class="unit-tabs">
              <button
                class="unit-tab"
                :class="{ active: params.units === 'kmh' }"
                @click="params.units = 'kmh'"
              >км/ч</button>
              <button
                class="unit-tab"
                :class="{ active: params.units === 'minpkm' }"
                @click="params.units = 'minpkm'"
              >мин/км</button>
            </div>
          </div>

          <div class="param-row">
            <div class="param-left">
              <span class="param-icon cardio">📍</span>
              <div>
                <div class="param-name">Расстояние</div>
                <div class="param-desc">Пройденная / пробежанная дистанция (км)</div>
              </div>
            </div>
            <label class="toggle" :class="{ on: params.distance }">
              <input type="checkbox" v-model="params.distance" />
              <span class="toggle-track"><span class="toggle-thumb" /></span>
            </label>
          </div>

          <div class="param-row">
            <div class="param-left">
              <span class="param-icon cardio">↗</span>
              <div>
                <div class="param-name">Наклон дорожки</div>
                <div class="param-desc">Угол подъёма беговой дорожки (%)</div>
              </div>
            </div>
            <label class="toggle" :class="{ on: params.incline }">
              <input type="checkbox" v-model="params.incline" />
              <span class="toggle-track"><span class="toggle-thumb" /></span>
            </label>
          </div>

          <div class="param-row">
            <div class="param-left">
              <span class="param-icon cardio">♥</span>
              <div>
                <div class="param-name">Пульс</div>
                <div class="param-desc">Частота сердечных сокращений (уд/мин)</div>
              </div>
            </div>
            <label class="toggle" :class="{ on: params.heartRate }">
              <input type="checkbox" v-model="params.heartRate" />
              <span class="toggle-track"><span class="toggle-thumb" /></span>
            </label>
          </div>
        </template>

        <!-- РАСТЯЖКА -->
        <template v-if="form.type === 'stretch'">
          <div class="param-row">
            <div class="param-left">
              <span class="param-icon stretch">⏱</span>
              <div>
                <div class="param-name">Длительность</div>
                <div class="param-desc">Время удержания позиции (сек)</div>
              </div>
            </div>
            <label class="toggle" :class="{ on: params.duration }">
              <input type="checkbox" v-model="params.duration" />
              <span class="toggle-track"><span class="toggle-thumb" /></span>
            </label>
          </div>

          <div class="param-row">
            <div class="param-left">
              <span class="param-icon stretch">⇄</span>
              <div>
                <div class="param-name">Сторона</div>
                <div class="param-desc">Левая / правая / обе стороны</div>
              </div>
            </div>
            <label class="toggle" :class="{ on: params.side }">
              <input type="checkbox" v-model="params.side" />
              <span class="toggle-track"><span class="toggle-thumb" /></span>
            </label>
          </div>

          <div class="param-row">
            <div class="param-left">
              <span class="param-icon stretch">〰</span>
              <div>
                <div class="param-name">Дыхательные циклы</div>
                <div class="param-desc">Количество вдохов-выдохов в позиции</div>
              </div>
            </div>
            <label class="toggle" :class="{ on: params.breathing }">
              <input type="checkbox" v-model="params.breathing" />
              <span class="toggle-track"><span class="toggle-thumb" /></span>
            </label>
          </div>
        </template>
      </section>
    </div>

    <!-- Save bar -->
    <div class="save-bar">
      <div v-if="!form.name.trim()" class="save-hint">Введите название упражнения</div>
      <button
        class="btn-save"
        :disabled="!form.name.trim()"
        @click="save"
      >
        {{ isEdit ? 'СОХРАНИТЬ ИЗМЕНЕНИЯ' : 'СОЗДАТЬ УПРАЖНЕНИЕ' }}
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { reactive, ref, computed, watch } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useExercisesStore, MUSCLE_GROUPS, MUSCLE_TAGS } from '@/stores/exercises'
import type { ExerciseType, MuscleGroup, ExerciseParams } from '@/types'

const router = useRouter()
const route = useRoute()
const store = useExercisesStore()

// ── Mode detection ─────────────────────────────────────────────────────────
const exerciseId = route.params.id as string | undefined
const isEdit = Boolean(exerciseId)
const existing = exerciseId ? store.getById(exerciseId) : undefined

// ── Form state ─────────────────────────────────────────────────────────────
interface FormData {
  name: string
  type: ExerciseType
  muscleGroup: MuscleGroup
  muscles: string[]
  description: string
}

const form = reactive<FormData>({
  name: existing?.name ?? '',
  type: existing?.type ?? 'strength',
  muscleGroup: existing?.muscleGroup ?? 'Грудь',
  muscles: [...(existing?.muscles ?? [])],
  description: existing?.description ?? '',
})

const params = reactive<ExerciseParams>(
  existing?.params ? { ...existing.params } : store.makeDefaultParams('strength')
)

const weightUnit = ref<'kg' | 'lb'>('kg')

// ── Consts ─────────────────────────────────────────────────────────────────
const types: { value: ExerciseType; label: string; icon: string }[] = [
  { value: 'strength', label: 'Силовое', icon: '🏋️' },
  { value: 'cardio',   label: 'Кардио',  icon: '🏃' },
  { value: 'stretch',  label: 'Растяжка', icon: '🧘' },
]

const muscleGroups = MUSCLE_GROUPS
const restPresets = [60, 90, 120, 180]

const availableTags = computed(() => MUSCLE_TAGS[form.muscleGroup] ?? [])

// ── Watchers ───────────────────────────────────────────────────────────────
watch(() => form.muscleGroup, () => {
  form.muscles = form.muscles.filter(m => availableTags.value.includes(m))
})

// ── Methods ────────────────────────────────────────────────────────────────
function setType(type: ExerciseType) {
  if (form.type === type) return
  form.type = type
  const defaults = store.makeDefaultParams(type)
  Object.assign(params, defaults)
}

function toggleMuscle(tag: string) {
  const idx = form.muscles.indexOf(tag)
  if (idx === -1) form.muscles.push(tag)
  else form.muscles.splice(idx, 1)
}

function formatRest(s: number) {
  if (s < 60) return `${s}с`
  const m = Math.floor(s / 60)
  const rem = s % 60
  return rem ? `${m}м ${rem}с` : `${m} мин`
}

function goBack() {
  router.push('/exercises')
}

async function save() {
  if (!form.name.trim()) return

  const data = {
    name: form.name.trim(),
    type: form.type,
    muscleGroup: form.muscleGroup,
    muscles: [...form.muscles],
    description: form.description.trim(),
    params: { ...params },
  }

  if (isEdit && exerciseId) {
    await store.update(exerciseId, data)
  } else {
    await store.add(data)
  }

  router.push('/exercises')
}
</script>

<style scoped>
.edit-view {
  display: flex;
  flex-direction: column;
  background: var(--bg-main);
}

/* ── Header ── */
.edit-header {
  position: sticky;
  top: 0;
  z-index: 5;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 16px;
  height: 52px;
  background: var(--bg-card);
  border-bottom: 1px solid var(--border);
}

.edit-title {
  font-size: 11px;
  font-weight: 700;
  letter-spacing: 1.5px;
  color: var(--text-primary);
}

.btn-back {
  background: var(--bg-input);
  border: 1px solid var(--border);
  border-radius: 8px;
  color: var(--text-muted);
  width: 34px;
  height: 34px;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  font-size: 16px;
  transition: all 0.2s;
}

.btn-back:hover { color: var(--accent); border-color: var(--accent); }

/* ── Body ── */
.edit-body {
  padding: 16px;
  display: flex;
  flex-direction: column;
  gap: 12px;
  padding-bottom: 120px; /* leave room for fixed save bar */
}

/* ── Card Sections ── */
.card-section {
  background: var(--bg-card);
  border: 1px solid var(--border);
  border-radius: 16px;
  overflow: hidden;
}

.section-heading {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 12px 16px;
  font-size: 10px;
  font-weight: 700;
  letter-spacing: 1.5px;
  color: var(--text-muted);
  background: var(--bg-input);
  border-bottom: 1px solid var(--border);
}

.section-hint {
  font-size: 9px;
  font-weight: 500;
  letter-spacing: 0.5px;
  color: var(--text-muted);
  opacity: 0.6;
  text-transform: none;
}

.section-desc {
  font-size: 12px;
  color: var(--text-muted);
  padding: 12px 16px 4px;
  line-height: 1.5;
  border-bottom: 1px solid var(--border);
}

/* Fields inside sections */
.field-group {
  padding: 12px 16px;
  border-bottom: 1px solid var(--border);
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.field-group:last-child { border-bottom: none; }

.field-label {
  font-size: 9px;
  font-weight: 700;
  letter-spacing: 1.3px;
  color: var(--text-muted);
}

.field-input {
  background: var(--bg-input);
  border: 1px solid var(--border);
  border-radius: 10px;
  color: var(--text-primary);
  padding: 11px 14px;
  font-size: 14px;
  font-family: inherit;
  transition: border-color 0.2s;
  width: 100%;
  box-sizing: border-box;
}

.field-input:focus { outline: none; border-color: var(--accent); }
.field-input::placeholder { color: var(--text-muted); }

.field-textarea {
  resize: vertical;
  min-height: 72px;
  line-height: 1.5;
}

.field-select { appearance: none; cursor: pointer; }

.select-wrapper { position: relative; }

.select-arrow {
  position: absolute;
  right: 14px;
  top: 50%;
  transform: translateY(-50%);
  color: var(--text-muted);
  pointer-events: none;
  font-size: 10px;
}

/* ── Type tabs ── */
.type-tabs {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 8px;
}

.type-tab {
  background: var(--bg-input);
  border: 1px solid var(--border);
  border-radius: 10px;
  color: var(--text-muted);
  padding: 11px 6px;
  font-size: 12px;
  font-weight: 600;
  font-family: inherit;
  cursor: pointer;
  text-align: center;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 4px;
  transition: all 0.2s;
}

.type-tab-icon { font-size: 18px; }

.type-tab.active.strength { background: rgba(232,255,71,0.12); border-color: var(--accent); color: var(--accent); }
.type-tab.active.cardio   { background: rgba(255,120,71,0.12); border-color: #ff7847; color: #ff7847; }
.type-tab.active.stretch  { background: rgba(71,200,255,0.12); border-color: #47c8ff; color: #47c8ff; }

/* ── Muscle tags ── */
.muscle-tags-grid {
  display: flex;
  flex-wrap: wrap;
  gap: 7px;
}

.muscle-tag-btn {
  background: var(--bg-input);
  border: 1px solid var(--border);
  border-radius: 20px;
  color: var(--text-muted);
  padding: 6px 13px;
  font-size: 11px;
  font-weight: 500;
  font-family: inherit;
  cursor: pointer;
  transition: all 0.2s;
}

.muscle-tag-btn.active {
  background: rgba(232,255,71,0.12);
  border-color: var(--accent);
  color: var(--accent);
}

/* ── Param rows ── */
.param-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  padding: 14px 16px;
  border-bottom: 1px solid var(--border);
}

.param-row:last-child { border-bottom: none; }

.param-left {
  display: flex;
  align-items: center;
  gap: 12px;
  flex: 1;
  min-width: 0;
}

.param-icon {
  width: 34px;
  height: 34px;
  border-radius: 9px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 15px;
  font-family: Georgia, serif;
  font-weight: 700;
  flex-shrink: 0;
}

.param-icon.strength { background: rgba(232,255,71,0.1); color: var(--accent); }
.param-icon.cardio   { background: rgba(255,120,71,0.1); color: #ff7847; }
.param-icon.stretch  { background: rgba(71,200,255,0.1); color: #47c8ff; }

.param-name {
  font-size: 13px;
  font-weight: 600;
  color: var(--text-primary);
  margin-bottom: 2px;
}

.param-desc {
  font-size: 11px;
  color: var(--text-muted);
  line-height: 1.4;
}

/* ── Toggle ── */
.toggle { position: relative; cursor: pointer; flex-shrink: 0; }
.toggle input { display: none; }

.toggle-track {
  display: block;
  width: 44px;
  height: 26px;
  background: var(--bg-input);
  border: 1px solid var(--border);
  border-radius: 13px;
  transition: all 0.25s;
  position: relative;
}

.toggle.on .toggle-track { background: var(--accent); border-color: var(--accent); }

.toggle-thumb {
  position: absolute;
  top: 3px;
  left: 3px;
  width: 18px;
  height: 18px;
  border-radius: 50%;
  background: var(--text-muted);
  transition: all 0.25s;
}

.toggle.on .toggle-thumb { left: 21px; background: #0d0d0f; }

/* ── Sub-params ── */
.param-sub {
  padding: 10px 16px 14px 62px;
  border-bottom: 1px solid var(--border);
  background: rgba(255,255,255,0.02);
}

.unit-tabs { display: flex; gap: 7px; }

.unit-tab {
  background: var(--bg-input);
  border: 1px solid var(--border);
  border-radius: 7px;
  color: var(--text-muted);
  padding: 6px 16px;
  font-size: 12px;
  font-weight: 600;
  font-family: inherit;
  cursor: pointer;
  transition: all 0.2s;
}

.unit-tab.active { background: rgba(232,255,71,0.12); border-color: var(--accent); color: var(--accent); }

.rest-presets { display: flex; gap: 7px; margin-bottom: 10px; flex-wrap: wrap; }

.preset-btn {
  background: var(--bg-input);
  border: 1px solid var(--border);
  border-radius: 7px;
  color: var(--text-muted);
  padding: 6px 12px;
  font-size: 12px;
  font-weight: 600;
  font-family: inherit;
  cursor: pointer;
  transition: all 0.2s;
}

.preset-btn.active { background: rgba(232,255,71,0.12); border-color: var(--accent); color: var(--accent); }

.rest-custom { display: flex; align-items: center; gap: 8px; }

.rest-input {
  background: var(--bg-input);
  border: 1px solid var(--border);
  border-radius: 8px;
  color: var(--text-primary);
  padding: 8px 12px;
  font-size: 14px;
  font-family: inherit;
  width: 80px;
  text-align: center;
  transition: border-color 0.2s;
}

.rest-input:focus { outline: none; border-color: var(--accent); }
.rest-unit { font-size: 12px; color: var(--text-muted); }

/* ── Save bar ── */
.save-bar {
  position: fixed;
  bottom: var(--nav-height);
  left: 50%;
  transform: translateX(-50%);
  width: 100%;
  max-width: 480px;
  padding: 12px 16px;
  background: linear-gradient(to top, var(--bg-main) 70%, transparent);
  display: flex;
  flex-direction: column;
  gap: 6px;
  z-index: 10;
}

.save-hint {
  font-size: 11px;
  color: var(--text-muted);
  text-align: center;
}

.btn-save {
  width: 100%;
  background: var(--accent);
  border: none;
  border-radius: 12px;
  color: #0d0d0f;
  padding: 15px;
  font-size: 13px;
  font-weight: 700;
  letter-spacing: 1px;
  font-family: inherit;
  cursor: pointer;
  transition: opacity 0.2s;
  box-shadow: 0 4px 20px rgba(232,255,71,0.3);
}

.btn-save:disabled { opacity: 0.35; cursor: not-allowed; box-shadow: none; }
.btn-save:not(:disabled):hover { opacity: 0.88; }
.btn-save:not(:disabled):active { opacity: 0.75; }
</style>
