import { useEffect, useRef, useState } from 'react'
import { flushSync } from 'react-dom'
import { checkValidity, fetchPaymentPlan, fetchProduct } from './api'
import type { PaymentPlan, Product } from './contracts'
import { formatPersianDate, formatToman } from './format'
import './App.css'

type PlanStatus = 'idle' | 'loading' | 'ready' | 'error' | 'expired' | 'checking' | 'verification-error'
interface Control {
  mounted: boolean; active: boolean; epoch: number; generation: number;
  snapshot: PaymentPlan | null; deadline: number; status: PlanStatus;
  timer?: ReturnType<typeof setTimeout>; productAbort?: AbortController;
  planAbort?: AbortController; checkAbort?: AbortController; checkEpoch: number;
}
const genericError = 'نمایش برنامه در حال حاضر ممکن نیست'
const expiryMessage = 'تاریخ این برنامه گذشته است؛ دوباره محاسبه کنید'
const installmentLabels = ['قسط اول', 'قسط دوم', 'قسط سوم', 'قسط چهارم']

export default function App() {
  const [product, setProduct] = useState<Product | null>(null)
  const [productStatus, setProductStatus] = useState<'loading' | 'ready' | 'error'>('loading')
  const [productError, setProductError] = useState('')
  const [status, setStatus] = useState<PlanStatus>('idle')
  const [message, setMessage] = useState('')
  const [visiblePlan, setVisiblePlan] = useState<PaymentPlan | null>(null)
  const control = useRef<Control>({ mounted: false, active: true, epoch: 0, generation: 0,
    snapshot: null, deadline: 0, status: 'idle', checkEpoch: -1 })

  function transition(next: PlanStatus, error = '') {
    const c = control.current
    c.status = next; setStatus(next); setMessage(error)
    setVisiblePlan(next === 'ready' ? c.snapshot : null)
  }
  function clearTimer() {
    clearTimeout(control.current.timer); control.current.timer = undefined
  }
  function grantBudget(serverTime: string, expiresAt: string, m0: number, m1: number) {
    const c = control.current
    const remaining = Math.max(0, Date.parse(expiresAt) - Date.parse(serverTime) - (m1 - m0))
    clearTimer()
    if (remaining === 0) { transition('expired'); return }
    c.deadline = m1 + remaining
    transition('ready')
    const generation = c.generation
    const tick = () => {
      if (!c.mounted || !c.active || generation !== c.generation || c.status !== 'ready') return
      const rest = c.deadline - performance.now()
      if (rest <= 0) { c.timer = undefined; transition('expired') }
      else c.timer = setTimeout(tick, rest)
    }
    c.timer = setTimeout(tick, remaining)
  }
  async function loadProduct() {
    const c = control.current
    c.productAbort?.abort()
    const abort = new AbortController(); c.productAbort = abort
    setProductStatus('loading'); setProductError('')
    try {
      const value = await fetchProduct(abort.signal)
      if (!c.mounted || abort.signal.aborted) return
      setProduct(value); setProductStatus('ready')
    } catch (error) {
      if (!c.mounted || abort.signal.aborted) return
      setProductStatus('error'); setProductError(error instanceof Error ? error.message : genericError)
    }
  }
  async function verify() {
    const c = control.current
    const snapshot = c.snapshot
    if (!c.mounted || !c.active || !snapshot) return
    if (c.checkAbort && !c.checkAbort.signal.aborted && c.checkEpoch === c.epoch) return
    c.checkAbort?.abort(); clearTimer()
    const abort = new AbortController(); c.checkAbort = abort
    const epoch = c.epoch; c.checkEpoch = epoch
    const generation = c.generation; const m0 = performance.now()
    transition('checking')
    try {
      const result = await checkValidity({ baseDate: snapshot.baseDate, expiresAt: snapshot.expiresAt }, abort.signal)
      const m1 = performance.now()
      if (!c.mounted || abort.signal.aborted || generation !== c.generation || snapshot !== c.snapshot) return
      if (!c.active || epoch !== c.epoch) return
      if (!result.isValid) { transition('expired'); return }
      grantBudget(result.serverTime, result.expiresAt, m0, m1)
    } catch (error) {
      if (c.mounted && c.active && epoch === c.epoch && generation === c.generation && !abort.signal.aborted)
        transition('verification-error', error instanceof Error ? error.message : genericError)
    } finally {
      if (c.checkAbort === abort) c.checkAbort = undefined
    }
  }
  async function loadPlan() {
    const c = control.current
    c.generation++; const generation = c.generation
    c.planAbort?.abort(); c.checkAbort?.abort(); c.checkAbort = undefined; clearTimer()
    c.snapshot = null; transition('loading')
    const abort = new AbortController(); c.planAbort = abort
    const epoch = c.epoch; const m0 = performance.now()
    try {
      const value = await fetchPaymentPlan(abort.signal)
      const m1 = performance.now()
      if (!c.mounted || generation !== c.generation || abort.signal.aborted) return
      c.snapshot = value; setProduct(value.product)
      if (!c.active || epoch !== c.epoch) {
        transition('checking')
        if (c.active) void verify()
      } else grantBudget(value.serverTime, value.expiresAt, m0, m1)
    } catch (error) {
      if (c.mounted && generation === c.generation && !abort.signal.aborted)
        transition('error', error instanceof Error ? error.message : genericError)
    } finally {
      if (c.planAbort === abort) c.planAbort = undefined
    }
  }

  useEffect(() => {
    const c = control.current
    c.mounted = true; c.active = document.visibilityState !== 'hidden'
    void loadProduct()
    const suspend = () => {
      c.active = false; c.epoch++; clearTimer()
      c.checkAbort?.abort(); c.checkAbort = undefined
      if (c.snapshot) flushSync(() => transition('checking'))
    }
    const restore = () => {
      if (!c.mounted) return
      // A restore without a preceding hidden event must also distrust the prior epoch.
      if (c.active && !(c.checkAbort && c.checkEpoch === c.epoch)) c.epoch++
      c.active = true
      if (c.snapshot) flushSync(() => { void verify() })
    }
    const visibility = () => { if (document.visibilityState === 'hidden') suspend(); else restore() }
    document.addEventListener('visibilitychange', visibility)
    document.addEventListener('freeze', suspend)
    document.addEventListener('resume', restore)
    window.addEventListener('pagehide', suspend)
    window.addEventListener('pageshow', restore)
    window.addEventListener('focus', restore)
    return () => {
      c.mounted = false; c.generation++; clearTimer()
      c.productAbort?.abort(); c.planAbort?.abort(); c.checkAbort?.abort()
      document.removeEventListener('visibilitychange', visibility)
      document.removeEventListener('freeze', suspend)
      document.removeEventListener('resume', restore)
      window.removeEventListener('pagehide', suspend)
      window.removeEventListener('pageshow', restore)
      window.removeEventListener('focus', restore)
    }
  }, [])

  return <main className="page" dir="rtl">
    <header><p className="eyebrow">پیش‌نمایش پرداخت</p><h1>فروش اقساطی</h1></header>
    <section className="card" aria-label="محصول">
      {productStatus === 'loading' && <p role="status">در حال دریافت اطلاعات محصول…</p>}
      {product && <><h2>{product.name}</h2><p className="price">{formatToman(product.priceToman)}</p></>}
      {productStatus === 'error' && <><p role="alert">{productError}</p><button onClick={() => void loadProduct()}>تلاش مجدد دریافت محصول</button></>}
      {product && <button onClick={() => void loadPlan()}>{['expired', 'checking', 'verification-error'].includes(status) ? 'محاسبهٔ دوباره' : 'مشاهدهٔ برنامهٔ پرداخت'}</button>}
    </section>
    {status === 'loading' && <p role="status">در حال دریافت برنامهٔ پرداخت…</p>}
    {status === 'checking' && <p role="status">در حال بررسی اعتبار برنامه…</p>}
    {status === 'expired' && <p className="notice" role="status">{expiryMessage}</p>}
    {(status === 'error' || status === 'verification-error') && <section className="notice">
      <p role="alert">{message}</p>
      <button onClick={() => { if (status === 'verification-error') void verify(); else void loadPlan() }}>
        {status === 'verification-error' ? 'بررسی دوبارهٔ اعتبار' : 'تلاش مجدد محاسبه'}
      </button>
    </section>}
    {status === 'ready' && visiblePlan && <section className="card" aria-label="برنامهٔ پرداخت">
      <h2>برنامهٔ پرداخت</h2><p>بدون سود و کارمزد</p>
      <dl><dt>تاریخ مبنای پیش‌نمایش</dt><dd>{formatPersianDate(visiblePlan.baseDate)}</dd>
        <dt>پیش‌پرداخت</dt><dd>{formatToman(visiblePlan.downPaymentToman)}</dd></dl>
      <div className="table-scroll"><table><thead><tr><th>قسط</th><th>مبلغ</th><th>سررسید</th></tr></thead>
        <tbody>{visiblePlan.installments.map(item => <tr key={item.number}><th scope="row">{installmentLabels[item.number - 1]}</th>
          <td>{formatToman(item.amountToman)}</td><td>{formatPersianDate(item.dueDate)}</td></tr>)}</tbody></table></div>
      <dl className="total"><dt>مجموع پرداخت</dt><dd>{formatToman(visiblePlan.totalPaymentToman)}</dd></dl>
    </section>}
  </main>
}
