# Quickstart Validation Guide

این راهنما برای پس از پیاده‌سازی است؛ مسیرهای اجرایی و فایل‌های قفل هنوز ایجاد نشده‌اند.
فرمان‌ها در این مرحله اجرا نشده‌اند و موفقیت build/test ادعا نمی‌شود.

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
در اولین setup آینده lockها با restore --use-lock-file و npm install ساخته و ثبت می‌شوند؛
این مرحله آن فرمان‌ها را اجرا نمی‌کند. نبود lock را با حذف بررسی locked دور نزنید.

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
