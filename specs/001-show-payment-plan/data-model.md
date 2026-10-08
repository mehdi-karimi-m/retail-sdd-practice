# Data Model: محصول و برنامهٔ پرداخت

این مدل در حافظه است؛ جدول دیتابیس، شناسهٔ سفارش، تاریخچه یا عملیات نوشتن ندارد.

## Product

| فیلد | مدل داخلی | قرارداد |
| --- | --- | --- |
| name | string غیرخالی | string |
| priceToman | long پس از اعتبارسنجی decimal دقیق | رشتهٔ عدد صحیح ۵ تا ۱۰۰ میلیارد |

منبع فقط‌خواندنی یک محصول نمونه با نام «محصول نمونه» و قیمت ۱٬۰۰۰٬۰۰۰ تومان ارائه می‌کند.
در تست می‌توان همین منبع را با مقدار معتبر، نامعتبر یا محصول مفقود جایگزین کرد؛ API عمومی
برای تغییر آن وجود ندارد. ورودی مرز اعتبارسنجی decimal? است؛ کسری و null قبل از تبدیل
به long رد می‌شوند، نه truncate. parser عمومی رشته/NaN/Infinity اضافه نشود.

## PaymentPlan

| فیلد | نوع داخلی/قرارداد | قاعده |
| --- | --- | --- |
| product | Product | snapshot محصول مرتبط با این محاسبه |
| currency | string | TOMAN |
| calendar | string | persian |
| timeZone | string | Asia/Tehran |
| baseDate | تاریخ بدون زمان / string | روز درخواست در تهران، YYYY/MM/DD |
| expiresAt | DateTimeOffset / string | نیمه‌شب شروع روز بعد تهران به UTC، RFC3339 با Z |
| downPaymentToman | long / string | floor(P × 30 / 100) |
| installments | چهار Installment | شماره‌های ۱ تا ۴ و ترتیب صعودی |
| totalPaymentToman | long / string | دقیقاً P |
| interestToman | long / string | صفر |
| feeToman | long / string | صفر |

## Installment

| فیلد | نوع | قاعده |
| --- | --- | --- |
| number | int / JSON integer | ۱، ۲، ۳ یا ۴ |
| amountToman | long / string | مثبت؛ سه قسط اول برابر، چهارم تسویه |
| dueDate | تاریخ بدون زمان / string | شمسی، YYYY/MM/DD؛ بدون offset یا ساعت |

## Validation and invariants

- ورودی decimal? ابتدا از نظر null، کسری و دامنه بررسی شود؛ P از نوع long و
  5 ≤ P ≤ 100000000000 است. مدل منبع غیرعددی را نمایندگی نمی‌کند؛ parser عمومی وجود ندارد.
- D = (P × 30) div 100؛ R = P − D؛ B = R div 4؛ قسط چهارم L = R − 3B.
- تقسیم صحیح برای مقادیر مثبت همان floor است؛ تمام عملیات long در checked انجام شوند.
- مبلغ‌ها: D > 0، B > 0، L > 0؛ D + B + B + B + L = P؛ 0 ≤ L − B ≤ 3.
- زمان UTC یک بار خوانده شود؛ تبدیل با TimeZoneInfo به تهران، سپس حذف زمان.
- PersianCalendar.AddMonths(baseDate, i) برای i از ۱ تا ۴، همواره از تاریخ پایه.
  روز مقصد min(روز پایه، تعداد روز ماه مقصد) است. هر نتیجه با فرمت invariant ASCII
  ساخته شود؛ DateTime داخل Calendar یک نمایندهٔ تاریخی است، نه timestamp سررسید.
- تمام تاریخ‌های شمسی باید واقعی و در محدودهٔ PersianCalendar باشند و چهار ماه مقصد
  نیز قابل محاسبه باشند؛ تاریخ خارج از محدوده یا خرابی invariant نتیجهٔ معتبر تولید نکند.
- expiresAt از همان snapshot ساعت UTC و نیمه‌شب روز بعد تهران به دست آید؛ نه از
  افزودن ۲۴ ساعت به زمان درخواست. شرط اعتبار now < expiresAt است.
- backend قبل از ارسال، invariants را بررسی کند. مبلغ یا تاریخ محاسبه‌شده توسط مشتری پذیرفته نمی‌شود.

## State transitions

مدل تجاری mutable وجود ندارد؛ هر درخواست یک snapshot جدید و بدون ماندگاری تولید می‌کند.
وضعیت مستقل محصول loading/ready/error و برنامه idle/loading/ready/error/expired است.
انقضا، شروع درخواست تازه و خطا نمایش برنامهٔ قبلی را کنار می‌گذارند؛ محصول معتبر حفظ می‌شود.
expired به loading فقط با اقدام کاربر می‌رود؛ timer یا بازگشت focus محاسبه انجام نمی‌دهند. راه‌اندازی دوبارهٔ سرور هیچ
سفارش یا برنامه‌ای را بازیابی نمی‌کند، زیرا این قابلیت چیزی ذخیره نمی‌کند.
