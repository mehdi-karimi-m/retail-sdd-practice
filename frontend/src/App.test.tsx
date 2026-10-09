import { act, fireEvent, screen } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { cappedPlan, deferred, plan, problem, product, response } from './test/fixtures'
import { click, flush, mockDefaultFetch, mountPlan, mountProduct, planHidden, productStays } from './test/ui'

beforeEach(() => {
  vi.spyOn(Date, 'now').mockReturnValue(Date.parse('2026-10-08T12:00:00Z'))
  vi.spyOn(performance, 'now').mockReturnValue(1000)
})

describe('T016 — product, payment display and error recovery', () => {
  it('loads only fixed product, then displays complete plan after user action', async () => {
    const fetchMock = mockDefaultFetch()
    await mountProduct(); productStays()
    expect(fetchMock).toHaveBeenCalledTimes(1)
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/product')
    expect(screen.queryByRole('textbox')).toBeNull()
    expect(screen.queryByText('ورود')).toBeNull()
    await click('مشاهدهٔ برنامهٔ پرداخت')
    expect(fetchMock.mock.calls[1]?.[0]).toBe('/api/product/payment-plan')
    for (const label of ['پیش‌پرداخت', 'قسط اول', 'قسط دوم', 'قسط سوم', 'قسط چهارم', 'مجموع پرداخت', 'بدون سود و کارمزد'])
      expect(screen.getByText(label)).toBeTruthy()
    expect(screen.getByText('۳۰۰٬۰۰۰ تومان')).toBeTruthy()
    expect(screen.getAllByText('۱۷۵٬۰۰۰ تومان')).toHaveLength(4)
    for (const date of ['۱۴۰۵/۰۷/۱۶', '۱۴۰۵/۰۸/۱۶', '۱۴۰۵/۰۹/۱۶', '۱۴۰۵/۱۰/۱۶', '۱۴۰۵/۱۱/۱۶'])
      expect(screen.getByText(date)).toBeTruthy()
    expect(screen.queryByText(/ثبت سفارش|پرداخت واقعی|ورود کاربر/)).toBeNull()
  })
  it('preserves every digit at the inclusive price cap', async () => {
    const value = cappedPlan()
    const fetchMock = vi.fn<typeof fetch>().mockResolvedValueOnce(response(value.product)).mockResolvedValueOnce(response(value))
    vi.stubGlobal('fetch', fetchMock)
    await mountProduct(); await click('مشاهدهٔ برنامهٔ پرداخت')
    expect(screen.getAllByText('۱۰۰٬۰۰۰٬۰۰۰٬۰۰۰ تومان').length).toBeGreaterThan(0)
    expect(screen.getByText('۳۰٬۰۰۰٬۰۰۰٬۰۰۰ تومان')).toBeTruthy()
    expect(screen.getAllByText('۱۷٬۵۰۰٬۰۰۰٬۰۰۰ تومان')).toHaveLength(4)
  })
  it.each(['network', 'problem without errors', 'bad JSON', 'bad structure'])('hides old plan and keeps product on %s', async mode => {
    const fetchMock = mockDefaultFetch(); await mountPlan()
    const pending = deferred<Response>(); fetchMock.mockImplementationOnce(() => pending.promise)
    await click(/مشاهدهٔ برنامهٔ پرداخت|محاسبهٔ دوباره/); planHidden(); productStays()
    await act(async () => {
      if (mode === 'network') pending.reject(new Error('offline'))
      else if (mode === 'problem without errors') pending.resolve(response(problem, 500))
      else if (mode === 'bad JSON') pending.resolve(new Response('{'))
      else pending.resolve(response({}))
    })
    planHidden(); productStays()
    expect(screen.getByRole('button', { name: /تلاش مجدد/ })).toBeTruthy()
    if (mode === 'problem without errors') expect(screen.getByText(problem.detail)).toBeTruthy()
    else expect(screen.getByText('نمایش برنامه در حال حاضر ممکن نیست')).toBeTruthy()
  })
  it('retrying calculation GETs a new plan without refetching a valid product', async () => {
    const fetchMock = mockDefaultFetch(); fetchMock.mockResolvedValueOnce(response(problem, 500))
    await mountPlan(); await click(/مشاهدهٔ برنامهٔ پرداخت|محاسبهٔ دوباره/)
    const fresh = plan(); fresh.baseDate = '1405/07/17'; fresh.serverTime = '2026-10-09T12:00:00Z'; fresh.expiresAt = '2026-10-09T20:30:00Z'
    fresh.installments = [
      { number: 1, amountToman: '175000', dueDate: '1405/08/17' },
      { number: 2, amountToman: '175000', dueDate: '1405/09/17' },
      { number: 3, amountToman: '175000', dueDate: '1405/10/17' },
      { number: 4, amountToman: '175000', dueDate: '1405/11/17' },
    ]
    fetchMock.mockResolvedValueOnce(response(fresh)); await click(/تلاش مجدد/)
    expect(screen.getByText('۱۴۰۵/۰۷/۱۷')).toBeTruthy()
    expect(fetchMock.mock.calls.map(x => x[0])).toEqual(['/api/product', '/api/product/payment-plan', '/api/product/payment-plan', '/api/product/payment-plan'])
  })
  it('offers initial product retry and retries only the failed product endpoint', async () => {
    const fetchMock = vi.fn<typeof fetch>().mockRejectedValueOnce(new Error('offline')).mockResolvedValueOnce(response(product))
    vi.stubGlobal('fetch', fetchMock)
    const { render } = await import('@testing-library/react'); const { default: App } = await import('./App')
    render(<App />); await flush()
    expect(screen.getByRole('button', { name: /تلاش مجدد/ })).toBeTruthy()
    await click(/تلاش مجدد/); productStays()
    expect(fetchMock.mock.calls.map(x => x[0])).toEqual(['/api/product', '/api/product'])
  })
  it('does not let an old response overwrite a newer request', async () => {
    const old = deferred<Response>(); const fresh = plan(); fresh.product.name = 'snapshot تازه'
    const fetchMock = vi.fn<typeof fetch>().mockResolvedValueOnce(response(product)).mockImplementationOnce(() => old.promise).mockResolvedValueOnce(response(fresh))
    vi.stubGlobal('fetch', fetchMock); await mountProduct()
    // Two user requests; the mock deliberately ignores AbortSignal to exercise generation checks.
    await click('مشاهدهٔ برنامهٔ پرداخت'); await click(/مشاهدهٔ برنامهٔ پرداخت|محاسبهٔ دوباره/)
    expect(screen.getByText('snapshot تازه')).toBeTruthy()
    await act(async () => { old.resolve(response(plan())) })
    expect(screen.getByText('snapshot تازه')).toBeTruthy()
  })
})
