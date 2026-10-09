const persianDigits = '۰۱۲۳۴۵۶۷۸۹'
function digits(text: string): string {
  return text.replace(/[0-9]/g, digit => persianDigits[digit.charCodeAt(0) - 48]!)
}
export function formatToman(amount: string): string {
  return `${digits(amount.replace(/\B(?=(\d{3})+(?!\d))/g, '٬'))} تومان`
}
export function formatPersianDate(date: string): string { return digits(date) }
