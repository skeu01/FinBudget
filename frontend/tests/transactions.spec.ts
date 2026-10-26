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
  uploadReceipt: vi.fn(),
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
    uploadReceipt: mocks.uploadReceipt,
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
      global: { stubs: { LoadingState: true, EmptyState: true } },
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

  it('reads a receipt and imports its items as individual records', async () => {
    mocks.uploadReceipt.mockResolvedValue({
      data: {
        id: 'receipt-1',
        ocrStatus: 'Succeeded',
        provider: 'Tesseract',
        uploadedAt: '2026-10-25T10:00:00Z',
        merchant: 'Mercadona',
        occurredOn: '2025-05-28',
        total: 14.11,
        vat: 1.45,
        currency: 'EUR',
        confidence: 89,
        items: [
          { description: 'Tortilla', quantity: 1, price: 4.5 },
          { description: 'Pan', quantity: 2, price: 0.76 },
          { description: 'Agua', quantity: 1, price: 1.2 },
        ],
      },
    })
    const wrapper = mount(TransactionsView, {
      props: { kind: 'expense' },
      global: { stubs: { LoadingState: true, EmptyState: true } },
    })
    await flushPromises()

    const input = wrapper.get('input[type="file"]')
    const file = new File(['receipt'], 'ticket.jpg', { type: 'image/jpeg' })
    Object.defineProperty(input.element, 'files', { configurable: true, value: [file] })
    await input.trigger('change')
    await flushPromises()

    expect(mocks.uploadReceipt).toHaveBeenCalledWith('household-1', file)
    expect(wrapper.get('#amount').element.value).toBe('14.11')
    expect(wrapper.get('#merchant').element.value).toBe('Mercadona')
    expect(wrapper.get('[aria-label="Descripción del artículo 1"]').element.value).toBe('Tortilla')
    expect(wrapper.get('[aria-label="Cantidad del artículo 2"]').element.value).toBe('2')
    expect(wrapper.get('[aria-label="Precio del artículo 1"]').element.value).toBe('4.5')
    expect(wrapper.text()).toContain('3 artículos listos para importar')

    await wrapper.get('form').trigger('submit')
    await flushPromises()

    expect(mocks.saveExpense).toHaveBeenCalledWith('household-1', expect.objectContaining({
      amount: 14.11,
      occurredOn: '2025-05-28',
      merchant: 'Mercadona',
      receiptId: 'receipt-1',
      items: [
        { description: 'Tortilla', quantity: 1, price: 4.5 },
        { description: 'Pan', quantity: 2, price: 0.76 },
        { description: 'Agua', quantity: 1, price: 1.2 },
      ],
    }), undefined)
    wrapper.unmount()
  })
})
