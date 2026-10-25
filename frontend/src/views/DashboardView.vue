<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { ArrowDownLeft, ArrowRight, ArrowUpRight, CalendarDays, ChevronRight, CircleDollarSign, Plus, Sparkles, Target, WalletCards } from '@lucide/vue'
import { RouterLink } from 'vue-router'
import { financeApi, getApiError } from '../api/client'
import { useHouseholdStore } from '../stores/household'
import type { Budget, Category, Expense, Income, SavingsGoal } from '../types/finance'
import { currency, dateLabel, monthLabel } from '../utils/format'
import PageHeader from '../components/PageHeader.vue'
import EmptyState from '../components/EmptyState.vue'
import LoadingState from '../components/LoadingState.vue'
import CategorySpendChart from '../components/CategorySpendChart.vue'
import MonthlyTrendChart from '../components/MonthlyTrendChart.vue'

const householdStore = useHouseholdStore()
const expenses = ref<Expense[]>([])
const incomes = ref<Income[]>([])
const budgets = ref<Budget[]>([])
const categories = ref<Category[]>([])
const savingsGoals = ref<SavingsGoal[]>([])
const loading = ref(true)
const error = ref('')
const selectedMonth = ref(formatMonth(new Date()))
const currencyCode = computed(() => householdStore.activeHousehold?.currencyCode || 'EUR')
const monthStart = computed(() => shiftMonth(selectedMonth.value, -5))
const monthEnd = computed(() => lastDayOfMonth(selectedMonth.value))
const monthTitle = computed(() => {
  const [year, month] = selectedMonth.value.split('-').map(Number)
  return monthLabel(month, year)
})
const currentExpenses = computed(() => expenses.value.filter(item => item.occurredOn.startsWith(selectedMonth.value)))
const currentIncomes = computed(() => incomes.value.filter(item => item.occurredOn.startsWith(selectedMonth.value)))
const totalExpenses = computed(() => currentExpenses.value.reduce((sum, item) => sum + item.amount, 0))
const totalIncomes = computed(() => currentIncomes.value.reduce((sum, item) => sum + item.amount, 0))
const balance = computed(() => totalIncomes.value - totalExpenses.value)
const categorySpend = computed(() => {
  const totals = new Map<string, number>()
  for (const expense of currentExpenses.value) totals.set(expense.categoryId, (totals.get(expense.categoryId) || 0) + expense.amount)
  return [...totals.entries()].map(([id, amount]) => ({
    id,
    name: categories.value.find(category => category.id === id)?.name || 'Sin categoría',
    amount,
  })).sort((a, b) => b.amount - a.amount)
})
const categoryChartItems = computed(() => {
  const topCategories = categorySpend.value.slice(0, 5)
  const otherAmount = categorySpend.value.slice(5).reduce((sum, item) => sum + item.amount, 0)
  return otherAmount > 0
    ? [...topCategories, { id: 'others', name: 'Otros', amount: otherAmount }]
    : topCategories
})
const categoryColors = ['#6f9f78', '#d49a70', '#9284aa', '#79a8a5', '#cb8f90', '#bac4a0']
const trend = computed(() => {
  const periods = Array.from({ length: 6 }, (_, index) => shiftMonth(selectedMonth.value, index - 5).slice(0, 7))
  const values = periods.map(period => {
    const monthExpenses = expenses.value.filter(item => item.occurredOn.startsWith(period))
      .reduce((sum, item) => sum + item.amount, 0)
    const monthIncomes = incomes.value.filter(item => item.occurredOn.startsWith(period))
      .reduce((sum, item) => sum + item.amount, 0)
    const [year, month] = period.split('-').map(Number)
    const label = monthLabel(month, year).slice(0, 3)
    return { label: `${label[0].toUpperCase()}${label.slice(1)}`, expenses: monthExpenses, incomes: monthIncomes, balance: monthIncomes - monthExpenses }
  })
  return {
    labels: values.map(item => item.label),
    expenses: values.map(item => item.expenses),
    incomes: values.map(item => item.incomes),
    balances: values.map(item => item.balance),
  }
})
const activeSavingsGoals = computed(() => savingsGoals.value
  .filter(goal => goal.status.toLowerCase() === 'active')
  .sort((first, second) => {
    if (!first.targetDate) return 1
    if (!second.targetDate) return -1
    return first.targetDate.localeCompare(second.targetDate)
  }))
const recentTransactions = computed(() => [
  ...currentExpenses.value.map(item => ({ id: item.id, title: item.merchant || item.description || 'Gasto', categoryId: item.categoryId, date: item.occurredOn, amount: -item.amount, type: 'expense' as const })),
  ...currentIncomes.value.map(item => ({ id: item.id, title: item.source || item.description || 'Ingreso', categoryId: item.categoryId, date: item.occurredOn, amount: item.amount, type: 'income' as const })),
].sort((a, b) => b.date.localeCompare(a.date)).slice(0, 5))
const budgetTotal = computed(() => budgets.value.reduce((sum, item) => sum + item.amountLimit, 0))
const budgetSpent = computed(() => budgets.value.reduce((sum, item) => sum + item.spent, 0))
const budgetConsumedPercent = computed(() => budgetTotal.value ? Math.round((budgetSpent.value / budgetTotal.value) * 100) : 0)

let loadSequence = 0
async function load() {
  const sequence = ++loadSequence
  const id = householdStore.activeHouseholdId
  if (!id) {
    expenses.value = []
    incomes.value = []
    budgets.value = []
    categories.value = []
    savingsGoals.value = []
    loading.value = false
    return
  }
  loading.value = true
  error.value = ''
  try {
    const [expenseData, incomeData, budgetData, categoryData, goalsData] = await Promise.all([
      financeApi.expenses(id, monthStart.value, monthEnd.value),
      financeApi.incomes(id, monthStart.value, monthEnd.value),
      financeApi.budgets(id, Number(selectedMonth.value.slice(0, 4)), Number(selectedMonth.value.slice(5, 7))),
      financeApi.categories(id),
      financeApi.savingsGoals(id),
    ])
    if (sequence !== loadSequence) return
    expenses.value = expenseData.data
    incomes.value = incomeData.data
    budgets.value = budgetData.data
    categories.value = categoryData.data
    savingsGoals.value = goalsData.data
  } catch (cause) {
    if (sequence !== loadSequence) return
    error.value = getApiError(cause, 'No se ha podido cargar el resumen.')
  } finally {
    if (sequence === loadSequence) loading.value = false
  }
}

watch([() => householdStore.activeHouseholdId, selectedMonth], load)
onMounted(load)

function goalPercent(goal: SavingsGoal) {
  return Math.min(100, Math.max(0, goal.targetAmount ? goal.contributedAmount / goal.targetAmount * 100 : 0))
}

function formatMonth(date: Date) {
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}`
}

function shiftMonth(month: string, offset: number) {
  const [year, monthNumber] = month.split('-').map(Number)
  const date = new Date(year, monthNumber - 1 + offset, 1)
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-01`
}

function lastDayOfMonth(month: string) {
  const [year, monthNumber] = month.split('-').map(Number)
  const date = new Date(year, monthNumber, 0)
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`
}
</script>

<template>
  <div>
    <PageHeader eyebrow="TU HOGAR, EN EQUILIBRIO" title="Resumen" description="Un vistazo tranquilo a cómo se mueve tu dinero.">
      <label class="month-picker"><CalendarDays :size="16" /><input v-model="selectedMonth" type="month" aria-label="Mes del resumen" /></label>
      <RouterLink to="/gastos" class="button-link"><Plus :size="16" /> Añadir movimiento</RouterLink>
    </PageHeader>

    <div v-if="!householdStore.activeHousehold" class="setup-card">
      <div class="setup-card__symbol"><Sparkles :size="23" /></div>
      <div><p class="eyebrow">EL PRIMER PASO</p><h2>Empieza por crear tu hogar</h2><p>Un espacio compartido para ordenar gastos, ingresos y planes en familia.</p></div>
      <RouterLink to="/configuracion" class="button-link">Crear mi hogar <ArrowRight :size="16" /></RouterLink>
    </div>

    <template v-else>
      <div class="welcome-strip"><span class="welcome-strip__icon"><Sparkles :size="17" /></span><span>Este es el resumen de <strong>{{ monthTitle }}</strong> en <strong>{{ householdStore.activeHousehold.name }}</strong>.</span><span class="welcome-strip__right">Todo en un mismo lugar <CircleDollarSign :size="16" /></span></div>
      <div v-if="error" class="inline-error" role="alert">{{ error }}</div>
      <LoadingState v-if="loading" />
      <template v-else>
        <section class="metric-grid">
          <article class="metric-card metric-card--balance">
            <div class="metric-card__top"><span>Balance del mes</span><span class="metric-icon metric-icon--dark"><CircleDollarSign :size="18" /></span></div>
            <strong class="metric-card__value">{{ currency(balance, currencyCode) }}</strong>
            <span class="metric-card__foot"><span class="positive-dot" /> Ingresos menos gastos</span>
            <div class="balance-decoration">↗</div>
          </article>
          <article class="metric-card">
            <div class="metric-card__top"><span>Ingresos</span><span class="metric-icon metric-icon--green"><ArrowDownLeft :size="17" /></span></div>
            <strong class="metric-card__value">{{ currency(totalIncomes, currencyCode) }}</strong>
            <span class="metric-card__foot">En {{ monthTitle }}</span>
          </article>
          <article class="metric-card">
            <div class="metric-card__top"><span>Gastos</span><span class="metric-icon metric-icon--peach"><ArrowUpRight :size="17" /></span></div>
            <strong class="metric-card__value">{{ currency(totalExpenses, currencyCode) }}</strong>
            <span class="metric-card__foot">En {{ monthTitle }}</span>
          </article>
          <article class="metric-card">
            <div class="metric-card__top"><span>Presupuestos usados</span><span class="metric-icon metric-icon--lavender"><WalletCards :size="17" /></span></div>
            <strong class="metric-card__value">{{ budgetConsumedPercent }}<small>%</small></strong>
            <span class="metric-card__foot">{{ currency(Math.max(0, budgetTotal - budgetSpent), currencyCode) }} disponibles</span>
          </article>
        </section>

        <section class="dashboard-grid">
          <article class="surface trend-card">
            <div class="surface-heading">
              <div><p class="eyebrow">TENDENCIA FINANCIERA</p><h2>Evolución mensual</h2></div>
              <span class="chart-period"><CalendarDays :size="14" /> Últimos 6 meses</span>
            </div>
            <div class="chart-legend" aria-label="Leyenda del gráfico">
              <span><i class="chart-legend__dot chart-legend__dot--income" />Ingresos</span>
              <span><i class="chart-legend__dot chart-legend__dot--expense" />Gastos</span>
              <span><i class="chart-legend__dot chart-legend__dot--balance" />Balance</span>
            </div>
            <MonthlyTrendChart
              :labels="trend.labels"
              :incomes="trend.incomes"
              :expenses="trend.expenses"
              :balances="trend.balances"
              :currency-code="currencyCode"
            />
          </article>

          <article class="surface spend-card">
            <div class="surface-heading"><div><p class="eyebrow">DÓNDE VA</p><h2>Gastos por categoría</h2></div><RouterLink to="/gastos" class="text-link">Ver gastos <ChevronRight :size="15" /></RouterLink></div>
            <div v-if="!categorySpend.length" class="compact-empty">Todavía no hay gastos registrados este mes.</div>
            <div v-else class="category-breakdown">
              <CategorySpendChart
                :labels="categoryChartItems.map(item => item.name)"
                :values="categoryChartItems.map(item => item.amount)"
                :colors="categoryColors"
                :total="totalExpenses"
                :currency-code="currencyCode"
              />
              <div class="category-legend">
                <div v-for="(item, index) in categoryChartItems" :key="item.id" class="category-legend__row">
                  <span class="category-legend__name"><i :style="{ backgroundColor: categoryColors[index] }" />{{ item.name }}</span>
                  <strong>{{ currency(item.amount, currencyCode) }}</strong>
                  <small>{{ totalExpenses ? Math.round(item.amount / totalExpenses * 100) : 0 }}%</small>
                </div>
              </div>
            </div>
            <div class="spend-foot"><span class="spend-foot__mark">✳</span> Distribución de gastos en {{ monthTitle }}.</div>
          </article>

          <article class="surface budget-card">
            <div class="surface-heading"><div><p class="eyebrow">PLAN DEL MES</p><h2>Presupuesto consumido</h2></div><span class="surface-heading__icon"><WalletCards :size="17" /></span></div>
            <template v-if="budgets.length">
              <div class="budget-total"><strong>{{ budgetConsumedPercent }}%</strong><span> · {{ currency(budgetSpent, currencyCode) }} de {{ currency(budgetTotal, currencyCode) }}</span></div>
              <div class="progress-track progress-track--large"><span class="progress-fill" :class="budgetConsumedPercent >= 100 ? 'progress-fill--danger' : budgetConsumedPercent >= 80 ? 'progress-fill--warning' : 'progress-fill--budget'" :style="{ width: `${Math.min(100, budgetConsumedPercent)}%` }" /></div>
              <div class="budget-caption"><span>Gastado</span><strong>{{ currency(Math.max(0, budgetTotal - budgetSpent), currencyCode) }} restante</strong></div>
              <div class="budget-lines">
                <div v-for="item in budgets.slice(0, 3)" :key="item.id" class="budget-line">
                  <span><i :class="['budget-status', { 'budget-status--warn': item.alertThresholdReached, 'budget-status--danger': item.isOverspent }]" />{{ categories.find(category => category.id === item.categoryId)?.name || 'Categoría' }}</span>
                  <strong>{{ Math.round(item.amountLimit ? item.spent / item.amountLimit * 100 : 0) }}%</strong>
                </div>
              </div>
            </template>
            <EmptyState v-else title="Sin presupuestos todavía" description="Define un límite mensual para planear con más tranquilidad." />
            <RouterLink to="/presupuestos" class="budget-link">Ir a presupuestos <ArrowRight :size="15" /></RouterLink>
          </article>

          <article class="surface activity-card">
            <div class="surface-heading"><div><p class="eyebrow">LO ÚLTIMO</p><h2>Actividad reciente</h2></div><RouterLink to="/gastos" class="text-link">Ver todo <ChevronRight :size="15" /></RouterLink></div>
            <div v-if="!recentTransactions.length" class="compact-empty">Tus movimientos aparecerán aquí cuando registres alguno.</div>
            <div v-else class="activity-list">
              <div v-for="item in recentTransactions" :key="item.id" class="activity-row">
                <span class="activity-icon" :class="item.type === 'income' ? 'activity-icon--income' : 'activity-icon--expense'">
                  <ArrowDownLeft v-if="item.type === 'income'" :size="17" /><ArrowUpRight v-else :size="17" />
                </span>
                <span class="activity-row__info"><strong>{{ item.title }}</strong><small>{{ categories.find(category => category.id === item.categoryId)?.name || 'Sin categoría' }} · {{ dateLabel(item.date) }}</small></span>
                <strong :class="item.amount > 0 ? 'amount-positive' : 'amount-negative'">{{ item.amount > 0 ? '+' : '−' }}{{ currency(Math.abs(item.amount), currencyCode) }}</strong>
              </div>
            </div>
          </article>

          <article class="surface savings-card">
            <div class="surface-heading">
              <div><p class="eyebrow">PASO A PASO, EN FAMILIA</p><h2>Objetivos de ahorro</h2></div>
              <span class="surface-heading__icon savings-heading-icon"><Target :size="17" /></span>
            </div>
            <div v-if="!activeSavingsGoals.length" class="savings-empty">
              <EmptyState title="Todo gran plan empieza con una meta" description="Crea un objetivo de ahorro y sigue su progreso en familia." />
            </div>
            <div v-else class="savings-goal-list">
              <div v-for="goal in activeSavingsGoals.slice(0, 3)" :key="goal.id" class="savings-goal">
                <div class="savings-goal__top">
                  <span class="savings-goal__icon"><Target :size="16" /></span>
                  <span class="savings-goal__name"><strong>{{ goal.name }}</strong><small>{{ goal.targetDate ? `Meta · ${dateLabel(goal.targetDate)}` : 'Objetivo de ahorro' }}</small></span>
                  <span class="savings-goal__percent">{{ Math.round(goalPercent(goal)) }}%</span>
                </div>
                <div class="progress-track"><span class="progress-fill progress-fill--savings" :style="{ width: `${goalPercent(goal)}%` }" /></div>
                <div class="savings-goal__amounts"><span>{{ currency(goal.contributedAmount, goal.currencyCode) }} ahorrados</span><strong>{{ currency(goal.targetAmount, goal.currencyCode) }}</strong></div>
              </div>
            </div>
          </article>
        </section>
      </template>
    </template>
  </div>
</template>
