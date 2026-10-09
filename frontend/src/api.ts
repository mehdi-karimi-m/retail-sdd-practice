import type { Product, PaymentPlan, ProblemDetails, ValidityRequest, ValidityResponse } from './contracts'
export function isProduct(_value: unknown): _value is Product { throw new Error('T028: product guard not implemented') }
export function isPaymentPlan(_value: unknown): _value is PaymentPlan { throw new Error('T028: plan guard not implemented') }
export function isValidityResponse(_value: unknown): _value is ValidityResponse { throw new Error('T028: validity guard not implemented') }
export function isProblemDetails(_value: unknown, _status: number): _value is ProblemDetails { throw new Error('T028: problem guard not implemented') }
export async function fetchProduct(_signal?: AbortSignal): Promise<Product> { throw new Error('T028: product fetch not implemented') }
export async function fetchPaymentPlan(_signal?: AbortSignal): Promise<PaymentPlan> { throw new Error('T028: plan fetch not implemented') }
export async function checkValidity(_snapshot: ValidityRequest, _signal?: AbortSignal): Promise<ValidityResponse> { throw new Error('T028: validity fetch not implemented') }
