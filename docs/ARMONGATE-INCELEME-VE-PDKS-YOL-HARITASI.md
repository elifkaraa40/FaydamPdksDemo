# Armongate İncelemesi ve Faydam PDKS Yol Haritası

## 1. Rapor değerlendirmesi

Mevcut rapor, Armongate ekranlarından görülebilen modülleri düzenli biçimde sınıflandırıyor. Özellikle erişim, PDKS, ziyaretçi, donanım, denetim ve raporlama başlıkları yeni ürünün kapsamını anlamak için yararlı. Bununla birlikte aşağıdaki ifadeler doğrulanmış teknik bulgu gibi değil, **gözlem veya varsayım** olarak yazılmalıdır:

- Sistemin ACaaS modeliyle çalıştığı, yalnızca ekran görüntülerinden kesinleştirilemez.
- `AppInitializerService`, WebSocket ve `Ready` durumu tarayıcı geliştirici araçları ya da kaynak koduyla görülmediyse teknik kanıt değildir.
- `api.armongate.com` adresinin kullanılması, verilerin yalnızca merkezi ve güvenli bir yerde tutulduğunu tek başına kanıtlamaz.
- Giriş verisinin "şifrelenmiş" gönderildiği yerine, doğrulandıysa "TLS/HTTPS ile aktarım sırasında şifrelenir" denmelidir. Parola ayrıca istemci tarafında şifrelenmek zorunda değildir.
- "Tekil ID" alanı veri bütünlüğünün kanıtı değil, kullanıcıya ait bir iş anahtarı gereksiniminin göstergesidir. Veritabanı kısıtları ayrıca incelenmelidir.
- Ekranlardan RBAC benzeri bir yapı çıkarılabilir; ancak rol, izin, kapsam ve istisna modeli API davranışıyla doğrulanmadan kesin RBAC tanımı yapılmamalıdır.

Raporun daha profesyonel olması için her tespit şu sınıflardan biriyle etiketlenmelidir: **Ekran gözlemi**, **Ağ trafiği bulgusu**, **Dokümantasyon bilgisi**, **Varsayım**. Her teknik iddianın yanına kanıt tarihi, ekran/API adresi ve mümkünse ekran görüntüsü eklenmelidir.

## 2. Eksik iş alanları

Yeni PDKS yalnızca giriş-çıkış listesinden oluşmamalıdır. Asgari ürün kapsamı:

1. Organizasyon, işyeri, bölüm, pozisyon ve çalışan yönetimi
2. Vardiya şablonları, takvim ataması, gece vardiyası ve tolerans kuralları
3. Ham terminal olayları ile hesaplanan puantaj kayıtlarının ayrı tutulması
4. Geç kalma, erken çıkma, eksik/fazla çalışma ve mükerrer basım kuralları
5. İzin, resmi tatil, hafta tatili, fazla mesai ve çok seviyeli onay akışları
6. Manuel düzeltme talebi, gerekçe, önceki/yeni değer ve değişmez denetim izi
7. Terminal/okuyucu sağlığı, çevrimdışı kayıt, tekrar gönderim ve olay tekilleştirme
8. Rol + izin + veri kapsamı (şirket/işyeri/bölüm/kendi kaydı) yetkilendirmesi
9. KVKK gereği veri minimizasyonu, saklama süresi, dışa aktarma ve silme süreçleri
10. Bordro entegrasyonu, zamanlanmış raporlar, CSV/XLSX içe-dışa aktarma

## 3. Önerilen çözüm yapısı

Yönetici tarafından belirlenen dört projeli yapı korunacaktır. `Web` ve mobil `Api` ayrı host olsa da iş kuralları çoğaltılmaz; ortak sözleşmeler `Core`, veri erişimi `Data` üzerinden paylaşılır.

```text
FaydamPDKS.Web/   MVC, Razor görünümleri, web paneline özel API ve cookie oturumu
FaydamPDKS.Api/   Mobil uygulamaya özel, /api/v1 sürümlü JWT API
FaydamPDKS.Core/  Domain modelleri, DTO'lar, enum'lar ve interface'ler
FaydamPDKS.Data/  DbContext, repository uygulamaları ve migrations
tests/
  Faydam.Pdks.Domain.Tests/
  Faydam.Pdks.Application.Tests/
  Faydam.Pdks.IntegrationTests/
docs/
```

Mevcut `Core` projesinin ASP.NET `ControllerBase` bağımlılığı kaldırılmalı; domain katmanı web çatısından bağımsız kalmalıdır. `Web` ve `Api` birbirini referanslamamalı, ikisi de `Core` ve `Data` üzerinden çalışmalıdır. Web oturumu güvenli cookie, mobil oturumu kısa ömürlü JWT + rotating refresh token kullanmalıdır.

## 4. Güvenlik ve kalite kararları

- Tarayıcı oturumu için JWT'yi `localStorage` içinde tutmak yerine güvenli, `HttpOnly`, `Secure`, `SameSite` cookie kullanılmalı.
- Parola ve JWT anahtarları kaynak kodda tutulmamalı; ortam değişkeni veya secret store kullanılmalı.
- CORS yalnızca bilinen istemci origin'lerine açılmalı; web arayüzü aynı origin ise CORS gerekmeyebilir.
- Açık kayıt uç noktası üretimde kapalı olmalı; kullanıcı oluşturma yetkili yönetici işlemi olmalı.
- Dosya yüklemede yalnızca uzantı değil içerik türü/imza doğrulaması yapılmalı; dosyalar yeniden adlandırılmalı ve mümkünse web kökü dışında tutulmalı.
- Tüm tarihler veritabanında UTC, sunumda kullanıcı saat dilimi ile işlenmeli.
- Kritik hesaplamalar birim testleriyle; yetki ve veri kapsamı entegrasyon testleriyle korunmalı.

## 5. SEO ve erişilebilirlik

PDKS'nin oturum gerektiren yönetim ekranları arama motorlarında indekslenmemelidir; burada hedef klasik SEO değil, semantik HTML, erişilebilirlik ve performanstır. Giriş ve özel sayfalarda `noindex, nofollow`; varsa herkese açık ürün sayfasında özgün başlık/açıklama, canonical URL, Open Graph ve yapılandırılmış veri kullanılmalıdır. Tüm ekranlarda doğru başlık sırası, klavye erişimi, görünür odak, form etiketi, hata özeti ve yeterli renk kontrastı zorunludur.

## 6. Uygulama sırası

1. Çözüm yapısını ve güvenlik temelini düzelt
2. Organizasyon, çalışan ve vardiya domain modelini kur
3. Ham geçiş olayı alma ve tekilleştirme hattını geliştir
4. Puantaj hesaplama motorunu testlerle oluştur
5. İzin/düzeltme/onay akışlarını ekle
6. Dashboard ve rapor ekranlarını gerçek sorgulara bağla
7. Denetim izi, KVKK, performans ve güvenlik kontrollerini tamamla
