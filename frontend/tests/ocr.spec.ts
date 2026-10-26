import { beforeEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'

const mocks = vi.hoisted(() => ({ uploadReceipt: vi.fn() }))

vi.mock('../src/api/client', () => ({
  financeApi: { uploadReceipt: mocks.uploadReceipt },
  getApiError: (_error: unknown, fallback: string) => fallback,
}))

vi.mock('../src/stores/household', () => ({
  useHouseholdStore: () => ({ activeHouseholdId: 'household-1' }),
}))

import OcrView from '../src/views/OcrView.vue'

describe('receipt OCR view', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    vi.stubGlobal('URL', {
      createObjectURL: vi.fn(() => 'blob:receipt'),
      revokeObjectURL: vi.fn(),
    })
  })

  it('uploads a supported receipt and presents the extracted fields', async () => {
    mocks.uploadReceipt.mockResolvedValue({
      data: {
        id: 'receipt-1',
        ocrStatus: 'Succeeded',
        provider: 'Tesseract',
        uploadedAt: '2026-10-25T10:00:00Z',
        merchant: 'Mercado',
        items: [],
        occurredOn: '2026-10-25',
        total: 25.5,
        vat: 2.1,
        currency: 'EUR',
        confidence: 95,
      },
    })
    const wrapper = mount(OcrView, {
      global: { stubs: { PageHeader: true } },
    })
    const input = wrapper.find('input[type="file"]')
    const file = new File(['receipt image'], 'receipt.png', { type: 'image/png' })
    Object.defineProperty(input.element, 'files', { configurable: true, value: [file] })
    await input.trigger('change')
    await wrapper.find('.ocr-upload-footer button').trigger('click')
    await flushPromises()

    expect(mocks.uploadReceipt).toHaveBeenCalledWith('household-1', file)
    expect(wrapper.text()).toContain('Lectura completada')
    expect(wrapper.text()).toContain('Mercado')
    expect(wrapper.text()).toContain('25,50')
    expect(wrapper.text()).toContain('2,10')
    wrapper.unmount()
  })

  it('rejects unsupported file types before calling the OCR API', async () => {
    const wrapper = mount(OcrView, {
      global: { stubs: { PageHeader: true } },
    })
    const input = wrapper.find('input[type="file"]')
    const file = new File(['not an image'], 'receipt.txt', { type: 'text/plain' })
    Object.defineProperty(input.element, 'files', { configurable: true, value: [file] })
    await input.trigger('change')

    expect(wrapper.get('[role="alert"]').text()).toContain('JPG o PNG')
    expect(mocks.uploadReceipt).not.toHaveBeenCalled()
    wrapper.unmount()
  })
})
