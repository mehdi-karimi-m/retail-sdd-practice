import type { PaymentPlan, ProblemDetails, Product, ValidityResponse } from '../contracts'
export const product: Product = { name: 'محصول نمونه', priceToman: '1000000', currency: 'TOMAN' }
export function plan(): PaymentPlan {
  return {
    product: { ...product }, currency: 'TOMAN', calendar: 'persian', timeZone: 'Asia/Tehran',
    baseDate: '1405/07/16', serverTime: '2026-10-08T12:00:00Z', expiresAt: '2026-10-08T20:30:00Z',
    downPaymentToman: '300000', totalPaymentToman: '1000000', interestToman: '0', feeToman: '0',
    installments: [
      { number: 1, amountToman: '175000', dueDate: '1405/08/16' },
      { number: 2, amountToman: '175000', dueDate: '1405/09/16' },
      { number: 3, amountToman: '175000', dueDate: '1405/10/16' },
      { number: 4, amountToman: '175000', dueDate: '1405/11/16' },
    ],
  }
}
export function cappedPlan(): PaymentPlan {
  const value = plan()
  value.product.priceToman = '100000000000'; value.totalPaymentToman = '100000000000'
  value.downPaymentToman = '30000000000'
  value.installments.forEach(x => { x.amountToman = '17500000000' })
  return value
}
export const problem: ProblemDetails = {
  type: 'about:blank', title: 'اطلاعات محصول معتبر نیست', status: 500,
  detail: 'قیمت خارج از دامنهٔ مجاز است.', code: 'invalid_product',
}
export function validity(isValid = true): ValidityResponse {
  return { baseDate: '1405/07/16', serverTime: '2026-10-08T20:29:00Z', expiresAt: '2026-10-08T20:30:00Z', isValid }
}
export function response(value: unknown, status = 200): Response {
  return new Response(JSON.stringify(value), { status, headers: { 'Content-Type': status >= 400 ? 'application/problem+json' : 'application/json' } })
}
export function deferred<T>() {
  let resolve!: (value: T) => void
  let reject!: (reason: unknown) => void
  const promise = new Promise<T>((a, b) => { resolve = a; reject = b })
  return { promise, resolve, reject }
}
