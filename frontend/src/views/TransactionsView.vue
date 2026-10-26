<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { ArrowDownLeft, ArrowUpRight, CalendarDays, FileImage, Plus, Search, Trash2, X } from '@lucide/vue'
import { financeApi, getApiError } from '../api/client'
import { useHouseholdStore } from '../stores/household'
import type { Category, Expense, ExpenseItem, Income, ReceiptOcr } from '../types/finance'
import { currency, dateLabel } from '../utils/format'
import PageHeader from '../components/PageHeader.vue'
import LoadingState from '../components/LoadingState.vue'
import EmptyState from '../components/EmptyState.vue'
import UiButton from '../components/UiButton.vue'

const props = defineProps<{ kind: 'expense' | 'income' }>()
const household = useHouseholdStore()
const rows = ref<(Expense | Income)[]>([])
const categories = ref<Category[]>([])
const loading = ref(true)
const saving = ref(false)
const error = ref('')
const search = ref('')
const month = ref(new Date().toISOString().slice(0, 7))
const modalOpen = ref(false)
const categoryId = ref('')
const amount = ref<number | null>(null)
const date = ref(new Date().toISOString().slice(0, 10))
const description = ref('')
const merchant = ref('')
const items = ref<ExpenseItem[]>([])
const editingId = ref<string | null>(null)
const receiptInput = ref<HTMLInputElement | null>(null)
const receiptResult = ref<ReceiptOcr | null>(null)
const receiptLoading = ref(false)
const receiptError = ref('')
const currencyCode = computed(() => household.activeHousehold?.currencyCode || 'EUR')
const isExpense = computed(() => props.kind === 'expense')
const title = computed(() => isExpense.value ? 'Gastos' : 'Ingresos')
const kindCategories = computed(() => categories.value.filter(category => category.kind.toLowerCase() === (isExpense.value ? 'expense' : 'income') && category.isActive))
const filteredRows = computed(() => rows.value.filter(row => {
  const label = isExpense.value ? (row as Expense).merchant : (row as Income).source
  const categoryName = categories.value.find(category => category.id === row.categoryId)?.name || ''
  const itemConcepts = isExpense.value
    ? ((row as Expense).items || []).map(item => item.description).join(' ')
    : ''
  return `${label || ''} ${row.description || ''} ${itemConcepts} ${categoryName}`.toLowerCase().includes(search.value.toLowerCase())
}))
const total = computed(() => rows.value.reduce((sum, item) => sum + item.amount, 0))

async function load() {
  const id = household.activeHouseholdId
  if (!id) { loading.value = false; return }
  loading.value = true
  error.value = ''
  const [year, monthNumber] = month.value.split('-').map(Number)
  const from = `${year}-${String(monthNumber).padStart(2, '0')}-01`
  const to = new Date(Date.UTC(year, monthNumber, 0)).toISOString().slice(0, 10)
  try {
    const [transactionResult, categoryResult] = await Promise.all([
      isExpense.value ? financeApi.expenses(id, from, to) : financeApi.incomes(id, from, to),
      financeApi.categories(id),
    ])
    rows.value = transactionResult.data
    categories.value = categoryResult.data
  } catch (cause) {
    error.value = getApiError(cause, 'No se pudieron cargar los movimientos.')
  } finally {
    loading.value = false
  }
}

watch([() => household.activeHouseholdId, () => props.kind, month], load)
onMounted(load)

function openCreate() {
  editingId.value = null
  receiptResult.value = null
  receiptError.value = ''
  categoryId.value = kindCategories.value[0]?.id || ''
  amount.value = null
  date.value = new Date().toISOString().slice(0, 10)
  description.value = ''
  merchant.value = ''
  items.value = []
  modalOpen.value = true
}

async function importReceiptFile(event: Event) {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0]
  input.value = ''
  if (!file) return

  const householdId = household.activeHouseholdId
  if (!householdId) {
    error.value = 'Crea o selecciona un hogar para continuar.'
    return
  }
  if (!['image/jpeg', 'image/png', 'application/pdf'].includes(file.type)) {
    error.value = 'Elige una imagen JPG o PNG, o un documento PDF.'
    return
  }
  if (file.size > 15 * 1024 * 1024) {
    error.value = 'El archivo no puede superar los 15 MB.'
    return
  }

  modalOpen.value = true
  receiptResult.value = null
  receiptError.value = ''
  receiptLoading.value = true
  editingId.value = null
  try {
    const response = await financeApi.uploadReceipt(householdId, file)
    receiptResult.value = response.data
    if (response.data.ocrStatus === 'Succeeded') {
      categoryId.value = kindCategories.value[0]?.id || ''
      amount.value = response.data.total
      date.value = response.data.occurredOn || new Date().toISOString().slice(0, 10)
      merchant.value = response.data.merchant || ''
      description.value = ''
      items.value = response.data.items.map(item => ({ ...item }))
    } else {
      receiptError.value = 'No se pudo leer el ticket. Prueba con una imagen más nítida.'
    }
  } catch (cause) {
    if (typeof cause === 'object' && cause !== null && 'response' in cause) {
      const response = (cause as { response?: { data?: ReceiptOcr } }).response?.data
      if (response?.id) receiptResult.value = response
    }
    receiptError.value = getApiError(cause, 'No se pudo analizar el ticket.')
  } finally {
    receiptLoading.value = false
  }
}

function openEdit(row: Expense | Income) {
  editingId.value = row.id
  receiptResult.value = null
  receiptError.value = ''
  categoryId.value = row.categoryId
  amount.value = row.amount
  date.value = row.occurredOn
  description.value = row.description || ''
  merchant.value = isExpense.value ? (row as Expense).merchant || '' : (row as Income).source || ''
  items.value = isExpense.value ? (row as Expense).items?.map(item => ({ ...item })) || [] : []
  modalOpen.value = true
}

function addItem() {
  items.value.push({ description: '', quantity: 1, price: 0 })
}

async function save() {
  const householdId = household.activeHouseholdId
  if (!householdId || !categoryId.value || !amount.value) {
    error.value = 'Elige una categoría e introduce un importe válido.'
    return
  }
  if (isExpense.value && items.value.some(item => !item.description.trim() || item.quantity <= 0 || item.price <= 0)) {
    error.value = 'Completa la descripción, cantidad y precio de cada artículo, o elimínalo.'
    return
  }
  saving.value = true
  error.value = ''
  try {
    if (isExpense.value) {
      await financeApi.saveExpense(householdId, {
        categoryId: categoryId.value, amount: amount.value, currencyCode: currencyCode.value,
        occurredOn: date.value, description: description.value || null, merchant: merchant.value || null, notes: null,
        items: items.value.length ? items.value.map(item => ({ description: item.description.trim(), quantity: item.quantity, price: item.price })) : null,
        ...(receiptResult.value?.ocrStatus === 'Succeeded' && !editingId.value ? { receiptId: receiptResult.value.id } : {}),
      }, editingId.value || undefined)
    } else {
      await financeApi.saveIncome(householdId, {
        categoryId: categoryId.value, amount: amount.value, currencyCode: currencyCode.value,
        occurredOn: date.value, description: description.value || null, source: merchant.value || null, notes: null,
      }, editingId.value || undefined)
    }
    modalOpen.value = false
    receiptResult.value = null
    await load()
  } catch (cause) {
    error.value = getApiError(cause, 'No se pudo guardar el movimiento.')
  } finally {
    saving.value = false
  }
}

async function remove(row: Expense | Income) {
  const householdId = household.activeHouseholdId
  if (!householdId || !window.confirm('¿Quieres eliminar este movimiento?')) return
  try {
    if (isExpense.value) await financeApi.deleteExpense(householdId, row.id)
    else await financeApi.deleteIncome(householdId, row.id)
    await load()
  } catch (cause) {
    error.value = getApiError(cause, 'No se pudo eliminar el movimiento.')
  }
}
</script>

<template>
  <div>
    <PageHeader :eyebrow="isExpense ? 'CADA GASTO CUENTA' : 'CADA INGRESO SUMA'" :title="title"
      :description="isExpense ? 'Registra y entiende los gastos de tu hogar.' : 'Lleva el control de lo que entra en casa.'">
      <label class="month-picker"><CalendarDays :size="16" /><input v-model="month" type="month" aria-label="Filtrar por mes" /></label>
      <input v-if="isExpense" ref="receiptInput" class="sr-only" type="file" accept=".jpg,.jpeg,.png,.pdf,image/jpeg,image/png,application/pdf" @change="importReceiptFile" />
      <UiButton v-if="isExpense" variant="secondary" @click="receiptInput?.click()"><FileImage :size="16" /> Leer ticket</UiButton>
      <UiButton @click="openCreate"><Plus :size="16" /> Añadir {{ isExpense ? 'gasto' : 'ingreso' }}</UiButton>
    </PageHeader>

    <div v-if="error" class="inline-error" role="alert">{{ error }}</div>
    <section class="transaction-overview surface">
      <div class="transaction-overview__text"><span class="eyebrow">{{ isExpense ? 'TOTAL DE GASTOS' : 'TOTAL DE INGRESOS' }} · {{ month }}</span><strong>{{ currency(total, currencyCode) }}</strong><small>{{ rows.length }} movimientos registrados</small></div>
      <div class="transaction-overview__icon" :class="isExpense ? 'activity-icon--expense' : 'activity-icon--income'"><ArrowUpRight v-if="isExpense" :size="22" /><ArrowDownLeft v-else :size="22" /></div>
      <div class="transaction-overview__decoration">✳</div>
    </section>

    <section class="surface table-card">
      <div class="table-toolbar">
        <div><p class="eyebrow">TU ACTIVIDAD</p><h2>Movimientos</h2></div>
        <label class="search-field"><Search :size="16" /><input v-model="search" placeholder="Buscar movimiento…" aria-label="Buscar movimiento" /></label>
      </div>
      <LoadingState v-if="loading" />
      <div v-else-if="!filteredRows.length" class="table-empty">
        <EmptyState :title="search ? 'No hay coincidencias' : `Aún no hay ${isExpense ? 'gastos' : 'ingresos'}`" :description="search ? 'Prueba otra búsqueda.' : 'Cuando registres un movimiento, lo verás aquí.'" />
        <UiButton v-if="!search" variant="secondary" @click="openCreate"><Plus :size="15" /> Añadir el primero</UiButton>
      </div>
      <div v-else class="table-wrap">
        <table class="data-table">
          <thead><tr><th>DETALLE</th><th>CATEGORÍA</th><th>FECHA</th><th class="text-right">IMPORTE</th><th aria-label="Acciones" /></tr></thead>
          <tbody>
            <tr v-for="row in filteredRows" :key="row.id" class="clickable-row" @click="openEdit(row)">
              <td><div class="table-detail"><span class="table-icon" :class="isExpense ? 'activity-icon--expense' : 'activity-icon--income'"><ArrowUpRight v-if="isExpense" :size="16" /><ArrowDownLeft v-else :size="16" /></span><span><strong>{{ (isExpense ? (row as Expense).merchant : (row as Income).source) || row.description || (isExpense ? 'Gasto' : 'Ingreso') }}</strong><small>{{ row.description || (isExpense && (row as Expense).items?.length ? `${(row as Expense).items?.length} artículos` : '—') }}</small></span></div></td>
              <td><span class="category-pill">{{ categories.find(category => category.id === row.categoryId)?.name || 'Sin categoría' }}</span></td>
              <td class="muted-cell">{{ dateLabel(row.occurredOn) }}</td>
              <td class="text-right"><strong :class="isExpense ? 'amount-negative' : 'amount-positive'">{{ isExpense ? '−' : '+' }}{{ currency(row.amount, currencyCode) }}</strong></td>
              <td><button class="icon-button row-delete" :aria-label="`Eliminar movimiento ${row.id}`" @click.stop="remove(row)"><Trash2 :size="16" /></button></td>
            </tr>
          </tbody>
        </table>
      </div>
      <p class="table-footer">Mostrando {{ filteredRows.length }} de {{ rows.length }} movimientos</p>
    </section>

    <div v-if="modalOpen" class="modal-backdrop" @click.self="modalOpen = false">
      <section class="modal-card" role="dialog" aria-modal="true" :aria-label="`${editingId ? 'Editar' : 'Añadir'} ${title.toLowerCase()}`">
        <div class="modal-heading"><div><p class="eyebrow">{{ editingId ? 'ACTUALIZAR' : 'NUEVO MOVIMIENTO' }}</p><h2>{{ editingId ? 'Editar' : 'Añadir' }} {{ isExpense ? 'gasto' : 'ingreso' }}</h2></div><button class="icon-button" aria-label="Cerrar" @click="modalOpen = false"><X :size="19" /></button></div>
        <form class="form-stack" @submit.prevent="save">
          <div v-if="receiptLoading" class="ocr-progress" role="status"><span><strong>Estamos leyendo tu ticket…</strong><small>Puede tardar unos segundos.</small></span></div>
          <div v-if="receiptResult?.ocrStatus === 'Succeeded'" class="receipt-import-summary">
            <strong>Ticket leído · {{ receiptResult.provider }}</strong>
            <span v-if="receiptResult.items?.length">{{ receiptResult.items.length }} artículos listos para importar</span>
            <span v-else>No se detectaron artículos; puedes añadirlos manualmente.</span>
          </div>
          <div v-if="receiptError" class="form-error" role="alert">{{ receiptError }}</div>
          <label class="field-label" for="amount">{{ isExpense ? 'Total del gasto' : 'Importe' }}</label>
          <div class="amount-input-wrap"><span>{{ currencyCode }}</span><input id="amount" v-model.number="amount" type="number" min="0.01" step="0.01" placeholder="0,00" required /></div>
          <label class="field-label" for="category">Categoría</label>
          <select id="category" v-model="categoryId" class="input" required><option value="" disabled>Elige una categoría</option><option v-for="category in kindCategories" :key="category.id" :value="category.id">{{ category.name }}</option></select>
          <p v-if="!kindCategories.length" class="field-hint">No hay categorías activas. Puedes crear una desde Configuración.</p>
          <label class="field-label" for="transaction-date">Fecha</label><input id="transaction-date" v-model="date" type="date" class="input" required />
          <label class="field-label" for="merchant">{{ isExpense ? 'Comercio' : 'Origen' }} <span class="optional-label">OPCIONAL</span></label>
          <input id="merchant" v-model="merchant" class="input" :placeholder="isExpense ? 'Ej. Supermercado' : 'Ej. Nómina'" maxlength="160" />
          <label v-if="isExpense" class="field-label">Artículos <span class="optional-label">OPCIONAL</span></label>
          <div v-if="isExpense" class="expense-items">
            <div v-for="(item, index) in items" :key="index" class="expense-item-row">
              <input v-model="item.description" class="input" :aria-label="`Descripción del artículo ${index + 1}`" placeholder="Descripción" maxlength="200" />
              <input v-model.number="item.quantity" class="input expense-item-quantity" type="number" min="0.001" step="any" :aria-label="`Cantidad del artículo ${index + 1}`" placeholder="Cantidad" />
              <div class="expense-item-price"><span>{{ currencyCode }}</span><input v-model.number="item.price" type="number" min="0.01" step="0.01" :aria-label="`Precio del artículo ${index + 1}`" placeholder="0,00" /></div>
              <button type="button" class="icon-button" :aria-label="`Quitar artículo ${index + 1}`" @click="items.splice(index, 1)"><Trash2 :size="15" /></button>
            </div>
            <button type="button" class="add-expense-item" @click="addItem"><Plus :size="14" /> Añadir artículo</button>
          </div>
          <label class="field-label" for="description">Descripción <span class="optional-label">OPCIONAL</span></label><textarea id="description" v-model="description" class="input transaction-description" placeholder="Un detalle para recordar" maxlength="2000" />
          <div v-if="error" class="form-error" role="alert">{{ error }}</div>
          <div class="modal-actions"><UiButton variant="secondary" @click="modalOpen = false">Cancelar</UiButton><UiButton type="submit" :disabled="saving || receiptLoading || !kindCategories.length || (receiptResult !== null && receiptResult.ocrStatus !== 'Succeeded')">{{ saving ? 'Guardando…' : receiptResult?.ocrStatus === 'Succeeded' ? 'Importar gasto' : 'Guardar movimiento' }}</UiButton></div>
        </form>
      </section>
    </div>
  </div>
</template>
