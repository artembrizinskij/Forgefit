<template>
  <div class="modal-overlay" @click.self="$emit('close')">
    <div class="modal card">
      <!-- Header -->
      <div class="modal-header">
        <button class="btn-back" @click="$emit('close')">←</button>
        <span class="modal-title">НАСТРОЙКА УПРАЖНЕНИЯ</span>
        <div style="width:30px" />
      </div>

      <!-- Exercise name -->
      <div class="ex-name-row">
        <span class="ex-name">{{ exercise.name }}</span>
      </div>

      <!-- Type selector -->
      <div class="type-section">
        <div class="type-tabs">
          <button
            v-for="t in types"
            :key="t.value"
            class="type-tab"
            :class="{ active: localType === t.value, [t.value]: true }"
            @click="setType(t.value)"
          >
            {{ t.label }}
          </button>
        </div>
      </div>

      <div class="params-body">
        <!-- Strength params -->
        <template v-if="localType === 'strength'">
          <div class="params-section-title">ПАРАМЕТРЫ ПОДХОДА</div>

          <div class="param-row">
            <div class="param-icon">⚖</div>
            <div class="param-info">
              <div class="param-name">Вес</div>
              <div class="param-desc">Рабочий вес в кг или фунтах</div>
            </div>
            <div class="param-controls">
              <label class="toggle" :class="{ on: params.weight }">
                <input type="checkbox" v-model="params.weight" />
                <span class="toggle-track"><span class="toggle-thumb" /></span>
              </label>
            </div>
          </div>

          <div v-if="params.weight" class="param-sub">
            <div class="unit-tabs">
              <button class="unit-tab" :class="{ active: weightUnit === 'kg' }" @click="weightUnit = 'kg'">кг</button>
              <button class="unit-tab" :class="{ active: weightUnit === 'lb' }" @click="weightUnit = 'lb'">фунты</button>
            </div>
          </div>

          <div class="param-row">
            <div class="param-icon">#</div>
            <div class="param-info">
              <div class="param-name">Повторения</div>
              <div class="param-desc">Количество повторений в подходе</div>
            </div>
            <label class="toggle" :class="{ on: params.reps }">
              <input type="checkbox" v-model="params.reps" />
              <span class="toggle-track"><span class="toggle-thumb" /></span>
            </label>
          </div>

          <div class="param-row">
            <div class="param-icon">⏱</div>
            <div class="param-info">
              <div class="param-name">Время под нагрузкой (TUT)</div>
              <div class="param-desc">Темп движения: опускание/пауза/подъём (сек)</div>
            </div>
            <label class="toggle" :class="{ on: params.tut }">
              <input type="checkbox" v-model="params.tut" />
              <span class="toggle-track"><span class="toggle-thumb" /></span>
            </label>
          </div>

          <div class="param-row">
            <div class="param-icon">☕</div>
            <div class="param-info">
              <div class="param-name">Отдых между подходами</div>
              <div class="param-desc">Автоматический таймер после подхода</div>
            </div>
            <label class="toggle" :class="{ on: params.rest }">
              <input type="checkbox" v-model="params.rest" />
              <span class="toggle-track"><span class="toggle-thumb" /></span>
            </label>
          </div>

          <div v-if="params.rest" class="param-sub">
            <label class="field-label">ДЛИТЕЛЬНОСТЬ ОТДЫХА</label>
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
            <div class="param-icon">RPE</div>
            <div class="param-info">
              <div class="param-name">РПЕ (субъективная нагрузка)</div>
              <div class="param-desc">Шкала от 1 до 10 — насколько тяжело</div>
            </div>
            <label class="toggle" :class="{ on: params.rpe }">
              <input type="checkbox" v-model="params.rpe" />
              <span class="toggle-track"><span class="toggle-thumb" /></span>
            </label>
          </div>
        </template>

        <!-- Cardio params -->
        <template v-if="localType === 'cardio'">
          <div class="params-section-title">ПАРАМЕТРЫ КАРДИО</div>

          <div class="param-row">
            <div class="param-icon">⏱</div>
            <div class="param-info">
              <div class="param-name">Длительность</div>
              <div class="param-desc">Общее время кардио сессии</div>
            </div>
            <label class="toggle" :class="{ on: params.duration }">
              <input type="checkbox" v-model="params.duration" />
              <span class="toggle-track"><span class="toggle-thumb" /></span>
            </label>
          </div>

          <div class="param-row">
            <div class="param-icon">⚡</div>
            <div class="param-info">
              <div class="param-name">Темп</div>
              <div class="param-desc">Скорость движения</div>
            </div>
            <label class="toggle" :class="{ on: params.pace }">
              <input type="checkbox" v-model="params.pace" />
              <span class="toggle-track"><span class="toggle-thumb" /></span>
            </label>
          </div>

          <div v-if="params.pace" class="param-sub">
            <div class="unit-tabs">
              <button class="unit-tab" :class="{ active: params.units === 'kmh' }" @click="params.units = 'kmh'">км/ч</button>
              <button class="unit-tab" :class="{ active: params.units === 'minpkm' }" @click="params.units = 'minpkm'">мин/км</button>
            </div>
          </div>

          <div class="param-row">
            <div class="param-icon">📍</div>
            <div class="param-info">
              <div class="param-name">Расстояние</div>
              <div class="param-desc">Пройденная/пробежанная дистанция (км)</div>
            </div>
            <label class="toggle" :class="{ on: params.distance }">
              <input type="checkbox" v-model="params.distance" />
              <span class="toggle-track"><span class="toggle-thumb" /></span>
            </label>
          </div>

          <div class="param-row">
            <div class="param-icon">↗</div>
            <div class="param-info">
              <div class="param-name">Наклон дорожки</div>
              <div class="param-desc">Угол подъёма беговой дорожки (%)</div>
            </div>
            <label class="toggle" :class="{ on: params.incline }">
              <input type="checkbox" v-model="params.incline" />
              <span class="toggle-track"><span class="toggle-thumb" /></span>
            </label>
          </div>

          <div class="param-row">
            <div class="param-icon">♥</div>
            <div class="param-info">
              <div class="param-name">Пульс</div>
              <div class="param-desc">Частота сердечных сокращений (уд/мин)</div>
            </div>
            <label class="toggle" :class="{ on: params.heartRate }">
              <input type="checkbox" v-model="params.heartRate" />
              <span class="toggle-track"><span class="toggle-thumb" /></span>
            </label>
          </div>
        </template>

        <!-- Stretch params -->
        <template v-if="localType === 'stretch'">
          <div class="params-section-title">ПАРАМЕТРЫ РАСТЯЖКИ</div>

          <div class="param-row">
            <div class="param-icon">⏱</div>
            <div class="param-info">
              <div class="param-name">Длительность</div>
              <div class="param-desc">Время удержания позиции (сек)</div>
            </div>
            <label class="toggle" :class="{ on: params.duration }">
              <input type="checkbox" v-model="params.duration" />
              <span class="toggle-track"><span class="toggle-thumb" /></span>
            </label>
          </div>

          <div class="param-row">
            <div class="param-icon">⇄</div>
            <div class="param-info">
              <div class="param-name">Сторона</div>
              <div class="param-desc">Левая / правая / обе стороны</div>
            </div>
            <label class="toggle" :class="{ on: params.side }">
              <input type="checkbox" v-model="params.side" />
              <span class="toggle-track"><span class="toggle-thumb" /></span>
            </label>
          </div>

          <div class="param-row">
            <div class="param-icon">〰</div>
            <div class="param-info">
              <div class="param-name">Дыхательные циклы</div>
              <div class="param-desc">Количество вдохов-выдохов в позиции</div>
            </div>
            <label class="toggle" :class="{ on: params.breathing }">
              <input type="checkbox" v-model="params.breathing" />
              <span class="toggle-track"><span class="toggle-thumb" /></span>
            </label>
          </div>
        </template>
      </div>

      <!-- Save -->
      <div class="modal-footer">
        <button class="btn-primary" @click="save">СОХРАНИТЬ УПРАЖНЕНИЕ</button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue'
import type { Exercise, ExerciseParams, ExerciseType } from '@/types'
import { useExercisesStore } from '@/stores/exercises'

const props = defineProps<{ exercise: Exercise }>()
const emit = defineEmits<{
  close: []
  save: [exercise: Exercise]
}>()

const store = useExercisesStore()

const localType = ref<ExerciseType>(props.exercise.type)
const params = reactive<ExerciseParams>({ ...props.exercise.params })
const weightUnit = ref<'kg' | 'lb'>('kg')

const types: { value: ExerciseType; label: string }[] = [
  { value: 'strength', label: 'Силовое' },
  { value: 'cardio', label: 'Кардио' },
  { value: 'stretch', label: 'Растяжка' },
]

const restPresets = [60, 90, 120, 180]

function setType(type: ExerciseType) {
  if (type === localType.value) return
  localType.value = type
  const defaults = store.makeDefaultParams(type)
  Object.assign(params, defaults)
}

function formatRest(s: number) {
  if (s < 60) return `${s}с`
  const m = Math.floor(s / 60)
  const rem = s % 60
  return rem ? `${m}м ${rem}с` : `${m} мин`
}

function save() {
  emit('save', {
    ...props.exercise,
    type: localType.value,
    params: { ...params },
  })
}
</script>

<style scoped>
.modal-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.75);
  display: flex;
  align-items: flex-end;
  justify-content: center;
  z-index: 100;
}

@media (min-width: 500px) {
  .modal-overlay { align-items: center; padding: 20px; }
  .modal { border-radius: 18px !important; max-height: 90vh; }
}

.card {
  background: var(--bg-card);
  border: 1px solid var(--border);
}

.modal {
  width: 100%;
  max-width: 480px;
  border-radius: 18px 18px 0 0;
  display: flex;
  flex-direction: column;
  max-height: 92vh;
  overflow: hidden;
}

.modal-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 14px 16px;
  border-bottom: 1px solid var(--border);
  flex-shrink: 0;
}

.modal-title {
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
  width: 30px;
  height: 30px;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  font-size: 14px;
  transition: all 0.2s;
}

.btn-back:hover { color: var(--accent); border-color: var(--accent); }

.ex-name-row {
  padding: 14px 18px 10px;
  border-bottom: 1px solid var(--border);
  flex-shrink: 0;
}

.ex-name {
  font-size: 17px;
  font-weight: 700;
  color: var(--text-primary);
}

.type-section {
  padding: 14px 18px;
  border-bottom: 1px solid var(--border);
  flex-shrink: 0;
}

.type-tabs {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 8px;
}

.type-tab {
  background: var(--bg-input);
  border: 1px solid var(--border);
  border-radius: 8px;
  color: var(--text-muted);
  padding: 10px 6px;
  font-size: 12px;
  font-weight: 600;
  font-family: inherit;
  cursor: pointer;
  text-align: center;
  transition: all 0.2s;
}

.type-tab.active.strength { background: rgba(232, 255, 71, 0.12); border-color: var(--accent); color: var(--accent); }
.type-tab.active.cardio   { background: rgba(255, 120, 71, 0.12); border-color: #ff7847; color: #ff7847; }
.type-tab.active.stretch  { background: rgba(71, 200, 255, 0.12); border-color: #47c8ff; color: #47c8ff; }

.params-body {
  flex: 1;
  min-height: 0;
  overflow-y: auto;
  padding: 0 18px 8px;
}

.params-section-title {
  font-size: 9px;
  font-weight: 700;
  letter-spacing: 1.5px;
  color: var(--text-muted);
  padding: 16px 0 8px;
}

.param-row {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 12px 0;
  border-bottom: 1px solid var(--border);
}

.param-icon {
  font-size: 16px;
  color: var(--accent);
  min-width: 26px;
  text-align: center;
  font-family: Georgia, serif;
  font-weight: 700;
  font-size: 13px;
}

.param-info {
  flex: 1;
}

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

/* Toggle */
.toggle {
  position: relative;
  cursor: pointer;
  flex-shrink: 0;
}

.toggle input { display: none; }

.toggle-track {
  display: block;
  width: 42px;
  height: 24px;
  background: var(--bg-input);
  border: 1px solid var(--border);
  border-radius: 12px;
  transition: all 0.25s;
  position: relative;
}

.toggle.on .toggle-track {
  background: var(--accent);
  border-color: var(--accent);
}

.toggle-thumb {
  position: absolute;
  top: 2px;
  left: 2px;
  width: 18px;
  height: 18px;
  border-radius: 50%;
  background: var(--text-muted);
  transition: all 0.25s;
}

.toggle.on .toggle-thumb {
  left: 20px;
  background: #0d0d0f;
}

/* Sub-params */
.param-sub {
  padding: 10px 0 14px 38px;
  border-bottom: 1px solid var(--border);
}

.field-label {
  font-size: 9px;
  font-weight: 700;
  letter-spacing: 1.2px;
  color: var(--text-muted);
  display: block;
  margin-bottom: 8px;
}

.rest-presets {
  display: flex;
  gap: 7px;
  margin-bottom: 10px;
}

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

.preset-btn.active {
  background: rgba(232, 255, 71, 0.12);
  border-color: var(--accent);
  color: var(--accent);
}

.rest-custom {
  display: flex;
  align-items: center;
  gap: 8px;
}

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

.rest-unit {
  font-size: 12px;
  color: var(--text-muted);
}

.unit-tabs {
  display: flex;
  gap: 7px;
}

.unit-tab {
  background: var(--bg-input);
  border: 1px solid var(--border);
  border-radius: 7px;
  color: var(--text-muted);
  padding: 6px 14px;
  font-size: 12px;
  font-weight: 600;
  font-family: inherit;
  cursor: pointer;
  transition: all 0.2s;
}

.unit-tab.active {
  background: rgba(232, 255, 71, 0.12);
  border-color: var(--accent);
  color: var(--accent);
}

/* Footer */
.modal-footer {
  padding: 14px 18px;
  border-top: 1px solid var(--border);
  flex-shrink: 0;
}

.btn-primary {
  display: block;
  width: 100%;
  background: var(--accent);
  border: none;
  border-radius: 10px;
  color: #0d0d0f;
  padding: 13px;
  font-size: 13px;
  font-weight: 700;
  letter-spacing: 1px;
  font-family: inherit;
  cursor: pointer;
  transition: opacity 0.2s;
}

.btn-primary:hover { opacity: 0.88; }
</style>
