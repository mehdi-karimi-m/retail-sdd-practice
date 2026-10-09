# Research: نمایش برنامهٔ پرداخت

تاریخ بررسی: 2026-10-08. پژوهش ابزارهای فرانت‌اند با عامل پژوهش انجام شد؛ انتخاب‌ها با
محیط محلی و منابع رسمی تطبیق داده شدند. هیچ وابستگی نصب یا برنامه‌ای ساخته نشده است.

## 1. نسخه‌ها و محیط

**Decision**: net10.0 با SDK 10.0.112 و runtime/ASP.NET Core 10.0.12 موجود؛ C# 14.
Node 24.18.0 و npm 11.16.0 موجود حفظ شوند. React/DOM 19.3.0، TypeScript 6.0.2،
Vite 8.3.3، plugin-react 6.1.2، types React/DOM 19.3.0 و types Node 24.19.1 پیشنهادی‌اند.

**Rationale**: Ubuntu 24.04 محیط فعلی است. .NET 10 LTS و Node 24 LTS هستند؛ Vite 8
با Node موجود سازگار است. ترکیب فرانت‌اند در قالب رسمی React/TS مشاهده شد. نسخه‌های SDK
قدیمی‌تر نیز نصب‌اند ولی هدف جدیدی در تغییر به آن‌ها وجود ندارد.

**Alternatives considered**: .NET 8/9، Node قدیمی، React framework با SSR؛ نیاز این صفحه
را بهتر برآورده نمی‌کنند. قالب main متحرک است؛ ادعای «آخرین نسخه» نداریم. نسخه‌ها baseline
پیشنهادی‌اند و نصب/restore واقعی در پیاده‌سازی سازگاری نهایی را تأیید می‌کند.

**Sources**:
- [سیاست پشتیبانی .NET](https://dotnet.microsoft.com/en-us/platform/support/policy)
- [نسخه‌های Node](https://nodejs.org/en/about/previous-releases)
- [نسخه‌های React](https://react.dev/versions)
- [قالب رسمی Vite React/TS](https://raw.githubusercontent.com/vitejs/vite/main/packages/create-vite/template-react-ts/package.json)
- [Vite 8 و حداقل Node](https://vite.dev/blog/announcing-vite8.html)
- [فرادادهٔ افزونهٔ React](https://raw.githubusercontent.com/vitejs/vite-plugin-react/main/packages/plugin-react/package.json)

در پیاده‌سازی global.json SDK موجود را تثبیت کند؛ package.json نسخه‌های مستقیم دقیق داشته
باشد، package-lock.json و packages.lock.json قابل ثبت باشند. npm ci و restore --locked-mode
برای اجرای تکرارپذیر پس از ایجاد lockها استفاده شوند. TypeScript strict، isolatedModules و
target ES2022 صریح باشند؛ فرمان build شامل tsc -b && vite build است، زیرا
[Vite فقط transpile می‌کند](https://vite.dev/guide/features.html).

## 2. پول و انتقال

**Decision**: تومان صحیح در long؛ P × 30 / 100 با تقسیم صحیح مثبت و checked، سپس محاسبهٔ
مانده و قسط پایه با تقسیم صحیح بر ۴. مبالغ در JSON رشتهٔ رقم ASCII و در TS string هستند.

**Rationale**: سقف ضرب ۳٬۰۰۰٬۰۰۰٬۰۰۰٬۰۰۰ در Int64 جا می‌شود؛ اعداد صحیح دقت دارند.
decimal فقط برای تشخیص قیمت خام کسری پیش از تبدیل استفاده می‌شود. فرانت‌اند نیاز به محاسبهٔ
مالی ندارد؛ رشته از coercion ناخواسته جلوگیری می‌کند. طبق توافق بازنگری، کنترل جمع فقط
در بک‌اند و آزمون قرارداد است؛ frontend بررسی ساختار و نمایش دقیق را انجام می‌دهد.

**Alternatives considered**: decimal برای تمام عملیات نیز درست است اما برای تومان صحیح لازم
نیست؛ double ممنوع است. JSON number در سقف فعلی هم می‌تواند دقیق باشد، ولی این انتخاب رشته
مرز قرارداد را روشن‌تر می‌کند. کتابخانهٔ decimal فرانت‌اند لازم نیست.

**Source**: [نوع Int64](https://learn.microsoft.com/en-us/dotnet/api/system.int64?view=net-10.0).

## 3. تقویم و ساعت

**Decision**: TimeProvider از DI؛ UTC یک‌بار در درخواست، TimeZoneInfo با Asia/Tehran،
تاریخ محلی به PersianCalendar؛ برای i=1..4 افزودن i ماه به همان تاریخ پایه.
اگر روز در مقصد نیست، GetDaysInMonth و حداقل روز پایه/آخر ماه ملاک باشند.

**Rationale**: مشخصات شمسی، پایان ماه و مبنای مستقل را قطعی کرده است. استفاده از
Gregorian DateTime.AddMonths یا افزودن زنجیره‌ای یک ماه با مشخصات ناسازگار است.
تاریخ خروجی بدون ساعت به صورت YYYY/MM/DD و با ارقام ASCII منتقل می‌شود.

**Alternatives considered**: محاسبه در مرورگر، offset ثابت +03:30، کتابخانهٔ تقویم ثالث؛
به دلیل ساعت مستقل دستگاه، قواعد tzdata و امکانات استاندارد .NET انتخاب نشدند.

**Sources**:
- [TimeProvider](https://learn.microsoft.com/en-us/dotnet/standard/datetime/timeprovider-overview)
- [PersianCalendar.AddMonths](https://learn.microsoft.com/en-us/dotnet/api/system.globalization.persiancalendar.addmonths?view=net-10.0)
- [GetDaysInMonth](https://learn.microsoft.com/en-us/dotnet/api/system.globalization.persiancalendar.getdaysinmonth?view=net-10.0)

## 4. ذخیره‌سازی و API

**Decision**: محصول نمونهٔ فقط‌خواندنی در حافظه و برنامهٔ محاسبه‌شده فقط در درخواست برنامه؛ دو GET و POST بدون اثر جانبیِ بررسی اعتبار
در قرارداد. دیتابیس، ORM، endpoint ساخت محصول یا تاریخ ورودی اضافه نشوند.

**Rationale**: هیچ نیاز به ماندگاری در FR-010 وجود ندارد. یک منبع نمونهٔ قابل جایگزینی در تست
برای این مرحله کافی است و بعداً تنها با مشخصات قابلیت جدید تغییر می‌کند.

**Alternatives considered**: SQLite/EF، فایل قابل ویرایش و repository عمومی؛ نیاز مصوب ندارند.

## 5. تست و خطا

**Decision**: xunit.v3 4.0.1 در پروژهٔ تست executable net10.0 و اجرا با dotnet run؛
Microsoft.AspNetCore.Mvc.Testing 10.0.12 برای WebApplicationFactory. Vitest 5.0.3 در محیط
برای parser/formatter؛ React Testing Library با peer @testing-library/dom و jsdom برای
تست رفتار کامپوننت، خطا/تلاش مجدد و انقضا. مسیر مرورگر در quickstart بررسی تکمیلی است.
ProblemDetails داخلی ASP.NET Core برای خطاهای استاندارد و code پایدار.

**Rationale**: آزمون قطعی ساعت/قیمت بدون endpoint مخصوص تست ممکن است. نسخهٔ پایدار xUnit
با .NET 8 به بالا سازگار است؛ انتخاب runner executable نیاز به ترکیب اضافی VSTest نمی‌آورد.
Vitest با Node24 و Vite8 طبق peer metadata سازگار است. ابزار E2E اضافه برای یک صفحه لازم نیست.

**Alternatives considered**: xUnit v2 legacy، تست با ساعت واقعی، تست فقط فرانت‌اند و Playwright
از ابتدا؛ به‌ترتیب عمر ابزار، nondeterminism، نبود پوشش مرجع محاسبات و وابستگی اضافی دارند.

**Sources**:
- [xUnit v3 رسمی](https://xunit.net/docs/getting-started/v3/getting-started)
- [بستهٔ پایدار xunit.v3 4.0.1](https://www.nuget.org/packages/xunit.v3/4.0.1)
- [Integration testing](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0)
- [خطاهای API](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/error-handling-api?view=aspnetcore-10.0)
- [Vitest guide](https://vitest.dev/guide/)
- [بستهٔ Mvc.Testing 10.0.12](https://www.nuget.org/packages/Microsoft.AspNetCore.Mvc.Testing/10.0.12)
- [انتشار Vitest 5.0.3](https://github.com/vitest-dev/vitest/releases/tag/v5.0.3)
- [Vitest metadata](https://raw.githubusercontent.com/vitest-dev/vitest/main/packages/vitest/package.json)

## 6. تصمیم‌های بازنگری تأییدشده

**Decision**: دو GET و محصول ثابت حفظ شوند؛ query ناشناخته بی‌اثر باشد؛ دادهٔ محصول داخلی
نامعتبر 500 با invalid_product بدهد. یک مرز decimal? پیش از long کافی است؛ parser عمومی
متن یا منابع آینده اضافه نشود. کنترل مالی در backend بماند. انقضا و تست کامپوننت اضافه شوند.

**Rationale**: این تصمیم‌ها در دورهای grilling با صاحب پروژه توافق و برای اعمال در اسناد
تأیید شدند. انقضا یک نیاز رفتاری صریح جدید در FR-015 است، نه پیچیدگی برای آینده.

**Alternatives considered**: یک پاسخ ترکیبی، رد تمام queryها، 422 برای دادهٔ داخلی، کنترل
جمع با BigInt در مرورگر، تست فقط دستی UI و نمایش برنامهٔ روز گذشته؛ پس از بررسی پذیرفته نشدند.

expiresAt در backend از نیمه‌شب شروع روز بعد تهران ساخته و به UTC منتقل شود؛ frontend زمان باقی‌مانده را از serverTime/expiresAt و RTT اندازه‌گیری‌شده با
performance.now می‌سازد؛ صحت ساعت دستگاه فرض نمی‌شود. پس از تعلیق POST بررسی اعتبار
بدون محاسبهٔ جدید لازم است. این تصمیم جای مقایسهٔ expiry با ساعت تقویمی دستگاه را گرفت.

برای تست کامپوننت React Testing Library با peer رسمی @testing-library/dom و jsdom پیشنهاد
می‌شوند؛ metadata رسمی React18/19 را می‌پذیرد. نسخهٔ دقیق این وابستگی‌های تست هنگام نصب
در package-lock تثبیت و با build/test تأیید شود؛ این مرحله چیزی نصب نمی‌کند.

**Sources**:
- [React Testing Library](https://testing-library.com/docs/react-testing-library/intro/)
- [Peer dependencies رسمی](https://raw.githubusercontent.com/testing-library/react-testing-library/main/package.json)
- [jsdom metadata رسمی](https://raw.githubusercontent.com/jsdom/jsdom/main/package.json)

## Research outcome

ابهام فنی حل‌نشده باقی نمانده است. پیشنهاد نسخه‌ها بر مشاهده و مستندات استوار است، اما
موفقیت restore/build هنوز آزموده نشده؛ این بررسی متعلق به پیاده‌سازی است.

## 7. رفع I1 — تصمیم مصوب 2026-10-09

**Decision**: serverTime و expiresAt UTC از بک‌اند، کسر کامل RTT از اختلاف آن‌ها و گذشت
زمان با performance.now؛ پس از inactivity/suspension بررسی stateless با سرور قبل از نمایش.
بررسی برنامهٔ تازه محاسبه نمی‌کند؛ failure نمایش قدیمی را معتبر نمی‌کند.

**Rationale**: ساعت تقویمی دستگاه می‌تواند عقب/جلو یا تغییر داده شود. ساعت یکنواخت برای
elapsed مناسب است ولی عبور زمان در تعلیق نباید فرض شود؛ بررسی تازهٔ سرور این شکاف را می‌بندد.
کسر کامل RTT محافظه‌کارانه است و می‌تواند تا یک RTT زودتر برنامه را پنهان کند؛ تأخیر پاسخ
به اعتبار اضافه نمی‌شود. این انتخاب فنی به درخواست صریح صاحب پروژه پاسخ می‌دهد.

**Alternatives considered**: Date.now، offset ساعت دستگاه، RTT/2 و محاسبهٔ خودکار برنامه
پس از focus؛ هیچ‌یک انتخاب نشدند. ذخیرهٔ برنامه یا سرویس همگام‌سازی دائمی ساعت لازم نیست.

**Source**: [W3C High Resolution Time](https://www.w3.org/TR/hr-time-3/) برای تفاوت ساعت
یکنواخت و تقویمی. راهبرد RTT/بررسی اعتبار تصمیم طراحی این پروژه است، نه نقل الزام استاندارد.

## تأیید بسته‌ها در Phase 1 — 2026-10-09

نسخه‌های مستقیم مصوب React19.3.0، TypeScript6.0.2، Vite8.3.3، plugin-react6.1.2 و
Vitest5.0.3 نصب شدند. نسخه‌های تکمیلی دقیق: React Testing Library16.3.3،
@testing-library/dom10.4.2 و jsdom30.1.2؛ peerهای React18/19 و DOM10 پذیرفته می‌شوند.
jsdom30.1.2 به Node ^22.22.2 یا ^24.15.0 یا >=26 نیاز دارد و Node24.18.0 محیط سازگار است.
فرادادهٔ نسخه‌ها از رجیستری رسمی npm با npm view بررسی و در package-lock قفل شدند.
ساخت TypeScript/Vite و بررسی موقت render/cleanup با React/Testing Library/jsdom موفق بود.
پیکربندی test environment و cleanup مشترک هنوز متعلق به T008 است؛ در Phase 1 ایجاد نشد.

منابع: [React Testing Library](https://testing-library.com/docs/react-testing-library/intro/)،
[jsdom](https://github.com/jsdom/jsdom)،
[xUnit v3 و runner اجرایی](https://xunit.net/docs/getting-started/v3/getting-started).
