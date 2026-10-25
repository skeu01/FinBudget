<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { Check, House, Plus, UserRound, WalletCards } from '@lucide/vue'
import { financeApi, getApiError } from '../api/client'
import { useAuthStore } from '../stores/auth'
import { useHouseholdStore } from '../stores/household'
import type { Category } from '../types/finance'
import { initials } from '../utils/format'
import PageHeader from '../components/PageHeader.vue'
import UiButton from '../components/UiButton.vue'

const auth = useAuthStore()
const householdStore = useHouseholdStore()
const displayName = ref(auth.user?.displayName || '')
const householdName = ref('')
const newHouseholdName = ref('')
const currencyCode = ref('EUR')
const timeZone = ref(Intl.DateTimeFormat().resolvedOptions().timeZone || 'Europe/Madrid')
const newCategory = ref('')
const categoryKind = ref<'Expense' | 'Income'>('Expense')
const categories = ref<Category[]>([])
const loading = ref(false)
const saving = ref(false)
const error = ref('')
const success = ref('')
const activeHousehold = computed(() => householdStore.activeHousehold)
const activeCategories = computed(() => categories.value.filter(category => category.isActive))

onMounted(async () => {
  displayName.value = auth.user?.displayName || ''
  householdName.value = activeHousehold.value?.name || ''
  currencyCode.value = activeHousehold.value?.currencyCode || 'EUR'
  timeZone.value = activeHousehold.value?.timeZone || timeZone.value
  await loadCategories()
})

async function loadCategories() {
  if (!householdStore.activeHouseholdId) { categories.value = []; return }
  loading.value = true
  try {
    const { data } = await financeApi.categories(householdStore.activeHouseholdId)
    categories.value = data
  } catch (cause) {
    error.value = getApiError(cause, 'No se pudieron cargar las categorías.')
  } finally {
    loading.value = false
  }
}

async function saveProfile() {
  saving.value = true
  error.value = ''
  success.value = ''
  try {
    await auth.updateProfile(displayName.value.trim())
    success.value = 'Tu perfil se ha actualizado.'
  } catch (cause) {
    error.value = getApiError(cause, 'No se pudo guardar tu perfil.')
  } finally {
    saving.value = false
  }
}

async function saveHousehold() {
  if (!householdStore.activeHouseholdId) return
  saving.value = true
  error.value = ''
  success.value = ''
  try {
    await householdStore.rename(householdName.value.trim())
    success.value = 'El hogar se ha actualizado.'
  } catch (cause) {
    error.value = getApiError(cause, 'No se pudo guardar el hogar.')
  } finally {
    saving.value = false
  }
}

async function createHousehold() {
  if (!newHouseholdName.value.trim()) return
  saving.value = true
  error.value = ''
  try {
    await householdStore.create(newHouseholdName.value.trim(), currencyCode.value, timeZone.value)
    householdName.value = newHouseholdName.value.trim()
    newHouseholdName.value = ''
    categories.value = []
    success.value = 'Tu hogar está listo. ¡Bienvenido!'
  } catch (cause) {
    error.value = getApiError(cause, 'No se pudo crear el hogar.')
  } finally {
    saving.value = false
  }
}

async function addCategory() {
  if (!householdStore.activeHouseholdId || !newCategory.value.trim()) return
  saving.value = true
  error.value = ''
  try {
    const { data } = await financeApi.createCategory(householdStore.activeHouseholdId, newCategory.value.trim(), categoryKind.value)
    categories.value = [...categories.value, data]
    newCategory.value = ''
    success.value = 'La categoría se ha creado.'
  } catch (cause) {
    error.value = getApiError(cause, 'No se pudo crear la categoría.')
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <div>
    <PageHeader eyebrow="TU ESPACIO, A TU MANERA" title="Configuración" description="Gestiona tu perfil, hogar y categorías." />
    <div v-if="error" class="inline-error" role="alert">{{ error }}</div>
    <div v-if="success" class="inline-success" role="status"><Check :size="16" />{{ success }}</div>

    <div class="settings-layout">
      <aside class="settings-nav surface">
        <a href="#profile" class="settings-nav__item settings-nav__item--active"><UserRound :size="17" /> Mi perfil</a>
        <a href="#household" class="settings-nav__item"><House :size="17" /> Mi hogar</a>
        <a href="#categories" class="settings-nav__item"><WalletCards :size="17" /> Categorías</a>
      </aside>
      <div class="settings-content">
        <section id="profile" class="surface settings-card">
          <div class="settings-card__heading"><span class="settings-icon"><UserRound :size="18" /></span><div><h2>Mi perfil</h2><p>Tu información personal.</p></div></div>
          <div class="profile-summary"><span class="avatar avatar--large">{{ initials(auth.user?.displayName || 'U') }}</span><span><strong>{{ auth.user?.email }}</strong><small>Cuenta personal · {{ auth.user?.role }}</small></span></div>
          <form class="settings-form" @submit.prevent="saveProfile">
            <label class="field-label" for="display-name">Nombre visible</label><input id="display-name" v-model="displayName" class="input" maxlength="120" required />
            <label class="field-label" for="email-address">Correo electrónico</label><input id="email-address" :value="auth.user?.email" class="input input--readonly" type="email" readonly />
            <div class="settings-actions"><UiButton type="submit" :disabled="saving">{{ saving ? 'Guardando…' : 'Guardar cambios' }}</UiButton></div>
          </form>
        </section>

        <section id="household" class="surface settings-card">
          <div class="settings-card__heading"><span class="settings-icon settings-icon--peach"><House :size="18" /></span><div><h2>Mi hogar</h2><p>El espacio que compartes con los tuyos.</p></div></div>
          <form v-if="activeHousehold" class="settings-form" @submit.prevent="saveHousehold">
            <label class="field-label" for="home-name">Nombre del hogar</label><input id="home-name" v-model="householdName" class="input" maxlength="120" required />
            <div class="settings-fields-row"><div><label class="field-label" for="home-currency">Moneda</label><select id="home-currency" v-model="currencyCode" class="input input--readonly" disabled><option>{{ currencyCode }}</option></select></div><div><label class="field-label" for="home-zone">Zona horaria</label><input id="home-zone" :value="activeHousehold.timeZone" class="input input--readonly" readonly /></div></div>
            <p class="field-hint">Para cambiar de hogar, utiliza el selector de la barra lateral.</p>
            <div class="settings-actions"><UiButton type="submit" variant="secondary" :disabled="saving">{{ saving ? 'Guardando…' : 'Actualizar hogar' }}</UiButton></div>
          </form>
          <form v-else class="settings-form" @submit.prevent="createHousehold">
            <p class="no-household-note">Todavía no formas parte de un hogar. Crea uno para empezar a compartir tus finanzas.</p>
            <label class="field-label" for="new-home-name">Nombre del hogar</label><input id="new-home-name" v-model="newHouseholdName" class="input" placeholder="Ej. Casa de los García" maxlength="120" required />
            <div class="settings-fields-row"><div><label class="field-label" for="new-currency">Moneda</label><select id="new-currency" v-model="currencyCode" class="input"><option value="EUR">Euro · EUR</option><option value="USD">Dólar estadounidense · USD</option><option value="MXN">Peso mexicano · MXN</option><option value="COP">Peso colombiano · COP</option></select></div><div><label class="field-label" for="new-zone">Zona horaria</label><input id="new-zone" v-model="timeZone" class="input" required maxlength="64" /></div></div>
            <div class="settings-actions"><UiButton type="submit" :disabled="saving">{{ saving ? 'Creando…' : 'Crear hogar' }}</UiButton></div>
          </form>
        </section>

        <section id="categories" class="surface settings-card">
          <div class="settings-card__heading"><span class="settings-icon settings-icon--lavender"><WalletCards :size="18" /></span><div><h2>Categorías</h2><p>Organiza los movimientos de tu hogar.</p></div></div>
          <div v-if="activeHousehold" class="category-settings">
            <div v-if="activeCategories.length" class="category-chip-list">
              <span v-for="category in activeCategories" :key="category.id" class="category-chip"><i :class="category.kind.toLowerCase() === 'income' ? 'category-chip__dot--income' : 'category-chip__dot--expense'" />{{ category.name }}<small>{{ category.kind.toLowerCase() === 'income' ? 'Ingreso' : 'Gasto' }}</small></span>
            </div>
            <p v-else class="field-hint">{{ loading ? 'Cargando categorías…' : 'Todavía no hay categorías. Añade la primera aquí abajo.' }}</p>
            <form class="category-create-form" @submit.prevent="addCategory">
              <input v-model="newCategory" class="input" placeholder="Nombre de la categoría" maxlength="80" required aria-label="Nombre de nueva categoría" />
              <select v-model="categoryKind" class="input category-kind-select" aria-label="Tipo de categoría"><option value="Expense">Gasto</option><option value="Income">Ingreso</option></select>
              <UiButton type="submit" variant="secondary" :disabled="saving"><Plus :size="15" /> Añadir</UiButton>
            </form>
            <p class="field-hint">Las categorías desactivadas no se pueden gestionar desde esta pantalla.</p>
          </div>
          <p v-else class="no-household-note">Crea o selecciona un hogar antes de gestionar categorías.</p>
        </section>
      </div>
    </div>
  </div>
</template>
