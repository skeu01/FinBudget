import axios, { AxiosError, type InternalAxiosRequestConfig } from 'axios'
import type {
  AuthSession,
  ApiProblem,
  Budget,
  Category,
  Expense,
  Household,
  Income,
  ReceiptOcr,
  SavingsGoal,
  User,
} from '../types/finance'

const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || '/api/v1',
  withCredentials: true,
  headers: { Accept: 'application/json' },
})

let accessToken: string | null = null
let refreshPromise: Promise<string | null> | null = null

export function setAccessToken(token: string | null) {
  accessToken = token
}

api.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  if (accessToken) config.headers.Authorization = `Bearer ${accessToken}`
  return config
})

api.interceptors.response.use(
  response => response,
  async (error: AxiosError<ApiProblem>) => {
    const request = error.config as (InternalAxiosRequestConfig & { _retried?: boolean }) | undefined
    if (error.response?.status !== 401 || !request || request._retried || request.url?.includes('/auth/')) {
      return Promise.reject(error)
    }

    request._retried = true
    refreshPromise ??= axios
      .post<AuthSession>(`${api.defaults.baseURL}/auth/refresh`, null, { withCredentials: true })
      .then(({ data }) => {
        setAccessToken(data.accessToken)
        return data.accessToken
      })
      .catch(() => {
        setAccessToken(null)
        window.dispatchEvent(new Event('session-expired'))
        return null
      })
      .finally(() => {
        refreshPromise = null
      })

    const token = await refreshPromise
    if (!token) return Promise.reject(error)
    request.headers.Authorization = `Bearer ${token}`
    return api(request)
  },
)

export const authApi = {
  login: (email: string, password: string) =>
    api.post<AuthSession>('/auth/login', { email, password }),
  register: (email: string, displayName: string, password: string) =>
    api.post<User>('/auth/register', { email, displayName, password }),
  refresh: () => api.post<AuthSession>('/auth/refresh'),
  logout: () => api.post('/auth/logout'),
  me: () => api.get<User>('/auth/me'),
}

export const householdApi = {
  list: () => api.get<Household[]>('/households'),
  create: (name: string, currencyCode: string, timeZone: string) =>
    api.post<Household>('/households', { name, currencyCode, timeZone }),
  update: (id: string, name: string) => api.put<Household>(`/households/${id}`, { name }),
}

export function householdPath(householdId: string, resource: string) {
  return `/households/${householdId}/${resource}`
}

export const financeApi = {
  categories: (householdId: string) => api.get<Category[]>(householdPath(householdId, 'categories')),
  expenses: (householdId: string, from?: string, to?: string) =>
    api.get<Expense[]>(householdPath(householdId, 'expenses'), { params: { from, to } }),
  incomes: (householdId: string, from?: string, to?: string) =>
    api.get<Income[]>(householdPath(householdId, 'incomes'), { params: { from, to } }),
  budgets: (householdId: string, year: number, month: number) =>
    api.get<Budget[]>(householdPath(householdId, 'budgets'), { params: { year, month } }),
  savingsGoals: (householdId: string) =>
    api.get<SavingsGoal[]>(householdPath(householdId, 'savings-goals')),
  saveExpense: (householdId: string, payload: Omit<Expense, 'id' | 'createdByMemberId'>, id?: string) =>
    id
      ? api.put<Expense>(`${householdPath(householdId, 'expenses')}/${id}`, payload)
      : api.post<Expense>(householdPath(householdId, 'expenses'), payload),
  deleteExpense: (householdId: string, id: string) =>
    api.delete(`${householdPath(householdId, 'expenses')}/${id}`),
  saveIncome: (householdId: string, payload: Omit<Income, 'id' | 'createdByMemberId'>, id?: string) =>
    id
      ? api.put<Income>(`${householdPath(householdId, 'incomes')}/${id}`, payload)
      : api.post<Income>(householdPath(householdId, 'incomes'), payload),
  deleteIncome: (householdId: string, id: string) =>
    api.delete(`${householdPath(householdId, 'incomes')}/${id}`),
  saveBudget: (householdId: string, payload: Omit<Budget, 'id' | 'spent' | 'remaining' | 'alertThresholdReached' | 'isOverspent'>, id?: string) =>
    id
      ? api.put<Budget>(`${householdPath(householdId, 'budgets')}/${id}`, payload)
      : api.post<Budget>(householdPath(householdId, 'budgets'), payload),
  deleteBudget: (householdId: string, id: string) =>
    api.delete(`${householdPath(householdId, 'budgets')}/${id}`),
  createCategory: (householdId: string, name: string, kind: 'Expense' | 'Income') =>
    api.post<Category>(householdPath(householdId, 'categories'), { name, kind, sortOrder: 0 }),
  uploadReceipt: (householdId: string, file: File) => {
    const body = new FormData()
    body.append('file', file)
    return api.post<ReceiptOcr>(householdPath(householdId, 'receipts'), body)
  },
}

export const userApi = {
  me: () => api.get<User>('/users/me'),
  update: (displayName: string) => api.put<User>('/users/me', { displayName }),
}

export function getApiError(error: unknown, fallback = 'No se pudo completar la operación.') {
  if (axios.isAxiosError<ApiProblem>(error)) {
    const problem = error.response?.data
    const validationMessage = problem?.errors ? Object.values(problem.errors).flat()[0] : undefined
    return validationMessage || problem?.detail || problem?.title || error.message || fallback
  }
  return error instanceof Error ? error.message : fallback
}

export type { AxiosError }
