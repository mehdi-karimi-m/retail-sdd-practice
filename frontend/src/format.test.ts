import { describe, expect, it } from 'vitest'
import { formatPersianDate, formatToman } from './format'

describe('T014 — exact display of every amount from the five fixed financial examples', () => {
  it.each([
    ['1000000', '۱٬۰۰۰٬۰۰۰'], ['300000', '۳۰۰٬۰۰۰'], ['175000', '۱۷۵٬۰۰۰'],
    ['1000010', '۱٬۰۰۰٬۰۱۰'], ['300003', '۳۰۰٬۰۰۳'], ['175001', '۱۷۵٬۰۰۱'], ['175004', '۱۷۵٬۰۰۴'],
    ['1000001', '۱٬۰۰۰٬۰۰۱'], ['5', '۵'], ['1', '۱'], ['0', '۰'],
    ['100000000000', '۱۰۰٬۰۰۰٬۰۰۰٬۰۰۰'], ['30000000000', '۳۰٬۰۰۰٬۰۰۰٬۰۰۰'], ['17500000000', '۱۷٬۵۰۰٬۰۰۰٬۰۰۰'],
  ])('preserves %s as integer TOMAN', (raw, display) => {
    expect(formatToman(raw)).toBe(`${display} تومان`)
  })
  it.each([
    ['1405/07/16', '۱۴۰۵/۰۷/۱۶'], ['1404/12/29', '۱۴۰۴/۱۲/۲۹'], ['1403/12/30', '۱۴۰۳/۱۲/۳۰'],
  ])('only changes display digits of %s', (raw, display) => {
    expect(formatPersianDate(raw)).toBe(display)
  })
})
