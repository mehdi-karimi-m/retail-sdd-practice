import { act, fireEvent, screen } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { deferred, plan, problem, product, response, validity } from './test/fixtures'
import { click, flush, mountPlan, mountProduct, planHidden, productStays } from './test/ui'

let mono: number
let wall: number
beforeEach(() => {
  mono = 1000; wall = Date.parse('2026-10-08T20:29:00Z')
  vi.useFakeTimers({ toFake: ['setTimeout', 'clearTimeout', 'setInterval', 'clearInterval'] })
  vi.spyOn(performance, 'now').mockImplementation(() => mono)
  vi.spyOn(Date, 'now').mockImplementation(() => wall)
})
function shortPlan() { const p = plan(); p.serverTime = '2026-10-08T20:29:00Z'; return p }
function initialFetch() {
  const mock = vi.fn<typeof fetch>().mockResolvedValueOnce(response(product)).mockResolvedValueOnce(response(shortPlan()))
  vi.stubGlobal('fetch', mock); return mock
}
async function advance(ms: number) {
  mono += ms
  await act(async () => { await vi.advanceTimersByTimeAsync(ms) })
}
async function hide(event = 'visibilitychange') {
  vi.spyOn(document, 'visibilityState', 'get').mockReturnValue('hidden')
  await act(async () => {
    fireEvent(document, new Event('visibilitychange'))
    if (event === 'pagehide') fireEvent(window, new Event('pagehide'))
    if (event === 'freeze') fireEvent(document, new Event('freeze'))
  })
}
async function resume(event = 'visibilitychange') {
  vi.spyOn(document, 'visibilityState', 'get').mockReturnValue('visible')
  await act(async () => {
    if (event === 'focus' || event === 'pageshow') fireEvent(window, new Event(event))
    else fireEvent(document, new Event(event))
  })
  await flush()
}
function expired() {
  planHidden(); productStays()
  expect(screen.getByText('تاریخ این برنامه گذشته است؛ دوباره محاسبه کنید')).toBeTruthy()
  expect(screen.getByRole('button', { name: 'محاسبهٔ دوباره' })).toBeTruthy()
}
function validityPost(mock: ReturnType<typeof initialFetch>, index = 2) {
  const [url, init] = mock.mock.calls[index]!
  expect(url).toBe('/api/product/payment-plan/validity'); expect(init?.method).toBe('POST')
  expect(JSON.parse(String(init?.body))).toEqual({ baseDate: '1405/07/16', expiresAt: '2026-10-08T20:30:00Z' })
}

describe('T017 — server reference, full RTT and independent monotonic clock', () => {
  it.each([-86400000, 86400000])('device wall clock offset %s cannot change expiry', async offset => {
    wall += offset; const mock = initialFetch(); await mountPlan()
    await advance(59999); expect(screen.getByText('پیش‌پرداخت')).toBeTruthy()
    await advance(1); expired(); expect(mock).toHaveBeenCalledTimes(2)
  })
  it.each([-86400000, 86400000])('wall clock jumps %s during display do not affect the budget', async jump => {
    const mock = initialFetch(); await mountPlan(); wall += jump
    await advance(30000); expect(screen.getByText('پیش‌پرداخت')).toBeTruthy()
    await advance(30000); expired(); expect(mock).toHaveBeenCalledTimes(2)
  })
  it.each([[0, 0, 60000], [1000, 1000, 58000], [1900, 100, 58000]])('subtracts full request+response delay %s+%s', async (outbound, inbound, remaining) => {
    const pending = deferred<Response>()
    const mock = vi.fn<typeof fetch>().mockResolvedValueOnce(response(product)).mockImplementationOnce(() => {
      mono += outbound; return pending.promise
    })
    vi.stubGlobal('fetch', mock); await mountProduct(); await click('مشاهدهٔ برنامهٔ پرداخت')
    mono += inbound; await act(async () => { pending.resolve(response(shortPlan())) })
    expect(screen.getByText('پیش‌پرداخت')).toBeTruthy()
    await advance(remaining - 1); expect(screen.getByText('پیش‌پرداخت')).toBeTruthy()
    await advance(1); expired(); expect(mock).toHaveBeenCalledTimes(2)
  })
  it('uses the approved m0=1000,m1=3000 example through m=61000', async () => {
    const pending = deferred<Response>(); const mock = initialFetch()
    mock.mockReset().mockResolvedValueOnce(response(product)).mockImplementationOnce(() => pending.promise)
    await mountProduct(); await click('مشاهدهٔ برنامهٔ پرداخت')
    mono = 3000; await act(async () => { pending.resolve(response(shortPlan())) })
    await advance(30000); expect(mono).toBe(33000); expect(screen.getByText('پیش‌پرداخت')).toBeTruthy()
    await advance(28000); expect(mono).toBe(61000); expired()
  })
  it.each([60000, 65000])('never displays a response delayed %sms', async delay => {
    const pending = deferred<Response>(); const mock = initialFetch()
    mock.mockReset().mockResolvedValueOnce(response(product)).mockImplementationOnce(() => pending.promise)
    await mountProduct(); await click('مشاهدهٔ برنامهٔ پرداخت'); mono += delay
    await act(async () => { pending.resolve(response(shortPlan())) })
    expired(); expect(mock).toHaveBeenCalledTimes(2)
  })
  it.each([2000, 60000])('includes %sms JSON parsing delay in the response budget', async delay => {
    const result = response(shortPlan())
    vi.spyOn(result, 'json').mockImplementation(async () => { mono += delay; return shortPlan() })
    const mock = initialFetch(); mock.mockReset().mockResolvedValueOnce(response(product)).mockResolvedValueOnce(result)
    await mountProduct(); await click('مشاهدهٔ برنامهٔ پرداخت')
    if (delay === 60000) expired()
    else {
      expect(screen.getByText('پیش‌پرداخت')).toBeTruthy()
      await advance(57999); expect(screen.getByText('پیش‌پرداخت')).toBeTruthy()
      await advance(1); expired()
    }
    expect(mock).toHaveBeenCalledTimes(2)
  })
  it.each(['2026-10-08T20:30:00Z', '2026-10-08T20:30:01Z'])('never displays expiry<=serverTime %s', async now => {
    const value = shortPlan(); value.serverTime = now
    const mock = initialFetch(); mock.mockReset().mockResolvedValueOnce(response(product)).mockResolvedValueOnce(response(value))
    await mountProduct(); await click('مشاهدهٔ برنامهٔ پرداخت'); expired()
    expect(mock).toHaveBeenCalledTimes(2)
  })
})

describe('T017 — suspension invalidates old time budget; checking never calculates a new plan', () => {
  it.each(['visibilitychange', 'focus', 'pageshow', 'resume'])('checks with server before redisplay after %s', async event => {
    const mock = initialFetch(); await mountPlan(); const pending = deferred<Response>()
    mock.mockImplementationOnce(() => pending.promise)
    await hide(); await resume(event)
    planHidden(); productStays(); validityPost(mock); expect(mock).toHaveBeenCalledTimes(3)
    // Monotonic time is deliberately stopped even though the server has crossed midnight.
    const result = validity(false); result.serverTime = '2026-10-08T20:30:01Z'
    await act(async () => { pending.resolve(response(result)) })
    expired(); expect(mock).toHaveBeenCalledTimes(3)
  })
  it.each(['pagehide', 'freeze'])('marks %s snapshot untrusted even if performance.now stopped', async event => {
    const mock = initialFetch(); await mountPlan(); const pending = deferred<Response>()
    mock.mockImplementationOnce(() => pending.promise)
    await hide(event); planHidden(); await resume(event === 'freeze' ? 'resume' : 'pageshow')
    planHidden(); validityPost(mock)
    await act(async () => { pending.resolve(response(validity())) })
    expect(screen.getByText('پیش‌پرداخت')).toBeTruthy(); expect(mock).toHaveBeenCalledTimes(3)
  })
  it('coalesces simultaneous visible/focus/pageshow into one pending check', async () => {
    const mock = initialFetch(); await mountPlan(); const pending = deferred<Response>()
    mock.mockImplementationOnce(() => pending.promise); await hide()
    vi.spyOn(document, 'visibilityState', 'get').mockReturnValue('visible')
    await act(async () => {
      fireEvent(document, new Event('visibilitychange')); fireEvent(window, new Event('focus')); fireEvent(window, new Event('pageshow'))
    })
    planHidden(); expect(mock).toHaveBeenCalledTimes(3); validityPost(mock)
  })
  it('successful check restores the same snapshot with a fresh RTT-adjusted budget', async () => {
    const mock = initialFetch(); await mountPlan(); const pending = deferred<Response>()
    mock.mockImplementationOnce(() => pending.promise); await hide(); await resume()
    mono += 2000; await act(async () => { pending.resolve(response(validity())) })
    expect(screen.getByText('۱۴۰۵/۰۷/۱۶')).toBeTruthy(); expect(screen.getByText('۳۰۰٬۰۰۰ تومان')).toBeTruthy()
    await advance(57999); expect(screen.getByText('پیش‌پرداخت')).toBeTruthy()
    await advance(1); expired(); expect(mock).toHaveBeenCalledTimes(3)
  })
  it('true validity delayed beyond its budget cannot restore the old plan', async () => {
    const mock = initialFetch(); await mountPlan(); const pending = deferred<Response>()
    mock.mockImplementationOnce(() => pending.promise); await hide(); await resume()
    mono += 60000; await act(async () => { pending.resolve(response(validity())) })
    expired(); expect(mock).toHaveBeenCalledTimes(3)
  })
  it.each(['network', 'HTTP500', 'JSON'])('failed %s check keeps old plan hidden; retry only POSTs', async mode => {
    const mock = initialFetch(); await mountPlan()
    if (mode === 'network') mock.mockRejectedValueOnce(new Error('offline'))
    else if (mode === 'HTTP500') mock.mockResolvedValueOnce(response(problem, 500))
    else mock.mockResolvedValueOnce(new Response('{'))
    await hide(); await resume(); planHidden(); productStays(); validityPost(mock)
    mock.mockResolvedValueOnce(response(validity()))
    await click('بررسی دوبارهٔ اعتبار')
    validityPost(mock, 3); expect(mock).toHaveBeenCalledTimes(4)
    expect(screen.getByText('پیش‌پرداخت')).toBeTruthy()
  })
  it('a pending plan request that crosses suspension cannot grant ready directly', async () => {
    const pending = deferred<Response>(); const check = deferred<Response>(); const mock = initialFetch()
    mock.mockReset().mockResolvedValueOnce(response(product)).mockImplementationOnce(() => pending.promise).mockImplementationOnce(() => check.promise)
    await mountProduct(); await click('مشاهدهٔ برنامهٔ پرداخت'); await hide(); await resume()
    await act(async () => { pending.resolve(response(shortPlan())) })
    planHidden(); validityPost(mock)
    await act(async () => { check.resolve(response(validity())) })
    expect(screen.getByText('پیش‌پرداخت')).toBeTruthy()
  })
  it('a validity response crossing suspension requires a new active-epoch check', async () => {
    const mock = initialFetch(); await mountPlan(); const old = deferred<Response>(); const fresh = deferred<Response>()
    mock.mockImplementationOnce(() => old.promise).mockImplementationOnce(() => fresh.promise)
    await hide(); await resume(); await hide(); await resume()
    await act(async () => { old.resolve(response(validity())) }); planHidden()
    expect(mock).toHaveBeenCalledTimes(4); validityPost(mock, 3)
    await act(async () => { fresh.resolve(response(validity())) })
    expect(screen.getByText('پیش‌پرداخت')).toBeTruthy()
  })
  it('manual recalculation after expiry GETs a fresh day; stale expiry callback cannot hide it', async () => {
    const mock = initialFetch(); await mountPlan(); await advance(60000); expired()
    const fresh = shortPlan(); fresh.baseDate = '1405/07/17'; fresh.serverTime = '2026-10-09T12:00:00Z'; fresh.expiresAt = '2026-10-09T20:30:00Z'
    fresh.installments.forEach(x => { x.dueDate = x.dueDate.slice(0, -2) + '17' })
    mock.mockResolvedValueOnce(response(fresh)); await click('محاسبهٔ دوباره')
    expect(screen.getByText('۱۴۰۵/۰۷/۱۷')).toBeTruthy()
    await advance(60000); expect(screen.getByText('پیش‌پرداخت')).toBeTruthy()
    expect(mock.mock.calls[2]?.[0]).toBe('/api/product/payment-plan'); expect(mock).toHaveBeenCalledTimes(3)
  })
  it('old validity cannot overwrite or expire a newly requested snapshot', async () => {
    const mock = initialFetch(); await mountPlan(); const old = deferred<Response>()
    mock.mockImplementationOnce(() => old.promise); await hide(); await resume()
    const fresh = shortPlan(); fresh.baseDate = '1405/07/17'; fresh.serverTime = '2026-10-09T12:00:00Z'; fresh.expiresAt = '2026-10-09T20:30:00Z'
    mock.mockResolvedValueOnce(response(fresh)); await click('محاسبهٔ دوباره')
    await act(async () => { old.resolve(response(validity(false))) })
    expect(screen.getByText('۱۴۰۵/۰۷/۱۷')).toBeTruthy(); expect(screen.getByText('پیش‌پرداخت')).toBeTruthy()
    expect(mock).toHaveBeenCalledTimes(4)
  })
  it('replacing a plan before its old timer fires preserves the replacement', async () => {
    const mock = initialFetch(); await mountPlan(); await advance(10000)
    const fresh = plan(); fresh.product.name = 'snapshot تازه'
    mock.mockResolvedValueOnce(response(fresh)); await click(/مشاهدهٔ برنامهٔ پرداخت|محاسبهٔ دوباره/)
    await advance(50000)
    expect(screen.getByText('snapshot تازه')).toBeTruthy()
    expect(screen.getByText('پیش‌پرداخت')).toBeTruthy()
    expect(mock).toHaveBeenCalledTimes(3)
  })
  it('unmount removes timers and lifecycle listeners and aborts outstanding work', async () => {
    const adds = [vi.spyOn(document, 'addEventListener'), vi.spyOn(window, 'addEventListener')]
    const removes = [vi.spyOn(document, 'removeEventListener'), vi.spyOn(window, 'removeEventListener')]
    const mock = initialFetch(); const view = await mountPlan(); const pending = deferred<Response>()
    mock.mockImplementationOnce(() => pending.promise); await hide(); await resume()
    const signal = mock.mock.calls[2]?.[1]?.signal
    view.unmount(); expect(vi.getTimerCount()).toBe(0); expect(signal?.aborted).toBe(true)
    for (const [index, spy] of adds.entries())
      for (const [event, listener] of spy.mock.calls)
        if (['visibilitychange', 'focus', 'pageshow', 'pagehide', 'freeze', 'resume'].includes(event))
          expect(removes[index]!.mock.calls.some(([removedEvent, removedListener]) => removedEvent === event && removedListener === listener)).toBe(true)
    await resume('focus'); await resume('pageshow'); await advance(60000)
    await act(async () => { pending.resolve(response(validity())) })
    expect(mock).toHaveBeenCalledTimes(3); expect(screen.queryByText('پیش‌پرداخت')).toBeNull()
  })
})
