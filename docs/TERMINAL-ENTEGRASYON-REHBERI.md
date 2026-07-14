# Faydam PDKS Terminal Entegrasyon Rehberi

Terminal kimliği mobil kullanıcı JWT'sinden ayrıdır. Her fiziksel cihaz yönetici panelinden kaydedilir ve yalnızca bir kez gösterilen 256 bit terminal anahtarı alır. Anahtar cihazın güvenli alanında saklanmalı; kaynak kod, düz metin ayar dosyası veya log içine yazılmamalıdır.

## Heartbeat

```http
POST /api/v1/terminals/{terminalId}/heartbeat
X-Terminal-Key: terminal-kayit-anahtari
Content-Type: application/json
```

```json
{
  "firmwareVersion": "1.2.3",
  "pendingEventCount": 7,
  "lastError": null
}
```

Başarılı yanıt `204 No Content` olur. Eksik veya hatalı kimlik bilgisi `401`, doğrulama hatası `400`, hız sınırı aşımı `429` döner. Terminal dakikada en fazla 120 heartbeat gönderebilir; önerilen aralık 60 saniyedir.

Panel, son heartbeat üzerinden beş dakika geçtiğinde cihazı çevrimdışı gösterir. `pendingEventCount`, bağlantı kesikken cihazda kalıcı olarak kuyruklanan fakat henüz merkeze aktarılmamış olay sayısıdır. Terminal bağlantı geldiğinde aynı olay kimliğiyle tekrar gönderim yapmalı; sunucu `deviceEventId` ile tekilleştirmelidir.

## Güvenlik kararları

- Terminal anahtarı veritabanında SHA-256 hash olarak saklanır.
- Anahtar doğrulaması zamanlama saldırılarına karşı sabit süreli karşılaştırmayla yapılır.
- Seri numarası sistem genelinde benzersizdir.
- Pasif terminal heartbeat gönderemez.
- Anahtar kaybolursa mevcut değeri görüntülemek yerine yeni anahtar üretme/rotasyon işlemi uygulanmalıdır.
- Üretimde istekler yalnızca HTTPS üzerinden kabul edilmeli; mümkünse terminal ağı IP allowlist veya mTLS ile sınırlandırılmalıdır.
