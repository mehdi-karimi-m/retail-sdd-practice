# گزارش اعتبارسنجی Phase 4 — 2026-10-09

T034–T036 تکمیل شدند. مبنای کد شروع بررسی e9f5a42 بود؛ کامیت و پوش انجام نشد.
انتظارهای تست‌های قبلی و قواعد مصوب تغییر نکردند. تنها اصلاح کد این فاز، پذیرش کدهای
ProblemDetails مطابق enum قرارداد1.2.0 است؛ تست رد unknown_error پیش از اصلاح شکست
رفتاری داشت (74 موفق/1 شکست)، سپس موفق شد. قابلیت یا وابستگی جدیدی اضافه نشد.

## T034: مسیر واقعی مرورگر و API

مرورگر واقعی Edge با اتصال extension، روی http://localhost:5173 و بک‌اند Kestrel
در Production روی http://localhost:5080 اجرا شد. Vite درخواست‌های /api را proxy کرد.

| اقدام | نتیجهٔ مشاهده‌شده |
| --- | --- |
| باز کردن صفحه بدون ورود | «محصول نمونه»، ۱٬۰۰۰٬۰۰۰ تومان و دکمهٔ مشاهده؛ بدون جدول قبل از کلیک |
| کلیک مشاهده | تاریخ مبنا۱۴۰۵/۰۷/۱۷، پیش‌پرداخت۳۰۰٬۰۰۰، چهار ردیف۱۷۵٬۰۰۰ تومان، مجموع۱٬۰۰۰٬۰۰۰ و «بدون سود و کارمزد» |
| سررسیدها | ۱۴۰۵/۰۸/۱۷، ۱۴۰۵/۰۹/۱۷، ۱۴۰۵/۱۰/۱۷، ۱۴۰۵/۱۱/۱۷، به ترتیب اول تا چهارم |
| کلیک دوباره | برنامهٔ کامل دوباره نمایش داده شد؛ رفتار درخواست تازه با تست کامپوننت نیز تأیید شد |
| توقف پردازش بک‌اند و کلیک مشاهده | پیام «نمایش برنامه در حال حاضر ممکن نیست» و «تلاش مجدد محاسبه»؛ جدول قبلی حذف و نام/قیمت حفظ شدند |
| راه‌اندازی دوباره و کلیک تلاش مجدد | جدول کامل بازگشت؛ بدون نیاز به reload یا دریافت مجدد محصول از سوی کاربر |
| نصب/ساخت مجدد و reload | صفحهٔ محصول و برنامه با کلیک دوباره صحیح بودند |

تصاویر: [نمایش اولیه](evidence/phase4-payment-plan.png)،
[بک‌اند متوقف](evidence/phase4-backend-offline.png)،
[تلاش مجدد موفق](evidence/phase4-retry-success.png)،
[نمایش نهایی](evidence/phase4-final-program.png).
در خواندن لاگ مرورگر پس از بازیابی، error/warn ثبت‌شده‌ای برنگشت؛ لاگ Vite هنگام توقف
بک‌اند ECONNREFUSED مورد انتظار داشت. این مشاهده ادعای نبود خطا در تمام مرورگرها نیست.

curl برای دو GET و queryهای priceToman=5/baseDate=1400/01/01/foo=bar اجرا شد؛ همگی
200 و no-store، مبلغ‌ها string و جمع پنج پرداخت دقیقاً1000000 بودند. پاسخ query فقط
serverTime متفاوتِ درخواست تازه داشت؛ قیمت، مبنا، سررسید و expiry تغییر نکردند.
POST validity برای snapshot جاری true و برای روز گذشته false داد؛ هیچ برنامه/مبلغی
برنگرداند. ورودی{}، مسیر ناشناخته و روش نامجاز به‌ترتیب400/404/405 با ProblemDetails
فارسی، no-store و بدون errors اختیاری پاسخ دادند. هشت پاسخ با required/type/enum/pattern/
additionalProperties و قیود ساختاری OpenAPI تطبیق داده شدند.
پاسخ‌ها در [شاهد API](evidence/phase4-api.json) ثبت شده‌اند؛ timestampها فقط شاهد همین اجرا هستند.

### انقضا و تعلیق طبق test-design

90 تست بک‌اند و137 تست فرانت‌اند موفق؛ صفر شکست، skipped یا execution error.
هر32 تست App.expiry.test.tsx اجرا شد: ساعت تقویمی یک روز عقب/جلو و پرش ساعت، RTT کامل
و نامتقارن، تأخیر parse، بودجهٔ صفر، انقضای دقیق، visibility/focus/pageshow/resume،
pagehide/freeze، توقف performance.now، درخواست pending عبورکرده از تعلیق، true/false،
شکست شبکه/500/JSON، تلاش مجدد فقط POST، محاسبهٔ دستی فقط GET، پاسخ/timer قدیمی و cleanup.
ساعت واقعی سیستم تغییر نکرد و endpoint/header تستی به برنامه افزوده نشد.
نام و وضعیت تست‌ها در [شاهد تست](evidence/phase4-tests.json) است.

## T035: نصب و ساخت تکرارپذیر

- dotnet restore retail.sln --locked-mode --force موفق؛ ارزیابی دوبارهٔ restore با همان lockها.
- dotnet build retail.sln --no-restore موفق: صفر هشدار و خطا.
- npm --prefix frontend ci موفق:95 بسته نصب و96 بسته audit؛ خروجی نصب صفر vulnerability گزارش کرد.
- npm --prefix frontend run build موفق: TypeScript و Vite؛ پس از اصلاح code نیز دوباره موفق.
- SHA-256 هر سه lock پیش/پس از نصب یکسان بود. npm ls --depth=0 نسخه‌های مستقیم را تأیید کرد.
- SDK10.0.112، ASP.NET/Core runtime10.0.12، Node24.18.0، npm11.16.0؛ نسخه‌های بسته‌ها در research ثبت شدند.
- ابزار/وابستگی جدید لازم نشد؛ gitignore خروجی‌ها را پوشش می‌دهد و git ls-files -ci --exclude-standard خالی بود.

بازیابی NuGet می‌تواند از cache عمومی بسته‌ها استفاده کند؛ این بررسی نصب آفلاین روی
ماشین خالی یا سیستم‌عامل دیگر را اثبات نمی‌کند. فایل‌های قفل و SDK بدون تغییر حفظ شدند.

## T036: نگاشت نهایی پذیرش به کد و شواهد

| الزام | محل پیاده‌سازی | شاهد موفق |
| --- | --- | --- |
| FR-001 | SampleProductSource، پاسخ product/plan، App | ApiTests و App.test؛ مرورگر نام/قیمت و snapshot مرتبط |
| FR-002 | مدل‌های JsonNumberHandling، format.ts | ApiTests، format.test، App.test سقف و مرورگر تومان بدون اعشار |
| FR-003 | PaymentPlanCalculator و App | پنج مثال CalculationTests، ApiTests و برچسب مرورگر |
| FR-004 | checked long، P*30/100 | پنج مثال ثابت، از جمله1000001؛ پیش‌پرداخت رو به پایین |
| FR-005 | remaining/4 و ساخت چهار قسط | CalculationTests و ApiTests؛ سه قسط برابر و مثبت |
| FR-006 | remaining-3*basis | مثال1000010 با قسط چهارم175004؛ باقی‌مانده فقط در قسط چهارم |
| FR-007 | invariant total==price، totalPaymentToman | پنج مثال ثابت، HTTP و جمع دقیق curl؛ نمایش total بدون محاسبهٔ مرورگر |
| FR-008 | PersianCalendar.AddMonths(baseDate,i)، App | چهار ردیف DueDateTests: ماه کوتاه، کبیسه، عبور سال و بازگشت روز۳۰؛ ترتیب UI |
| FR-009 | App، downPayment/total جدا | App.test و مشاهدهٔ مرورگر |
| FR-010 | فقط سه endpoint، منبع حافظه و UI پیش‌نمایش | ApiTests روش/مسیر نامجاز؛ بازبینی کد بدون DB/auth/order/payment/admin |
| FR-011 | مرز decimal?، ProblemDetails، api guards و حالت error | Calculation/Api/Validity/api/App tests؛ قطع واقعی بک‌اند و تلاش مجدد |
| FR-012 | TimeProvider و TehranDateProvider | TehranDateTests snapshot واحد/TZ/نیمه‌شب؛ ApiTests و تاریخ مبنای مرورگر |
| FR-013 | Validate پیش از تبدیل به long | null،5.5،0،-1،1..4،بالاتر سقف رد؛5 و100میلیارد پذیرفته؛ تست HTTP و نمایش سقف |
| FR-014 | SampleProductSource ثابت، بدون فرم قیمت/تاریخ | ApiTests query بی‌اثر، App.test و مرورگر؛ curl قیمت نمونه را حفظ کرد |
| FR-015 | expiry نیمه‌شب سرور، grantBudget و lifecycle | Tehran/Api/Validity tests،32 تست انقضا؛ برنامهٔ تازه فقط با کلیک |
| FR-016 | وضعیت مستقل محصول، generation/abort و retry | App.test و App.expiry؛ قطع/بازیابی واقعی، حفظ محصول و حذف جدول قدیمی |
| FR-017 | serverTime/expiresAt، performance.now، POST snapshot | 32 تست ساعت/تأخیر/تعلیق؛ ValidityApiTests شمارش calculator صفر و true/false؛ curl |

| معیار موفقیت | شاهد و نتیجه |
| --- | --- |
| SC-001 | API، App.test و مرورگر: تمام اجزای برنامه و دقیقاًچهار ردیف |
| SC-002 | پنج مثال مالی ثابت و HTTP: جمع دقیق و سود/کارمزد صفر |
| SC-003 | format.test، سقف در App.test و مرورگر: تومان بدون اعشار و حفظ رقم‌ها |
| SC-004 | چهار مثال ثابت DueDateTests و API: ماه مستقل و سیاست پایان ماه |
| SC-005 | مرورگر: مشاهدهٔ پیش‌پرداخت/چهار سررسید/کل بدون ورود یا عملیات خرید |
| SC-006 | خطاهای source/calculator/HTTP/JSON/شبکه و مرورگر: پیام و عدم نمایش برنامهٔ نامعتبر |
| SC-007 | تست نیمه‌شب و lifecycle: برنامهٔ منقضی پنهان و بدون GET خودکار |
| SC-008 | App/expiry و خطای واقعی: محصول باقی؛ retry محاسبه GET و retry اعتبار POST همان snapshot |
| SC-009 | تست‌های مستقل wall/monotonic و RTT: checking مقدم بر نمایش؛ failure مجوز نمایش نیست |

شش اصل constitution1.2.0 رعایت شدند: معماری کوچک، مشخصات پیش از کد، قواعد در بک‌اند،
پول صحیح و انتقال رشته‌ای، expectedهای ثابت و عدم افزودن قابلیت خارج دامنه.
مهارت‌ها، spec، plan، قرارداد و اصول پروژه تغییر نکردند. hook before/after وجود ندارد؛
.specify/extensions.yml هنگام شروع و پایان موجود نبود. git diff --check موفق است.

## حدود نتیجه و باز کردن برنامه

مسیر اصلی و قطع/بازیابی بک‌اند در Edge واقعی آزموده شد. انقضا/تعلیق در jsdom با fetch
mock و ساعت جعلی و بک‌اند با TestServer/TimeProvider ثابت تأیید شد؛ خواب واقعی OS،
عبور واقعی نیمه‌شب، BFCache واقعی و همهٔ مرورگرها آزموده نشدند. این محدودیت مطابق
روش test-design است، نه ادعای E2E برای آن سناریوها. استقرار production در دامنه نیست.

دو سرویس در پایان بررسی روی5080/5173 فعال‌اند؛ [صفحهٔ برنامه](http://localhost:5173)
باز است. برای اجرا در آینده از ریشهٔ مخزن در دو ترمینال:

```bash
dotnet run --project backend/Retail.Api --urls http://localhost:5080
npm --prefix frontend run dev -- --port 5173 --strictPort
```

لاگ‌های خام در /tmp/retail-phase4-* موقت‌اند. برای بازتولید، فرمان‌های quickstart و
تست‌های مصوب کافی‌اند؛ تصاویر و خلاصه‌های evidence در مخزن قابل ثبت هستند.
