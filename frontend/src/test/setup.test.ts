import { createElement } from 'react'
import { render } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'

const originalFetch = globalThis.fetch

describe('jsdom test isolation', () => {
  it('supports DOM rendering, fetch stubs and controlled timers', () => {
    render(createElement('p', null, 'infrastructure verification'))
    expect(document.body.textContent).toContain('infrastructure verification')
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response('{}')))
    vi.useFakeTimers()
    const callback = vi.fn()
    setTimeout(callback, 100)
    vi.advanceTimersByTime(100)
    expect(callback).toHaveBeenCalledOnce()
    setTimeout(callback, 1000)
    vi.stubEnv('PHASE_TWO_CHECK', 'temporary')
  })

  it('leaves no DOM, fetch, environment or fake timers from the previous test', () => {
    expect(document.body.innerHTML).toBe('')
    expect(globalThis.fetch).toBe(originalFetch)
    expect(vi.isFakeTimers()).toBe(false)
    expect(import.meta.env.PHASE_TWO_CHECK).toBeUndefined()
  })
})
