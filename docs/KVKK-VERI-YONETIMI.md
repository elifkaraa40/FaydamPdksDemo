# Faydam PDKS KVKK Veri Yönetimi Taslağı

Bu belge teknik tasarım kontrol listesidir; hukuki görüş veya şirket politikası yerine geçmez. Saklama ve silme süreleri veri sorumlusu, hukuk, insan kaynakları ve bordro birimleri tarafından yazılı olarak onaylanmadan uygulamada otomatik silme başlatılmamalıdır.

## İşlenen temel veri grupları

| Veri | Amaç | Erişim | Teknik koruma |
|---|---|---|---|
| Kimlik/iletişim ve sicil | Personel hesabı ve organizasyon | Personelin kendisi, yetkili yönetici | Rol kontrolü, güvenli cookie/JWT |
| Ham giriş-çıkış olayı | Devam ve puantaj hesabı | Personelin kendisi, yetkili yönetici | UTC kayıt, cihaz olayı tekilleştirme |
| İzin ve düzeltme talebi | İK/onay süreçleri | Talep sahibi, yetkili yönetici | Sahiplik kontrolü, durum makinesi |
| Bildirim | Süreç sonucu bilgilendirme | Yalnızca ilgili personel | Kullanıcı kimliğiyle kapsam filtresi |
| Denetim kaydı | Güvenlik ve hesap verebilirlik | Yalnızca yönetici | Append-only uygulama arayüzü, eski/yeni değer |
| Terminal sağlığı | Cihaz işletimi | Yalnızca yönetici | Hash'lenmiş cihaz anahtarı |

Parola hash’i, JWT, refresh token ve terminal anahtarı kişisel veri dışa aktarımına dahil edilmez.

## Veri sahibi erişimi

Kimliği doğrulanmış çalışan `GET /api/v1/me/export` ile yalnızca kendisine ait profil, ham geçiş, izin, puantaj düzeltme ve bildirim verilerini JSON olarak alabilir. Endpoint başka kullanıcı kimliği kabul etmez; kullanıcı kapsamı JWT `sub` claim’inden çıkarılır.

## Saklama ve silme kararı

Üretim öncesinde aşağıdaki tablo şirketçe doldurulmalıdır:

| Veri grubu | Onaylı saklama süresi | Süre başlangıcı | Süre sonunda işlem |
|---|---:|---|---|
| Ham geçiş olayları | Belirlenecek | Olay tarihi | Silme veya anonimleştirme |
| Hesaplanan puantaj/rapor | Belirlenecek | İlgili bordro dönemi | Arşiv/silme |
| İzin ve düzeltme talepleri | Belirlenecek | Talebin sonuçlanması | Arşiv/silme |
| Bildirimler | Belirlenecek | Oluşturulma tarihi | Silme |
| Denetim kayıtları | Belirlenecek | İşlem tarihi | Güvenli arşiv/silme |
| Pasif personel profili | Belirlenecek | İşten ayrılış tarihi | Anonimleştirme/silme |

Silme işlemi doğrudan controller içinde yapılmamalı; zamanlanmış görev, işlem özeti, etkilenen kayıt sayısı, denetim kaydı, hata halinde geri alma ve yedek politikasıyla uygulanmalıdır.

## Üretim kontrol listesi

- Aydınlatma metni ve erişim kanalı belirlendi.
- Rol/veri kapsamı matrisi veri sorumlusu tarafından onaylandı.
- Saklama tablosundaki tüm “Belirlenecek” alanları kapatıldı.
- Yedeklerin saklama ve imha süresi ana veritabanıyla uyumlu hale getirildi.
- Loglarda parola, token, terminal anahtarı ve gereksiz kişisel veri bulunmadığı doğrulandı.
- Veri ihlali müdahale ve yetki iptal prosedürü test edildi.
