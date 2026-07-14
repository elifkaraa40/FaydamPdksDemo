# Faydam PDKS

Faydam PDKS; web yönetim paneli ve mobil uygulama için ayrı API sunan, ASP.NET Core 8 ve PostgreSQL tabanlı personel devam kontrol sistemi temelidir.

## Çözüm yapısı

```text
FaydamPDKS.Web    MVC yönetim paneli, güvenli cookie oturumu
FaydamPDKS.Api    Yalnızca mobil uygulamanın kullandığı /api/v1 JWT API
FaydamPDKS.Core   Domain modelleri, DTO'lar, enum'lar, arayüzler ve puantaj motoru
FaydamPDKS.Data   EF Core DbContext, repository'ler ve PostgreSQL migration'ları
FaydamPDKS.Tests  Birim ve servis testleri
```

`Web` ve `Api` birbirini referanslamaz. Ortak iş sözleşmeleri `Core`, kalıcı veri erişimi `Data` katmanındadır.

## Gereksinimler

- .NET 8 SDK
- PostgreSQL 15 veya üzeri
- EF Core CLI: `dotnet tool install --global dotnet-ef`

## Yerel kurulum

1. PostgreSQL üzerinde `faydam_pdks` veritabanı oluşturun.

PostgreSQL kurulu değilse `.env.example` dosyasını `.env` adıyla kopyalayıp güçlü bir parola belirledikten sonra yalnızca veritabanını container içinde başlatabilirsiniz:

```powershell
Copy-Item .env.example .env
docker compose up -d postgres
docker compose ps
```

2. Secret değerlerini kaynak koda yazmadan yapılandırın:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=faydam_pdks;Username=postgres;Password=PAROLANIZ" --project FaydamPDKS.Web
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=faydam_pdks;Username=postgres;Password=PAROLANIZ" --project FaydamPDKS.Api
dotnet user-secrets set "Jwt:Key" "EN-AZ-32-KARAKTERLIK-GELISTIRME-ANAHTARI" --project FaydamPDKS.Api
```

3. Şemayı oluşturun ve uygulamaları çalıştırın:

```powershell
dotnet ef database update --project FaydamPDKS.Data --startup-project FaydamPDKS.Api
dotnet run --project FaydamPDKS.Api
dotnet run --project FaydamPDKS.Web
```

Geliştirme ortamında `SeedDemoData=true` olduğunda web uygulaması örnek yönetici, personel ve standart vardiya verisi oluşturur. Demo parolaları yalnızca yerel geliştirme içindir; üretimde seeding kapalı tutulmalıdır.

## Mobil uygulama

Mobil istemci yalnızca `FaydamPDKS.Api` projesine bağlanır. Swagger geliştirme ortamında `/swagger` adresindedir. Kimlik doğrulama kısa ömürlü access token ve döndürülen, hash'lenerek saklanan refresh token kullanır.

Ayrıntılı istek/yanıt örnekleri için [mobil API entegrasyon rehberine](docs/MOBIL-API-ENTEGRASYON-REHBERI.md) bakın.

## Uygulanan PDKS işlevleri

- İşyeri, bölüm, personel ve rol yönetimi
- Gündüz/gece vardiyası ile tarih aralıklı personel ataması
- Genel veya işyeri bazlı resmi tatil, hafta tatili ve özel çalışma günü
- Mobil giriş/çıkış olaylarında cihaz kimliğiyle tekilleştirme
- Vardiyaya göre geç kalma, eksik çıkış ve fazla mesai hesabı
- Mobil izin talebi ve web yönetici onayı
- Mobil puantaj düzeltme talebi ve web yönetici onayı
- Kalıcı mobil bildirimler
- Tarih filtreli puantaj raporu ve UTF-8 CSV dışa aktarma
- Kritik onay kararlarında eski/yeni değerli denetim izi
- Hash'lenmiş cihaz anahtarıyla terminal heartbeat ve çevrimdışı/kuyruk izleme
- Çalışanın yalnızca kendi verilerini kapsayan KVKK JSON dışa aktarımı

Fiziksel okuyucu entegrasyonu için [terminal entegrasyon rehberine](docs/TERMINAL-ENTEGRASYON-REHBERI.md) bakın.
Saklama ve veri sahibi süreçleri için [KVKK veri yönetimi taslağına](docs/KVKK-VERI-YONETIMI.md) bakın.

## Veritabanı değişiklikleri

Model değişikliğinde migration `Data` projesine eklenir:

```powershell
dotnet ef migrations add DegisiklikAdi --project FaydamPDKS.Data --startup-project FaydamPDKS.Api
dotnet ef database update --project FaydamPDKS.Data --startup-project FaydamPDKS.Api
```

Üretim veritabanında migration çalıştırmadan önce yedek alınmalı ve üretilen SQL gözden geçirilmelidir.
Tüm migration zincirini tekrar çalıştırmaya dayanıklı PostgreSQL betiği olarak üretmek için:

```powershell
dotnet ef migrations script --idempotent --project FaydamPDKS.Data --startup-project FaydamPDKS.Api --output output/FaydamPDKS_Migrations.sql
```

Bu çalışma alanında doğrulanmış örnek betik [output/FaydamPDKS_Migrations.sql](output/FaydamPDKS_Migrations.sql) altında yer alır.

## Kalite kontrolü

```powershell
dotnet restore FaydamPDKS.sln
dotnet build FaydamPDKS.sln --no-restore
dotnet test FaydamPDKS.sln --no-restore
```

Canlılık kontrolleri:

- `/health/live`: uygulama süreci çalışıyor mu?
- `/health/ready`: uygulama PostgreSQL'e bağlanıp sorgu çalıştırabiliyor mu?

Özel yönetim ekranları arama motorlarına kapalıdır (`noindex, nofollow`). Buradaki kalite hedefi klasik SEO yerine semantik HTML, klavye erişimi, performans ve güvenli oturum yönetimidir.

## Güvenlik notları

- Üretim sırları yalnızca ortam değişkeni veya secret store üzerinden verilir.
- Web oturumu `HttpOnly`, `Secure`, `SameSite=Lax` cookie kullanır.
- Mobil API JWT doğrulaması ve rotating refresh token kullanır.
- Açık kullanıcı kaydı yoktur; personeli yalnızca yönetici oluşturur.
- Saatler veritabanında UTC, ekranda `Europe/Istanbul` saat diliminde değerlendirilir.
- Üretim öncesinde KVKK saklama/silme politikası, yedekleme, merkezi loglama ve gerçek terminal entegrasyonu ayrıca yapılandırılmalıdır.
