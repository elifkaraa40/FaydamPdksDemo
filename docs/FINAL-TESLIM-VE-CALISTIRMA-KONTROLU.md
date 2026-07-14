# FaydamPDKS final teslim ve çalıştırma kontrolü

## Teslim kapsamı

- `FaydamPDKS.Web`: MVC yönetim paneli ve web oturumu
- `FaydamPDKS.Api`: sürümlenmiş mobil API ve terminal uçları
- `FaydamPDKS.Core`: modeller, DTO'lar, enum'lar ve servis sözleşmeleri
- `FaydamPDKS.Data`: EF Core, PostgreSQL, servisler ve migration'lar
- `FaydamPDKS.Tests`: birim, servis, güvenlik ve HTTP host testleri

Personel, organizasyon, vardiya, iş takvimi, izin, puantaj, düzeltme talebi,
bildirim, denetim kaydı, terminal yönetimi ve kişisel veri dışa aktarma akışları
uygulanmıştır. Mobil ekip için sözleşme `MOBIL-API-ENTEGRASYON-REHBERI.md`
dosyasındadır.

## Doğrulama sonucu — 14 Temmuz 2026

- Çözüm derleniyor.
- 32 testin 32'si başarılı.
- Web giriş sayfası ve API hata sözleşmesi gerçek HTTP host üzerinden test edildi.
- NuGet güvenlik taramasında bilinen zafiyetli paket bulunmadı.
- PostgreSQL için idempotent migration betiği `output/FaydamPDKS_Migrations.sql`
  olarak üretildi.

## İlk yerel çalıştırma

Docker zorunlu değildir. Bilgisayarda çalışan PostgreSQL 18 servisi kullanılabilir.
Parolaları kaynak koda veya `appsettings.json` içine yazmayın; user-secrets kullanın:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=faydampdks;Username=postgres;Password=PAROLANIZ" --project FaydamPDKS.Web
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=faydampdks;Username=postgres;Password=PAROLANIZ" --project FaydamPDKS.Api
dotnet user-secrets set "Jwt:Key" "EN_AZ_32_KARAKTER_RASTGELE_GIZLI_ANAHTAR" --project FaydamPDKS.Api
dotnet ef database update --project FaydamPDKS.Data --startup-project FaydamPDKS.Web
dotnet run --project FaydamPDKS.Web
dotnet run --project FaydamPDKS.Api
```

Geliştirme verisi gerekiyorsa yalnızca geliştirme ortamında `SeedDemoData=true`
ayarlanabilir. Varsayılan demo hesapları üretimde kesinlikle kullanılmamalıdır.

Docker tercih edilirse `compose.yaml` aynı bağımlılıkları taşınabilir biçimde başlatır;
ancak mevcut PostgreSQL servisi varken ayrıca Docker kurmak projeyi tamamlamak için
gerekli değildir.

## Yayın öncesi şirkete bağlı kararlar

- Gerçek alan adı, HTTPS sertifikası ve ters proxy ayarları
- Güçlü üretim JWT anahtarı ve gizli bilgilerin kasa/ortam değişkenlerinde tutulması
- KVKK saklama ve silme sürelerinin şirket politikasıyla kesinleştirilmesi
- Yedekleme, log merkezi ve alarm/izleme altyapısı
- Kullanılacak fiziksel terminal markasının olay protokolüne özel adaptör

Bu maddeler kod eksiği değil, hedef şirket ve sunucu ortamına göre yapılacak yayın
konfigürasyonlarıdır.
