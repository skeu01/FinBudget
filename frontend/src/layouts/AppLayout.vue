<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { RouterLink, RouterView, useRoute, useRouter } from 'vue-router'
import {
  ArrowDownLeft, ArrowUpRight, Bell, ChevronDown, CircleHelp, House,
  LayoutDashboard, LogOut, Menu, ReceiptText, Settings2, Sparkles, WalletCards, X,
} from '@lucide/vue'
import { useAuthStore } from '../stores/auth'
import { useHouseholdStore } from '../stores/household'
import { getApiError } from '../api/client'
import { initials } from '../utils/format'
import UiButton from '../components/UiButton.vue'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const household = useHouseholdStore()
const sidebarOpen = ref(false)
const toast = ref('')
const showHouseholdForm = ref(false)
const householdName = ref('')
const householdLoading = ref(false)
const greeting = computed(() => {
  const hour = new Date().getHours()
  return hour < 12 ? 'Buenos días' : hour < 19 ? 'Buenas tardes' : 'Buenas noches'
})
const navigation = [
  { label: 'GENERAL', items: [
    { name: 'dashboard', label: 'Resumen', icon: LayoutDashboard },
    { name: 'expenses', label: 'Gastos', icon: ArrowUpRight },
    { name: 'incomes', label: 'Ingresos', icon: ArrowDownLeft },
    { name: 'budgets', label: 'Presupuestos', icon: WalletCards },
  ] },
  { label: 'HERRAMIENTAS', items: [
    { name: 'ocr', label: 'Leer un ticket', icon: ReceiptText, badge: 'OCR' },
    { name: 'settings', label: 'Configuración', icon: Settings2 },
  ] },
]

watch(() => route.fullPath, () => { sidebarOpen.value = false })

async function loadHouseholds() {
  if (!auth.isAuthenticated) return
  try {
    await household.load()
  } catch (error) {
    toast.value = getApiError(error, 'No se han podido cargar tus hogares.')
  }
}

watch(() => auth.isAuthenticated, loadHouseholds, { immediate: true })

function selectHousehold(event: Event) {
  household.setActive((event.target as HTMLSelectElement).value || null)
}

async function createHousehold() {
  if (!householdName.value.trim()) return
  householdLoading.value = true
  toast.value = ''
  try {
    await household.create(householdName.value.trim(), 'EUR', 'Europe/Madrid')
    showHouseholdForm.value = false
    householdName.value = ''
  } catch (error) {
    toast.value = getApiError(error, 'No se pudo crear el hogar.')
  } finally {
    householdLoading.value = false
  }
}

async function logout() {
  await auth.logout()
  household.setActive(null)
  await router.push('/login')
}
</script>

<template>
  <div class="app-shell">
    <button v-if="sidebarOpen" class="mobile-backdrop" aria-label="Cerrar menú" @click="sidebarOpen = false" />
    <aside class="sidebar" :class="{ 'sidebar--open': sidebarOpen }">
      <RouterLink to="/" class="brand">
        <span class="brand__mark"><House :size="19" stroke-width="2.4" /></span>
        <span class="brand__name">Fin<span>Budget</span><small>FINANZAS EN FAMILIA</small></span>
      </RouterLink>

      <div class="household-picker">
        <span class="picker-label">TU ESPACIO</span>
        <label class="household-select">
          <span class="household-avatar"><House :size="16" /></span>
          <select :value="household.activeHouseholdId ?? ''" aria-label="Selecciona un hogar" @change="selectHousehold">
            <option value="" disabled>{{ household.households.length ? 'Selecciona un hogar' : 'Crea tu primer hogar' }}</option>
            <option v-for="item in household.households" :key="item.id" :value="item.id">{{ item.name }}</option>
          </select>
          <ChevronDown :size="15" class="select-chevron" />
        </label>
        <button class="add-household" @click="showHouseholdForm = !showHouseholdForm">+ Añadir hogar</button>
        <form v-if="showHouseholdForm" class="household-form" @submit.prevent="createHousehold">
          <input v-model="householdName" class="input input--small" placeholder="Nombre del hogar" maxlength="120" required />
          <UiButton type="submit" size="sm" :disabled="householdLoading">Crear hogar</UiButton>
        </form>
      </div>

      <nav class="nav-groups" aria-label="Navegación principal">
        <div v-for="group in navigation" :key="group.label" class="nav-group">
          <p class="nav-group__label">{{ group.label }}</p>
          <RouterLink v-for="item in group.items" :key="item.name" :to="{ name: item.name }" class="nav-link" active-class="nav-link--active">
            <component :is="item.icon" :size="18" stroke-width="1.8" />
            <span>{{ item.label }}</span>
            <span v-if="'badge' in item" class="nav-badge">{{ item.badge }}</span>
          </RouterLink>
        </div>
      </nav>

      <div class="sidebar-bottom">
        <div class="tip-card">
          <div class="tip-card__icon"><Sparkles :size="16" /></div>
          <strong>Un pequeño hábito</strong>
          <p>Revisar tus gastos 5 minutos a la semana puede marcar la diferencia.</p>
        </div>
        <button class="profile-card" @click="router.push({ name: 'settings' })">
          <span class="avatar">{{ initials(auth.user?.displayName || auth.user?.email || 'U') }}</span>
          <span class="profile-card__text"><strong>{{ auth.user?.displayName || 'Mi cuenta' }}</strong><small>{{ auth.user?.email }}</small></span>
          <ChevronDown :size="15" />
        </button>
      </div>
    </aside>

    <div class="main-column">
      <header class="topbar">
        <button class="icon-button mobile-menu" aria-label="Abrir menú" @click="sidebarOpen = true"><Menu :size="20" /></button>
        <div class="breadcrumb"><span>Mi espacio</span><span class="breadcrumb__slash">/</span><strong>{{ route.meta.title || navigation.flatMap(group => group.items).find(item => item.name === route.name)?.label || 'Resumen' }}</strong></div>
        <div class="topbar__right">
          <span class="topbar-greeting">{{ greeting }}, <strong>{{ auth.user?.displayName?.split(' ')[0] || 'qué bueno verte' }}</strong></span>
          <button class="icon-button notification-button" aria-label="Notificaciones"><Bell :size="18" /><i /></button>
          <button class="icon-button help-button" aria-label="Ayuda"><CircleHelp :size="18" /></button>
          <button class="topbar-logout" title="Cerrar sesión" aria-label="Cerrar sesión" @click="logout"><LogOut :size="17" /></button>
        </div>
      </header>

      <main class="main-content">
        <div v-if="toast" class="toast" role="alert"><span>{{ toast }}</span><button aria-label="Cerrar aviso" @click="toast = ''"><X :size="16" /></button></div>
        <RouterView />
      </main>
      <footer class="app-footer"><span>Hecho para vivir con más calma.</span><span>FinBudget <span class="footer-dot">·</span> {{ new Date().getFullYear() }}</span></footer>
    </div>
  </div>
</template>
