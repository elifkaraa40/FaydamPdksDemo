from pathlib import Path
from reportlab.lib import colors
from reportlab.lib.enums import TA_CENTER, TA_LEFT
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import getSampleStyleSheet, ParagraphStyle
from reportlab.lib.units import mm
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.platypus import (
    SimpleDocTemplate, Paragraph, Spacer, PageBreak, Table, TableStyle,
    KeepTogether, ListFlowable, ListItem
)

ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT / "output" / "pdf" / "FaydamPDKS_Armongate_Inceleme_Raporu.pdf"
OUTPUT.parent.mkdir(parents=True, exist_ok=True)

FONT_DIR = Path(r"C:\Windows\Fonts")
pdfmetrics.registerFont(TTFont("Arial", str(FONT_DIR / "arial.ttf")))
pdfmetrics.registerFont(TTFont("Arial-Bold", str(FONT_DIR / "arialbd.ttf")))

NAVY = colors.HexColor("#0F172A")
INDIGO = colors.HexColor("#4F46E5")
SLATE = colors.HexColor("#64748B")
LINE = colors.HexColor("#E2E8F0")
PALE = colors.HexColor("#F8FAFC")
GREEN = colors.HexColor("#047857")
AMBER = colors.HexColor("#B45309")
RED = colors.HexColor("#BE123C")

styles = getSampleStyleSheet()
styles.add(ParagraphStyle(name="TitleTR", fontName="Arial-Bold", fontSize=28, leading=34, textColor=NAVY, spaceAfter=12))
styles.add(ParagraphStyle(name="SubtitleTR", fontName="Arial", fontSize=12, leading=18, textColor=SLATE))
styles.add(ParagraphStyle(name="H1TR", fontName="Arial-Bold", fontSize=18, leading=23, textColor=NAVY, spaceBefore=10, spaceAfter=10))
styles.add(ParagraphStyle(name="H2TR", fontName="Arial-Bold", fontSize=12.5, leading=17, textColor=INDIGO, spaceBefore=10, spaceAfter=6))
styles.add(ParagraphStyle(name="BodyTR", fontName="Arial", fontSize=9.5, leading=14.2, textColor=NAVY, spaceAfter=6))
styles.add(ParagraphStyle(name="SmallTR", fontName="Arial", fontSize=8, leading=11.5, textColor=SLATE))
styles.add(ParagraphStyle(name="TagTR", fontName="Arial-Bold", fontSize=7.5, leading=10, textColor=INDIGO))
styles.add(ParagraphStyle(name="CoverMeta", fontName="Arial", fontSize=9, leading=14, textColor=SLATE))
styles.add(ParagraphStyle(name="Cell", fontName="Arial", fontSize=7.6, leading=10.3, textColor=NAVY))
styles.add(ParagraphStyle(name="CellBold", fontName="Arial-Bold", fontSize=7.6, leading=10.3, textColor=NAVY))
styles.add(ParagraphStyle(name="CellHeader", fontName="Arial-Bold", fontSize=7.6, leading=10.3, textColor=colors.white))

def P(text, style="BodyTR"):
    return Paragraph(text, styles[style])

def bullets(items, level=0):
    return ListFlowable(
        [ListItem(P(item), leftIndent=10) for item in items],
        bulletType="bullet", start="circle", leftIndent=16 + level * 10,
        bulletFontName="Arial", bulletFontSize=6, bulletColor=INDIGO,
        spaceAfter=6,
    )

def table(data, widths, header=True):
    converted = []
    for r, row in enumerate(data):
        converted.append([P(str(cell), "CellHeader" if header and r == 0 else "Cell") for cell in row])
    t = Table(converted, colWidths=widths, repeatRows=1 if header else 0, hAlign="LEFT")
    commands = [
        ("VALIGN", (0, 0), (-1, -1), "TOP"),
        ("GRID", (0, 0), (-1, -1), .4, LINE),
        ("LEFTPADDING", (0, 0), (-1, -1), 6),
        ("RIGHTPADDING", (0, 0), (-1, -1), 6),
        ("TOPPADDING", (0, 0), (-1, -1), 6),
        ("BOTTOMPADDING", (0, 0), (-1, -1), 6),
        ("ROWBACKGROUNDS", (0, 1), (-1, -1), [colors.white, PALE]),
    ]
    if header:
        commands += [("BACKGROUND", (0, 0), (-1, 0), NAVY), ("TEXTCOLOR", (0, 0), (-1, 0), colors.white)]
    t.setStyle(TableStyle(commands))
    return t

def header_footer(canvas, doc):
    canvas.saveState()
    if doc.page > 1:
        canvas.setStrokeColor(LINE)
        canvas.setLineWidth(.5)
        canvas.line(20 * mm, 282 * mm, 190 * mm, 282 * mm)
        canvas.setFont("Arial-Bold", 7.5)
        canvas.setFillColor(NAVY)
        canvas.drawString(20 * mm, 286 * mm, "FAYDAM PDKS · ARMONGATE İNCELEME RAPORU")
        canvas.setFont("Arial", 7.5)
        canvas.setFillColor(SLATE)
        canvas.drawRightString(190 * mm, 12 * mm, f"Sayfa {doc.page}")
        canvas.drawString(20 * mm, 12 * mm, "14 Temmuz 2026 · Sürüm 1.0")
    canvas.restoreState()

story = []

# Cover
story += [Spacer(1, 32 * mm), P("FAYDAM PDKS", "TagTR"), Spacer(1, 5 * mm),
          P("Armongate Sistem İncelemesi<br/>ve Yeni PDKS Gereksinimleri", "TitleTR"),
          P("Mevcut ürün gözlemlerinin doğrulanabilirlik açısından düzeltilmiş analizi, eksik iş alanları ve Faydam PDKS için önerilen ürün kapsamı.", "SubtitleTR"),
          Spacer(1, 20 * mm)]
cover_box = Table([[P("Rapor türü", "SmallTR"), P("Sistem inceleme ve gereksinim analizi", "BodyTR")],
                   [P("Hazırlayan", "SmallTR"), P("Elif Kara · Staj Projesi", "BodyTR")],
                   [P("Tarih", "SmallTR"), P("14 Temmuz 2026", "BodyTR")],
                   [P("Durum", "SmallTR"), P("Düzeltilmiş profesyonel sürüm", "BodyTR")]], colWidths=[34 * mm, 105 * mm])
cover_box.setStyle(TableStyle([("BACKGROUND", (0, 0), (-1, -1), PALE), ("BOX", (0, 0), (-1, -1), .6, LINE),
                               ("INNERGRID", (0, 0), (-1, -1), .4, LINE), ("VALIGN", (0, 0), (-1, -1), "TOP"),
                               ("LEFTPADDING", (0, 0), (-1, -1), 9), ("RIGHTPADDING", (0, 0), (-1, -1), 9),
                               ("TOPPADDING", (0, 0), (-1, -1), 9), ("BOTTOMPADDING", (0, 0), (-1, -1), 9)]))
story += [cover_box, Spacer(1, 35 * mm), P("GİZLİLİK NOTU", "TagTR"), P("Bu rapor, kullanıcı tarafından sağlanan ekran ve kullanım gözlemlerine dayanır. Armongate'in yayınlanmamış kaynak kodu, veritabanı şeması veya özel altyapı dokümanı incelenmemiştir. Bu nedenle doğrulanamayan teknik ayrıntılar kesin bulgu olarak sunulmamıştır.", "CoverMeta"), PageBreak()]

story += [P("1. Yönetici özeti", "H1TR"),
          P("Armongate, erişim kontrolü, personel devam takibi, ziyaretçi yönetimi, donanım izleme ve raporlama işlevlerini aynı yönetim deneyiminde birleştiren kapsamlı bir ürün görünümü sunmaktadır. İncelenen ekranlar, yeni Faydam PDKS için değerli bir referans oluşturmakla birlikte yalnızca arayüz gözlemlerinden altyapı teknolojisi veya güvenlik seviyesi hakkında kesin sonuç çıkarılamaz."),
          P("İlk raporun güçlü yönü, modülleri ayrıntılı biçimde sınıflandırmasıdır. Temel düzeltme ihtiyacı ise gözlem, teknik kanıt ve varsayımın aynı kesinlik seviyesinde yazılmış olmasıdır. Bu sürümde iddialar yeniden sınıflandırılmış; PDKS'nin bordro, vardiya, izin, veri bütünlüğü, KVKK ve denetim gereksinimleri eklenmiştir."),
          P("Öncelikli sonuçlar", "H2TR"), bullets([
              "Faydam PDKS, ham geçiş olayları ile hesaplanmış puantaj sonuçlarını ayrı tutmalıdır.",
              "Web paneli ve mobil API ayrı istemci sınırlarına sahip olmalı; ortak iş kuralları Core katmanında paylaşılmalıdır.",
              "Rol kontrolü tek başına yeterli değildir; şirket, işyeri, bölüm ve kendi kaydı gibi veri kapsamı da uygulanmalıdır.",
              "Mobil olaylar çevrimdışı kuyruk, benzersiz olay kimliği ve tekrar gönderim desteğiyle idempotent işlenmelidir.",
              "Her manuel düzeltme ve yönetici kararı değişmez denetim iziyle saklanmalıdır."
          ]),
          P("Raporun sınırı", "H2TR"),
          P("Bu çalışma bir ürün ve arayüz incelemesidir; penetrasyon testi, kaynak kod denetimi, hukuki uygunluk görüşü veya Armongate'in resmi teknik mimari belgesi değildir."), PageBreak()]

story += [P("2. İnceleme yöntemi ve kanıt sınıfları", "H1TR"),
          P("Profesyonel bir sistem incelemesinde her tespit, dayandığı kanıtla birlikte yazılmalıdır. Bu raporda aşağıdaki sınıflandırma kullanılmıştır:"),
          table([
              ["Sınıf", "Tanım", "Kullanım örneği"],
              ["Ekran gözlemi", "Arayüzde doğrudan görülen alan, menü veya davranış.", "Raporlar menüsünde PDKS ve donanım kategorileri bulunması."],
              ["Ağ trafiği bulgusu", "Tarayıcı geliştirici araçlarında görülen istek, protokol veya alan.", "Bir WebSocket bağlantısının URL ve mesajlarıyla doğrulanması."],
              ["Dokümantasyon bilgisi", "Üreticinin resmi dokümanında açıklanan özellik.", "Desteklenen kart/okuyucu protokolleri."],
              ["Varsayım", "Mevcut kanıtla doğrulanamayan ancak olası yorum.", "Ürünün tamamen ACaaS modeliyle sunulduğu iddiası."]
          ], [28 * mm, 67 * mm, 67 * mm]),
          Spacer(1, 5 * mm),
          P("İlk rapordaki kesinlik düzeltmeleri", "H2TR"),
          table([
              ["İlk ifade", "Düzeltilmiş ifade"],
              ["Sistem ACaaS modelinde çalışır.", "Sistem merkezi servis kullanan bir yapı izlenimi vermektedir; dağıtım ve lisans modeli resmi dokümanla doğrulanmalıdır."],
              ["AppInitializerService WebSocket bağlantısını kurar.", "Bu isim geliştirici araçlarında veya kaynak çıktısında görülmüşse ağ trafiği bulgusu olarak, aksi halde varsayım olarak belirtilmelidir."],
              ["Giriş verileri şifrelenmiş biçimde gönderilir.", "HTTPS doğrulandıysa verilerin TLS ile aktarım sırasında korunduğu söylenebilir; parolanın uygulama düzeyinde ayrıca şifrelenip şifrelenmediği bilinmemektedir."],
              ["Merkezi API verilerin güvenli yerde tutulduğunu kanıtlar.", "Merkezi API kullanımı veri konumunu ve güvenlik kontrollerini tek başına kanıtlamaz."],
              ["Tekil ID veri bütünlüğünün kanıtıdır.", "Tekil ID bir iş anahtarı gereksinimini gösterir; benzersiz indeks ve kısıtlar ayrıca doğrulanmalıdır."]
          ], [64 * mm, 98 * mm]), PageBreak()]

story += [P("3. Gözlemlenen ürün kapsamı", "H1TR"),
          P("Aşağıdaki modüller, kullanıcının Armongate incelemesinde gördüğü ekran ve menülere göre yeniden düzenlenmiştir. Teknik uygulama ayrıntıları doğrulanmadıkça ürün davranışı seviyesinde açıklanmıştır."),
          P("Kimlik doğrulama ve hesap yönetimi", "H2TR"), bullets([
              "Kullanıcı adı/e-posta ve parola ile oturum açma.",
              "Kişisel bilgi, parola, cihaz ve bildirim tercihlerinin yönetimi.",
              "Rol veya yetki alanına göre menü ve işlem erişimi."
          ]),
          P("Erişim kontrolü", "H2TR"), bullets([
              "Kullanıcı, kimlik, grup, bölge ve erişim noktası tanımları.",
              "Kapı, turnike veya bariyer geçişlerinin yetkiye göre değerlendirilmesi.",
              "Gerçekleşen ve reddedilen geçişlerin zaman, kullanıcı ve bölgeyle kaydı."
          ]),
          P("PDKS ve izin yönetimi", "H2TR"), bullets([
              "Giriş-çıkış hareketlerinden devam durumu üretme.",
              "Geç başlama, erken çıkma, eksik çalışma ve fazla çalışma sınıfları.",
              "Çalışma planları ve izin taleplerinin onay süreci."
          ]),
          P("Ziyaretçi, donanım ve raporlar", "H2TR"), bullets([
              "Ziyaretçi ön kayıt, aktif ziyaret ve ziyaret geçmişi.",
              "Okuyucu/terminal olayları, bağlantı ve arıza görünümü.",
              "Erişim, PDKS, ziyaretçi, donanım ve denetim raporları.",
              "Zamanlanmış rapor veya görevlerin otomatik çalıştırılması."
          ]), PageBreak()]

story += [P("4. Uçtan uca iş akışları", "H1TR"),
          P("4.1 Personel tanımlama ve yetkilendirme", "H2TR"),
          table([
              ["Adım", "İşlem", "Beklenen kontrol"],
              ["1", "Personel kaydı oluşturulur.", "Sicil/e-posta benzersizliği, zorunlu alanlar."],
              ["2", "Şirket, işyeri, bölüm ve pozisyon atanır.", "Organizasyon kapsamı ve geçerlilik tarihleri."],
              ["3", "Vardiya/takvim atanır.", "Çakışan atama ve gece vardiyası kontrolü."],
              ["4", "Kart veya mobil kimlik tanımlanır.", "Kimlik benzersizliği, aktiflik ve süre."],
              ["5", "Bölge/kapı yetkisi verilir.", "Rol, grup, zaman profili ve istisnalar."],
              ["6", "İşlem denetim izine yazılır.", "Kim, ne zaman, hangi eski/yeni değer."],
          ], [14 * mm, 67 * mm, 81 * mm]),
          P("4.2 Mobil geçiş olayı", "H2TR"), bullets([
              "Mobil uygulama kullanıcı oturumunu kısa ömürlü access token ile doğrular.",
              "Cihaz, olay zamanı, bölge ve benzersiz deviceEventId üretir.",
              "Bağlantı yoksa olay şifreli yerel kuyrukta saklanır.",
              "Sunucu aynı deviceEventId değerini ikinci kez işlemez.",
              "Ham olay değişmeden saklanır; puantaj motoru bu olaydan ayrı sonuç üretir."
          ]),
          P("4.3 İzin talebi ve onay", "H2TR"), bullets([
              "Personel mobil uygulamadan izin türü, tarih ve gerekçe gönderir.",
              "Sistem tarih sırası, geçmiş tarih, çakışma ve hak kontrolü yapar.",
              "Yetkili yönetici yalnızca kendi veri kapsamındaki talebi görür.",
              "Yönetici kendi talebini onaylayamaz; karar tarihi ve notu kaydedilir.",
              "Onaylanan izin puantaj hesabına otomatik yansır."
          ]), PageBreak()]

story += [P("5. Rol ve veri kapsamı modeli", "H1TR"),
          P("Rol tabanlı erişim kontrolü, işlemin yapılabilirliğini belirler. Kurumsal PDKS'de ayrıca kullanıcının hangi personel kayıtları üzerinde işlem yapabileceği tanımlanmalıdır."),
          table([
              ["Rol", "Temel yetki", "Önerilen veri kapsamı"],
              ["Sistem yöneticisi", "Sistem ayarları, roller, entegrasyon ve tüm modüller.", "Yetkilendirildiği şirketler; kritik işlemler için ek doğrulama."],
              ["İK / PDKS sorumlusu", "Personel, vardiya, izin, düzeltme ve rapor işlemleri.", "Atanmış şirket/işyeri/bölümler."],
              ["Bölüm yöneticisi", "Ekip devamlılığı ve izin onayı.", "Yönettiği organizasyon ağacı."],
              ["Güvenlik görevlisi", "Geçiş ve ziyaretçi operasyonları.", "Atanmış tesis ve erişim noktaları."],
              ["Personel", "Kendi profil, puantaj ve izin işlemleri.", "Yalnızca kendi kaydı."]
          ], [34 * mm, 64 * mm, 64 * mm]),
          Spacer(1, 5 * mm),
          P("Yetki matrisi örneği", "H2TR"),
          table([
              ["İşlem", "Admin", "İK", "Yönetici", "Personel"],
              ["Personel oluşturma", "Evet", "Kapsamında", "Hayır", "Hayır"],
              ["Puantaj görüntüleme", "Tümü", "Kapsamında", "Ekibi", "Kendi"],
              ["İzin onaylama", "Evet", "Kapsamında", "Ekibi", "Hayır"],
              ["Ham olayı değiştirme", "Hayır*", "Hayır*", "Hayır", "Hayır"],
              ["Düzeltme kaydı", "Evet", "Kapsamında", "Talep", "Talep"],
              ["Denetim raporu", "Evet", "Kapsamında", "Hayır", "Hayır"]
          ], [48 * mm, 27 * mm, 30 * mm, 30 * mm, 27 * mm]),
          P("* Ham olay silinmemeli veya üzerine yazılmamalıdır. Hatalı kayıt, yeni bir düzeltme kaydı ve gerekçeyle etkisizleştirilmelidir.", "SmallTR"), PageBreak()]

story += [P("6. İlk raporda eksik kalan PDKS alanları", "H1TR"),
          table([
              ["Alan", "Neden gereklidir", "Asgari gereksinim"],
              ["Organizasyon", "Yetki ve rapor kapsamının temelidir.", "Şirket, işyeri, bölüm, pozisyon, yönetici ağacı."],
              ["Vardiya", "Puantaj hesaplamasının girdisidir.", "Gece vardiyası, tolerans, mola, esnek vardiya, geçerlilik tarihi."],
              ["Takvim", "Tatil ve çalışma günlerini ayırır.", "Resmi tatil, hafta tatili, yarım gün, özel gün."],
              ["Puantaj motoru", "Ham olayları iş sonucuna dönüştürür.", "Eksik/çift basım, geç/erken, fazla mesai, vardiya kesişimi."],
              ["Düzeltme akışı", "Hatalı basımlar kaçınılmazdır.", "Talep, gerekçe, ek belge, çok seviyeli onay, eski/yeni değer."],
              ["Bordro entegrasyonu", "Sonuçların ücret hesabına aktarımı gerekir.", "Dönem kilidi, dışa aktarım, yeniden hesaplama ve mutabakat."],
              ["Terminal dayanıklılığı", "Ağ kesintisi veri kaybına yol açmamalıdır.", "Offline tampon, tekrar deneme, sıra ve tekilleştirme."],
              ["Veri yaşam döngüsü", "Kişisel veriler süresiz tutulmamalıdır.", "Saklama politikası, arşiv, anonimleştirme ve silme kaydı."],
          ], [34 * mm, 59 * mm, 69 * mm]), PageBreak()]

story += [P("7. Önerilen Faydam PDKS mimarisi", "H1TR"),
          P("Yönetici tarafından belirlenen dört projeli çözüm yapısı korunmalıdır. Web ve mobil API birbirini referanslamaz; ikisi de ortak Core sözleşmeleri ve Data uygulamaları üzerinden çalışır."),
          table([
              ["Proje", "Sorumluluk", "İçermemesi gerekenler"],
              ["FaydamPDKS.Web", "MVC/Razor paneli, web controller'ları, cookie oturumu.", "Mobil JWT sözleşmeleri veya doğrudan migration."],
              ["FaydamPDKS.Api", "Sürümlenmiş mobil API, JWT/refresh token, OpenAPI.", "Razor görünümü veya web cookie akışı."],
              ["FaydamPDKS.Core", "Model, DTO, enum, interface ve saf iş kuralları.", "DbContext, ControllerBase veya sunum bağımlılığı."],
              ["FaydamPDKS.Data", "DbContext, EF eşlemeleri, repository ve migrations.", "HTTP controller veya kullanıcı arayüzü."]
          ], [32 * mm, 71 * mm, 59 * mm]),
          P("Güvenlik kararları", "H2TR"), bullets([
              "Web oturumunda HttpOnly, Secure ve SameSite cookie; mobilde kısa ömürlü JWT ve döndürülen refresh token.",
              "Parola, veritabanı bağlantısı ve JWT anahtarı kaynak kodda değil secret store veya ortam değişkeninde tutulur.",
              "Mobil refresh token veritabanında açık metin değil güçlü hash olarak saklanır.",
              "Giriş uçlarında rate limit; tüm değiştirici işlemlerde doğrulama ve denetim izi uygulanır.",
              "Web formlarında antiforgery; API'de tutarlı 400/401/403/409/429 hata sözleşmesi kullanılır."
          ]), PageBreak()]

story += [P("8. Kullanılabilirlik ve erişilebilirlik değerlendirmesi", "H1TR"),
          P("İlk rapordaki boş veri ekranı, toplu kullanıcı yükleme, organizasyon ağacı ve başarısız zamanlanmış görev bildirimi önerileri korunmuştur. Bunlara aşağıdaki tasarım ilkeleri eklenmelidir:"),
          bullets([
              "Boş durumlar yalnızca 'veri yok' dememeli; yetkili kullanıcıya sonraki güvenli eylemi göstermelidir.",
              "Filtreler URL veya kalıcı sorgu durumuyla paylaşılabilir olmalı; tarih ve organizasyon kapsamı açıkça görünmelidir.",
              "Renk tek durum göstergesi olmamalı; metin, ikon ve erişilebilir etiket birlikte kullanılmalıdır.",
              "Tüm formlar klavye ile kullanılmalı, görünür odak ve alan bazlı hata mesajı içermelidir.",
              "Uzun tablolar mobilde öncelikli sütunlara indirgenmeli; dışa aktarım arka planda hazırlanmalıdır.",
              "Kritik onay ve toplu işlem öncesinde kapsam ve etkilenen kayıt sayısı gösterilmelidir."
          ]),
          P("SEO hakkında doğru yaklaşım", "H2TR"),
          P("PDKS yönetim paneli oturum gerektiren özel bir uygulamadır; bu ekranların arama motorlarında indekslenmesi istenmez. Bu nedenle panel ve giriş ekranlarında <b>noindex, nofollow</b> kullanılmalıdır. Buradaki kalite hedefi klasik SEO'dan çok semantik HTML, erişilebilirlik, hızlı yükleme ve güvenli başlıklardır. Yalnızca herkese açık bir ürün tanıtım sitesi oluşturulursa canonical URL, açıklama, Open Graph ve yapılandırılmış veri uygulanmalıdır."), PageBreak()]

story += [P("9. Riskler ve kontrol önerileri", "H1TR"),
          table([
              ["Risk", "Etki", "Önerilen kontrol"],
              ["Mükerrer mobil olay", "Çift giriş/çıkış ve hatalı puantaj.", "Benzersiz deviceEventId ve veritabanı unique index."],
              ["Saat dilimi hatası", "Yanlış gün/vardiya hesabı.", "UTC saklama, kullanıcı saat diliminde sunum, DST testleri."],
              ["Refresh token sızıntısı", "Uzun süreli hesap ele geçirme.", "Hash saklama, rotasyon, cihaz bazlı iptal ve yeniden kullanım tespiti."],
              ["Yetki kapsamı hatası", "Başka bölümün kişisel verisine erişim.", "Rol + veri kapsamı politikası ve entegrasyon testleri."],
              ["Ham olayın değiştirilmesi", "Denetim ve bordro güvenilirliğinin kaybı.", "Append-only olay, düzeltme kaydı ve audit log."],
              ["Ağ kesintisi", "Geçiş veya puantaj olayının kaybı.", "Offline kuyruk, tekrar deneme, gözlemlenebilir senkronizasyon."],
              ["Toplu işlem hatası", "Çok sayıda yanlış personel/puantaj kaydı.", "Ön izleme, doğrulama raporu, idempotent import ve geri alma planı."],
          ], [38 * mm, 56 * mm, 68 * mm]),
          P("KVKK ve mahremiyet", "H2TR"),
          P("PDKS verileri çalışan davranışı ve konumuyla ilişkilendirilebilen kişisel verilerdir. İşleme amacı, erişim rolleri, saklama süresi ve çalışan bilgilendirmesi kurumun hukuk/uyum sorumlularıyla belirlenmelidir. Biyometrik veri kullanılacaksa daha yüksek risk ve özel nitelikli veri gereksinimleri ayrıca değerlendirilmelidir."), PageBreak()]

story += [P("10. Aşamalı geliştirme planı", "H1TR"),
          table([
              ["Aşama", "Kapsam", "Çıkış ölçütü"],
              ["1 · Temel", "Dört proje, güvenli kimlik, roller, kullanıcı ve organizasyon.", "Temiz derleme, migration, yetki testleri."],
              ["2 · Olay hattı", "Terminal/mobil olay, idempotency, offline senkronizasyon.", "Tekrar gönderimde tek kayıt; izlenebilir hata."],
              ["3 · Puantaj", "Vardiya, takvim, geç/erken, eksik ve fazla mesai.", "Kural senaryoları otomatik testlerle doğrulanmış."],
              ["4 · İzin/onay", "Mobil talep, web onay, kapsam ve self-approval engeli.", "Uçtan uca durum geçişleri ve audit."],
              ["5 · Rapor/bordro", "Dönem, kilit, dışa aktarım ve mutabakat.", "Aynı girdiden tekrarlanabilir sonuç."],
              ["6 · Operasyon", "Gözlemlenebilirlik, yedekleme, güvenlik ve performans.", "Yük testi, geri yükleme denemesi, güvenlik kontrol listesi."],
          ], [25 * mm, 83 * mm, 54 * mm]),
          Spacer(1, 7 * mm),
          P("Başarı ölçütleri", "H2TR"), bullets([
              "Aynı ham olay kümesi aynı kural sürümünde aynı puantaj sonucunu üretir.",
              "Kritik işlem için kim, ne zaman, neyi değiştirdi sorusu cevaplanabilir.",
              "Mobil ağ kesintisinden sonra veri kaybı ve mükerrer kayıt oluşmaz.",
              "Yetkisiz kullanıcı başka personelin profil, puantaj veya izin verisini göremez.",
              "Web ve mobil sözleşmeler otomatik test ve OpenAPI ile doğrulanır."
          ]), PageBreak()]

story += [P("11. Sonuç", "H1TR"),
          P("Armongate incelemesi, Faydam PDKS'nin modül kapsamını belirlemek için güçlü bir başlangıçtır. İlk rapordaki ana eksik, teknik iddiaların kanıt seviyesi belirtilmeden kesin sonuç gibi sunulması ve puantajın bordro/operasyon ayrıntılarının yeterince ele alınmamasıdır."),
          P("Önerilen Faydam PDKS; gösterge panelinden ibaret olmayan, ham olay bütünlüğünü koruyan, vardiya ve izin kurallarını test edilebilir biçimde uygulayan, mobil çevrimdışı davranışı yöneten ve tüm yönetici kararlarını denetlenebilir kılan bir sistem olarak geliştirilmelidir."),
          P("Bu rapor, ürün kapsamı ve mimari yön için temel kabul edilmelidir. Her geliştirme aşamasında gereksinimler test senaryolarına dönüştürülmeli; güvenlik, KVKK ve bordro kuralları ilgili kurum paydaşlarıyla doğrulanmalıdır."),
          Spacer(1, 14 * mm),
          table([
              ["Hazırlanan ek çıktılar", "Konum"],
              ["Mobil API entegrasyon rehberi", "docs/MOBIL-API-ENTEGRASYON-REHBERI.md"],
              ["Mimari ve yol haritası", "docs/ARMONGATE-INCELEME-VE-PDKS-YOL-HARITASI.md"],
              ["Uygulama çözümü", "FaydamPDKS.sln"],
              ["Otomatik testler", "FaydamPDKS.Tests"],
          ], [62 * mm, 100 * mm])]

doc = SimpleDocTemplate(
    str(OUTPUT), pagesize=A4, rightMargin=20 * mm, leftMargin=20 * mm,
    topMargin=20 * mm, bottomMargin=20 * mm,
    title="Armongate Sistem İncelemesi ve Yeni PDKS Gereksinimleri",
    author="Elif Kara",
    subject="Faydam PDKS sistem analizi",
)
doc.build(story, onFirstPage=header_footer, onLaterPages=header_footer)
print(OUTPUT)
