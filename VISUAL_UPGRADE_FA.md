# Robot Heist — Visual & Mobility Update

پروژه Unity 2022.1.24f1. نسخه قبل (سه جزیره، پل، ماشین، فروشگاه، سلاح) حفظ شده است.

## اجرای نسخه
فایل را Extract کنید. پوشه unity را از Unity Hub باز کنید. Library حذف شده تا از نو ساخته شود. Scene اصلی Assets/Scenes/Main.unity است. Play را بزنید. DLL ویندوز نسخه قبلی حفظ شده است.

## حرکت
- WASD: حرکت سریع 8 m/s (قبلاً 5.5)
- Shift: اسپرینت 12 m/s (قبلاً 9)
- Alt چپ: حرکت آرام 3.2 m/s
- RMB: نشانه‌گیری و حرکت جانبی 5.8 m/s
- C/Ctrl: خم شدن 3.6 m/s؛ Space پرش؛ F خودرو.
- بافر پرش 0.14 ثانیه، مهلت پرش بعد لبه 0.10 ثانیه، شتاب و توقف سریع‌تر، FOV تطبیقی و فاصله دوربین نرم.
- انیمیشن روی استخوان‌های موجود و به صورت procedural است؛ موشن کپچر نیست.

## گرافیک آفلاین
مش‌های TexBox اکنون tangent دارند؛ Normal Map می‌تواند روی جاده، دیوار و کف به درستی نمایش داده شود. Normal Map تولیدی آب و شن، حرکت موج، MSAA چهار نمونه، HDR دوربین، فاصله دید و سایه بیشتر، گوشه‌های گرد روی خودروهای ساده و قطعات مدل‌های سلاح و متریال‌های براق/فلزی. با توجه به توان GPU تنظیمات را تست کنید.

## نصب اختیاری تکسچرهای اینترنتی
در Unity، خارج از Play Mode، منوی Robot Heist > Art > Download CC0 PBR textures (2K) را بزنید. این ابزار تلاش می‌کند ۹ مجموعه متریال Poly Haven را از CDN رسمی دریافت کند (Diffuse, Normal GL, Roughness, AO در صورت وجود). Roughness به Smoothness در کانال آلفا تبدیل می‌شود. مقادیر metallic برای فلز به صورت تقریبی تنظیم شده‌اند، نه نقشه واقعی فلز. تصاویر در Assets/Resources/ExternalArt/Textures ذخیره می‌شوند و هنگام اجرای بعدی اولویت دارند. در صورت عدم دسترسی یا خطای دانلود، متریال‌های قدیمی باقی می‌مانند. دانلود از محیط تهیه این ZIP ممکن نبود، بنابراین فایل‌های خارجی و اجرای ابزار دانلود تأیید نشده‌اند. هیچ مدل خارجی جدیدی در ZIP وجود ندارد.

## مدل‌های خارجی (اختیاری)
منابع را از https://polyhaven.com/models یا مدل‌های دارای مجوز مناسب (CC0/CC BY با رعایت انتساب) تهیه کنید. FBX یا OBJ را import کنید و متریال‌های Standard را تنظیم کنید. برای مدل‌های glTF باید قبل از واردکردن به FBX تبدیل شوند یا importer مناسب نصب شود. مدل نهایی را به صورت Prefab در Assets/Resources/ExternalArt/Models ذخیره کنید. واحد متر؛ محور Y بالا، Z جلو. Prefab صرفاً بصری، بدون اسکریپت خارجی یا Rigidbody فعال. فقط از منبع معتبر استفاده کنید.
- Rental.prefab، SUV.prefab، Sport.prefab: مرکز X/Z، کف Y=0، اندازه حدود 1.85 x 1.55 x 4.3 متر (X/Y/Z). چهار محور چرخ اختیاری با نام WheelFL, WheelFR, WheelRL, WheelRR (محور X چرخش، محور Y فرمان). برخورد ماشین با ابعاد فعلی مستقل از مدل است.
- Pistol.prefab، SMG.prefab، Shotgun.prefab، Rifle.prefab، LMG.prefab: مبدأ دست راست، +Z جلو، سه Transform با نام GripR, GripL, Muzzle. این نقاط محل IK و دهانه را تعیین می‌کنند. مدل فاقد نقاط لازم کنار گذاشته می‌شود و مدل قبلی جایگزین آن است.
- Bench.prefab: کف Y=0، عرض حدود 1.8 و عمق 0.5 متر. Collider اصلی بازی حفظ می‌شود.
- درخت‌های خارجی را به شکل Prefab در Assets/Resources/Models/Trees قرار دهید (قابلیت موجود پروژه).

## منابع انتخاب شده برای دانلود اختیاری
Poly Haven (CC0): https://polyhaven.com/license
- https://polyhaven.com/a/asphalt_02
- https://polyhaven.com/a/sand_02
- https://polyhaven.com/a/red_brick
- https://polyhaven.com/a/concrete_wall_009
- https://polyhaven.com/a/concrete_wall_004
- https://polyhaven.com/a/wooden_planks
- https://polyhaven.com/a/metal_plate_03
- https://polyhaven.com/a/concrete_brick_wall_001
- https://polyhaven.com/a/oak_wood_planks
برای مدل‌های سبک و غیرواقع‌گرایانه خودرو: https://kenney.nl/assets/car-kit (CC0). این بسته داخل ZIP وارد نشده است.
مجوز مدل‌های قدیمی که از سورس اولیه کاربر آمده‌اند مستقلاً بررسی نشده است.

## وضعیت تست
تست هسته C++: ALL OK. کد Unity و ابزار Editor به صورت ایستا بررسی شده‌اند؛ Unity Editor و رندر واقعی در محیط در دسترس نبوده‌اند. سرعت، کنترل دوربین، سایه، پرش کنار پل، ورود و خروج ماشین و عملکرد import باید در Play Mode بررسی شوند. نسخه جدید جایگزین کامل هنر حرفه‌ای نیست.
