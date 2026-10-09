import { act, fireEvent, render, screen } from '@testing-library/react'
import { expect, vi } from 'vitest'
import App from '../App'
import { plan, product, response } from './fixtures'

export async function flush() { await act(async () => { await Promise.resolve() }) }
export async function mountProduct() {
  const view = render(<App />)
  await flush()
  expect(screen.getAllByText('محصول نمونه').length).toBeGreaterThan(0)
  return view
}
export async function click(name: string | RegExp) {
  await act(async () => { fireEvent.click(screen.getByRole('button', { name })) })
  await flush()
}
export async function mountPlan() {
  const view = await mountProduct()
  await click('مشاهدهٔ برنامهٔ پرداخت')
  expect(screen.getByText('پیش‌پرداخت')).toBeTruthy()
  return view
}
export function mockDefaultFetch() {
  const mock = vi.fn<typeof fetch>().mockResolvedValueOnce(response(product)).mockResolvedValueOnce(response(plan()))
  vi.stubGlobal('fetch', mock)
  return mock
}
export function productStays() {
  expect(screen.getAllByText('محصول نمونه').length).toBeGreaterThan(0)
  expect(screen.getAllByText('۱٬۰۰۰٬۰۰۰ تومان').length).toBeGreaterThan(0)
}
export function planHidden() { expect(screen.queryByText('پیش‌پرداخت')).toBeNull() }
