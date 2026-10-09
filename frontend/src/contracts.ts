// Contract shapes; api.ts validates responses before they enter UI state.
export interface Product { name: string; priceToman: string; currency: 'TOMAN' }
export interface Installment { number: number; amountToman: string; dueDate: string }
export interface PaymentPlan {
  product: Product; currency: 'TOMAN'; calendar: 'persian'; timeZone: 'Asia/Tehran';
  baseDate: string; serverTime: string; expiresAt: string; downPaymentToman: string;
  installments: Installment[]; totalPaymentToman: string; interestToman: string; feeToman: string;
}
export interface ProblemDetails {
  type: string; title: string; status: number; detail: string; code: string;
  errors?: Record<string, string[]>;
}
export interface ValidityRequest { baseDate: string; expiresAt: string }
export interface ValidityResponse extends ValidityRequest { serverTime: string; isValid: boolean }
