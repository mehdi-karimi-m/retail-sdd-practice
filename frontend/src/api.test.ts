import { beforeEach, describe, expect, it, vi } from 'vitest'
import { checkValidity, fetchPaymentPlan, fetchProduct, isPaymentPlan, isProblemDetails, isProduct, isValidityResponse } from './api'
import { cappedPlan, plan, problem, product, response, validity } from './test/fixtures'

describe('T015 — required-only runtime contract validation', () => {
  it.each(['baseDate', 'serverTime', 'expiresAt', 'downPaymentToman'])('rejects trailing whitespace in %s', field => {
    const value = plan()
    expect(isPaymentPlan({ ...value, [field]: String(value[field as keyof typeof value]) + '\n' })).toBe(false)
  })
  it('honors additionalProperties=false on successful response schemas', () => {
    expect(isProduct({ ...product, extra: 1 })).toBe(false)
    expect(isPaymentPlan({ ...plan(), extra: 1 })).toBe(false)
    expect(isValidityResponse({ ...validity(), installments: [] })).toBe(false)
  })
  it('accepts full valid product, plan, cap and validity response', () => {
    expect(isProduct(product)).toBe(true)
    expect(isPaymentPlan(plan())).toBe(true)
    expect(isPaymentPlan(cappedPlan())).toBe(true)
    expect(isValidityResponse(validity())).toBe(true)
  })
  it.each(['name', 'priceToman', 'currency'])('rejects missing product required field %s', key => {
    const value: Record<string, unknown> = { ...product }; delete value[key]
    expect(isProduct(value)).toBe(false)
  })
  it.each(['product', 'currency', 'calendar', 'timeZone', 'baseDate', 'serverTime', 'expiresAt', 'downPaymentToman', 'installments', 'totalPaymentToman', 'interestToman', 'feeToman'])('rejects missing plan required field %s', key => {
    const value: Record<string, unknown> = { ...plan() }; delete value[key]
    expect(isPaymentPlan(value)).toBe(false)
  })
  it.each([
    ['downPaymentToman', 300000], ['totalPaymentToman', 1000000], ['interestToman', 0],
    ['downPaymentToman', '300000.5'], ['downPaymentToman', '-1'], ['downPaymentToman', '۰'],
    ['feeToman', '1'], ['currency', 'IRR'], ['calendar', 'gregorian'], ['timeZone', 'UTC'],
    ['baseDate', '2026-10-08'], ['serverTime', 'bad'], ['expiresAt', '2026-10-09T00:00:00+03:30'],
    ['serverTime', '2026-02-30T12:00:00Z'], ['expiresAt', '2026-10-08T25:00:00Z'],
  ])('rejects malformed %s', (key, value) => expect(isPaymentPlan({ ...plan(), [key]: value })).toBe(false))
  it.each(['number', 'amountToman', 'dueDate'])('rejects missing installment required %s', key => {
    const value = plan(); const installment: Record<string, unknown> = { ...value.installments[0] }
    delete installment[key]; const raw = { ...value, installments: [installment, ...value.installments.slice(1)] }
    expect(isPaymentPlan(raw)).toBe(false)
  })
  it.each(['missing', 'duplicate', 'number amount', 'bad due date'])('rejects %s installment structure', mode => {
    const value = plan()
    if (mode === 'missing') value.installments.pop()
    if (mode === 'duplicate') value.installments[1]!.number = 1
    if (mode === 'bad due date') value.installments[0]!.dueDate = 'bad'
    const raw = mode === 'number amount' ? { ...value, installments: [{ ...value.installments[0], amountToman: 175000 }, ...value.installments.slice(1)] } : value
    expect(isPaymentPlan(raw)).toBe(false)
  })
  it.each(['baseDate', 'expiresAt', 'serverTime', 'isValid'])('rejects missing validity required %s', key => {
    const value: Record<string, unknown> = { ...validity() }; delete value[key]
    expect(isValidityResponse(value)).toBe(false)
  })
  it.each([{ isValid: 'true' }, { baseDate: 'bad' }, { serverTime: 'bad' }, { expiresAt: 'bad' }])('rejects malformed validity %j', changes => expect(isValidityResponse({ ...validity(), ...changes })).toBe(false))
  it.each([undefined, {}, { priceToman: ['قیمت نامعتبر است'] }])('accepts valid ProblemDetails with optional errors %j', errors => {
    expect(isProblemDetails(errors === undefined ? problem : { ...problem, errors }, 500)).toBe(true)
  })
  it.each([null, [], { priceToman: 'bad' }, { priceToman: [1] }])('rejects errors present with invalid shape %j', errors => expect(isProblemDetails({ ...problem, errors }, 500)).toBe(false))
  it.each(['type', 'title', 'status', 'detail', 'code'])('rejects missing ProblemDetails required %s', key => {
    const value: Record<string, unknown> = { ...problem }; delete value[key]
    expect(isProblemDetails(value, 500)).toBe(false)
  })
  it('rejects status mismatch but accepts allowed problem extensions', () => {
    expect(isProblemDetails(problem, 400)).toBe(false)
    expect(isProblemDetails({ ...problem, traceId: 'extension' }, 500)).toBe(true)
  })
})

describe('T015 — fetch and externally observable errors', () => {
  const fetchMock = vi.fn<typeof fetch>()
  beforeEach(() => { fetchMock.mockReset(); vi.stubGlobal('fetch', fetchMock) })
  it('GETs product without input and forwards cancellation', async () => {
    const signal = new AbortController().signal; fetchMock.mockResolvedValue(response(product))
    await expect(fetchProduct(signal)).resolves.toEqual(product)
    expect(fetchMock).toHaveBeenCalledWith('/api/product', expect.objectContaining({ signal }))
  })
  it('GETs the whole payment plan', async () => {
    fetchMock.mockResolvedValue(response(plan()))
    await expect(fetchPaymentPlan()).resolves.toEqual(plan())
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/product/payment-plan')
  })
  it('POSTs only old snapshot to validity; never GETs a new plan', async () => {
    const snapshot = { baseDate: '1405/07/16', expiresAt: '2026-10-08T20:30:00Z' }
    fetchMock.mockResolvedValue(response(validity()))
    await expect(checkValidity(snapshot)).resolves.toEqual(validity())
    expect(fetchMock).toHaveBeenCalledTimes(1)
    const [url, init] = fetchMock.mock.calls[0]!
    expect(url).toBe('/api/product/payment-plan/validity'); expect(init?.method).toBe('POST')
    expect(JSON.parse(String(init?.body))).toEqual(snapshot)
  })
  it('rejects a validity response referring to a different snapshot', async () => {
    const snapshot = { baseDate: '1405/07/16', expiresAt: '2026-10-08T20:30:00Z' }
    fetchMock.mockResolvedValue(response({ ...validity(), baseDate: '1405/07/17', expiresAt: '2026-10-09T20:30:00Z' }))
    await expect(checkValidity(snapshot)).rejects.toThrow('نمایش برنامه در حال حاضر ممکن نیست')
  })
  it('preserves valid Persian detail when errors is absent', async () => {
    fetchMock.mockResolvedValue(response(problem, 500))
    await expect(fetchPaymentPlan()).rejects.toThrow(problem.detail)
  })
  it.each(['network', 'JSON', 'invalid 200', 'invalid errors', 'status mismatch'])('reports generic error for %s', async mode => {
    if (mode === 'network') fetchMock.mockRejectedValue(new Error('private network detail'))
    else if (mode === 'JSON') fetchMock.mockResolvedValue(new Response('{'))
    else if (mode === 'invalid 200') fetchMock.mockResolvedValue(response({}))
    else if (mode === 'invalid errors') fetchMock.mockResolvedValue(response({ ...problem, errors: null }, 500))
    else fetchMock.mockResolvedValue(response({ ...problem, status: 400 }, 500))
    await expect(fetchPaymentPlan()).rejects.toThrow('نمایش برنامه در حال حاضر ممکن نیست')
  })
})
