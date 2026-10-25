import { computed, ref } from 'vue'
import { defineStore } from 'pinia'
import { householdApi } from '../api/client'
import type { Household } from '../types/finance'

const householdKey = 'casa-clara.household-id'

export const useHouseholdStore = defineStore('household', () => {
  const households = ref<Household[]>([])
  const activeHouseholdId = ref<string | null>(localStorage.getItem(householdKey))
  const activeHousehold = computed(
    () => households.value.find(household => household.id === activeHouseholdId.value) ?? null,
  )

  async function load() {
    const { data } = await householdApi.list()
    households.value = data
    if (!data.some(household => household.id === activeHouseholdId.value)) {
      setActive(data[0]?.id ?? null)
    }
  }

  function setActive(id: string | null) {
    activeHouseholdId.value = id
    if (id) localStorage.setItem(householdKey, id)
    else localStorage.removeItem(householdKey)
  }

  async function create(name: string, currencyCode: string, timeZone: string) {
    const { data } = await householdApi.create(name, currencyCode, timeZone)
    households.value = [...households.value, data]
    setActive(data.id)
    return data
  }

  async function rename(name: string) {
    if (!activeHouseholdId.value) throw new Error('No hay un hogar seleccionado.')
    const { data } = await householdApi.update(activeHouseholdId.value, name)
    households.value = households.value.map(item => item.id === data.id ? data : item)
  }

  return { households, activeHouseholdId, activeHousehold, load, setActive, create, rename }
})
