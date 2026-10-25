import { beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'

const mocks = vi.hoisted(() => ({
  budgets: vi.fn(),
  categories: vi.fn(),
  saveBudget: vi.fn(),
}))

vi.mock('../src/api/client', () => ({
  financeApi: {
    budgets: mocks.budgets,
    categories: mocks.categories,
    saveBudget: mocks.saveBudget,
  },
  getApiError: (_error: unknown, fallback: string) => fallback,
}))

vi.mock('../src/stores/household', () => ({
  useHouseholdStore: () => ({
    activeHouseholdId: 'household-1',
    activeHousehold: { currencyCode: 'EUR' },
  }),
}))

import BudgetsView from '../src/views/BudgetsView.vue'

describe('budgets view', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    mocks.budgets.mockResolvedValue({ data: [] })
    mocks.categories.mockResolvedValue({
      data: [{ id: 'food', name: 'Alimentación', kind: 'Expense', isActive: true }],
    })
    mocks.saveBudget.mockResolvedValue({ data: {} })
  })

  it('creates a monthly budget with the selected category and household currency', async () => {
    const wrapper = mount(BudgetsView, {
      global: { stubs: { PageHeader: true, LoadingState: true, EmptyState: true } },
    })
    await flushPromises()
    expect(mocks.budgets).toHaveBeenCalledWith(
      'household-1',
      expect.any(Number),
      expect.any(Number),
    )

    await wrapper.find('.budget-empty button').trigger('click')
    await wrapper.find('#budget-category').setValue('food')
    await wrapper.find('#budget-amount').setValue('350')
    await wrapper.find('form').trigger('submit')
    await flushPromises()

    expect(mocks.saveBudget).toHaveBeenCalledWith('household-1', expect.objectContaining({
      categoryId: 'food',
      amountLimit: 350,
      alertThresholdPercent: 80,
      currencyCode: 'EUR',
      year: expect.any(Number),
      month: expect.any(Number),
    }), undefined)
    wrapper.unmount()
  })
})
