# Mobil Hedefler Test Listesi

Bu belge, mobil uygulama ve mobil API için tamamlanan hedeflerin gerçek cihaz,
emülatör ve yönetici ekranları üzerinden kabul kontrolünü takip etmek amacıyla
hazırlanmıştır.

## Genel test hazırlığı

- [ ] API güncel dal ve veritabanı migrationları ile çalışıyor.
- [ ] Mobil uygulama güncel `main` dalından kurulmuş durumda.
- [ ] Bir personel ve bir yönetici test hesabı hazır.
- [ ] Giriş ve çıkış için geçerli QR kodları hazır.
- [ ] Türkçe ve İngilizce dil seçenekleri ayrı ayrı kontrol edilecek.
- [ ] Koyu ve açık tema temel ekranlarda kontrol edilecek.

## Hedef 1 — Cihaz oturumu ve QR güvenliği

Durum: Masaüstündeki mevcut cihaz akışı çalıştı. Gerçek telefon ve ikinci cihaz
testi tekrar yapılacak.

- [ ] Personel cihaz A’dan giriş yaptığında Profilim ekranında aktif cihaz sayısı
      ve cihaz A görünür.
- [ ] Aynı hesap cihaz B’den açıldığında cihaz değişikliği anlaşılır biçimde
      bildirilir.
- [ ] Pasif kalan cihaz A’dan QR okutulduğunda işlem reddedilir ve puantaj kaydı
      oluşmaz.
- [ ] Aktif cihaz B’den QR okutulduğunda işlem başarıyla kaydedilir.
- [ ] Aynı `deviceEventId` ile gönderilen ikinci QR isteği yeni kayıt oluşturmaz.
- [ ] “Tüm cihazlardan çıkış yap” işlemi 404 dönmeden tamamlanır.
- [ ] Toplu çıkıştan sonra eski access/refresh tokenlarla QR işlemi yapılamaz.
- [ ] İnternet yokken kullanıcı teknik olmayan, anlaşılır bir hata görür.

## Hedef 2 — İzin çakışması, takvim ve yarım gün

- [ ] Aynı izin türünde çakışan bekleyen/onaylı kayıt için açıklayıcı modal açılır.
- [ ] Modalda çakışan izin tarihleri gösterilir.
- [ ] “Kapat” kullanıcıyı izin listesinde bırakır.
- [ ] “Başka tarih dene” tek tarih aralığı takvimini yeniden açar.
- [ ] Mobildeki izin takvimi Türkçedir ve başlangıç-bitiş tek ekranda seçilir.
- [ ] Farklı izin türünde yeni talep oluşturulmasına izin verilir.
- [ ] İş günü sayısı doğru gösterilir; hafta sonu/tatil kuralları API ile aynıdır.
- [ ] Yarım gün izin talebi oluşturulabilir ve listede doğru görünür.
- [ ] İngilizce dilde yeni metinler ve takvim karşılıkları doğru görünür.

## Hedef 3 — Çalışma konumu tarih kuralları

- [ ] 90 günden uzun tarih aralığı mobilde ve API’de reddedilir.
- [ ] Başlangıç tarihi bitiş tarihinden sonra seçilemez.
- [ ] Çakışan çalışma konumu talebinde Snackbar yerine açıklayıcı modal açılır.
- [ ] Modalda mümkünse çakışan kaydın tarihleri gösterilir.
- [ ] “Başka tarih dene” formu yeniden açar ve tarihler tekrar seçilebilir.
- [ ] Çakışmayan tarih aralığı başarıyla kaydedilir.
- [ ] Türkçe ve İngilizce hata metinleri doğru görünür.

## Hedef 4 — Puantaj dışa aktarma

- [ ] CSV, Excel ve PDF indirme seçeneklerinin üçü de görünür.
- [ ] Dosyalar `Downloads/Puantaj` klasörüne tarih aralıklı adla kaydedilir.
- [ ] İndirme sırasında ikinci indirme başlatılamaz ve yükleniyor göstergesi görünür.
- [ ] “Dosya hazır” penceresi dosya adını ve kayıt konumunu gösterir.
- [ ] CSV, Excel ve PDF “Dosyayı aç” seçeneğiyle uygun uygulamada açılır.
- [ ] PDF A4 yatay, okunabilir ve webdeki sütun düzeniyle uyumludur.
- [ ] Excel başlığı biçimlidir, ilk satır sabittir, filtre ve uygun sütun genişlikleri
      bulunur.
- [ ] CSV UTF-8 BOM içerir ve Türkçe karakterler bozulmaz.
- [ ] Uygun dosya uygulaması bulunmadığında anlaşılır hata gösterilir.
- [ ] Gerçek Android telefon ve emülatörde dosya konumu/açma akışı tekrar test edilir.

## Hedef 5 — Gerçek zamanlı bildirimler

Durum: Otomatik mobil ve API testleri geçti. Firebase yapılandırıldı. Gerçek cihaz
ön plan, arka plan ve kapalı uygulama testleri bekliyor.

- [ ] Android 13 ve üzerindeki açıklama ekranından sonra sistem bildirim izni açılır.
- [ ] İzin reddedildiğinde Bildirimler ekranı ayarlara yönlendirme gösterir.
- [ ] Uygulama açıkken yeni bildirim üst banner olarak görünür.
- [ ] Uygulama arka plandayken Android bildirim panelinde görünür.
- [ ] Uygulama tamamen kapalıyken bildirim paneline ulaşır.
- [ ] Bildirime dokununca izin, puantaj düzeltme veya çalışma konumu ekranına gider.
- [ ] Okunmamış bildirim sayacı anlık güncellenir.
- [ ] Eski bildirimler uygulama her açıldığında tekrar banner olarak gösterilmez.
- [ ] Türkçe ve İngilizce bildirim içerikleri uygulama diline uyar.
- [ ] Çıkış sonrası cihaz tokenı pasifleştirilir ve eski kullanıcı bildirim almaz.
- [ ] Firebase geçici olarak kullanılamazsa iş işlemi başarısız olmaz; kayıt
      PostgreSQL’de kalır ve teslimat tekrar denenir.

## Hedef 6 — Günlük puantaj ve QR işlem geçmişi

Durum: API’de 6 ilgili test ve mobilde 11 test geçti. Kullanıcı arayüzü testi
bekliyor.

- [ ] Önceki gün giriş/çıkış yapan personel yeni gün uygulamayı açtığında eski
      çıkışı görmez.
- [ ] Bugün kayıt yoksa giriş alanı “Kayıt yok” gösterir.
- [ ] Bugün kayıt yoksa çıkış alanı “Henüz çıkış yapılmadı” gösterir.
- [ ] Durum alanı “Bugün için kayıt yok” gösterir.
- [ ] Bugünkü ilk girişten önce kalan eski çıkış bugünkü karta taşınmaz.
- [ ] QR girişinden sonra ana sayfa otomatik yenilenir ve yalnızca bugünkü giriş
      görünür.
- [ ] QR çıkışı yapılmadan çıkış alanında eski bir saat görünmez.
- [ ] QR çıkışından sonra bugünkü çıkış ve “Tamamlandı” durumu görünür.
- [ ] Ana sayfa sekmesine dönüldüğünde günlük özet yeniden API’den alınır.
- [ ] Uygulama arka plandan döndüğünde günlük özet yeniden alınır.
- [ ] Tarih değiştiğinde eski yerel değerler temizlenir; yüklenirken eski veri
      gösterilmez.
- [ ] Çalışılan, geç kalma ve fazla mesai değerleri yalnızca bugünkü kayda aittir.
- [ ] “QR İşlem Geçmişi” ekranı son mobil QR kayıtlarını yeniden eskiye sıralar.
- [ ] Geçmişte her satırın tarih, saat ve Giriş/Çıkış türü doğru görünür.
- [ ] QR geçmişi boş, hata ve yenileme durumlarında anlaşılır ekran gösterir.
- [ ] Türkçe ve İngilizce boş durum, başlık ve işlem türleri doğru görünür.

## Hedef 7 — Hesabım ekranı

Durum: Henüz uygulanmadı. Uygulama tamamlandığında ayrıntılı senaryolar bu bölüme
eklenecek.

- [ ] Kullanıcı “Hesabım / My Account” başlığını görür.
- [ ] Profil fotoğrafı, hesap ayarları, şifre değiştirme ve kişisel veri indirme
      akışları API ile birlikte doğrulanır.
