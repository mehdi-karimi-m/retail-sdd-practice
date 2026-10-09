# گزارش اجرای قرمز T009–T017

تاریخ: 2026-10-09. این گزارش شواهد نوشتن و اجرای تست است، نه تأیید موفقیت محصول.

## ساخت و پیکربندی

- `dotnet build retail.sln --no-restore`: موفق، صفر خطا و صفر هشدار.
- `npm --prefix frontend run build`: بررسی TypeScript تمام فایل‌های src و ساخت Vite موفق.
- تمام فایل‌های تست کشف و اجرا شدند؛ هیچ suite با صفر تست/خرابی import یا تنظیمات وجود نداشت.
- xUnit execution errors=0؛ خطاهای assertion/NotImplemented در شمار failed قرار دارند.
- شش تست زیرساخت بک‌اند و چهار تست زیرساخت فرانت‌اند همچنان سبز هستند.

## نتیجهٔ اجرا

| فایل | کل | موفق | شکست |
| --- | ---: | ---: | ---: |
| CalculationTests.cs | 19 | 0 | 19 |
| DueDateTests.cs | 9 | 0 | 9 |
| TehranDateTests.cs | 7 | 0 | 7 |
| ApiTests.cs | 26 | 2 | 24 |
| ValidityApiTests.cs | 20 | 0 | 20 |
| InfrastructureTests.cs | 6 | 6 | 0 |
| setup.node.test.ts | 2 | 2 | 0 |
| App.expiry.test.tsx | 32 | 0 | 32 |
| App.test.tsx | 9 | 0 | 9 |
| api.test.ts | 69 | 0 | 69 |
| format.test.ts | 17 | 0 | 17 |
| setup.test.ts | 2 | 2 | 0 |

بک‌اند: 87 تست، 8 موفق و 79 شکست.
فرانت‌اند: 131 تست، 4 موفق و 127 شکست.
هر دو فرمان تست exit code=1 دادند؛ این نتیجه در مرحلهٔ قرمز مورد انتظار است.

## شواهد علت شکست

- محاسبات: `NotImplementedException: T021/T022: payment calculation`؛ اعتبارسنجی
  نامعتبر نیز ArgumentException انتظار دارد و NotImplementedException دریافت می‌کند.
- سررسید/زمان تهران: `T022: Persian due dates` و `T023: Tehran clock snapshot`؛
  هیچ فرمول یا تبدیل تاریخ برای سبز کردن تست اضافه نشده است.
- GET/POST قابلیت: انتظار200/400/405/500 و پاسخ404، یا فقدان no-store برای مسیر محصول
  مفقود؛ endpointها هنوز وجود ندارند. schema تست از OpenAPI1.2.0 خوانده می‌شود؛ نمونه‌های
  خود قرارداد و پاسخ مسیر ناشناخته معتبرند، اما این دو تست سبز به معنی ساخت قابلیت نیستند.
- parser/fetch/formatter: پیام‌های `T027` و `T028 ... not implemented`؛ انتظار پذیرش
  قرارداد یا پیام فارسی با placeholder فعلی برآورده نمی‌شود.
- UI/انقضا: عنصر «محصول نمونه» پیدا نمی‌شود چون App خالی است. سناریوها تا پایان نوشته
  شده‌اند، ولی assertionهای بعد از این پیش‌شرط هنوز اجرا نشده‌اند؛ پس نتیجهٔ RTT، تعلیق،
  retry و race تا زمان پیاده‌سازی محصول تأیید نشده است.

## دامنهٔ پوشش و محدودیت

- T009: پنج مثال مالی ثابت، دو مرز معتبر، decimal? خالی/کسری/خارج دامنه/قسط صفر و نام خالی.
- T010/T011: چهار جدول مستقل شمسی، تاریخ نامعتبر/خارج محدوده، سه لحظهٔ مرز تهران،
  clock متغیر با یک خواندن و TZ متفاوت با بازیابی تنظیمات در finally.
- T012/T013: هر دو GET، schema، رشتهٔ مبالغ/جمع دقیق، query بی‌اثر، بدنه و روش نامجاز،
  خطای منبع/محاسبه، تغییر روز، POST validity صحیح/بدشکل/منقضی/روز آینده/expiry ناسازگار،
  no-store، خرابی ساعت و عدم فراخوانی calculator/source هنگام بررسی.
- T014/T015: تمام مبلغ‌های متمایز پنج مثال، تاریخ نمایشی، requiredهای قرارداد، errors
  اختیاری و ساختارش، timestamp/metadata/اقساط، fetch و خطا؛ هیچ جمع پول در فرانت‌اند نیست.
- T016/T017: نمایش/سقف/خطا/retry و response قدیمی؛ ساعت تقویمی مستقل از monotonic،
  RTT کامل با شبکه نامتقارن و تأخیر parse، بودجهٔ صفر، lifecycle/تعلیق با توقف ساعت،
  بررسی pending/true/false/شکست و retry فقط POST، درخواست عبورکرده از تعلیق، snapshot و
  callback قدیمی، محاسبهٔ دستی روز تازه و cleanup timer/listener/fetch در unmount.

مدل‌ها و امضاهای تولید فقط برای کامپایل ساخته شده‌اند؛ بدنه‌های سرویس/formatter/parser/fetch
عمداً NotImplemented هستند و App مقدار null برمی‌گرداند. Program و main.tsx به هیچ
endpoint یا صفحهٔ قابلیت متصل نشده‌اند. T018–T036 کامل محسوب نمی‌شوند.

## بازتولید

```bash
dotnet build retail.sln --no-restore
dotnet run --project tests/Retail.Api.Tests --no-build --no-restore -- -result-xml /tmp/retail-us1-backend.xml
npm --prefix frontend run build
npm --prefix frontend run test -- --run --reporter=json --outputFile=/tmp/retail-us1-frontend.json
```

خروجی‌های خام اجرای همین نوبت در /tmp/retail-us1-backend.xml،
/tmp/retail-us1-backend.log، /tmp/retail-us1-frontend.json و /tmp/retail-us1-frontend.log هستند؛
این فایل‌های موقت در گیت ثبت نمی‌شوند. فایل حاضر خلاصهٔ قابل ثبت شواهد است.
