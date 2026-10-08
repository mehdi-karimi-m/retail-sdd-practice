---
description: "وظایف اجرای قابلیت نمایش برنامهٔ پرداخت اقساطی محصول"
---

# Tasks: نمایش برنامهٔ پرداخت اقساطی محصول

**Input**: اسناد `specs/001-show-payment-plan/`

**Created**: 2026-10-09

**Prerequisites**: [plan.md](plan.md)، [spec.md](spec.md)، [research.md](research.md)،
[data-model.md](data-model.md)، [قرارداد API](contracts/api.md)، [OpenAPI](contracts/openapi.json)،
[test-design.md](test-design.md) و [quickstart.md](quickstart.md).

**Tests**: تست‌های محاسبات، تاریخ، قرارداد و کامپوننت طبق تصمیم‌های مصوب لازم‌اند.
مقادیر expected ثابت باشند؛ تولید expected با اجرای کد تحت آزمون مجاز نیست.

**Organization**: مشخصات فقط یک داستان US1 با اولویت P1 دارد؛ همهٔ رفتارهای برنامه، خطا و
انقضا در همان داستان اجرا می‌شوند. این فایل فهرست کار آینده است؛ هیچ وظیفه‌ای اجرا نشده است.

## Format: `[ID] [P?] [Story] Description`

- شناسه‌های T001 تا T034 ترتیب پیشنهادی اجرای ترتیبی را نشان می‌دهند.
- `[P]` فقط فرصت موازی در گروه‌های مشخص‌شدهٔ بخش Dependencies است؛ پیش‌نیازها باید تمام شوند.
- `[US1]` فقط برای وظایف داستان کاربر استفاده شده است.
- وابستگی هر کار در توضیح آن آمده است؛ دو کار روی یک فایل هم‌زمان اجرا نشوند.

## Path Conventions

مسیرها نسبت به ریشهٔ مخزن‌اند: `backend/Retail.Api/`، `tests/Retail.Api.Tests/` و
`frontend/`. سند وظایف در `specs/001-show-payment-plan/tasks.md` است.
پروژهٔ موجود `retail.sln` حفظ و تکمیل شود. دیتابیس، ORM، authentication، سفارش، پرداخت،
پنل مدیریت یا endpoint قیمت/تاریخ دلخواه ایجاد نشود. مهارت‌های `.agents/skills/` تغییر نکنند.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: ایجاد پروژه‌های کوچک و تثبیت ابزارها مطابق research؛ بدون منطق قابلیت.

- [ ] T001 تثبیت SDK موجود 10.0.112 در `global.json` و ثبت نسخهٔ Node24/npm11 و روش restore/build در `specs/001-show-payment-plan/quickstart.md`؛ محیط فعلی را بررسی و بدون نیاز به قابلیت جدید به SDK قدیمی‌تر تغییر ندهید.
- [ ] T002 ایجاد پروژهٔ Minimal API با net10.0/C#14 در `backend/Retail.Api/Retail.Api.csproj` و پروژهٔ تست executable net10.0 در `tests/Retail.Api.Tests/Retail.Api.Tests.csproj` و افزودن هر دو به `retail.sln`؛ نمونه‌های scaffold خارج از دامنه حذف شوند. وابستگی: T001.
- [ ] T003 [P] ایجاد React/TypeScript در `frontend/package.json` و `frontend/src/main.tsx` با React/DOM19.3.0، TS6.0.2، Vite8.3.3 و plugin-react6.1.2؛ `frontend/tsconfig.json` و تنظیمات وابسته strict، isolatedModules و target ES2022 داشته باشند و build برابر `tsc -b && vite build` باشد. وابستگی: T001؛ موازی با T002.
- [ ] T004 تثبیت xunit.v3 4.0.1 و Microsoft.AspNetCore.Mvc.Testing10.0.12 در `tests/Retail.Api.Tests/Retail.Api.Tests.csproj`، reference پروژهٔ API و lockهای `backend/Retail.Api/packages.lock.json` و `tests/Retail.Api.Tests/packages.lock.json`؛ اجرای native تست با dotnet run باشد. وابستگی: T002.
- [ ] T005 [P] تنظیم Vitest5.0.3، React Testing Library، peer `@testing-library/dom` و jsdom در `frontend/package.json`؛ نسخه‌های پایدار سازگار ابزارهای تست را بررسی و دقیق تثبیت، test را vitest و lock را در `frontend/package-lock.json` تولید کنید؛ `frontend/vite.config.ts` پورت5173 و proxy مسیر /api به localhost:5080 داشته باشد. وابستگی: T003؛ موازی با T004.

**Checkpoint**: پروژه‌ها و lockها موجود، TypeScript قابل بررسی و اجرای هر دو runner مشخص باشد.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: زیرساخت حداقلی اجرای API و تست‌ها؛ مدل‌ها و قواعد قابلیت در US1 می‌آیند.

**CRITICAL**: پیش از شروع فاز۳، T001 تا T008 کامل شوند.

- [ ] T006 آماده‌سازی DI، ProblemDetails با JSON camelCase و لاگ خطا در `backend/Retail.Api/Program.cs`؛ timezone `Asia/Tehran` و امکانات PersianCalendar/ICU در شروع بررسی و خرابی صریح گزارش شوند؛ fallback به UTC/میلادی یا globalization invariant مجاز نیست؛ API برای تست با WebApplicationFactory قابل دسترسی باشد. وابستگی: T004 و تکمیل فاز۱.
- [ ] T007 ایجاد harness در `tests/Retail.Api.Tests/ApiTestFactory.cs` و ساعت جعلی در `tests/Retail.Api.Tests/TestTimeProvider.cs` برای HTTP، ساعت ثابت/شمارش خواندن ساعت و جایگزینی منبع محصول از DI؛ endpoint یا header مخصوص تست اضافه نشود؛ hooks جایگزینیِ منبع داستان در T012/T019 تکمیل شوند. وابستگی: T006.
- [ ] T008 [P] پیکربندی تست Node و jsdom در `frontend/vitest.config.ts` و cleanup DOM/fetch/fake timers در `frontend/src/test/setup.ts`؛ تست‌ها ساعت واقعی و backend زنده لازم نداشته باشند. وابستگی: T005 و تکمیل فاز۱؛ موازی با T006–T007.

**Checkpoint**: زیرساخت آماده است؛ تست‌های داستان می‌توانند در فایل‌های جدا نوشته شوند.

---

## Phase 3: User Story 1 - مشاهدهٔ محصول و برنامهٔ کامل پرداخت (Priority: P1) 🎯 MVP

**Goal**: نمایش محصول ثابت و برنامهٔ دقیق بدون سود/کارمزد با چهار سررسید شمسی، مدیریت خطا،
انقضای پایان روز تهران و محاسبهٔ دوباره صرفاً با اقدام کاربر.

**Independent Test**: محصول نمونهٔ ۱٬۰۰۰٬۰۰۰ تومان با ساعت ثابت، پیش‌پرداخت ۳۰۰٬۰۰۰ و
چهار قسط ۱۷۵٬۰۰۰ داشته باشد؛ تاریخ‌ها و expiresAt مطابق test-design باشند. قیمت‌های مرزی
و نامعتبر با DI آزموده شوند. کامپوننت در انقضا برنامه را کنار بگذارد، محصول را نگه دارد و
هیچ درخواست خودکاری نفرستد؛ کلیک محاسبهٔ دوباره برنامهٔ روز تازه را نمایش دهد.

### Tests for User Story 1

تست‌ها پیش از منطق تولید نوشته شوند. پس از ایجاد حداقل type/signature لازم، شکست رفتاری
آن‌ها مشاهده شود؛ خطای compile ناشی از نبود مدل به‌جای شکست معیار پذیرش تلقی نشود.
تست‌های T009 تا T016 پس از فاز۲ مستقل و قابل نوشتن موازی‌اند؛ اجرای سبز در T030/T031 است.

- [ ] T009 [P] [US1] نوشتن تست‌های مالی و اعتبارسنجی در `tests/Retail.Api.Tests/CalculationTests.cs` با پنج ردیف expected ثابت test-design: قیمت‌های1000000،1000010،1000001،5 و100000000000؛ کنترل floor پیش‌پرداخت، سه قسط برابر، تسویهٔ قسط چهارم، جمع دقیق، مثبت بودن و سود/کارمزد صفر؛ رد1..4،0،منفی،5.5m،null،بالاتر از سقف و نام خالی؛ FR-003 تا FR-007 و FR-013. وابستگی: فاز۲.
- [ ] T010 [P] [US1] نوشتن تست سررسیدها در `tests/Retail.Api.Tests/DueDateTests.cs` با چهار ردیف ثابت test-design برای1405/07/16،1405/06/31،1404/11/30 و1403/11/30؛ ماه کوتاه، leap، عبور از سال و بازگشت روز۳۰ را بررسی و افزودن زنجیره‌ای ماه را آشکار کنید؛ FR-008 و سناریوهای۴/۸/۹/۱۰. وابستگی: فاز۲.
- [ ] T011 [P] [US1] نوشتن تست ساعت تهران و expiry در `tests/Retail.Api.Tests/TehranDateTests.cs` با UTCهای12:00،20:29:59 و20:30 در2026-10-08 و expectedهای test-design؛ یک‌بار خواندن ساعت، استقلال از TZ ماشین، عدم fallback، خطای تاریخ خارج از محدوده و نیمه‌شب بعد تهران به‌جای now+24h بررسی شوند؛ FR-012/015. وابستگی: فاز۲.
- [ ] T012 [P] [US1] نوشتن تست HTTP هر دو GET در `tests/Retail.Api.Tests/ApiTests.cs` با `ApiTestFactory.cs` موجود؛ schema/فیلدهای الزامی OpenAPI1.1.0، snapshot محصول، مبالغ string، no-store، جمع دقیق و expiresAt با UTC Z بررسی شوند؛ محصول مفقود404، نام/قیمت نامعتبر500 invalid_product، خرابی محاسبه500، body نامجاز400، روش نامجاز405 و مسیر ناشناخته404 ProblemDetails بدون stack trace یا برنامهٔ ناقص باشند؛ queryهای priceToman/baseDate/foo با ساعت ثابت پاسخ یکسان200 بدهند. وابستگی: فاز۲.
- [ ] T013 [P] [US1] نوشتن تست نمایش پنج مثال مبلغ و سقف در `frontend/src/format.test.ts`؛ رشتهٔ مبلغ با گروه‌بندی و ارقام فارسی/تومان، بدون اعشار یا از دست دادن رقم نمایش داده شود و تاریخ شمسی فقط تبدیل نمایشی ارقام داشته باشد؛ SC-003. وابستگی: فاز۲.
- [ ] T014 [P] [US1] نوشتن تست ساختار قرارداد و fetch در `frontend/src/api.test.ts`؛ پاسخ معتبر، فیلد مفقود، مبلغ JSON number، قسط مفقود، metadata غلط و expiresAt نامعتبر، JSON خراب و ProblemDetails/خطای شبکه بررسی شوند؛ هیچ تست یا کد کنترل جمع مالی در frontend اضافه نشود. وابستگی: فاز۲.
- [ ] T015 [P] [US1] نوشتن تست DOM با fetch mock در `frontend/src/App.test.tsx` برای محصول ثابت، کلیک مشاهده، نمایش مبالغ/تاریخ/مجموع و مثال سقف؛ loading/error باید نام و قیمت معتبر را حفظ و برنامهٔ قبلی را پنهان کنند؛ تلاش مجدد فقط GET برنامه را تکرار کند؛ خطای دریافت اولیهٔ محصول نیز تلاش مجدد داشته باشد؛ سناریوهای۱/۲/۵/۶/۱۳/۱۶/۱۹ و FR-016. وابستگی: فاز۲.
- [ ] T016 [P] [US1] نوشتن تست انقضای کامپوننت در `frontend/src/App.expiry.test.tsx` با fake timers/sاعت ثابت: قبل deadline معتبر، در برابری منقضی، visibility/focus/pageshow پس از تعلیق، پاسخ ازقبل‌منقضی، کلیک محاسبهٔ دوباره و روز تازه، عدم fetch خودکار، پاسخ قدیمی دیررس، callback قدیمی و cleanup هنگام unmount؛ تمام ردیف‌های مصوب test-design و سناریوهای۱۷/۱۸/۲۰ پوشش داده شوند. وابستگی: فاز۲.

### Implementation for User Story 1

- [ ] T017 [US1] تعریف مدل‌های `backend/Retail.Api/Models/Product.cs`، `backend/Retail.Api/Models/Installment.cs` و `backend/Retail.Api/Models/PaymentPlan.cs`؛ قیود data-model عیناً رعایت شوند: name «string غیرخالی»، priceToman «رشتهٔ عدد صحیح ۵ تا ۱۰۰ میلیارد»، product «snapshot محصول مرتبط با این محاسبه»، currency «TOMAN»، calendar «persian»، timeZone «Asia/Tehran»، baseDate «روز درخواست در تهران، YYYY/MM/DD»، expiresAt «نیمه‌شب شروع روز بعد تهران به UTC، RFC3339 با Z»، downPaymentToman «floor(P × 30 / 100)»، installments «شماره‌های ۱ تا ۴ و ترتیب صعودی»، totalPaymentToman «دقیقاً P»، interestToman/feeToman «صفر»، number «۱، ۲، ۳ یا ۴»، amountToman «مثبت؛ سه قسط اول برابر، چهارم تسویه»، dueDate «شمسی، YYYY/MM/DD؛ بدون offset یا ساعت»؛ مبالغ داخلی long و serialization قرارداد string باشد. وابستگی: T009–T012 نوشته شده باشند.
- [ ] T018 [P] [US1] تعریف typeهای TS Product/Installment/PaymentPlan/ProblemDetails در `frontend/src/contracts.ts` مطابق فیلدهای الزامی `contracts/openapi.json`؛ مبلغ string، number شمارهٔ قسط، baseDate/dueDate رشتهٔ شمسی و expiresAt رشتهٔ UTC Z باشند؛ فیلدها optional نشوند و type assertion جای runtime validation قرار نگیرد. وابستگی: T013–T016 نوشته شده باشند؛ موازی با T017.
- [ ] T019 [US1] پیاده‌سازی منبع محصول قابل جایگزینی با DI و مرز اعتبارسنجی آن در `backend/Retail.Api/SampleProductSource.cs`؛ محصول ثابت «محصول نمونه»/1000000، ورودی decimal?؛ قید «ورودی decimal? ابتدا از نظر null، کسری و دامنه بررسی شود؛ P از نوع long و 5 ≤ P ≤ 100000000000 است» پیش از تبدیل اجرا شود؛ نام خالی رد و parser عمومی متن/NaN/Infinity یا endpoint ویرایش ایجاد نشود؛ فرانت‌اند قیمت تعیین نکند. وابستگی: T017 و T009.
- [ ] T020 [US1] پیاده‌سازی بخش مالی در `backend/Retail.Api/PaymentPlanCalculator.cs` با checked long و روابط دقیق «D = (P × 30) div 100؛ R = P − D؛ B = R div 4؛ قسط چهارم L = R − 3B» و «D > 0، B > 0، L > 0؛ D + B + B + B + L = P؛ 0 ≤ L − B ≤ 3»؛ سود/کارمزد صفر و محاسبات مستقل از HTTP باشند. وابستگی: T017، T019 و T009.
- [ ] T021 [US1] تکمیل ساخت چهار سررسید در `backend/Retail.Api/PaymentPlanCalculator.cs` با PersianCalendar.AddMonths(baseDate,i)، هر بار از تاریخ پایه، و قید «روز مقصد min(روز پایه، تعداد روز ماه مقصد) است»؛ فرمت invariant ASCII و کنترل محدودهٔ تقویم؛ Gregorian AddMonths یا افزودن ماه از قسط قبلی مجاز نیست. وابستگی: T020 و T010؛ همان فایل T020 و غیرموازی با آن.
- [ ] T022 [P] [US1] پیاده‌سازی clock snapshot و تاریخ مبنا/expiry در `backend/Retail.Api/TehranDateProvider.cs` با TimeProvider و TimeZoneInfo؛ قید «زمان UTC یک بار خوانده شود» و «expiresAt از همان snapshot ساعت UTC و نیمه‌شب روز بعد تهران به دست آید؛ نه از افزودن ۲۴ ساعت به زمان درخواست»؛ UTC Z و خطای صریح محدوده/تبدیل، بدون timezone ثابت دستی. وابستگی: T017 و T011؛ موازی با T019–T021.
- [ ] T023 [US1] ثبت منبع/validator در DI و اجرای GET `/api/product` در `backend/Retail.Api/Program.cs`؛ نام/قیمت معتبر و TOMAN با string serialization و no-store؛ query ناشناخته بی‌اثر، body نامجاز400، محصول مفقود404 و دادهٔ داخلی نامعتبر500 invalid_product؛ log فنی در سرور و پیام فارسی بدون stack trace، قالب قرارداد برای404/405 نیز رعایت شود. وابستگی: T019 و T012.
- [ ] T024 [US1] ثبت calculator/date provider و اجرای GET `/api/product/payment-plan` در `backend/Retail.Api/Program.cs`؛ snapshot کامل محصول/برنامه، کنترل invariants فقط در backend، expiresAt الزامی، no-store و ProblemDetailsهای قرارداد؛ query قیمت/تاریخ را تغییر ندهد و هیچ auth/ذخیره‌سازی/endpoint نوشتن اضافه نشود. وابستگی: T021، T022، T023 و T012.
- [ ] T025 [P] [US1] پیاده‌سازی formatter رشتهٔ پول و تاریخ در `frontend/src/format.ts` مطابق T013؛ گروه‌بندی سه‌رقمی و ارقام فارسی/پسوند تومان، بدون Number/parseFloat/rounding یا محاسبهٔ جمع؛ Date.parse فقط برای expiresAt در جریان انقضا مجاز است، نه تاریخ شمسی. وابستگی: T018 و T013؛ موازی با کارهای backend پس از آماده‌شدن پیش‌نیازها.
- [ ] T026 [US1] پیاده‌سازی fetch و type guard ساختاری در `frontend/src/api.ts` برای دو مسیر قرارداد و مصرف typeهای `contracts.ts`؛ بررسی شکل رشتهٔ مبلغ، metadata، چهار قسط شماره‌دار، شکل تاریخ و expiresAt UTC معتبر، خطای شبکه/JSON/ProblemDetails قابل نمایش؛ ساخت برنامه/کنترل جمع یا دامنهٔ مالی در مرورگر انجام نشود؛ AbortSignal پشتیبانی شود. وابستگی: T018 و T014؛ مستقل از backend زنده با mock قابل آزمون است.
- [ ] T027 [US1] ساخت صفحهٔ محصول و برنامه در `frontend/src/App.tsx` و سبک سادهٔ RTL در `frontend/src/App.css`؛ نام/قیمت از GET محصول، برنامه با کلیک از GET جدا، نمایش تاریخ مبنا/پیش‌پرداخت/چهار قسط مبلغ و سررسید/مجموع/بدون سود و کارمزد؛ وضعیت محصول جدا از برنامه باشد و فرم قیمت/تاریخ، router یا state library اضافه نشود. وابستگی: T025، T026 و T015.
- [ ] T028 [US1] افزودن جریان انتظار/خطا/تلاش مجدد و جلوگیری از پاسخ قدیمی در `frontend/src/App.tsx`؛ برنامه هنگام درخواست تازه کنار گذاشته و محصول معتبر حفظ شود؛ تلاش مجدد فقط endpoint ناموفق را تکرار کند و پاسخ قدیمی با AbortController یا شناسهٔ درخواست برنامهٔ تازه را بازنویسی نکند. وابستگی: T027 و T015.
- [ ] T029 [US1] افزودن حالت expired و timer/visibilitychange/focus/pageshow در `frontend/src/App.tsx`؛ قید «شرط اعتبار now < expiresAt است» رعایت شود، پاسخ رسیده پس از expiry هرگز ready نشود؛ جدول و مبالغ برنامه پنهان، محصول معتبر حفظ و پیام «تاریخ این برنامه گذشته است؛ دوباره محاسبه کنید» با دکمهٔ محاسبهٔ دوباره نشان داده شود؛ هیچ fetch خودکاری انجام نشود؛ timer/listener و callbackهای قدیمی پاک/بی‌اثر شوند. وابستگی: T028 و T016؛ همان فایل UI و غیرموازی با T027–T028.
- [ ] T030 [US1] اجرای تست‌های `tests/Retail.Api.Tests/Retail.Api.Tests.csproj` با dotnet run و اصلاح فقط کد/تست مرتبط تا تمام مثال‌های مالی، تقویم، نیمه‌شب، expiry و قرارداد موفق شوند؛ expectedهای مصوب برای سبز کردن تست عوض نشوند؛ نبود endpoint نوشتن و عدم پذیرش قیمت/تاریخ query اثبات شود. وابستگی: T024 و T009–T012.
- [ ] T031 [P] [US1] اجرای مجموعهٔ `frontend/package.json` با `npm --prefix frontend run test -- --run` و بررسی نوع/ساخت با `npm --prefix frontend run build`؛ همهٔ تست‌های `format.test.ts`، `api.test.ts`، `App.test.tsx` و `App.expiry.test.tsx` موفق شوند؛ انتظار مبالغ و تعداد درخواست‌ها و clock/timer مستقل از زمان واقعی باشد. وابستگی: T029 و T013–T016؛ موازی با T030.

**Checkpoint**: US1 فقط وقتی کامل است که تمام تست‌های T030/T031 موفق و مسیر کاملِ
محصول، برنامه، خطا و انقضا قابل نمایش باشد؛ حذف انقضا از MVP یا محدودکردن به happy path مجاز نیست.

---

## Phase 4: Polish & Cross-Cutting Concerns

**Purpose**: بررسی مسیر واقعی، تکرارپذیری و تطبیق نهایی؛ بدون افزودن قابلیت یا ابزار اضافی.

- [ ] T032 اجرای مسیر مرورگر و curl طبق `specs/001-show-payment-plan/quickstart.md` پس از T030/T031؛ نام/قیمت، برچسب‌ها، چهار قسط، تاریخ مبنا، خطا با توقف backend و تلاش مجدد بررسی شوند؛ انقضا/تعلیق با تست clock جعلی اجرا شود، نه تغییر ساعت واقعی سیستم؛ نتیجه و محدودیت‌ها در همان راهنما ثبت شوند.
- [ ] T033 بررسی restore --locked-mode و npm ci/build با lockهای `backend/Retail.Api/packages.lock.json`، `tests/Retail.Api.Tests/packages.lock.json` و `frontend/package-lock.json` و تکمیل فرمان‌ها/نسخه‌های واقعاً نصب‌شده در `specs/001-show-payment-plan/quickstart.md` و `research.md`؛ `.gitignore` فقط اگر خروجی ابزار جدید پوشش ندارد تکمیل و قواعد موجود حفظ شوند. وابستگی: T032؛ بررسی دوباره فقط اگر تغییر نسخه/lock رخ داده است.
- [ ] T034 بازبینی تطابق همهٔ FR-001..FR-016 و SC-001..SC-008 با `specs/001-show-payment-plan/spec.md`، قرارداد، تست‌ها و کد؛ وضعیت انجام کار و شواهد آزمون/محدودیت‌ها در `specs/001-show-payment-plan/tasks.md` ثبت شود؛ فقط پس از تحقق معیار checkbox تکمیل شود؛ قابلیت اضافه، دیتابیس و تغییر مهارت‌ها وجود نداشته باشند و بدون دستور مستقل کاربر commit/push انجام نشود. وابستگی: T032–T033.

---

## Dependencies & Execution Order

### Phase Dependencies

```text
Phase 1 (T001–T005)
  → Phase 2 (T006–T008)
    → Phase 3 / US1 (T009–T031)
      → Phase 4 (T032–T034)
```

### User Story Dependencies

US1 تنها داستان است و به داستان دیگری وابسته نیست. بدون دیتابیس، حساب، سفارش یا پرداخت
قابل پیاده‌سازی و آزمون مستقل است. US2/US3 مصنوعی از خطا و انقضا ایجاد نشده‌اند.

### Within Each User Story

گراف پیش‌نیازهای اصلی؛ جزئیات هر یال در توضیح همان وظیفه آمده است:

```text
Foundation → {T009,T010,T011,T012,T013,T014,T015,T016} [tests first]
{T009..T012} → T017 → T019 → T020 → T021
{T017,T011} → T022
{T019,T012} → T023
{T023,T021,T022,T012} → T024 → T030
{T013..T016} → T018 → {T025,T026} → T027 → T028 → T029 → T031
{T030,T031} → T032 → T033 → T034
```

تست‌ها با امضاهای حداقلی سپس شکست رفتاری بررسی شوند؛ مدل پیش از سرویس، سرویس پیش از
endpoint و هردو شاخهٔ backend/frontend پیش از یکپارچه‌سازی و بررسی مرورگر باشند.
T023 و T024 روی Program.cs و T027 تا T029 روی App.tsx ترتیبی هستند.

### Parallel Opportunities

- پس از T001: T002 و T003؛ پس از تکمیل ایجاد پروژه‌های متناظر: T004 و T005.
- پس از فاز۱: T008 در کنار زنجیرهٔ T006→T007؛ مانع فاز۲ قبل از US1 حفظ شود.
- پس از فاز۲: T009 تا T016 در فایل‌های تست جدا؛ fixtureهای مشترک را هم‌زمان تغییر ندهید.
- پس از نوشتن تست‌ها: T017 و T018؛ سپس T022 در کنار T019→T020→T021 و T025/T026
  در شاخهٔ frontend با رعایت وابستگی‌هایشان.
- پس از تکمیل کد هر شاخه: T030 و T031؛ مرورگر T032 منتظر هر دو است.

این موارد امکان زمان‌بندی هستند، نه دستور ایجاد زیرعامل یا آغاز پیاده‌سازی در این مرحله.

---

## Parallel Example: User Story 1

پس از فاز۲، وظایف مستقل نمونه:

```text
T009: tests/Retail.Api.Tests/CalculationTests.cs
T010: tests/Retail.Api.Tests/DueDateTests.cs
T011: tests/Retail.Api.Tests/TehranDateTests.cs
T012: tests/Retail.Api.Tests/ApiTests.cs
T015: frontend/src/App.test.tsx
T016: frontend/src/App.expiry.test.tsx
```

پس از آماده‌شدن مدل‌ها و تست‌ها، سه شاخهٔ پول، clock و نمایش روی فایل‌های متفاوت کار
می‌کنند؛ تغییرات Program.cs و App.tsx در هر شاخه ترتیبی باقی بمانند.

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. فاز۱ و فاز۲ را کامل کنید؛ پروژه‌ها و runnerها کوچک و قابل اجرا باشند.
2. تست‌های ثابت پذیرش را بنویسید و شکست رفتاری مشاهده کنید.
3. مدل و اعتبارسنجی، محاسبات/clock، endpointها و صفحه را مطابق dependencies بسازید.
4. خطا، حفظ محصول، انقضا و محاسبهٔ دوباره را همراه happy path کامل کنید.
5. T030 تا T034 را انجام و نتایج واقعی گزارش کنید؛ انتشار یا کامیت خودکار در این فهرست نیست.

### Incremental Delivery

داخل US1 می‌توان محصول، قواعد مالی/تاریخ، API و نمایش را به‌ترتیب نمایش آموزشی داد؛
اما تکمیل مستقل داستان فقط پس از پوشش همهٔ رفتارهای مصوب و تست‌ها اعلام شود.
قابلیت بعدی تنها از مشخصات جدید می‌آید، نه توسعهٔ ضمنی این MVP.

### Parallel Team Strategy

در صورت مجوز کار موازی، بعد از فاز۲ گروه آزمون‌ها و سپس شاخه‌های backend/frontend با
فایل‌های جدا تقسیم شوند. قرارداد مصوب مشترک بماند؛ اگر ناسازگاری واقعی محیط/وابستگی کشف
شد، آن را حل و در research ثبت کنید؛ قواعد کسب‌وکار برای سازگاری ابزار عوض نشوند.

---

## Notes

- ۳۴ وظیفه: Setup پنج، Foundational سه، US1 بیست‌وسه، بررسی نهایی سه.
- ۸ وظیفهٔ نوشتن تست و ۲ وظیفهٔ اجرای مجموعه‌ها در US1؛ بررسی مرورگر در فاز۴ مکمل است.
- coverage: سناریوهای۱–۲۰ و FR-001–016 / SC-001–008 به وظایف تست و اجرا نگاشت شده‌اند.
- هیچ تست یا کدی در مرحلهٔ تولید این سند اجرا/ساخته نشده و هیچ checkbox تکمیل نیست.
- اصل V تست‌های backend و توافق Q4 تست کامپوننت را لازم می‌کنند؛ تست‌ها اختیاری نیستند.
- همهٔ مسیرها هدف پیاده‌سازی آینده‌اند؛ این فرمان فقط همین tasks.md را ایجاد کرده است.
