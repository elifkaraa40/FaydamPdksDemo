# FaydamPDKS Mobil API Entegrasyon Rehberi

Bu belge mobil uygulama ile backend ekibinin aynı sözleşme üzerinden çalışması içindir. Mobil istemci yalnızca `FaydamPDKS.Api` projesine bağlanır; web paneline ait controller veya cookie oturumunu kullanmaz.

## Temel adres ve sürümleme

- Geliştirme: `https://localhost:7072/api/v1`
- Üretim: ortam kurulumunda belirlenecektir.
- Tüm mobil uçlar URL üzerinden sürümlenir: `/api/v1/...`
- İstek ve yanıt içerik türü: `application/json; charset=utf-8`
- Tarih-saat değerleri ISO 8601 ve saat dilimli gönderilir: `2026-07-14T09:05:00+03:00`

## Kimlik doğrulama

Mobil uygulama kısa ömürlü JWT access token ve döndürülebilen (rotating) refresh token kullanır. Access token yalnızca işletim sisteminin güvenli alanında (Android Keystore/iOS Keychain) tutulmalıdır. Loglara token, parola veya kişisel veri yazılmamalıdır.

### `POST /auth/login`

```json
{
  "email": "personel@faydam.com",
  "password": "kullanici-parolasi",
  "deviceName": "Elif'in telefonu"
}
```

Başarılı yanıt:

```json
{
  "accessToken": "eyJ...",
  "refreshToken": "opaque-random-value",
  "expiresAt": "2026-07-14T10:05:00+03:00",
  "user": {
    "id": "3c52690b-d15a-4545-86ec-c431ce71efc7",
    "fullName": "Demo Personel",
    "email": "personel@faydam.com",
    "role": "Personel",
    "profileImageUrl": null
  }
}
```

### Token yenileme ve çıkış

- `POST /auth/refresh`: Süresi dolmadan/yetkisiz yanıttan sonra token çiftini yeniler. Eski refresh token yeniden kullanılamaz.
- `POST /auth/logout`: İlgili cihazın refresh token'ını iptal eder.
- API `401` dönerse mobil uygulama bir kez refresh denemeli; tekrar `401` alırsa giriş ekranına dönmelidir.

## Personel uçları

| Metot | Yol | Açıklama |
|---|---|---|
| `GET` | `/me` | Oturum açan kullanıcının profili |
| `PUT` | `/me` | Telefon ve kullanıcı tercihlerini güncelleme |
| `GET` | `/me/export` | Kullanıcının kendi kişisel verilerini JSON dışa aktarma |
| `GET` | `/attendance/today` | Bugünkü vardiya ve puantaj özeti |
| `GET` | `/attendance?from=2026-07-01&to=2026-07-31` | Tarih aralığı puantaj listesi |
| `POST` | `/attendance/events` | Mobil geçiş olayı gönderme |
| `GET` | `/attendance-corrections` | Kullanıcının puantaj düzeltme talepleri |
| `POST` | `/attendance-corrections` | Gerekçeli giriş/çıkış düzeltme talebi |
| `GET` | `/leave-requests` | Kullanıcının izin talepleri |
| `POST` | `/leave-requests` | Yeni izin talebi |
| `DELETE` | `/leave-requests/{id}` | Henüz işleme alınmamış talebi iptal |
| `GET` | `/notifications` | Bildirim listesi |
| `POST` | `/notifications/{id}/read` | Bildirimi okundu olarak işaretleme |

### Profil güncelleme

`PUT /me` yalnızca kullanıcının değiştirmesine izin verilen iletişim ve bildirim tercihlerini kabul eder. Rol, e-posta ve kullanıcı kimliği bu uçtan değiştirilemez.

```json
{
  "phoneNumber": "0555 000 00 00",
  "isEmailNotificationEnabled": true,
  "isSmsNotificationEnabled": false
}
```

### İzin talebi oluşturma

`POST /leave-requests` isteğinde `leaveType` değerleri `Annual`, `Sick`, `Excuse` veya `Unpaid` olabilir.

```json
{
  "leaveType": "Annual",
  "startDate": "2026-07-20",
  "endDate": "2026-07-24",
  "reason": "Yıllık izin"
}
```

API geçmiş tarihli, başlangıcı bitişinden sonra olan, 365 günden uzun veya mevcut aktif izinle çakışan talepleri reddeder. Yalnızca `Pending` durumundaki talep `DELETE /leave-requests/{id}` ile iptal edilebilir.

### Bildirimler

İzin talebi web panelinden onaylandığında veya reddedildiğinde API kullanıcısı için kalıcı bildirim oluşturur. Mobil istemci `GET /notifications` ile okunmamış kayıtlar önce gelecek şekilde en yeni 100 bildirimi alır.

```json
{
  "id": "48e43e6d-26e1-462f-8efe-a0c6d7edaa21",
  "type": "LeaveApproved",
  "title": "İzin talebiniz onaylandı",
  "message": "20.07.2026 - 24.07.2026 tarihli izin talebiniz onaylandı.",
  "relatedEntityId": "71b1782a-00d6-4949-b51d-65ad5dad8325",
  "createdAt": "2026-07-14T10:30:00Z",
  "readAt": null,
  "isRead": false
}
```

Mobil uygulama bildirim detayını gösterdikten sonra `POST /notifications/{id}/read` çağrısı yapar. Kullanıcı başka bir personele ait bildirimi okuyamaz veya değiştiremez.

### `POST /attendance/events`

Her mobil olayda cihaz tarafından üretilmiş benzersiz `deviceEventId` gönderilir. Bağlantı kesilirse aynı olay tekrar gönderilebilir; sunucu bu alanla idempotent davranır ve mükerrer puantaj oluşturmaz.

```json
{
  "eventType": "Entry",
  "occurredAt": "2026-07-14T08:57:12+03:00",
  "deviceEventId": "01J2S9R4W6P8K0N7M3F2A1B5C9",
  "zoneId": 12
}
```

### Puantaj geçmişi

`GET /attendance?from=2026-07-01&to=2026-07-31` yalnızca oturum açan personelin kayıtlarını döndürür. Tarihler dahil olarak değerlendirilir; tek sorgu en fazla 90 gün olabilir ve gelecek tarih kabul edilmez. Kayıt olmayan günler de `NoRecord` durumuyla sonuçta yer alır; böylece mobil takvim gün atlamadan gösterilebilir.

Puantaj `status` değerleri: `Complete`, `MissingEntry`, `MissingExit`, `NoRecord` ve `NonWorkingDay`. Cumartesi/pazar ile yönetici tarafından tanımlanan resmi tatillerde kayıt yoksa `NonWorkingDay`, beklenen süre `0` döner. Çalışma dışı günde giriş/çıkış varsa çalışılan süre fazla mesai olarak hesaplanır. İşyeri bazlı özel çalışma günü, aynı tarihteki genel tatil veya hafta sonu kuralından önceliklidir.

### Puantaj düzeltme talebi

Personel, terminalde eksik veya hatalı görünen geçmiş bir gün için `POST /attendance-corrections` çağrısı yapar:

```json
{
  "workDate": "2026-07-14",
  "requestedEntry": "09:00:00",
  "requestedExit": "18:00:00",
  "reason": "Terminal çıkış kaydımı oluşturmadı."
}
```

Yalnızca bugün ve son 90 gün kabul edilir. Giriş ile çıkış aynı olamaz; çıkış girişten küçükse gece çalışması kabul edilip çıkış ertesi güne taşınır. Aynı gün için ikinci bekleyen talep `409` döner. Yönetici kararı sonrasında mobil bildirim oluşur. Onaylanan düzeltme ham terminal olayını silmez; günlük puantaj hesabında denetlenebilir düzeltme değeri olarak kullanılır.

## Standart hata biçimi

Tüm hatalar aynı yapıyla döner:

```json
{
  "code": "VALIDATION_ERROR",
  "message": "İstek doğrulanamadı.",
  "errors": {
    "email": ["Geçerli bir e-posta adresi girin."]
  },
  "traceId": "00-a1b2c3..."
}
```

Mobil uygulama kullanıcıya `message` alanını gösterebilir; `traceId` destek kaydına eklenir. Beklenen kodlar: `VALIDATION_ERROR` (400), `UNAUTHENTICATED` (401), `FORBIDDEN` (403), `NOT_FOUND` (404), `CONFLICT` (409), `RATE_LIMITED` (429), `INTERNAL_ERROR` (500).

## Mobil ekip için geliştirme kuralları

1. DTO alan adları yayımlandıktan sonra geriye uyumsuz değiştirilmez; yeni alanlar opsiyonel eklenir.
2. Ağ çağrılarında makul timeout ve exponential backoff kullanılır; `POST` tekrarları yalnızca idempotency anahtarı varsa yapılır.
3. Çevrimdışı geçiş olayları şifreli yerel depoda kuyruklanır ve bağlantı geldiğinde zaman sırasıyla gönderilir.
4. Uygulama sertifika doğrulamasını kapatmaz. Geliştirme sertifikası yalnızca debug yapılandırmasında ele alınır.
5. API sözleşmesinin kaynak doğrusu Swagger/OpenAPI çıktısıdır; bu belge kullanım rehberidir.
