<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { AlertTriangle, CalendarDays, Check, CircleAlert, Plus, Trash2, WalletCards, X } from '@lucide/vue'
import { financeApi, getApiError } from '../api/client'
import { useHouseholdStore } from '../stores/household'
import type { Budget, Category } from '../types/finance'
import { currency, monthLabel } from '../utils/format'
import PageHeader from '../components/PageHeader.vue'
import LoadingState from '../components/LoadingState.vue'
import EmptyState from '../components/EmptyState.vue'
import UiButton from '../components/UiButton.vue'

const household = useHouseholdStore()
const budgets = ref<Budget[]>([])
const categories = ref<Category[]>([])
const loading = ref(true)
const saving = ref(false)
const error = ref('')
const month = ref(new Date().toISOString().slice(0, 7))
const modalOpen = ref(false)
const editingId = ref<string | null>(null)
const categoryId = ref('')
const amountLimit = ref<number | null>(null)
const threshold = ref(80)
const currencyCode = computed(() => household.activeHousehold?.currencyCode || 'EUR')
const availableCategories = computed(() => categories.value.filter(item => item.kind.toLowerCase() === 'expense' && item.isActive))
const selectableCategories = computed(() => availableCategories.value.filter(category =>
  category.id === categoryId.value || !budgets.value.some(budget => budget.categoryId === category.id && budget.id !== editingId.value),
))

async function load() {
  const id = household.activeHouseholdId
  if (!id) { loading.value = false; return }
  loading.value = true
  error.value = ''
  const [year, monthNumber] = month.value.split('-').map(Number)
  try {
    const [budgetResult, categoryResult] = await Promise.all([
      financeApi.budgets(id, year, monthNumber),
      financeApi.categories(id),
    ])
    budgets.value = budgetResult.data
    categories.value = categoryResult.data
  } catch (cause) {
    error.value = getApiError(cause, 'No se pudieron cargar los presupuestos.')
  } finally {
    loading.value = false
  }
}

watch([() => household.activeHouseholdId, month], load)
onMounted(load)

const monthTitle = computed(() => {
  const [year, monthNumber] = month.value.split('-').map(Number)
  return monthLabel(monthNumber, year)
})
const totalLimit = computed(() => budgets.value.reduce((sum, item) => sum + item.amountLimit, 0))
const totalSpent = computed(() => budgets.value.reduce((sum, item) => sum + item.spent, 0))
const overallPercent = computed(() => totalLimit.value ? Math.round(totalSpent.value / totalLimit.value * 100) : 0)

function categoryName(id: string) {
  return categories.value.find(category => category.id === id)?.name || 'Categoría'
}

function openCreate() {
  editingId.value = null
  categoryId.value = availableCategories.value.find(category => !budgets.value.some(budget => budget.categoryId === category.id))?.id || availableCategories.value[0]?.id || ''
  amountLimit.value = null
  threshold.value = 80
  modalOpen.value = true
}

function openEdit(budget: Budget) {
  editingId.value = budget.id
  categoryId.value = budget.categoryId
  amountLimit.value = budget.amountLimit
  threshold.value = budget.alertThresholdPercent
  modalOpen.value = true
}

async function save() {
  const householdId = household.activeHouseholdId
  if (!householdId || !categoryId.value || !amountLimit.value) {
    error.value = 'Selecciona una categoría e introduce un límite válido.'
    return
  }
  saving.value = true
  error.value = ''
  const [year, monthNumber] = month.value.split('-').map(Number)
  try {
    await financeApi.saveBudget(householdId, {
      categoryId: categoryId.value, year, month: monthNumber, amountLimit: amountLimit.value,
      alertThresholdPercent: threshold.value, currencyCode: currencyCode.value,
    }, editingId.value || undefined)
    modalOpen.value = false
    await load()
  } catch (cause) {
    error.value = getApiError(cause, 'No se pudo guardar el presupuesto.')
  } finally {
    saving.value = false
  }
}

async function remove(budget: Budget) {
  if (!household.activeHouseholdId || !window.confirm(`¿Eliminar el presupuesto de ${categoryName(budget.categoryId)}?`)) return
  try {
    await financeApi.deleteBudget(household.activeHouseholdId, budget.id)
    await load()
  } catch (cause) {
    error.value = getApiError(cause, 'No se pudo eliminar el presupuesto.')
  }
}
</script>

<template>
  <div>
    <PageHeader eyebrow="PLANEAR PARA VIVIR CON CALMA" title="Presupuestos" description="Define límites realistas y mantén el rumbo de cada mes.">
      <label class="month-picker"><CalendarDays :size="16" /><input v-model="month" type="month" aria-label="Mes del presupuesto" /></label>
      <UiButton @click="openCreate"><Plus :size="16" /> Nuevo presupuesto</UiButton>
    </PageHeader>
    <div v-if="error" class="inline-error" role="alert">{{ error }}</div>
    <LoadingState v-if="loading" />
    <template v-else>
      <section class="budget-summary surface">
        <div class="budget-summary__icon"><WalletCards :size="20" /></div>
        <div class="budget-summary__copy"><p class="eyebrow">VISTA GENERAL · {{ monthTitle }}</p><h2>{{ currency(totalSpent, currencyCode) }} <span>gastados de {{ currency(totalLimit, currencyCode) }}</span></h2></div>
        <div class="budget-summary__percent" :class="{ 'text-danger': overallPercent >= 100, 'text-warning': overallPercent >= 80 && overallPercent < 100 }">{{ overallPercent }}<small>%</small></div>
        <div class="budget-summary__progress"><div class="progress-track progress-track--large"><span class="progress-fill" :class="overallPercent >= 100 ? 'progress-fill--danger' : overallPercent >= 80 ? 'progress-fill--warning' : 'progress-fill--budget'" :style="{ width: `${Math.min(100, overallPercent)}%` }" /></div><div><span>Gastado</span><strong>{{ currency(Math.max(0, totalLimit - totalSpent), currencyCode) }} disponible</strong></div></div>
      </section>

      <div class="budget-list-heading"><div><p class="eyebrow">POR CATEGORÍA</p><h2>Tus límites mensuales</h2></div><span class="budget-count">{{ budgets.length }} {{ budgets.length === 1 ? 'presupuesto' : 'presupuestos' }}</span></div>
      <div v-if="!budgets.length" class="surface budget-empty">
        <EmptyState title="Tu primer plan empieza aquí" description="Asigna un límite mensual a una categoría para saber cuánto puedes gastar con tranquilidad." />
        <UiButton variant="secondary" @click="openCreate"><Plus :size="16" /> Crear presupuesto</UiButton>
      </div>
      <section v-else class="budget-grid">
        <article v-for="budget in budgets" :key="budget.id" class="budget-item surface">
          <div class="budget-item__top">
            <span class="budget-category-icon" :class="budget.isOverspent ? 'budget-category-icon--danger' : budget.alertThresholdReached ? 'budget-category-icon--warn' : ''"><CircleAlert v-if="budget.isOverspent || budget.alertThresholdReached" :size="17" /><WalletCards v-else :size="17" /></span>
            <div class="budget-item__category"><strong>{{ categoryName(budget.categoryId) }}</strong><span>Límite mensual</span></div>
            <div class="budget-item__actions"><button class="icon-button" :aria-label="`Editar ${categoryName(budget.categoryId)}`" @click="openEdit(budget)"><span class="edit-glyph">✎</span></button><button class="icon-button row-delete" :aria-label="`Eliminar ${categoryName(budget.categoryId)}`" @click="remove(budget)"><Trash2 :size="16" /></button></div>
          </div>
          <div class="budget-item__amounts"><strong>{{ currency(budget.spent, currencyCode) }}</strong><span> de {{ currency(budget.amountLimit, currencyCode) }}</span><span class="budget-item__percentage" :class="{ 'text-danger': budget.isOverspent, 'text-warning': budget.alertThresholdReached && !budget.isOverspent }">{{ Math.round(budget.amountLimit ? budget.spent / budget.amountLimit * 100 : 0) }}%</span></div>
          <div class="progress-track progress-track--large"><span class="progress-fill" :class="budget.isOverspent ? 'progress-fill--danger' : budget.alertThresholdReached ? 'progress-fill--warning' : 'progress-fill--budget'" :style="{ width: `${Math.min(100, budget.amountLimit ? budget.spent / budget.amountLimit * 100 : 0)}%` }" /></div>
          <div class="budget-item__foot"><span v-if="budget.isOverspent" class="status-text status-text--danger"><AlertTriangle :size="14" /> Te has pasado {{ currency(Math.abs(budget.remaining), currencyCode) }}</span><span v-else-if="budget.alertThresholdReached" class="status-text status-text--warning"><AlertTriangle :size="14" /> Cerca de tu límite</span><span v-else class="status-text"><Check :size="14" /> Dentro de tu plan</span><span>{{ currency(Math.max(0, budget.remaining), currencyCode) }} disponible</span></div>
          <div class="budget-threshold">Alerta al {{ budget.alertThresholdPercent }}% del límite</div>
        </article>
      </section>
    </template>

    <div v-if="modalOpen" class="modal-backdrop" @click.self="modalOpen = false">
      <section class="modal-card" role="dialog" aria-modal="true" aria-label="Presupuesto mensual">
        <div class="modal-heading"><div><p class="eyebrow">{{ editingId ? 'AJUSTAR PLAN' : 'NUEVO PLAN' }}</p><h2>{{ editingId ? 'Editar presupuesto' : 'Crear presupuesto' }}</h2></div><button class="icon-button" aria-label="Cerrar" @click="modalOpen = false"><X :size="19" /></button></div>
        <form class="form-stack" @submit.prevent="save">
          <label class="field-label" for="budget-category">Categoría</label>
          <select id="budget-category" v-model="categoryId" class="input" required><option value="" disabled>Elige una categoría</option><option v-for="category in selectableCategories" :key="category.id" :value="category.id">{{ category.name }}</option></select>
          <label class="field-label" for="budget-amount">Límite mensual</label><div class="amount-input-wrap"><span>{{ currencyCode }}</span><input id="budget-amount" v-model.number="amountLimit" type="number" min="0.01" step="0.01" placeholder="0,00" required /></div>
          <label class="field-label" for="budget-threshold">Avisarme al {{ threshold }}%</label><input id="budget-threshold" v-model.number="threshold" type="range" min="50" max="100" step="5" class="range-input" /><div class="range-scale"><span>50%</span><span>100%</span></div>
          <p class="field-hint">Periodo: {{ monthTitle }} · la alerta aparece al alcanzar este porcentaje.</p>
          <div v-if="error" class="form-error" role="alert">{{ error }}</div>
          <div class="modal-actions"><UiButton variant="secondary" @click="modalOpen = false">Cancelar</UiButton><UiButton type="submit" :disabled="saving || !selectableCategories.length">{{ saving ? 'Guardando…' : 'Guardar presupuesto' }}</UiButton></div>
        </form>
      </section>
    </div>
  </div>
</template>
