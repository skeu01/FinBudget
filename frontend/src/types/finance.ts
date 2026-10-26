export interface User {
  id: string
  email: string
  displayName: string
  role: string
}

export interface Household {
  id: string
  name: string
  currencyCode: string
  timeZone: string
  createdAt: string
  archivedAt: string | null
}

export interface Category {
  id: string
  parentCategoryId: string | null
  name: string
  kind: 'Expense' | 'Income' | string
  isActive: boolean
  sortOrder: number
}

export interface Expense {
  id: string
  categoryId: string
  amount: number
  currencyCode: string
  occurredOn: string
  description: string | null
  merchant: string | null
  notes: string | null
  createdByMemberId: string
  items?: ExpenseItem[] | null
}

export interface ExpenseItem {
  description: string
  quantity: number
  price: number
}

export interface Income {
  id: string
  categoryId: string
  amount: number
  currencyCode: string
  occurredOn: string
  description: string | null
  source: string | null
  notes: string | null
  createdByMemberId: string
}

export interface Budget {
  id: string
  categoryId: string
  year: number
  month: number
  amountLimit: number
  alertThresholdPercent: number
  spent: number
  remaining: number
  currencyCode: string
  alertThresholdReached: boolean
  isOverspent: boolean
}

export interface SavingsGoal {
  id: string
  name: string
  description: string | null
  targetAmount: number
  contributedAmount: number
  currencyCode: string
  targetDate: string | null
  status: 'Active' | 'Completed' | 'Cancelled' | string
  createdAt: string
}

export interface ReceiptOcr {
  id: string
  householdId: string
  originalFilename: string
  mediaType: string
  sizeBytes: number
  receiptStatus: string
  provider: string
  ocrStatus: string
  attemptNumber: number
  occurredOn: string | null
  merchant: string | null
  items: ExpenseItem[]
  total: number | null
  vat: number | null
  currency: string
  confidence: number | null
  errorCode: string | null
  uploadedAt: string
}

export interface AuthSession {
  accessToken: string
  accessTokenExpiresAt: string
  user: User
}

export interface ApiProblem {
  title?: string
  detail?: string
  errors?: Record<string, string[]>
}
