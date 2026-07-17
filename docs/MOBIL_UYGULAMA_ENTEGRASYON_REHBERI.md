# Faydam PDKS Mobil Uygulama Entegrasyon Rehberi

Bu belge mobil uygulamayı geliştiren ekip için bağlayıcı entegrasyon sözleşmesidir. Mobil uygulama backend kurallarını kendi içinde yeniden üretmemeli; API'nin döndürdüğü durum ve hata kodlarını esas almalıdır.

## Bugünkü değişikliklerin mobil kontrol listesi

- [ ] SMS doğrulama ekranı, doğrulama kodu alanı ve SMS ile ilgili bütün istemci kodları kaldırılacak.
- [ ] Kayıt ekranı ad soyad + telefon + parola olacak; parola 8-72 karakter doğrulanacak.
- [ ] Giriş ekranı telefon + parola kullanacak; uygulama tekrar açıldığında refresh token ile oturum sürdürülecek.
- [ ] Yeni kullanıcı `PendingApproval` ekranına yönlendirilecek; bu ekranda QR, izin, mola ve puantaj menüleri açılmayacak.
- [ ] Kullanıcı uygulamayı öne getirdiğinde hesap durumu yenilenecek; yönetici onayladıysa token refresh edilip ana ekran açılacak.
- [ ] Giriş ve çıkış için işyerindeki iki ayrı statik QR desteklenmeye devam edecek; dinamik QR istenmeyecek.
- [ ] Aynı QR geçişi art arda okutulduğunda `DUPLICATE_TRANSITION` kullanıcıya anlaşılır biçimde gösterilecek.
- [ ] Giriş kaydı olmadan çıkış okutulmasına mobil engel konulmayacak; backend bunu `MissingEntry` olarak işleyecek.
- [ ] QR sonucunda telefon zamanı değil API'nin döndürdüğü sunucu zamanı gösterilecek.
- [ ] 08:30-18:00 vardiyası ve 12:30-13:30 planlı ücretsiz öğle arası bilgi amaçlı gösterilebilecek.
- [ ] Mobil “mola ver” işlemi planlı öğle arasından ayrı ilave ücretsiz mola olarak ele alınacak.
- [ ] Mola başlayan/bitiren çalışan için yönetici veya çalışana bildirim üretilmeyecek.
- [ ] Moladaki çalışanlar `active-colleagues` API'sinden gösterilecek; istemci kendi listesini tahmin etmeyecek.
- [ ] İzin listesinde takvim günü yanında esas olarak `workDayCount` gösterilecek; hafta sonları ve resmi/özel tatiller iş günü sayılmayacak.
- [ ] Tam gün, sabah yarım gün ve öğleden sonra yarım gün izin seçenekleri eklenecek.
- [ ] İzin türü, izin durumu ve gün bölümü etiketleri Türkçe/İngilizce localization dosyalarından gösterilecek.
- [ ] Personel ana sayfasında günlük çalışma, beklenen süre, fazla mesai ve eksik kayıt durumu gösterilecek.
- [ ] Çalışma kayıtları tarih aralığıyla görüntülenebilecek ve kişisel veri dışa aktarma seçeneği sunulacak.
- [ ] İzin onayı, izin reddi, puantaj düzeltme sonucu ve eksik giriş gibi güncel bildirimler uygulama açılışında yenilenecek.
- [ ] Mola bildirim türü eklenmeyecek.
- [ ] Türkçe ve İngilizce bütün mobil metinler merkezi localization dosyasında tutulacak; ekrana sabit metin yazılmayacak.
- [ ] Hata, boş liste, yükleniyor, çevrimdışı, kamera izni reddi ve oturum süresi dolma ekranları tasarlanacak.

## 1. Kesinleşen ürün kararları

- SMS doğrulaması yoktur ve eklenmeyecektir.
- Kayıt alanları: ad soyad, Türkiye cep telefonu ve parola.
- Parola 8–72 karakter olmalıdır.
- Yeni kayıt `PendingApproval` durumunda oluşturulur.
- Yönetici web panelinden personel numarası ve departman atayarak hesabı onaylar.
- Onay bekleyen kullanıcı QR, izin, mola ve puantaj uçlarını kullanamaz.
- Giriş telefon ve parolayla yapılır.
- Kullanıcı her açılışta parola girmemelidir. Access ve refresh token güvenli cihaz deposunda saklanmalıdır.
- İşyerinde ayrı, statik giriş ve çıkış QR kodları kullanılacaktır.
- Dinamik QR veya SMS maliyeti oluşturulmayacaktır.
- Vardiya 08:30–18:00, planlı öğle arası 12:30–13:30'dur.
- Planlı öğle arası ücretsizdir. Mobil uygulamadan başlatılan diğer molalar ilave ücretsiz mola olarak çalışma süresinden düşer.

## 2. Genel API kuralları

- Temel adres geliştirme/canlı ortam yapılandırmasından gelmelidir; kaynak koda sabit yazılmamalıdır.
- API öneki: `/api/v1`
- JSON alan adları camelCase döner.
- Tarihler ISO-8601 kullanılmalıdır.
- Yetkili isteklerde `Authorization: Bearer <accessToken>` gönderilmelidir.
- Her cihaz işlemi için benzersiz `deviceEventId` üretin. Öneri: UUID v4.
- Hata gövdesi genel olarak şöyledir:

```json
{
  "code": "DUPLICATE_TRANSITION",
  "message": "Aynı geçiş türü art arda okutulamaz.",
  "details": null,
  "traceId": "..."
}
```

Mobil uygulama karar verirken `message` yerine `code` alanını kullanmalıdır. Mesaj kullanıcıya gösterilebilir.

## 3. Kayıt, giriş ve oturum akışı

### Kayıt

`POST /api/v1/phone-auth/register`

```json
{
  "fullName": "Elif Yılmaz",
  "phoneNumber": "05551234567",
  "password": "Guvenli123!",
  "deviceName": "Samsung A54"
}
```

Telefon `05xxxxxxxxx`, `5xxxxxxxxx` veya `+905xxxxxxxxx` biçiminde gönderilebilir. Başarılı kayıt access/refresh token döndürür; kullanıcı durumu `PendingApproval` olur.

Muhtemel hata kodları:

- `PHONE_ALREADY_REGISTERED`: Bu telefonla daha önce kayıt var; giriş ekranına yönlendir.
- `VALIDATION_ERROR`: Alanları form üzerinde göster.
- `INVALID_PHONE`: Telefon biçimi geçersiz.

### Telefon ve parola ile giriş

`POST /api/v1/phone-auth/login`

```json
{
  "phoneNumber": "05551234567",
  "password": "Guvenli123!",
  "deviceName": "Samsung A54"
}
```

Başarılı yanıt:

```json
{
  "accessToken": "jwt",
  "refreshToken": "random-token",
  "expiresAt": "2026-07-17T12:00:00Z",
  "user": {
    "id": "uuid",
    "fullName": "Elif Yılmaz",
    "email": "pending-...@phone.local",
    "role": "Personel",
    "profileImageUrl": null,
    "accountStatus": "PendingApproval",
    "phoneNumber": "+905551234567"
  }
}
```

`email` alanı mobil kayıtlı kullanıcı için sistemsel olabilir; mobil arayüzde gösterilmesi zorunlu değildir.

### Token yenileme

`POST /api/v1/auth/refresh`

```json
{ "refreshToken": "saklanan-refresh-token" }
```

Yanıttaki hem access hem refresh token eskilerinin üzerine yazılmalıdır. Refresh token rotasyonu vardır; eski token tekrar kullanılmamalıdır.

### Çıkış

`POST /api/v1/auth/logout`

```json
{ "refreshToken": "saklanan-refresh-token" }
```

Başarılı yanıt `204` olur. Sonrasında cihazdaki iki token da silinmelidir.

### Uygulama açılış algoritması

1. Güvenli depoda refresh token yoksa giriş ekranını göster.
2. Access token geçerliyse hesap durumunu sorgula.
3. Access token süresi bittiyse bir kez refresh isteği yap.
4. Refresh başarılıysa yeni iki tokenı kaydet.
5. Refresh `401` dönerse tokenları sil ve giriş ekranını göster.
6. `accountStatus == PendingApproval` ise yalnızca onay bekleme ekranını göster.
7. Onay bekleme ekranında `GET /api/v1/me/status` periyodik değil; kullanıcı yenilediğinde veya uygulama öne geldiğinde çağrılmalıdır.
8. Durum `Active` olduğunda bir kez refresh yapılarak aktif durumlu yeni JWT alınmalı ve ana ekrana geçilmelidir.

### Hesap durumu

`GET /api/v1/me/status`

```json
{
  "accountStatus": "Active",
  "canUseApplication": true,
  "message": "Hesabınız aktif."
}
```

Durumlar: `PendingApproval`, `Active`, `Rejected`, `Suspended`.

API diğer korumalı uçlarda onay bekleyen hesaba `403 ACCOUNT_PENDING` döndürür. Interceptor bu hatada onay bekleme ekranına yönlendirmelidir.

## 4. QR giriş ve çıkış

`POST /api/v1/qr-attendance/scan`

```json
{
  "qrValue": "kameradan-okunan-ham-deger",
  "occurredAt": "2026-07-17T08:29:00+03:00",
  "deviceEventId": "uuid-v4"
}
```

Backend güvenlik için telefon saatini değil sunucu saatini esas alır. `occurredAt` sözleşme uyumu için gönderilse de ekranda başarı yanıtındaki zaman kullanılmalıdır.

Başarılı yanıt (`201`):

```json
{
  "eventType": "Giris",
  "workplaceName": "Merkez İşyeri",
  "zoneName": "Faydam Merkez Giriş-Çıkış",
  "occurredAt": "2026-07-17T05:29:03Z"
}
```

Kurallar:

- Aynı `deviceEventId` tekrar gönderilirse `409 DUPLICATE_EVENT`.
- Aynı geçiş türü art arda okutulursa `409 DUPLICATE_TRANSITION`.
- Geçersiz/pasif/eski QR için `400 INVALID_OR_INACTIVE_QR`.
- Giriş yapmadan çıkış okutmak kabul edilir. Gün `MissingEntry` olarak raporlanır ve yöneticiye bildirim oluşur.
- Çıkış sırasında aktif ilave mola varsa backend otomatik kapatır.
- Kamera yalnızca QR tarama sayfasında açılmalı; başarıdan sonra tarama kilitlenerek çift gönderim önlenmelidir.

## 5. Ana sayfa ve puantaj

Bugün: `GET /api/v1/attendance/today`

Tarih aralığı: `GET /api/v1/attendance?from=2026-07-01&to=2026-07-31`

```json
{
  "workDate": "2026-07-17",
  "status": "Complete",
  "firstEntry": "2026-07-17T05:30:00Z",
  "lastExit": "2026-07-17T15:00:00Z",
  "workedMinutes": 510,
  "expectedMinutes": 510,
  "lateMinutes": 0,
  "overtimeMinutes": 0
}
```

Durumlar en az: `Complete`, `MissingEntry`, `MissingExit`, `NoRecord`, `NonWorkingDay`.

Ana ekranda gösterilmesi gerekenler:

- Bugünkü durum
- İlk giriş / son çıkış
- Çalışılan ve beklenen süre
- Fazla mesai
- Aktif mola bilgisi
- Bekleyen izin/bildirim özeti
- Büyük ve belirgin “QR okut” butonu

## 6. Molalar

- Mevcut durum: `GET /api/v1/breaks/current`
- Başlat: `POST /api/v1/breaks/start`
- Bitir: `POST /api/v1/breaks/{breakId}/end`
- Geçmiş: `GET /api/v1/breaks?from=2026-07-01&to=2026-07-31`
- Moladaki ekip arkadaşları: `GET /api/v1/breaks/active-colleagues`

Başlatma gövdesi:

```json
{ "deviceEventId": "uuid-v4" }
```

Bitirme gövdesi aynıdır. Güncel durum:

```json
{ "isOnBreak": true, "breakId": "uuid", "startedAt": "2026-07-17T10:15:00Z" }
```

Hata kodları:

- `BREAK_ALREADY_ACTIVE`
- `BREAK_REQUIRES_ACTIVE_ATTENDANCE`
- `ACTIVE_BREAK_NOT_FOUND`
- `DUPLICATE_EVENT`

Mola başlatmak için son 24 saat içinde açık giriş olmalıdır. Öğle arası otomatik planlıdır; mobil mola butonu ilave mola kaydıdır. Mola için yönetici bildirimi gönderilmez.

## 7. İzinler

- Liste: `GET /api/v1/leave-requests`
- Oluştur: `POST /api/v1/leave-requests`
- Bekleyen talebi iptal: `DELETE /api/v1/leave-requests/{id}`

```json
{
  "leaveType": "Annual",
  "startDate": "2026-07-17",
  "endDate": "2026-07-20",
  "reason": "Aile ziyareti",
  "dayPortion": "FullDay"
}
```

`dayPortion`: `FullDay`, `Morning`, `Afternoon`. Yarım gün seçildiyse başlangıç ve bitiş aynı gün olmalıdır.

İzin türleri API enum değerleriyle gönderilir; kullanıcıya Türkçe/İngilizce etiket mobilde çevrilir. Kullanıcıya `calendarDayCount` değil esas olarak `workDayCount` gösterilmelidir. Hafta sonları ve çalışma takvimindeki tatiller iş günü hesabına dahil edilmez.

Hatalar: `INVALID_LEAVE_REQUEST`, `OVERLAPPING_LEAVE_REQUEST`, `LEAVE_REQUEST_NOT_CANCELLABLE`.

## 8. Puantaj düzeltme talepleri

- Liste: `GET /api/v1/attendance-corrections`
- Oluştur: `POST /api/v1/attendance-corrections`

```json
{
  "workDate": "2026-07-16",
  "requestedEntry": "08:30:00",
  "requestedExit": "18:00:00",
  "reason": "Girişte QR okutmayı unuttum."
}
```

Gerekçe en az 10, en fazla 500 karakterdir. Aynı gün için bekleyen talep varsa `409 PENDING_CORRECTION_EXISTS` döner.

## 9. Bildirimler

- Liste: `GET /api/v1/notifications`
- Okundu: `POST /api/v1/notifications/{id}/read` → `204`

Liste öğesinde `isRead`, `readAt`, `type`, `title`, `message` ve `relatedEntityId` bulunur. Uygulama açıldığında ve ana sayfa yenilendiğinde liste çağrılmalıdır. Mola için bildirim gösterilmemelidir.

## 10. Profil ve veri dışa aktarma

- Profil: `GET /api/v1/me`
- Profil güncelle: `PUT /api/v1/me`
- Kişisel veri: `GET /api/v1/me/export`

SMS kullanılmadığı için mobil arayüzde “SMS bildirimleri” seçeneği gösterilmemelidir. Profil güncellemesinde `isSmsNotificationEnabled: false` gönderilebilir.

## 11. Flutter mimari önerisi

Önerilen paketler:

- `dio`: HTTP ve interceptor
- `flutter_secure_storage`: access/refresh token
- `mobile_scanner`: QR okuma
- `go_router`: durum tabanlı yönlendirme
- Projede zaten kullanılan state yönetimi varsa onu koruyun; sırf bu entegrasyon için ikinci bir state yönetimi eklemeyin.

Token deposu:

```dart
class TokenStore {
  TokenStore(this.storage);
  final FlutterSecureStorage storage;

  Future<String?> get accessToken => storage.read(key: 'access_token');
  Future<String?> get refreshToken => storage.read(key: 'refresh_token');

  Future<void> save(String access, String refresh) async {
    await storage.write(key: 'access_token', value: access);
    await storage.write(key: 'refresh_token', value: refresh);
  }

  Future<void> clear() async {
    await storage.delete(key: 'access_token');
    await storage.delete(key: 'refresh_token');
  }
}
```

Interceptor temel davranışı:

```dart
class AuthInterceptor extends Interceptor {
  AuthInterceptor(this.store, this.refreshSession, this.onSessionExpired,
      this.onAccountPending);

  final TokenStore store;
  final Future<bool> Function() refreshSession;
  final VoidCallback onSessionExpired;
  final VoidCallback onAccountPending;
  bool _refreshing = false;

  @override
  void onRequest(RequestOptions options, RequestInterceptorHandler handler) async {
    final token = await store.accessToken;
    if (token != null) options.headers['Authorization'] = 'Bearer $token';
    handler.next(options);
  }

  @override
  void onError(DioException error, ErrorInterceptorHandler handler) async {
    final code = error.response?.data is Map
        ? error.response?.data['code'] as String?
        : null;
    if (error.response?.statusCode == 403 && code == 'ACCOUNT_PENDING') {
      onAccountPending();
      return handler.next(error);
    }
    if (error.response?.statusCode != 401 ||
        error.requestOptions.extra['retried'] == true || _refreshing) {
      return handler.next(error);
    }
    _refreshing = true;
    final refreshed = await refreshSession();
    _refreshing = false;
    if (!refreshed) {
      await store.clear();
      onSessionExpired();
      return handler.next(error);
    }
    final token = await store.accessToken;
    final request = error.requestOptions;
    request.extra['retried'] = true;
    request.headers['Authorization'] = 'Bearer $token';
    try {
      return handler.resolve(await Dio().fetch(request));
    } catch (_) {
      return handler.next(error);
    }
  }
}
```

Gerçek uygulamada eşzamanlı `401` istekleri tek refresh çağrısında sıraya alınmalıdır. Refresh isteği aynı interceptor üzerinden sonsuz döngüye sokulmamalıdır.

QR gönderimi:

```dart
Future<void> submitQr(String rawValue) async {
  if (_scanLocked) return;
  _scanLocked = true;
  try {
    final response = await dio.post('/api/v1/qr-attendance/scan', data: {
      'qrValue': rawValue,
      'occurredAt': DateTime.now().toUtc().toIso8601String(),
      'deviceEventId': const Uuid().v4(),
    });
    showSuccess(response.data);
  } on DioException catch (error) {
    showApiError(error.response?.data);
  } finally {
    await Future<void>.delayed(const Duration(seconds: 2));
    _scanLocked = false;
  }
}
```

## 12. Mobil ekran listesi

1. Açılış/splash ve oturum kontrolü
2. Telefon + parola ile giriş
3. Ad soyad + telefon + parola ile kayıt
4. Yönetici onayı bekleniyor / reddedildi / askıya alındı
5. Ana sayfa ve bugünkü puantaj özeti
6. QR tarama ve sonuç ekranı
7. Mola durumu, başlat/bitir ve moladaki çalışanlar
8. Çalışma kayıtları ve tarih filtresi
9. İzin listesi ve izin oluşturma
10. Puantaj düzeltme listesi ve talep oluşturma
11. Bildirimler
12. Profil, dil/tema ve güvenli çıkış

## 13. Tasarım beklentileri

- Material 3 veya mevcut tasarım sisteminin tutarlı kullanımı
- Tek tip renk, boşluk, radius, buton ve yazı hiyerarşisi
- Ana ekranda tek birincil işlem: QR okut
- Durumlar yalnızca renkle değil ikon ve metinle anlatılmalı
- Boş, yükleniyor, hata ve çevrimdışı durumları her listede tasarlanmalı
- Sunucu hatası doğrudan teknik metin olarak gösterilmemeli
- Türkçe ve İngilizce tüm metinler localization dosyasında tutulmalı
- Tarihler cihaz diline, API değerleri ISO-8601'e göre işlenmeli
- QR kamera izni reddedildiğinde ayarlara yönlendiren açıklama gösterilmeli
- Erişilebilir dokunma alanı en az 48x48 dp olmalı

## 14. Kabul testleri

- 7 karakterli parola kayıt ekranında ve API'de reddediliyor.
- Aynı telefonla ikinci kayıt oluşturulamıyor.
- Yeni kayıt onay bekleme ekranına düşüyor ve QR kullanamıyor.
- Yönetici onayından sonra durum yenilenip ana ekran açılıyor.
- Uygulama kapatılıp açıldığında tekrar parola istemiyor.
- Geçersiz refresh token giriş ekranına döndürüyor.
- Giriş QR'ı iki kez okutulduğunda ikinci istek uygun hata gösteriyor.
- Giriş olmadan çıkış kabul ediliyor ve kayıt `MissingEntry` görünüyor.
- Çıkış aktif ilave molayı otomatik kapatıyor.
- Cuma–pazartesi izin aralığında hafta sonu iş günü sayılmıyor.
- Yarım gün izinde başlangıç ve bitiş farklıysa istek reddediliyor.
- Aynı gün için ikinci bekleyen düzeltme talebi oluşturulamıyor.
- Türkçe/İngilizce değişiminde taşma veya kesilme olmuyor.
- Access token ve refresh token düz metin tercih deposuna yazılmıyor.

Bu sözleşmede bulunmayan yeni bir mobil davranış API varsayımıyla geliştirilmemeli; önce backend sözleşmesi netleştirilmelidir.
# Uzaktan çalışma ve saha görevi

Bu özellik `Features:WorkLocations` ile açılıp kapatılabilir. Hiç plan tanımlanmazsa mevcut ofis/QR akışı aynen devam eder.

- `GET /api/v1/work-locations/today`: Bugünkü çalışma konumunu döndürür. `workLocation`: `Office`, `Remote` veya `Field`; `recordSource`: `QR` veya `WorkLocationPlan`.
- `GET /api/v1/work-locations/field-requests`: Personelin saha görevi talepleri.
- `POST /api/v1/work-locations/field-requests`: Gelecek tarihli saha görevi talebi oluşturur.
- `DELETE /api/v1/work-locations/field-requests/{id}`: Yalnızca bekleyen talebi iptal eder.

Saha talebi gövdesi: `startDate`, `endDate`, `recurrenceType` (`EveryWorkday`/`SelectedWeekdays`), `days`, `projectName`, `customerName`, `fieldAddress`, `reason`. `SelectedWeekdays` seçilirse hafta içinden en az bir gün zorunludur.

Geçmiş tarihli saha çalışması, mevcut `POST /api/v1/attendance-corrections` uç noktasına `correctionType: "PastFieldWork"` gönderilerek istenir. Bu türde `projectName`, `customerName`, `fieldAddress` kullanılabilir; son 90 gün kuralı geçerlidir. Yönetici onayladığında tek günlük saha planı oluşur.

Puantajda gerçek QR kaydı varsa ofis kaydı her zaman planın önüne geçer. QR yok ve onaylı uzaktan/saha planı varsa vardiyanın beklenen süresi `isPlannedDuration: true` olarak yazılır; bu süre ölçülmüş çalışma süresi değildir. Mobil arayüzde QR alanı yerine “Uzaktan çalışma” veya “Saha görevi” gösterilmelidir.
