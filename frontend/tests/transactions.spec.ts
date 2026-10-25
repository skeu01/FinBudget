import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'

const mocks = vi.hoisted(() => ({
  expenses: vi.fn(),
  incomes: vi.fn(),
  categories: vi.fn(),
  saveExpense: vi.fn(),
  saveIncome: vi.fn(),
  deleteExpense: vi.fn(),
  deleteIncome: vi.fn(),
}))

vi.mock('../src/api/client', () => ({
  financeApi: {
    expenses: mocks.expenses,
    incomes: mocks.incomes,
    categories: mocks.categories,
    saveExpense: mocks.saveExpense,
    saveIncome: mocks.saveIncome,
    deleteExpense: mocks.deleteExpense,
    deleteIncome: mocks.deleteIncome,
  },
  getApiError: (_error: unknown, fallback: string) => fallback,
}))

vi.mock('../src/stores/household', () => ({
  useHouseholdStore: () => ({
    activeHouseholdId: 'household-1',
    activeHousehold: { currencyCode: 'EUR' },
  }),
}))

import TransactionsView from '../src/views/TransactionsView.vue'

describe('expense transactions view', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    mocks.expenses.mockResolvedValue({ data: [] })
    mocks.categories.mockResolvedValue({
      data: [{ id: 'food', name: 'Alimentación', kind: 'Expense', isActive: true }],
    })
    mocks.saveExpense.mockResolvedValue({ data: {} })
  })

  afterEach(() => vi.restoreAllMocks())

  it('loads expenses and submits a new expense to the selected household', async () => {
    const wrapper = mount(TransactionsView, {
      props: { kind: 'expense' },
      global: { stubs: { PageHeader: true, LoadingState: true, EmptyState: true } },
    })
    await flushPromises()

    expect(mocks.expenses).toHaveBeenCalledWith(
      'household-1',
      expect.stringMatching(/^\d{4}-\d{2}-01$/),
      expect.stringMatching(/^\d{4}-\d{2}-\d{2}$/),
    )
    await wrapper.find('.table-empty button').trigger('click')
    await wrapper.find('#amount').setValue('42.50')
    await wrapper.find('#category').setValue('food')
    await wrapper.find('#merchant').setValue('Mercado')
    await wrapper.find('#description').setValue('Compra semanal')
    await wrapper.find('form').trigger('submit')
    await flushPromises()

    expect(mocks.saveExpense).toHaveBeenCalledWith('household-1', expect.objectContaining({
      categoryId: 'food',
      amount: 42.5,
      currencyCode: 'EUR',
      merchant: 'Mercado',
      description: 'Compra semanal',
    }), undefined)
    wrapper.unmount()
  })
})
