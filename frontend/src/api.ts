import type { Product, PaymentPlan, ProblemDetails, ValidityRequest, ValidityResponse } from './contracts'

const genericError = 'نمایش برنامه در حال حاضر ممکن نیست'
const problemCodes = ['invalid_request', 'product_not_found', 'invalid_product', 'calculation_failed',
  'internal_error', 'not_found', 'method_not_allowed']
const money = /^[1-9][0-9]{0,11}$/
const persianDate = /^[0-9]{4}\/(0[1-9]|1[0-2])\/(0[1-9]|[12][0-9]|3[01])$/
const utc = /^[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}(\.[0-9]{1,7})?Z$/
function record(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value)
}
function stringMatch(value: unknown, pattern: RegExp): value is string {
  return typeof value === 'string' && pattern.exec(value)?.[0] === value
}
function utcTimestamp(value: unknown): value is string {
  if (!stringMatch(value, utc)) return false
  const parsed = Date.parse(value)
  return Number.isFinite(parsed) && new Date(parsed).toISOString().slice(0, 19) === value.slice(0, 19)
}
function onlyFields(value: Record<string, unknown>, fields: string[]): boolean {
  return Object.keys(value).every(key => fields.includes(key))
}
export function isProduct(value: unknown): value is Product {
  return record(value) && onlyFields(value, ['name', 'priceToman', 'currency']) && typeof value.name === 'string' && value.name.trim().length > 0 &&
    stringMatch(value.priceToman, money) && value.currency === 'TOMAN'
}
export function isPaymentPlan(value: unknown): value is PaymentPlan {
  return record(value) && onlyFields(value, ['product', 'currency', 'calendar', 'timeZone', 'baseDate', 'serverTime', 'expiresAt', 'downPaymentToman', 'installments', 'totalPaymentToman', 'interestToman', 'feeToman']) && isProduct(value.product) && value.currency === 'TOMAN' &&
    value.calendar === 'persian' && value.timeZone === 'Asia/Tehran' &&
    stringMatch(value.baseDate, persianDate) && utcTimestamp(value.serverTime) && utcTimestamp(value.expiresAt) &&
    stringMatch(value.downPaymentToman, money) && stringMatch(value.totalPaymentToman, money) &&
    value.interestToman === '0' && value.feeToman === '0' && Array.isArray(value.installments) &&
    value.installments.length === 4 && value.installments.every((item: unknown, index: number) =>
      record(item) && onlyFields(item, ['number', 'amountToman', 'dueDate']) && item.number === index + 1 && stringMatch(item.amountToman, money) && stringMatch(item.dueDate, persianDate))
}
export function isValidityResponse(value: unknown): value is ValidityResponse {
  return record(value) && onlyFields(value, ['baseDate', 'serverTime', 'expiresAt', 'isValid']) && stringMatch(value.baseDate, persianDate) && utcTimestamp(value.serverTime) &&
    utcTimestamp(value.expiresAt) && typeof value.isValid === 'boolean'
}
export function isProblemDetails(value: unknown, status: number): value is ProblemDetails {
  return record(value) && typeof value.type === 'string' && typeof value.title === 'string' &&
    value.status === status && typeof value.detail === 'string' && typeof value.code === 'string' && problemCodes.includes(value.code) &&
    (!Object.hasOwn(value, 'errors') || (record(value.errors) && Object.values(value.errors).every(item =>
      Array.isArray(item) && item.every(message => typeof message === 'string'))))
}
class DisplayError extends Error {}
async function request<T>(url: string, guard: (value: unknown) => value is T, init: RequestInit): Promise<T> {
  try {
    const result = await fetch(url, { cache: 'no-store', ...init })
    const value: unknown = await result.json()
    if (!result.ok) throw new DisplayError(isProblemDetails(value, result.status) ? value.detail : genericError)
    if (!guard(value)) throw new DisplayError(genericError)
    return value
  } catch (error) {
    if (error instanceof DisplayError) throw error
    if (error instanceof Error && error.name === 'AbortError') throw error
    throw new DisplayError(genericError)
  }
}
export function fetchProduct(signal?: AbortSignal): Promise<Product> {
  return request('/api/product', isProduct, { signal })
}
export function fetchPaymentPlan(signal?: AbortSignal): Promise<PaymentPlan> {
  return request('/api/product/payment-plan', isPaymentPlan, { signal })
}
export async function checkValidity(snapshot: ValidityRequest, signal?: AbortSignal): Promise<ValidityResponse> {
  const result = await request('/api/product/payment-plan/validity', isValidityResponse, {
    method: 'POST', signal, headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ baseDate: snapshot.baseDate, expiresAt: snapshot.expiresAt }),
  })
  if (result.baseDate !== snapshot.baseDate || result.expiresAt !== snapshot.expiresAt) throw new DisplayError(genericError)
  return result
}
