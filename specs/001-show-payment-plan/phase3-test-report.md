# گزارش تکمیل Phase 3 — 2026-10-09

T018–T033 اجرا شدند؛ T034–T036 اجرا نشده‌اند. هیچ کامیت یا پوشی در این مرحله انجام نشد.
گزارش [اجرای قرمز](red-test-report.md) سابقهٔ مرحلهٔ پیش از پیاده‌سازی را حفظ می‌کند.

## نتیجهٔ بررسی

- `dotnet build retail.sln --no-restore`: موفق، صفر هشدار و صفر خطا.
- `dotnet run --project tests/Retail.Api.Tests --no-build --no-restore`: 90 تست موفق،
  صفر failed/skipped و صفر execution error.
- `npm --prefix frontend run build`: بررسی نوع و ساخت Vite موفق.
- `npm --prefix frontend run test -- --run`: 136 تست موفق، صفر شکست/تست معلق.
- 32 تست App.expiry.test.tsx و 9 تست App.test.tsx تا انتها اجرا شدند؛ برخلاف مرحلهٔ
  قرمز، پیش‌شرط نمایش محصول مانع اجرای assertionهای پس از آن نبود.
- لاگ Vitest هشدار act/flushSync یا unhandled error نداشت؛ git diff --check موفق است.

| فایل | تعداد | نتیجه |
| --- | ---: | --- |
| ApiTests.cs | 26 | موفق |
| App.expiry.test.tsx | 32 | موفق |
| App.test.tsx | 9 | موفق |
| CalculationTests.cs | 19 | موفق |
| DueDateTests.cs | 9 | موفق |
| InfrastructureTests.cs | 6 | موفق |
| TehranDateTests.cs | 7 | موفق |
| ValidityApiTests.cs | 23 | موفق |
| api.test.ts | 74 | موفق |
| format.test.ts | 17 | موفق |
| setup.node.test.ts | 2 | موفق |
| setup.test.ts | 2 | موفق |

## رفتار پیاده‌شده

- مرز decimal? قیمت را پیش از تبدیل به long بررسی می‌کند؛ مبلغ کسری، null، زیر5 و
  بالای100 میلیارد رد می‌شوند. محاسبات checked long، floor و تسویهٔ قسط چهارم در بک‌اندند.
- PersianCalendar هر سررسید را مستقل از روز پایه می‌سازد. TimeProvider یک snapshot UTC
  می‌دهد؛ روز تهران، serverTime و نیمه‌شب بعد از همان snapshot تعیین می‌شوند.
- دو GET و POST بررسی اعتبار مطابق قرارداد، با no-store و خطای فارسی؛ POST منبع محصول
  یا calculator را فراخوانی نمی‌کند. قیمت/تاریخ query بی‌اثر است و اثر تجاری/ذخیره‌سازی وجود ندارد.
- JSON مبالغ رشته‌ای است؛ فرانت‌اند فقط گروه‌بندی و ارقام نمایشی را تغییر می‌دهد.
  type guardها requiredها و ساختار را کنترل می‌کنند؛ errors اختیاری و توسعه‌های مجاز
  ProblemDetails حفظ می‌شوند، ولی فیلد اضافه در schemaهای موفقِ additionalProperties=false رد می‌شود.
- UI به main.tsx متصل است؛ محصول، چهار قسط، مبنا، پیش‌پرداخت، مجموع و بدون سود/کارمزد
  را با RTL نمایش می‌دهد. خطا/انتظار/انقضا محصول معتبر را حفظ و برنامهٔ قبلی را پنهان می‌کنند.
- R=max(0,expiresAt−serverTime−RTT) و elapsed با performance.now؛ ساعت تقویمی دستگاه
  مرجع اعتبار نیست. دریافت و parse کامل در RTT لحاظ می‌شوند.
- hidden/pagehide/freeze اعتماد قبلی را کنار می‌گذارند؛ restore/focus/pageshow/resume
  پیش از نمایش فقط POST اعتبار می‌فرستند. true همان snapshot را برمی‌گرداند، false
  منقضی و failure پنهان می‌ماند. محاسبهٔ تازه فقط با اقدام کاربر است.
- نسل درخواست و دورهٔ lifecycle پاسخ قدیمی و round-trip عبورکرده از تعلیق را خنثی
  می‌کنند؛ timer/listener/fetch هنگام تعویض یا unmount پاک/لغو می‌شوند.

## تغییر تست‌ها

تمام انتظارهای قبلی حفظ شدند؛ هیچ مبلغ، تاریخ، وضعیت HTTP، شمارش درخواست یا assertion
برای سبز شدن تضعیف/حذف نشد. سه تست بک‌اند اضافه شدند: تاریخ سرور خارج محدوده باید500
باشد، newline تاریخ درخواست باید400 باشد، و Content-Type غیرJSON بدون خواندن ساعت رد شود.
پنج تست فرانت‌اند اضافه شدند: چهار مورد newline در رشته‌ها و یک مورد فیلد اضافهٔ
نامجاز. این موارد شکاف‌های قرارداد/مرز خطا را می‌بندند؛ معیار مالی تازه‌ای اضافه نمی‌کنند.

## دامنهٔ تأیید و محدودیت

تأیید UI با React Testing Library/jsdom و fetch mock است؛ backend HTTP با TestServer/
WebApplicationFactory و ساعت DI ثابت اجرا شد. این نتیجه پوشش خودکار Phase 3 را تأیید
می‌کند؛ اجرای مرورگر واقعی، curl، بازبینی نهایی lockها و بازبینی سراسری فاز4 هنوز انجام
نشده‌اند. تست ساعت سیستم واقعی را تغییر نمی‌دهد و فقط کنترل‌های موقت تست را جایگزین می‌کند.
دیتابیس، حساب، سفارش، پرداخت، استقرار و قابلیت اضافی ساخته نشده‌اند؛ وابستگی جدیدی لازم نبود.
.gitignore موجود خروجی‌ها و گزارش‌های ماشینی موقت را پوشش می‌دهد؛ فایل نامناسب ثبت‌شده‌ای یافت نشد.

## بازتولید

```bash
dotnet build retail.sln --no-restore
dotnet run --project tests/Retail.Api.Tests --no-build --no-restore -- -result-xml /tmp/retail-us1-green-backend.xml
npm --prefix frontend run build
npm --prefix frontend run test -- --run --reporter=json --outputFile=/tmp/retail-us1-green-frontend.json
```

خروجی خام این نوبت در /tmp/retail-us1-green-backend.xml و .log و
/tmp/retail-us1-green-frontend.json و .log قرار دارد؛ این فایل‌های موقت کامیت نمی‌شوند.
