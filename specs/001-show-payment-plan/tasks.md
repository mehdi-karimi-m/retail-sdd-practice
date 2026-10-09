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
انقضا و بررسی اعتبار در همان داستان اجرا می‌شوند. این فایل وضعیت وظایف را نگه می‌دارد؛ Phase 1 و Phase 2 در2026-10-09 اجرا شدند و فقط کارهای واقعاً تکمیل‌شده تیک دارند. اصلاح I1 در 2026-10-09 زمان مرجع سرور و بررسی اعتبار را اضافه کرد.

## Format: `[ID] [P?] [Story] Description`

- شناسه‌های T001 تا T036 ترتیب پیشنهادی اجرای ترتیبی را نشان می‌دهند.
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

- [X] T001 تثبیت SDK موجود 10.0.112 در `global.json` و ثبت نسخهٔ Node24/npm11 و روش restore/build در `specs/001-show-payment-plan/quickstart.md`؛ محیط فعلی را بررسی و بدون نیاز به قابلیت جدید به SDK قدیمی‌تر تغییر ندهید.
- [X] T002 ایجاد پروژهٔ Minimal API با net10.0/C#14 در `backend/Retail.Api/Retail.Api.csproj` و پروژهٔ تست executable net10.0 در `tests/Retail.Api.Tests/Retail.Api.Tests.csproj` و افزودن هر دو به `retail.sln`؛ نمونه‌های scaffold خارج از دامنه حذف شوند. وابستگی: T001.
- [X] T003 [P] ایجاد React/TypeScript در `frontend/package.json` و `frontend/src/main.tsx` با React/DOM19.3.0، TS6.0.2، Vite8.3.3 و plugin-react6.1.2؛ `frontend/tsconfig.json` و تنظیمات وابسته strict، isolatedModules و target ES2022 داشته باشند و build برابر `tsc -b && vite build` باشد. وابستگی: T001؛ موازی با T002.
- [X] T004 تثبیت xunit.v3 4.0.1 و Microsoft.AspNetCore.Mvc.Testing10.0.12 در `tests/Retail.Api.Tests/Retail.Api.Tests.csproj`، reference پروژهٔ API و lockهای `backend/Retail.Api/packages.lock.json` و `tests/Retail.Api.Tests/packages.lock.json`؛ اجرای native تست با dotnet run باشد. وابستگی: T002.
- [X] T005 [P] تنظیم Vitest5.0.3، React Testing Library، peer `@testing-library/dom` و jsdom در `frontend/package.json`؛ نسخه‌های پایدار سازگار ابزارهای تست را بررسی و دقیق تثبیت، test را vitest و lock را در `frontend/package-lock.json` تولید کنید؛ `frontend/vite.config.ts` پورت5173 و proxy مسیر /api به localhost:5080 داشته باشد. وابستگی: T003؛ موازی با T004.

**Checkpoint**: پروژه‌ها و lockها موجود، TypeScript قابل بررسی و اجرای هر دو runner مشخص باشد.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: زیرساخت حداقلی اجرای API و تست‌ها؛ مدل‌ها و قواعد قابلیت در US1 می‌آیند.

**CRITICAL**: پیش از شروع فاز۳، T001 تا T008 کامل شوند.

- [X] T006 آماده‌سازی DI، ProblemDetails با JSON camelCase و لاگ خطا در `backend/Retail.Api/Program.cs`؛ timezone `Asia/Tehran` و امکانات PersianCalendar/ICU در شروع بررسی و خرابی صریح گزارش شوند؛ fallback به UTC/میلادی یا globalization invariant مجاز نیست؛ API برای تست با WebApplicationFactory قابل دسترسی باشد. وابستگی: T004 و تکمیل فاز۱.
- [X] T007 ایجاد harness در `tests/Retail.Api.Tests/ApiTestFactory.cs` و ساعت جعلی در `tests/Retail.Api.Tests/TestTimeProvider.cs` برای HTTP، ساعت ثابت/شمارش خواندن ساعت و جایگزینی منبع محصول از DI؛ endpoint یا header مخصوص تست اضافه نشود؛ hooks جایگزینیِ منبع داستان در T012/T020 تکمیل شوند. وابستگی: T006.
- [X] T008 [P] پیکربندی تست Node و jsdom در `frontend/vitest.config.ts` و cleanup DOM/fetch/fake timers در `frontend/src/test/setup.ts`؛ تست‌ها ساعت واقعی و backend زنده لازم نداشته باشند. وابستگی: T005 و تکمیل فاز۱؛ موازی با T006–T007.

**Checkpoint**: زیرساخت آماده است؛ تست‌های داستان می‌توانند در فایل‌های جدا نوشته شوند.

---

## Phase 3: User Story 1 - مشاهدهٔ محصول و برنامهٔ کامل پرداخت (Priority: P1) 🎯 MVP

**Goal**: نمایش محصول ثابت و برنامهٔ دقیق بدون سود/کارمزد با چهار سررسید شمسی، مدیریت خطا،
انقضای پایان روز تهران و محاسبهٔ دوباره صرفاً با اقدام کاربر.

**Independent Test**: محصول نمونهٔ ۱٬۰۰۰٬۰۰۰ تومان با ساعت ثابت، پیش‌پرداخت ۳۰۰٬۰۰۰ و
چهار قسط ۱۷۵٬۰۰۰ داشته باشد؛ تاریخ‌ها و expiresAt مطابق test-design باشند. قیمت‌های مرزی
و نامعتبر با DI آزموده شوند. کامپوننت در انقضا برنامه را کنار بگذارد، محصول را نگه دارد و
هیچ درخواست محاسبهٔ خودکاری نفرستد؛ بازگشت به صفحه ابتدا فقط اعتبار را با سرور بررسی کند؛ کلیک محاسبهٔ دوباره برنامهٔ روز تازه را نمایش دهد.

### Tests for User Story 1

تست‌ها پیش از منطق تولید نوشته شوند. پس از ایجاد حداقل type/signature لازم، شکست رفتاری
آن‌ها مشاهده شود؛ خطای compile ناشی از نبود مدل به‌جای شکست معیار پذیرش تلقی نشود.
تست‌های T009 تا T017 پس از فاز۲ مستقل و قابل نوشتن موازی‌اند؛ اجرای سبز در T032/T033 است.

- [X] T009 [P] [US1] نوشتن تست‌های مالی و اعتبارسنجی در `tests/Retail.Api.Tests/CalculationTests.cs` با پنج ردیف expected ثابت test-design: قیمت‌های1000000،1000010،1000001،5 و100000000000؛ کنترل floor پیش‌پرداخت، سه قسط برابر، تسویهٔ قسط چهارم، جمع دقیق، مثبت بودن و سود/کارمزد صفر؛ در مرز decimal? رد1..4 به دلیل قسط صفر،0،منفی،5.5m،null به معنی قیمت خالی و بالاتر از سقف پیش از تبدیل به long بررسی شوند؛ نام خالی نیز رد شود؛ سناریوی۱۵ فقط عدد دقیق یا null است و تست/parser ورودی متنی اضافه نشود؛ FR-003 تا FR-007، FR-011 و FR-013. وابستگی: فاز۲.
- [X] T010 [P] [US1] نوشتن تست سررسیدها در `tests/Retail.Api.Tests/DueDateTests.cs` با چهار ردیف ثابت test-design برای1405/07/16،1405/06/31،1404/11/30 و1403/11/30؛ ماه کوتاه، leap، عبور از سال و بازگشت روز۳۰ را بررسی و افزودن زنجیره‌ای ماه را آشکار کنید؛ FR-008 و سناریوهای۴/۸/۹/۱۰. وابستگی: فاز۲.
- [X] T011 [P] [US1] نوشتن تست ساعت تهران و expiry در `tests/Retail.Api.Tests/TehranDateTests.cs` با UTCهای12:00،20:29:59 و20:30 در2026-10-08 و expectedهای test-design؛ یک‌بار خواندن ساعت، استقلال از TZ ماشین، عدم fallback، خطای تاریخ خارج از محدوده و نیمه‌شب بعد تهران به‌جای now+24h بررسی شوند؛ FR-012/015. وابستگی: فاز۲.
- [X] T012 [P] [US1] نوشتن تست HTTP هر دو GET در `tests/Retail.Api.Tests/ApiTests.cs` با `ApiTestFactory.cs` موجود؛ schema/فیلدهای الزامی OpenAPI1.2.0، snapshot محصول، مبالغ string، no-store، جمع دقیق و serverTime/expiresAt با UTC Z از یک snapshot ساعت بررسی شوند؛ محصول مفقود404، نام/قیمت نامعتبر500 invalid_product، خرابی محاسبه500، body نامجاز400، روش نامجاز405 و مسیر ناشناخته404 ProblemDetails بدون stack trace یا برنامهٔ ناقص باشند؛ queryهای priceToman/baseDate/foo با ساعت ثابت پاسخ یکسان200 بدهند. وابستگی: فاز۲.
- [X] T013 [P] [US1] نوشتن تست قرارداد بررسی اعتبار در `tests/Retail.Api.Tests/ValidityApiTests.cs`؛ POST `/api/product/payment-plan/validity` با baseDate/expiresAt snapshot قبلی، پیش از expiry معتبر و در برابری/پس از expiry نامعتبر، روز آینده/گذشته و expiry ناسازگار false؛ JSON/فیلد/تاریخ بدشکل400 و خطای سرور500؛ پاسخ فقط baseDate/serverTime/expiresAt/isValid و no-store، calculator با spy یا شمارندهٔ تست هرگز فراخوانی نشود و هیچ برنامهٔ تازه تولید نشود؛ ساعت سرور ثابت باشد. وابستگی: فاز۲.
- [X] T014 [P] [US1] نوشتن تست نمایش پنج مثال مبلغ و سقف در `frontend/src/format.test.ts`؛ رشتهٔ مبلغ با گروه‌بندی و ارقام فارسی/تومان، بدون اعشار یا از دست دادن رقم نمایش داده شود و تاریخ شمسی فقط تبدیل نمایشی ارقام داشته باشد؛ SC-003. وابستگی: فاز۲.
- [X] T015 [P] [US1] نوشتن تست ساختار قرارداد و fetch در `frontend/src/api.test.ts`؛ پاسخ معتبر، فیلد required مفقود، مبلغ JSON number، قسط مفقود، metadata غلط و serverTime/expiresAt نامعتبر و پاسخ validity بدشکل، JSON خراب و ProblemDetails/خطای شبکه بررسی شوند؛ ProblemDetails معتبر بدون errors و با errors معتبر/خالی پذیرفته، ولی errors حاضر با null/آرایه/مقدار غیرآرایه/عضو غیررشته رد شود؛ نبود فیلد اختیاری باعث رد پاسخ نشود و status با HTTP تطبیق داده شود؛ هیچ تست یا کد کنترل جمع مالی در frontend اضافه نشود. وابستگی: فاز۲.
- [X] T016 [P] [US1] نوشتن تست DOM با fetch mock در `frontend/src/App.test.tsx` برای محصول ثابت، کلیک مشاهده، نمایش مبالغ/تاریخ/مجموع و مثال سقف؛ loading/error باید نام و قیمت معتبر را حفظ و برنامهٔ قبلی را پنهان کنند؛ تلاش مجدد محاسبه فقط GET برنامه را تکرار کند و در پاسخ معتبر برنامهٔ تازه نمایش دهد؛ خطای دریافت اولیهٔ محصول نیز تلاش مجدد داشته باشد؛ ProblemDetails معتبر بدون errors باید detail فارسی را نمایش دهد و به خطای ساختاری بدل نشود؛ سناریوهای۱/۲/۵/۶/۱۳/۱۶/۱۹ و FR-016. وابستگی: فاز۲.
- [X] T017 [P] [US1] نوشتن تست انقضای کامپوننت در `frontend/src/App.expiry.test.tsx` با fake timers/sاعت ثابت: ساعت تقویمی یک روز عقب/جلو و پرش ساعت بدون تغییر نتیجه، RTT نامتقارن و پاسخ دیررس، گذشت زمان یکنواخت، بودجهٔ صفر منقضی، visibility/focus/pageshow/resume پس از تعلیق با توقف performance.now، checking تا پاسخ سرور و verification-error در شکست بررسی، پاسخ درخواستی که از مرز تعلیق عبور کرده بودجهٔ معتبر ندهد، پاسخ ازقبل‌منقضی، کلیک محاسبهٔ دوباره و روز تازه، فقط POST validity خودکار در بازگشت و عدم GET محاسبهٔ خودکار، پاسخ قدیمی دیررس، callback قدیمی و cleanup هنگام unmount؛ تمام ردیف‌های مصوب test-design و سناریوهای۱۷/۱۸/۲۰/۲۱–۲۴ پوشش داده شوند. وابستگی: فاز۲.

### Implementation for User Story 1

- [X] T018 [US1] تعریف مدل‌های `backend/Retail.Api/Models/Product.cs`، `backend/Retail.Api/Models/Installment.cs` و `backend/Retail.Api/Models/PaymentPlan.cs`؛ قیود data-model عیناً رعایت شوند: name «string غیرخالی»، priceToman «رشتهٔ عدد صحیح ۵ تا ۱۰۰ میلیارد»، product «snapshot محصول مرتبط با این محاسبه»، currency «TOMAN»، calendar «persian»، timeZone «Asia/Tehran»، baseDate «روز درخواست در تهران، YYYY/MM/DD»، serverTime «زمان مرجع سرور از همان snapshot ساعت UTC، RFC3339 با Z»، expiresAt «نیمه‌شب شروع روز بعد تهران به UTC، RFC3339 با Z»، downPaymentToman «floor(P × 30 / 100)»، installments «شماره‌های ۱ تا ۴ و ترتیب صعودی»، totalPaymentToman «دقیقاً P»، interestToman/feeToman «صفر»، number «۱، ۲، ۳ یا ۴»، amountToman «مثبت؛ سه قسط اول برابر، چهارم تسویه»، dueDate «شمسی، YYYY/MM/DD؛ بدون offset یا ساعت»؛ مبالغ داخلی long و serialization قرارداد string باشد. وابستگی: T009–T013 نوشته شده باشند.
- [X] T019 [P] [US1] تعریف typeهای TS Product/Installment/PaymentPlan/ProblemDetails/ValidityRequest/ValidityResponse در `frontend/src/contracts.ts` مطابق فیلدهای الزامی `contracts/openapi.json`؛ مبلغ string، number شمارهٔ قسط، baseDate/dueDate رشتهٔ شمسی و serverTime/expiresAt رشتهٔ UTC Z و isValid boolean باشند؛ baseDate/expiresAt درخواست validity فقط snapshot قبلی را معرفی کنند؛ فقط فیلدهای required هر schema الزامی باشند؛ ProblemDetails.errors با نوع `errors?: Record<string, string[]>` اختیاری بماند؛ نبود آن خطای معتبر را رد نکند و حضورش مستلزم بررسی ساختار باشد؛ type assertion جای runtime validation قرار نگیرد. وابستگی: T014–T017 نوشته شده باشند؛ موازی با T018.
- [X] T020 [US1] پیاده‌سازی منبع محصول قابل جایگزینی با DI و مرز اعتبارسنجی آن در `backend/Retail.Api/SampleProductSource.cs`؛ محصول ثابت «محصول نمونه»/1000000، ورودی decimal?؛ قید «ورودی decimal? ابتدا از نظر null، کسری و دامنه بررسی شود؛ P از نوع long و 5 ≤ P ≤ 100000000000 است» پیش از تبدیل اجرا شود؛ نام خالی رد و parser عمومی متن/NaN/Infinity یا endpoint ویرایش ایجاد نشود؛ فرانت‌اند قیمت تعیین نکند. وابستگی: T018 و T009.
- [X] T021 [US1] پیاده‌سازی بخش مالی در `backend/Retail.Api/PaymentPlanCalculator.cs` با checked long و روابط دقیق «D = (P × 30) div 100؛ R = P − D؛ B = R div 4؛ قسط چهارم L = R − 3B» و «D > 0، B > 0، L > 0؛ D + B + B + B + L = P؛ 0 ≤ L − B ≤ 3»؛ سود/کارمزد صفر و محاسبات مستقل از HTTP باشند. وابستگی: T018، T020 و T009.
- [X] T022 [US1] تکمیل ساخت چهار سررسید در `backend/Retail.Api/PaymentPlanCalculator.cs` با PersianCalendar.AddMonths(baseDate,i)، هر بار از تاریخ پایه، و قید «روز مقصد min(روز پایه، تعداد روز ماه مقصد) است»؛ فرمت invariant ASCII و کنترل محدودهٔ تقویم؛ Gregorian AddMonths یا افزودن ماه از قسط قبلی مجاز نیست. وابستگی: T021 و T010؛ همان فایل T021 و غیرموازی با آن.
- [X] T023 [P] [US1] پیاده‌سازی clock snapshot و serverTime/تاریخ مبنا/expiry و تابع بررسی روز/expiry snapshot قبلی بدون محاسبهٔ اقساط در `backend/Retail.Api/TehranDateProvider.cs` با TimeProvider و TimeZoneInfo؛ قید «زمان UTC یک بار خوانده شود» و «expiresAt از همان snapshot ساعت UTC و نیمه‌شب روز بعد تهران به دست آید؛ نه از افزودن ۲۴ ساعت به زمان درخواست»؛ UTC Z و خطای صریح محدوده/تبدیل، بدون timezone ثابت دستی. وابستگی: T018 و T011؛ موازی با T020–T022.
- [X] T024 [US1] ثبت منبع/validator در DI و اجرای GET `/api/product` در `backend/Retail.Api/Program.cs`؛ نام/قیمت معتبر و TOMAN با string serialization و no-store؛ query ناشناخته بی‌اثر، body نامجاز400، محصول مفقود404 و دادهٔ داخلی نامعتبر500 invalid_product؛ log فنی در سرور و پیام فارسی بدون stack trace، قالب قرارداد برای404/405 نیز رعایت شود. وابستگی: T020 و T012.
- [X] T025 [US1] ثبت calculator/date provider و اجرای GET `/api/product/payment-plan` در `backend/Retail.Api/Program.cs`؛ snapshot کامل محصول/برنامه همراه serverTime همان ساعت مبنا، کنترل invariants فقط در backend، expiresAt الزامی، no-store و ProblemDetailsهای قرارداد؛ query قیمت/تاریخ را تغییر ندهد و هیچ auth/ذخیره‌سازی/عملیات نوشتن تجاری اضافه نشود؛ POST validity صرفاً بررسی بدون اثر جانبی است. وابستگی: T022، T023، T024 و T012.
- [X] T026 [US1] پیاده‌سازی POST بدون اثر جانبی `/api/product/payment-plan/validity` در `backend/Retail.Api/Program.cs` و مدل‌های `backend/Retail.Api/Models/ValidityRequest.cs` و `ValidityResponse.cs`؛ مطابق OpenAPI1.2.0 فقط baseDate/expiresAt ورودی الزامی، بررسی ساختار/تاریخ و استخراج expiry استاندارد از snapshot قبلی با TehranDateProvider، isValid برای روز جاری و serverTime<expiry و تطبیق deadline؛ bad request400 و failure500، no-store؛ calculator، ذخیره‌سازی و GET برنامه فراخوانی نشوند. وابستگی: T025، T023 و T013؛ همان Program.cs و غیرموازی با endpointهای قبل.
- [X] T027 [P] [US1] پیاده‌سازی formatter رشتهٔ پول و تاریخ در `frontend/src/format.ts` مطابق T014؛ گروه‌بندی سه‌رقمی و ارقام فارسی/پسوند تومان، بدون Number/parseFloat/rounding یا محاسبهٔ جمع؛ Date.parse فقط برای serverTime/expiresAt در جریان انقضا مجاز است، نه تاریخ شمسی. وابستگی: T019 و T014؛ موازی با کارهای backend پس از آماده‌شدن پیش‌نیازها.
- [X] T028 [US1] پیاده‌سازی fetch و type guard ساختاری در `frontend/src/api.ts` برای دو GET قرارداد و POST validity و مصرف typeهای `contracts.ts`؛ بررسی شکل رشتهٔ مبلغ، metadata، چهار قسط شماره‌دار، شکل تاریخ و serverTime/expiresAt UTC معتبر، ValidityResponse و تطبیق پاسخ با snapshot جاری، خطای شبکه/JSON/ProblemDetails قابل نمایش؛ فقط required قرارداد الزامی باشد؛ ProblemDetails بدون errors پذیرفته و errors حاضر به‌صورت object غیرnull/غیرآرایه با مقادیر آرایهٔ رشته‌ها بررسی شود؛ ساخت برنامه/کنترل جمع یا دامنهٔ مالی در مرورگر انجام نشود؛ AbortSignal پشتیبانی شود. وابستگی: T019 و T015؛ مستقل از backend زنده با mock قابل آزمون است.
- [X] T029 [US1] ساخت صفحهٔ محصول و برنامه در `frontend/src/App.tsx` و سبک سادهٔ RTL در `frontend/src/App.css`؛ نام/قیمت از GET محصول، برنامه با کلیک از GET جدا، نمایش تاریخ مبنا/پیش‌پرداخت/چهار قسط مبلغ و سررسید/مجموع/بدون سود و کارمزد؛ وضعیت محصول جدا از برنامه باشد و فرم قیمت/تاریخ، router یا state library اضافه نشود. وابستگی: T027، T028 و T016.
- [X] T030 [US1] افزودن جریان انتظار/خطا/تلاش مجدد و جلوگیری از پاسخ قدیمی در `frontend/src/App.tsx`؛ برنامه هنگام درخواست تازه کنار گذاشته و محصول معتبر حفظ شود؛ تلاش مجدد فقط endpoint ناموفق را تکرار کند و پاسخ قدیمی با AbortController یا شناسهٔ درخواست برنامهٔ تازه را بازنویسی نکند. وابستگی: T029 و T016.
- [X] T031 [US1] پیاده‌سازی بودجهٔ اعتبار و checking/verification-error/expired در `frontend/src/App.tsx`؛ m0/m1 و RTT از performance.now، R=max(0,expiresAt−serverTime−RTT)، باقی‌مانده R−elapsed یکنواخت؛ Date.now/timeOrigin ممنوع، بودجهٔ صفر هرگز ready نشود؛ شناسهٔ دورهٔ lifecycle پاسخ round-trip عبورکرده از تعلیق را نامطمئن کند و نیازمند بررسی تازهٔ همان snapshot باشد؛ hidden/pagehide/freeze اعتماد قبلی را کنار بگذارد، visible/focus/pageshow/resume فقط POST validity و پیش از تأیید برنامه را پنهان کند؛ true و بودجهٔ مثبت همان snapshot را برگرداند، false منقضی و شکست بررسی پنهان بماند؛ اقدام بررسی دوباره فقط POST و محاسبهٔ دوباره فقط GET با کلیک باشد؛ محصول معتبر حفظ، timer/listener/request پاک و پاسخ قدیمی بی‌اثر شود. وابستگی: T030 و T017؛ همان فایل UI و غیرموازی با T029–T030.
- [X] T032 [US1] اجرای تست‌های `tests/Retail.Api.Tests/Retail.Api.Tests.csproj` با dotnet run و اصلاح فقط کد/تست مرتبط تا تمام مثال‌های مالی، تقویم، نیمه‌شب، expiry و قرارداد موفق شوند؛ expectedهای مصوب برای سبز کردن تست عوض نشوند؛ نبود endpoint نوشتن تجاری و بی‌اثر بودن قیمت/تاریخ query و صفر بودن فراخوانی calculator در POST validity اثبات شود. وابستگی: T026 و T009–T013.
- [X] T033 [P] [US1] اجرای مجموعهٔ `frontend/package.json` با `npm --prefix frontend run test -- --run` و بررسی نوع/ساخت با `npm --prefix frontend run build`؛ همهٔ تست‌های `format.test.ts`، `api.test.ts`، `App.test.tsx` و `App.expiry.test.tsx` موفق شوند؛ انتظار مبالغ و تعداد درخواست‌ها و clock/timer مستقل از زمان واقعی باشد. وابستگی: T031 و T014–T017؛ موازی با T032.

**Checkpoint**: US1 فقط وقتی کامل است که تمام تست‌های T032/T033 موفق و مسیر کاملِ
محصول، برنامه، خطا و انقضا قابل نمایش باشد؛ حذف انقضا از MVP یا محدودکردن به happy path مجاز نیست.

---

## Phase 4: Polish & Cross-Cutting Concerns

**Purpose**: بررسی مسیر واقعی، تکرارپذیری و تطبیق نهایی؛ بدون افزودن قابلیت یا ابزار اضافی.

- [X] T034 اجرای مسیر مرورگر و curl طبق `specs/001-show-payment-plan/quickstart.md` پس از T032/T033؛ نام/قیمت، برچسب‌ها، چهار قسط، تاریخ مبنا، خطا با توقف backend و تلاش مجدد بررسی شوند؛ انقضا/تعلیق با تست clock جعلی اجرا شود، نه تغییر ساعت واقعی سیستم؛ نتیجه و محدودیت‌ها در همان راهنما ثبت شوند.
- [X] T035 بررسی restore --locked-mode و npm ci/build با lockهای `backend/Retail.Api/packages.lock.json`، `tests/Retail.Api.Tests/packages.lock.json` و `frontend/package-lock.json` و تکمیل فرمان‌ها/نسخه‌های واقعاً نصب‌شده در `specs/001-show-payment-plan/quickstart.md` و `research.md`؛ `.gitignore` فقط اگر خروجی ابزار جدید پوشش ندارد تکمیل و قواعد موجود حفظ شوند. وابستگی: T034؛ بررسی دوباره فقط اگر تغییر نسخه/lock رخ داده است.
- [X] T036 بازبینی تطابق همهٔ FR-001..FR-017 و SC-001..SC-009 با `specs/001-show-payment-plan/spec.md`، قرارداد، تست‌ها و کد؛ وضعیت انجام کار و شواهد آزمون/محدودیت‌ها در `specs/001-show-payment-plan/tasks.md` ثبت شود؛ فقط پس از تحقق معیار checkbox تکمیل شود؛ قابلیت اضافه، دیتابیس و تغییر مهارت‌ها وجود نداشته باشند و بدون دستور مستقل کاربر commit/push انجام نشود. وابستگی: T034–T035.

---

## Dependencies & Execution Order

### Phase Dependencies

```text
Phase 1 (T001–T005)
  → Phase 2 (T006–T008)
    → Phase 3 / US1 (T009–T033)
      → Phase 4 (T034–T036)
```

### User Story Dependencies

US1 تنها داستان است و به داستان دیگری وابسته نیست. بدون دیتابیس، حساب، سفارش یا پرداخت
قابل پیاده‌سازی و آزمون مستقل است. US2/US3 مصنوعی از خطا و انقضا ایجاد نشده‌اند.

### Within Each User Story

گراف پیش‌نیازهای اصلی؛ جزئیات هر یال در توضیح همان وظیفه آمده است:

```text
Foundation → {T009,T010,T011,T012,T013,T014,T015,T016,T017} [tests first]
{T009..T013} → T018 → T020 → T021 → T022
{T018,T011} → T023
{T020,T012} → T024
{T024,T022,T023,T012} → T025
{T025,T023,T013} → T026 → T032
{T014..T017} → T019 → {T027,T028} → T029 → T030 → T031 → T033
{T032,T033} → T034 → T035 → T036
```

تست‌ها با امضاهای حداقلی سپس شکست رفتاری بررسی شوند؛ مدل پیش از سرویس، سرویس پیش از
endpoint و هردو شاخهٔ backend/frontend پیش از یکپارچه‌سازی و بررسی مرورگر باشند.
T024 تا T026 روی Program.cs و T029 تا T031 روی App.tsx ترتیبی هستند.

### Parallel Opportunities

- پس از T001: T002 و T003؛ پس از تکمیل ایجاد پروژه‌های متناظر: T004 و T005.
- پس از فاز۱: T008 در کنار زنجیرهٔ T006→T007؛ مانع فاز۲ قبل از US1 حفظ شود.
- پس از فاز۲: T009 تا T017 در فایل‌های تست جدا؛ fixtureهای مشترک را هم‌زمان تغییر ندهید.
- پس از نوشتن تست‌ها: T018 و T019؛ سپس T023 در کنار T020→T021→T022 و T027/T028
  در شاخهٔ frontend با رعایت وابستگی‌هایشان.
- پس از تکمیل کد هر شاخه: T032 و T033؛ مرورگر T034 منتظر هر دو است.

این موارد امکان زمان‌بندی هستند، نه دستور ایجاد زیرعامل یا آغاز پیاده‌سازی در این مرحله.

---

## Parallel Example: User Story 1

پس از فاز۲، وظایف مستقل نمونه:

```text
T009: tests/Retail.Api.Tests/CalculationTests.cs
T010: tests/Retail.Api.Tests/DueDateTests.cs
T011: tests/Retail.Api.Tests/TehranDateTests.cs
T012: tests/Retail.Api.Tests/ApiTests.cs
T016: frontend/src/App.test.tsx
T017: frontend/src/App.expiry.test.tsx
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
5. T032 تا T036 را انجام و نتایج واقعی گزارش کنید؛ انتشار یا کامیت خودکار در این فهرست نیست.

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

- ۳۶ وظیفه: Setup پنج، Foundational سه، US1 بیست‌وپنج، بررسی نهایی سه.
- ۹ وظیفهٔ نوشتن تست و ۲ وظیفهٔ اجرای مجموعه‌ها در US1؛ بررسی مرورگر در فاز۴ مکمل است.
- coverage: سناریوهای۱–۲۴ و FR-001–017 / SC-001–009 به وظایف تست و اجرا نگاشت شده‌اند.
- در مرحلهٔ تولید اولیه هیچ کدی ساخته نشده بود؛ وضعیت فعلی Phase 1/2 در checkboxها و quickstart ثبت می‌شود.
- اصل V تست‌های backend و توافق Q4 تست کامپوننت را لازم می‌کنند؛ تست‌ها اختیاری نیستند.
- مسیرهای Phase 1/2 ایجاد شده‌اند؛ مسیرهای وظایف بعدی هنوز هدف پیاده‌سازی آینده‌اند.

### شواهد تکمیل Phase 2 — 2026-10-09

- T006: DI برای TimeProvider و timezone، خطاهای فارسی camelCase، ثبت استثنا در لاگ،
  Program عمومی برای WebApplicationFactory؛ راه‌اندازی invariant (با دو حالت culture)
  و TZDIR خالی به‌طور صریح شکست خوردند؛ هیچ fallback وجود ندارد.
- T007: factory با TestServer، ساعت UTC قابل کنترل/شمارش و callback جایگزینی سرویس‌ها؛
  هیچ route/header تست در API ایجاد نشد. اتصال callback به نوع منبع محصول در T012/T020 است.
- T008: پروژه‌های Node و jsdom در Vitest، setup مشترک cleanup DOM/fetch/timers/mocks/env؛
  چهار تست جداسازی موفق؛ محیط تست backend زنده یا ساعت واقعی لازم ندارد.
- ساخت .NET با صفر هشدار/خطا و شش تست زیرساخت موفق؛ ساخت TS/Vite و چهار تست frontend موفق.
- فقط T001–T008 کامل‌اند؛ Phase 3 آغاز نشده، تست‌های پذیرش مالی/تاریخ و endpointها ساخته نشده‌اند.

### شواهد نوشتن تست‌های US1 — 2026-10-09

فقط T009–T017 پس از ساخت موفق و اجرای قرمز تیک خوردند؛
[گزارش اجرا](red-test-report.md) شمارش و علت شکست هر گروه را ثبت می‌کند.
بک‌اند87 تست (8 موفق/79 شکست، صفر execution error) و فرانت‌اند131 تست
(4 موفق/127 شکست) اجرا شدند. تمام شکست‌ها ناشی از رفتار غایب است؛ کامپایل و تنظیمات سالم‌اند.
مدل/امضاهای حداقلی placeholder هستند؛ منطق تولید، اتصال App، endpoint یا serialization
قابلیت ساخته نشده است. T018–T036 همچنان بازند؛ تیک نوشتن تست، موفقیت محصول نیست.

### شواهد تکمیل پیاده‌سازی US1 — 2026-10-09

T018–T033 پس از پیاده‌سازی و موفقیت آزمون‌ها کامل شدند؛
[گزارش Phase 3](phase3-test-report.md) رفتار، شمارش و محدودیت‌ها را ثبت می‌کند.
90 تست بک‌اند و136 تست فرانت‌اند موفق‌اند؛ تمام32 سناریوی انقضا/تعلیق تا انتها اجرا شدند.
ساخت .NET با صفر هشدار/خطا و build TypeScript/Vite موفق است. انتظارهای قبلی تغییر نکردند؛
فقط سه تست تکمیلی backend و پنج تست مرز ساختار frontend اضافه شدند.
T034–T036 اجرا نشده‌اند و باز مانده‌اند؛ کامیت و پوش انجام نشد.


## شواهد تکمیل Phase 4 — 2026-10-09

T034–T036 کامل شدند. مسیر واقعی Edge: نمایش محصول/برنامه، توقف بک‌اند، حذف جدول قبلی،
پیام خطا، حفظ محصول و تلاش مجدد موفق؛ تصاویر در evidence ثبت شدند. curl هشت پاسخ API
را با OpenAPI1.2.0، no-store، مبالغ string، query بی‌اثر و validity جاری/گذشته تطبیق داد.
90 تست بک‌اند و137 تست فرانت‌اند موفق‌اند؛32 تست انقضا/تعلیق با ساعت جعلی اجرا شدند.
ساعت سیستم تغییر نکرد. یک تست رد ProblemDetails.code خارج enum اضافه و شکاف مربوط
در api.ts اصلاح شد؛ expectedهای قبلی تغییر نکردند.

restore --locked-mode --force، npm ci و ساخت هر دو پروژه موفق؛ SHA-256 هر سه lock
یکسان ماند. تمام FR-001..FR-017 و SC-001..SC-009 با کد/قرارداد/تست‌ها نگاشت و بررسی
شدند؛ مهارت‌ها و قواعد رفتاری تغییر نکردند، قابلیت یا وابستگی جدید اضافه نشد.
جزئیات، شواهد و روش باز کردن در [phase4-validation-report.md](phase4-validation-report.md)
و quickstart.md است. تعلیق واقعی OS، BFCache، عبور واقعی نیمه‌شب و نصب روی دستگاه
خالی آزموده نشدند؛ روش خودکار مصوب آن‌ها در jsdom/TestServer اجرا شد. کامیت/پوش انجام نشد.
