# Quickstart Validation Guide

این راهنما شامل آماده‌سازی Phase 1 و بررسی‌های پذیرش پس از پیاده‌سازی است.
Phase 1 فقط زیرساخت پروژه‌ها را آماده می‌کند؛ endpointها و رفتار قابلیت هنوز اجرا نشده‌اند.

## پیش‌نیازها

.NET SDK 10.0.112، ASP.NET Core runtime 10.0.12؛ Node24.18.0/npm11.16.0 موجود.
نسخه‌های فرانت‌اند و تست در research.md ثبت شده‌اند. ICU و tzdata لازم‌اند؛ backend باید
Asia/Tehran را در شروع پیدا کند. پورت backend=5080 و frontend=5173 در طراحی است.
پروژهٔ تست xUnit v3 executable با target net10.0 است؛ فرمان dotnet run اجراکنندهٔ آزمون آن است.
تست‌های کامپوننت Vitest باید با React Testing Library، @testing-library/dom و محیط jsdom
اجرا شوند؛ تست‌های parser/formatter نیز در همین فرمان test قرار دارند.

## بازیابی و بررسی

از ریشهٔ مخزن، پس از ایجاد پروژه‌ها، نسخه‌های دقیق و lockها:

```bash
dotnet restore retail.sln --locked-mode
dotnet build retail.sln --no-restore
dotnet run --project tests/Retail.Api.Tests --no-restore
npm --prefix frontend ci
npm --prefix frontend run build
npm --prefix frontend run test -- --run
```

اسکریپت build فرانت‌اند باید `tsc -b && vite build` و test باید `vitest` باشد.
برای اولین setup، lockها با `dotnet restore retail.sln --use-lock-file` و
`npm --prefix frontend install` تولید می‌شوند؛ سپس فرمان‌های locked-mode و npm ci بالا
برای بازیابی تکرارپذیر استفاده شوند. نبود lock را با حذف بررسی locked دور نزنید.

انتظار: همهٔ تست‌های test-design.md موفق؛ هیچ قیمت کسری یا قسط صفر پذیرفته نشود؛
هیچ مورد NEEDS CLARIFICATION باقی نماند. پروژه‌های Api و Api.Tests باید در retail.sln قرار گیرند.

## اجرا

در دو ترمینال:

```bash
dotnet run --project backend/Retail.Api --urls http://localhost:5080
```

```bash
npm --prefix frontend run dev -- --port 5173 --strictPort
```

Vite باید /api را به http://localhost:5080 proxy کند؛ localhost:5173 صفحهٔ محصول را نشان دهد.

## بررسی قرارداد

```bash
curl -i http://localhost:5080/api/product
curl -i http://localhost:5080/api/product/payment-plan
curl -i 'http://localhost:5080/api/product/payment-plan?priceToman=5'
```

هر سه پاسخ 200 و no-store هستند؛ priceToman=5 بی‌اثر است و قیمت محصول نمونه عوض نمی‌شود. مثال پاسخ کامل و نوع تمام فیلدها
در [contracts/api.md](contracts/api.md) است. قیمت محصول نمونه ۱٬۰۰۰٬۰۰۰، پیش‌پرداخت ۳۰۰٬۰۰۰
و چهار قسط ۱۷۵٬۰۰۰ تومان است. serverTime زمان مرجع UTC است؛ تاریخ‌های پاسخ واقعی بر اساس روز جاری تهران‌اند؛ نمونهٔ
1405/07/16 فقط با ساعت ثابت تست انتظار می‌رود، نه با clock واقعی هر روز.

قیمت‌های مرزی، دادهٔ نامعتبر، leap و midnight از طریق DI در تست‌ها اجرا می‌شوند؛ برای آن‌ها
endpoint عمومی یا گزینهٔ UI اضافه نکنید. جدول‌ها و expectedها در test-design.md هستند.

## بررسی کاربر

1. صفحه را بدون ورود باز کنید؛ نام، قیمت صحیح و دکمهٔ مشاهدهٔ برنامه موجود باشند.
2. برنامه را مشاهده کنید؛ تاریخ مبنا، پیش‌پرداخت، چهار قسط با سررسید، مجموع و برچسب
   بدون سود و کارمزد باید قابل تشخیص باشند؛ همهٔ مبالغ تومان بدون اعشار و تاریخ‌ها شمسی.
3. دوباره مشاهده کنید؛ درخواست تازه و همان دادهٔ معتبر در همان روز، بدون ایجاد سفارش یا پرداخت.
4. backend را موقتاً متوقف و مشاهدهٔ برنامه را دوباره درخواست کنید؛ پیام خطا قابل فهم و
   امکان تلاش مجدد دیده شود؛ برنامهٔ قبلی به‌عنوان نتیجهٔ درخواست تازه نشان داده نشود.
5. backend را اجرا و تلاش مجدد کنید؛ فقط درخواست برنامه تکرار و برنامهٔ معتبر دوباره نمایش داده شود.
6. در تست کامپوننت با clock/timer جعلی به expiresAt برسید؛ برنامه کنار گذاشته شود، نام و
   قیمت بمانند و پیام انقضا دیده شود؛ هیچ fetch خودکاری انجام نشود. بازگشت به صفحهٔ معلق
   برنامه را تا POST بررسی سرور پنهان نگه دارد؛ true فقط همان برنامه را بازنمایش دهد،
   false یا failure برنامهٔ قدیمی را معتبر نکند. هیچ GET محاسبهٔ خودکاری انجام نشود؛
   پس از کلیک محاسبهٔ دوباره پاسخ روز تازه نمایش داده شود.
7. نمونهٔ expiresAt در پاسخ با مبنای 1405/07/16 برابر 2026-10-08T20:30:00Z است؛
   با ساعت واقعی، مقدار بر اساس روز جاری تهران خواهد بود. جدول انقضا و سناریوهای دقیق
   در test-design.md هستند؛ برای تست انقضا ساعت سیستم واقعی را تغییر ندهید.

این بررسی‌ها همراه تست‌ها مبنای اعلام تکمیل‌اند؛ ساخت موفق به‌تنهایی تأیید معیارهای پذیرش نیست.

## بررسی اعتبار بدون محاسبهٔ تازه

از baseDate/expiresAt پاسخ واقعی GET برنامه استفاده کنید؛ این مثال ثابت امروز ممکن است
منقضی باشد و false طبیعی است. هیچ قیمت یا تاریخ خرید دلخواهی برای محاسبه ارسال نمی‌شود:

```bash
curl -i -X POST http://localhost:5080/api/product/payment-plan/validity -H 'Content-Type: application/json' --data '{"baseDate":"1405/07/16","expiresAt":"2026-10-08T20:30:00Z"}'
```

انتظار200/no-store و فقط isValid/baseDate/serverTime/expiresAt؛ بدون محصول یا قسط.
تست‌های ساعت عقب/جلو و تأخیر با Date.now و performance.now جدا در Vitest اجرا شوند.
پس از بازگشت از تب غیرفعال شبکه را در mock قطع کنید: برنامه باید پنهان بماند؛ تلاش مجدد
بررسی فقط POST validity بزند. stopwatch و زمان دستگاه واقعی را تغییر ندهید.

## آماده‌سازی Phase 1 — 2026-10-09

محیط بررسی شد: SDK10.0.112، ASP.NET Core10.0.12، Node24.18.0 و npm11.16.0.
`global.json` با rollForward=disable SDK را تثبیت می‌کند؛ `.nvmrc` نسخهٔ Node و
packageManager در `frontend/package.json` نسخهٔ npm را ثبت می‌کنند.
فرمان `dotnet --version` از ریشه باید10.0.112 باشد. آماده‌سازی شامل پروژه‌های API/تست،
React/TS و وابستگی‌های تست است؛ پیکربندی jsdom/cleanup و harness متعلق به Phase 2 است.

نتیجهٔ بررسی همین فاز:

- `dotnet restore retail.sln --locked-mode` موفق؛ lock هر دو پروژه موجود است.
- `dotnet build retail.sln --no-restore` موفق با صفر خطا و صفر هشدار.
- `dotnet run --project tests/Retail.Api.Tests --no-build --no-restore -- -assemblyInfo`
  xUnit4.0.1 روی .NET10.0.12 را تأیید کرد؛ `-list full` نیز موفق و بدون تست بود.
- `npm --prefix frontend ci` و `npm --prefix frontend run build` موفق.
- Vitest5.0.3 با `npm --prefix frontend run test -- --run --passWithNoTests` اجرا شد؛
  صفر فایل تست وجود دارد. این گزینه فقط بررسی setup است و در script دائمی test ثبت نشده است.
- بررسی موقت render/cleanup با React، Testing Library و jsdom موفق؛ تست موقت در مخزن
  نگهداری نشد. harness، cleanup مشترک و vitest.config متعلق به T008 هستند.
- `.gitignore` موجود bin/obj، node_modules، dist و tsbuildinfo را پوشش می‌دهد؛
  تغییر لازم نبود و فایل نامناسب ثبت‌شده‌ای با قواعد فعلی یافت نشد.
- منبع mirror-runflare در تنظیمات عمومی محیط باعث تأخیر restore شد؛ `NuGet.Config`
  پروژه فقط منبع رسمی nuget.org را تعیین می‌کند و تنظیمات عمومی دستگاه را تغییر نمی‌دهد.
- محدودیت sandbox برای شبکه و برخی فرمان‌های SDK با مجوز اجرای بیرون sandbox رفع شد.

فقط T001–T005 کامل شدند. برنامهٔ بک‌اند هنوز هیچ endpoint قابلیت ندارد و فرانت‌اند
فقط entry point خالی دارد؛ تست‌های مالی/تاریخ/API/UI هنوز نوشته نشده‌اند. بخش‌های بررسی
قرارداد و کاربر در این راهنما متعلق به فازهای بعدند و در این مرحله اجرا نشده‌اند.

## آماده‌سازی Phase 2 — 2026-10-09

T006–T008 کامل شدند. `Program.cs` امکانات فرهنگ fa-IR/PersianCalendar و timezone تهران
را در شروع بررسی می‌کند؛ TimeProvider.System در DI است و ApiTestFactory آن را با ساعت
ثابت تست جایگزین می‌کند. callback سرویس‌های factory برای جایگزینی منبع آینده فراهم است؛
مدل و منبع محصول هنوز وجود ندارند.

خطاهای عمومی 400/404/405/500 با application/problem+json، type/title/status/detail/code
و متن فارسی ارسال می‌شوند؛ errors الزامی نیست. استثنا در سرور ثبت و از پاسخ حذف می‌شود،
حتی در Development. pipeline فاقد endpoint محصول/برنامه/اعتبار است.

بررسی‌های انجام‌شده:

- `dotnet build retail.sln --no-restore`: صفر هشدار و صفر خطا.
- `dotnet run --project tests/Retail.Api.Tests --no-build --no-restore`: شش تست موفق؛
  404، خطاهای400/405 بدون body، استثنای500 در Production/Development و جایگزینی DI/ساعت.
- اجرای DLL بک‌اند در پردازش جدا با DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1، با
  PREDEFINED_CULTURES_ONLY=0 نیز، و با TZDIR پوشهٔ خالی: شکست صریح پیش از سرویس‌دهی؛
  محیط و ساعت سیستم اصلی تغییر نکردند. فایل core dump تولید نشد.
- `npm --prefix frontend run test -- --run`: چهار تست موفق در دو پروژهٔ Node/jsdom؛
  DOM، fetch و timerها و متغیر محیطی میان تست‌ها پاک شدند.
- `npm --prefix frontend run build`: بررسی نوع شامل vitest.config و ساخت Vite موفق.
- `git diff --check`: موفق؛ قواعد فعلی gitignore خروجی‌های این ابزارها را پوشش می‌دهند.

Vitest فایل‌های `*.node.test.ts` را در Node و سایر `*.test.ts/tsx` یا `*.spec.ts/tsx`
را در jsdom اجرا می‌کند. تست‌ها import صریح APIهای vitest دارند؛ globals=false است.
این تست‌های زیرساخت جای معیارهای پذیرش Phase 3 را نمی‌گیرند. فاز سوم اجرا نشده است.

منابع تنظیمات: [خطاهای ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/error-handling?view=aspnetcore-10.0)،
[WebApplicationFactory](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0)،
[setupFiles در Vitest](https://vitest.dev/config/setupfiles).
