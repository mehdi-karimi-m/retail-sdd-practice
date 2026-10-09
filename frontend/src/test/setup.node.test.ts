import { describe, expect, it, vi } from 'vitest'

const originalFetch = globalThis.fetch

describe('Node test isolation', () => {
  it('uses Node without a DOM and can mock time and fetch', () => {
    expect(typeof document).toBe('undefined')
    vi.useFakeTimers()
    vi.setSystemTime(new Date('2026-10-08T12:00:00Z'))
    expect(Date.now()).toBe(1791460800000)
    vi.stubGlobal('fetch', vi.fn())
  })

  it('restores real timers and fetch', () => {
    expect(vi.isFakeTimers()).toBe(false)
    expect(globalThis.fetch).toBe(originalFetch)
  })
})
