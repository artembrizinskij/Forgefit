<template>
  <div class="modal-overlay" @click.self="$emit('close')">
    <div class="modal card">
      <div class="modal-header">
        <span class="modal-title">{{ isEdit ? 'РЕДАКТИРОВАТЬ' : 'НОВОЕ УПРАЖНЕНИЕ' }}</span>
        <button class="btn-close" @click="$emit('close')">✕</button>
      </div>

      <div class="modal-body">
        <!-- Name -->
        <div class="field-group">
          <label class="field-label">НАЗВАНИЕ</label>
          <input
            v-model="form.name"
            class="field-input"
            placeholder="Жим штанги лёжа"
            maxlength="80"
          />
        </div>

        <!-- Type -->
        <div class="field-group">
          <label class="field-label">ТИП</label>
          <div class="type-tabs">
            <button
              v-for="t in types"
              :key="t.value"
              class="type-tab"
              :class="{ active: form.type === t.value, [t.value]: true }"
              @click="setType(t.value)"
            >
              {{ t.label }}
            </button>
          </div>
        </div>

        <!-- Muscle Group -->
        <div class="field-group">
          <label class="field-label">ГРУППА МЫШЦ</label>
          <div class="select-wrapper">
            <select v-model="form.muscleGroup" class="field-input field-select">
              <option v-for="g in muscleGroups" :key="g" :value="g">{{ g }}</option>
            </select>
            <span class="select-arrow">▼</span>
          </div>
        </div>

        <!-- Muscle Tags -->
        <div class="field-group">
          <label class="field-label">МЫШЦЫ (выберите)</label>
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

        <!-- Description -->
        <div class="field-group">
          <label class="field-label">ОПИСАНИЕ</label>
          <textarea
            v-model="form.description"
            class="field-input field-textarea"
            placeholder="Базовое упражнение для..."
            rows="3"
          />
        </div>
      </div>

      <div class="modal-footer">
        <button class="btn-secondary" @click="$emit('close')">Отмена</button>
        <button class="btn-primary" :disabled="!form.name.trim()" @click="save">
          {{ isEdit ? 'СОХРАНИТЬ' : 'СОЗДАТЬ' }}
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { reactive, computed, watch } from 'vue'
import type { Exercise, ExerciseType, MuscleGroup } from '@/types'
import { MUSCLE_GROUPS, MUSCLE_TAGS, useExercisesStore } from '@/stores/exercises'

const props = defineProps<{ exercise: Exercise | null }>()
const emit = defineEmits<{
  close: []
  save: [data: Exercise | Omit<Exercise, 'id'>]
}>()

const store = useExercisesStore()
const isEdit = computed(() => props.exercise !== null)

const muscleGroups = MUSCLE_GROUPS

const types: { value: ExerciseType; label: string }[] = [
  { value: 'strength', label: 'Силовое' },
  { value: 'cardio', label: 'Кардио' },
  { value: 'stretch', label: 'Растяжка' },
]

interface FormData {
  name: string
  type: ExerciseType
  muscleGroup: MuscleGroup
  muscles: string[]
  description: string
}

const form = reactive<FormData>({
  name: props.exercise?.name ?? '',
  type: props.exercise?.type ?? 'strength',
  muscleGroup: props.exercise?.muscleGroup ?? 'Грудь',
  muscles: [...(props.exercise?.muscles ?? [])],
  description: props.exercise?.description ?? '',
})

const availableTags = computed(() => MUSCLE_TAGS[form.muscleGroup] ?? [])

// When muscle group changes, clear muscles that don't belong to the new group
watch(() => form.muscleGroup, () => {
  form.muscles = form.muscles.filter(m => availableTags.value.includes(m))
})

function setType(type: ExerciseType) {
  form.type = type
}

function toggleMuscle(tag: string) {
  const idx = form.muscles.indexOf(tag)
  if (idx === -1) form.muscles.push(tag)
  else form.muscles.splice(idx, 1)
}

function save() {
  if (!form.name.trim()) return
  const params = props.exercise?.params ?? store.makeDefaultParams(form.type)

  if (isEdit.value && props.exercise) {
    emit('save', {
      ...props.exercise,
      name: form.name.trim(),
      type: form.type,
      muscleGroup: form.muscleGroup,
      muscles: [...form.muscles],
      description: form.description.trim(),
      params: form.type !== props.exercise.type ? store.makeDefaultParams(form.type) : params,
    })
  } else {
    emit('save', {
      name: form.name.trim(),
      type: form.type,
      muscleGroup: form.muscleGroup,
      muscles: [...form.muscles],
      description: form.description.trim(),
      params: store.makeDefaultParams(form.type),
    })
  }
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
  padding: 0;
}

@media (min-width: 500px) {
  .modal-overlay { align-items: center; padding: 20px; }
  .modal { border-radius: 18px !important; max-height: 90vh; }
}

.card {
  background: var(--bg-card);
  border: 1px solid var(--border);
  border-radius: 18px 18px 0 0;
}

.modal {
  width: 100%;
  max-width: 480px;
  display: flex;
  flex-direction: column;
  max-height: 92vh;
  overflow: hidden;
}

.modal-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 16px 18px;
  border-bottom: 1px solid var(--border);
  flex-shrink: 0;
}

.modal-title {
  font-size: 12px;
  font-weight: 700;
  letter-spacing: 1.5px;
  color: var(--text-primary);
}

.btn-close {
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
  font-size: 12px;
  transition: all 0.2s;
}

.btn-close:hover { color: var(--error); border-color: var(--error); }

.modal-body {
  flex: 1;
  min-height: 0;
  overflow-y: auto;
  padding: 18px;
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.field-group {
  display: flex;
  flex-direction: column;
  gap: 7px;
}

.field-label {
  font-size: 9px;
  font-weight: 700;
  letter-spacing: 1.5px;
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

.field-select {
  appearance: none;
  cursor: pointer;
}

.select-wrapper {
  position: relative;
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

.type-tabs {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 7px;
}

.type-tab {
  background: var(--bg-input);
  border: 1px solid var(--border);
  border-radius: 8px;
  color: var(--text-muted);
  padding: 9px 6px;
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

.muscle-tags-grid {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
}

.muscle-tag-btn {
  background: var(--bg-input);
  border: 1px solid var(--border);
  border-radius: 20px;
  color: var(--text-muted);
  padding: 5px 12px;
  font-size: 11px;
  font-weight: 500;
  font-family: inherit;
  cursor: pointer;
  transition: all 0.2s;
}

.muscle-tag-btn.active {
  background: rgba(232, 255, 71, 0.12);
  border-color: var(--accent);
  color: var(--accent);
}

.modal-footer {
  display: flex;
  gap: 10px;
  padding: 14px 18px;
  border-top: 1px solid var(--border);
  flex-shrink: 0;
}

.btn-secondary {
  flex: 1;
  background: var(--bg-input);
  border: 1px solid var(--border);
  border-radius: 10px;
  color: var(--text-secondary);
  padding: 12px;
  font-size: 13px;
  font-weight: 600;
  font-family: inherit;
  cursor: pointer;
  transition: border-color 0.2s;
}

.btn-secondary:hover { border-color: var(--text-muted); }

.btn-primary {
  flex: 2;
  background: var(--accent);
  border: none;
  border-radius: 10px;
  color: #0d0d0f;
  padding: 12px;
  font-size: 13px;
  font-weight: 700;
  letter-spacing: 0.8px;
  font-family: inherit;
  cursor: pointer;
  transition: opacity 0.2s;
}

.btn-primary:disabled { opacity: 0.4; cursor: not-allowed; }
.btn-primary:not(:disabled):hover { opacity: 0.88; }
</style>
