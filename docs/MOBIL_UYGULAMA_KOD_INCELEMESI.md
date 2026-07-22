# Faydam PDKS Mobil Uygulama Kod İncelemesi

İncelenen depo: `nzlyrncvk-nzlyrncvk/faydam-inovasyon-pdks-`

İnceleme tarihi: 20 Temmuz 2026

## Sonuç

Mobil uygulama mevcut haliyle mağaza/son kullanıcı yayınına hazır değildir. QR ile giriş-çıkış ve izin işlemlerinin bir bölümü API'ye bağlıdır; ana sayfa ile mola ekranında ise örnek/sabit veriler kullanılmaktadır. Kayıt sonrası yönetici onayı akışı uygulamanın yeniden açılmasında bozulmaktadır. Release sürümü debug anahtarıyla imzalanmakta ve Android uygulaması şifresiz HTTP trafiğine izin vermektedir.

## Arkadaşa gönderilecek zorunlu değişiklik listesi

### Değiştirilecek dosyalar

- `lib/config/api_config.dart`: üretim API adresini yalnızca `--dart-define=API_BASE_URL=https://.../api/v1` ile almalı; release sürümünde yerel HTTP varsayılanı bulunmamalı.
- `lib/services/api_service.dart`: kayıt, hesap durumu, gerçek puantaj geçmişi, mola, bildirim, düzeltme ve çalışma yeri uçları eklenmeli; kullanılmayan `/attendance/events` çağrısı silinmeli; eşzamanlı 401 yenileme yarışı giderilmeli.
- `lib/app_provider.dart`: hesap durumu ve kullanıcı modeli tutulmalı; tema tercihi kalıcı hale getirilmeli. Çalışmayan dil seçimi gerçek yerelleştirme yapılana kadar gösterilmemeli.
- `lib/main.dart`: oturum açılırken önce `/me/status` çağrılmalı. `PendingApproval` kullanıcı `/me` çağrısına gönderilmemeli; onay bekleme ekranına yönlendirilmeli.
- `lib/login_screen.dart`: parola alt sınırı backend ile aynı, 8 karakter olmalı. İşlevsiz “beni hatırla” kaldırılmalı veya gerçekten uygulanmalı. Kayıt ekranına bağlantı eklenmeli.
- `lib/home_screen.dart`: sabit `08:30`, fazla mesai ve haftalık satırlar kaldırılıp `/attendance/today` ve `/attendance?from=...&to=...` sonuçları gösterilmeli. `/work-locations/today` ile uzaktan/saha/ofis bilgisi eklenmeli.
- `lib/mola_screen.dart`: yerel sayaç ve sabit çalışan adları kaldırılmalı; `/breaks/current`, `/breaks/start`, `/breaks/{id}/end`, `/breaks/active-colleagues` kullanılmalı.
- `lib/izin_screen.dart`: dinamik map yerine tipli model kullanılmalı; `dayPortion` (`FullDay`, `FirstHalf`, `SecondHalf`) gönderilmeli.
- `lib/profile_screen.dart`: backend'in kabul etmediği `fullName` güncellemesi kaldırılmalı; telefonla birlikte mevcut e-posta/SMS tercihleri eksiksiz gönderilmeli. Kişisel veri dışa aktarımı ve bildirimler erişilebilir olmalı.
- `android/app/src/main/AndroidManifest.xml`: release yapılandırmasında `android:usesCleartextTraffic="false"` olmalı.
- `android/app/build.gradle.kts`: debug imzası release için kullanılmamalı; yükleme anahtarı ortam değişkenleri/yerel `key.properties` üzerinden bağlanmalı.
- `pubspec.yaml`: paket adı düzenlenmeli, gerçek yerelleştirme seçilecekse `flutter_localizations` ve `intl` yapılandırılmalı.

### Eklenecek dosyalar

- `lib/models/session_models.dart`
- `lib/models/attendance_models.dart`
- `lib/models/break_models.dart`
- `lib/models/leave_models.dart`
- `lib/models/notification_models.dart`
- `lib/services/api_exception.dart`
- `lib/register_screen.dart`
- `lib/pending_approval_screen.dart`
- `lib/notifications_screen.dart`
- `test/services/api_service_test.dart`
- `test/session_gate_test.dart`

### Silinecek yapı

- Depo içindeki ikinci ve boş Flutter projesi olan `faydam_pdkspro/` kaldırılmalı. Tek depoda iki proje bulunması build ve teslim sürecini belirsizleştiriyor.

## Backend ile doğru API sözleşmesi

| İşlev | Metot ve yol | Mobil durum |
|---|---|---|
| Kayıt | `POST /auth/register` | Eksik |
| Giriş | `POST /auth/login` | Var |
| Token yenileme | `POST /auth/refresh` | Var, yarış koşulu var |
| Çıkış | `POST /auth/logout` | Var |
| Hesap/onay durumu | `GET /me/status` | Eksik, kritik |
| Profil | `GET /me` | Var |
| Profil güncelleme | `PUT /me` | Yanlış gövde gönderiyor |
| Kişisel veri dışa aktarımı | `GET /me/export` | Eksik |
| Bugünkü puantaj | `GET /attendance/today` | Serviste var, ekran kullanmıyor |
| Puantaj aralığı | `GET /attendance?from=&to=` | Eksik |
| QR okutma | `POST /qr-attendance/scan` | Var |
| Aktif mola | `GET /breaks/current` | Eksik |
| Mola başlat/bitir | `POST /breaks/start`, `POST /breaks/{id}/end` | Eksik |
| Moladaki çalışma arkadaşları | `GET /breaks/active-colleagues` | Eksik; ekranda sahte veri var |
| İzinler | `GET/POST/DELETE /leave-requests` | Kısmen var |
| Puantaj düzeltme | `GET/POST /attendance-corrections` | Eksik |
| Bildirimler | `GET /notifications`, `POST /notifications/{id}/read` | Eksik |
| Günün çalışma yeri | `GET /work-locations/today` | Eksik |
| Saha çalışma talepleri | `/work-locations/field-requests` | Eksik |

## Kritik sözleşme ayrıntıları

- Parola en az 8 karakterdir. Mobil uygulamadaki 6 karakter kontrolü yanlıştır.
- Kayıt olan kullanıcı `PendingApproval` durumunda token alır. Bu kullanıcı yalnızca `/me/status`, `/auth/refresh` ve `/auth/logout` uçlarına erişebilir.
- `PUT /me` gövdesi `phoneNumber`, `isEmailNotificationEnabled`, `isSmsNotificationEnabled` alanlarını kabul eder. `fullName` kabul etmez.
- İzin oluştururken `dayPortion` alanı gönderilmelidir; varsayılan `FullDay` olsa da yarım gün seçeneği kullanıcıya sunulmalıdır.
- QR `qrValue` değeri değiştirilmeden gönderilmelidir. Her cihaz olayı için kalıcı ve benzersiz `deviceEventId` üretilmelidir.
- `FlutterSecureStorage.deleteAll()` kullanılmamalı; yalnızca uygulamanın access/refresh/expiry anahtarları silinmelidir.

## Yayına çıkış kabul ölçütleri

- Android release APK/AAB gerçek release anahtarıyla imzalanıyor.
- Release sürümünde yalnızca HTTPS API kullanılıyor.
- Yeni kayıt, onay bekleme, ret, askıya alma ve aktif hesaba geçiş akışları test edildi.
- Ana sayfa, mola, izin ve QR ekranlarında hiçbir örnek/sabit personel veya puantaj verisi kalmadı.
- Uçak modu, timeout, 401/403, token yenileme ve çift dokunma/çift QR senaryoları test edildi.
- En az servis testleri ve oturum yönlendirme widget testleri CI içinde çalışıyor.
- Gizlilik politikası, KVKK aydınlatma metni, destek adresi ve mağaza görselleri hazır.

## Önemli teslim notu

Tam sınıflar hazırlanırken backend sözleşmesi kaynak alınmalıdır; mevcut mobil sınıflar kopyalanıp yalnızca URL değiştirilmemelidir. Özellikle `ApiService`, `SessionGate`, `HomeScreen` ve `MolaScreen` birlikte değiştirilmezse uygulama görünürde çalışsa bile yanlış/sahte bilgi göstermeye devam eder.
