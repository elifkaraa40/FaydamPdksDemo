using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace FaydamPDKS.Web.Localization;

public sealed class UiTranslationMiddleware(RequestDelegate next)
{
    private static readonly IReadOnlyDictionary<string, string> English = new Dictionary<string, string>
    {
        ["Kontrol Paneli"] = "Dashboard", ["Çalışma Kayıtlarım"] = "My Work Records", ["Çalışma kayıtlarım"] = "My work records",
        ["İzinlerim"] = "My Leaves", ["Bildirimlerim"] = "My Notifications", ["Hesabım"] = "My Account", ["Çıkış"] = "Sign out",
        ["Personel"] = "Employees", ["Raporlama"] = "Reporting", ["İşlemler"] = "Operations", ["Yardım"] = "Help",
        ["Yeni bildirimleriniz var"] = "You have new notifications", ["Tümünü göster"] = "Show all", ["Görüntüle ve kapat"] = "View and dismiss",
        ["PDKS Merkezi"] = "PDKS Center", ["Giriş-Çıkış"] = "Check-in/out", ["İzin Yönetimi"] = "Leave Management",
        ["Vardiya ve Planlama"] = "Shifts and Planning", ["Çalışma Takvimi"] = "Work Calendar", ["Tanımlamalar"] = "Definitions",
        ["Raporlama Merkezi"] = "Reporting Center", ["PDKS Raporları"] = "PDKS Reports", ["Denetim Kayıtları"] = "Audit Logs",
        ["İşlemler Merkezi"] = "Operations Center", ["Puantaj Düzeltmeleri"] = "Attendance Corrections", ["İzin Onayları"] = "Leave Approvals",
        ["Geçiş Kayıtları"] = "Transition Records", ["Ana içeriğe geç"] = "Skip to main content", ["Ana menü"] = "Main menu",
        ["Personel veya sayfa ara"] = "Search employees or pages", ["okunmamış bildirim"] = "unread notifications",
        ["Personel portalı"] = "Employee portal", ["Günlük çalışma durumunuz ve bu aya ait puantaj özetiniz."] = "Your daily work status and monthly attendance summary.",
        ["Çalışma kayıtlarımı aç"] = "Open my work records", ["Bugünkü durum"] = "Today's status", ["Bugün çalışılan"] = "Worked today",
        ["Bu ay fazla mesai"] = "Overtime this month", ["Bekleyen izin"] = "Pending leave", ["İzinlerimi aç"] = "Open my leaves",
        ["Bugünkü hareket"] = "Today's activity", ["Giriş ve çıkış"] = "Check-in and check-out", ["İlk giriş"] = "First check-in",
        ["Son çıkış"] = "Last check-out", ["Çalışılan"] = "Worked", ["Fazla mesai"] = "Overtime", ["Bu ay"] = "This month",
        ["Puantaj dağılımı"] = "Attendance distribution", ["Tamamlanan"] = "Completed", ["Eksik kayıt"] = "Missing record",
        ["Çalışma dışı"] = "Non-working", ["Kayıt yok"] = "No record", ["Giriş eksik"] = "Missing check-in", ["Çıkış eksik"] = "Missing check-out",
        ["Tamamlandı"] = "Completed", ["Beklenen"] = "Expected", ["Hesaplanan toplam"] = "Calculated total",
        ["Giriş-çıkış, çalışma süresi ve mola geçmişinizi inceleyin."] = "Review your check-in/out, working time and break history.",
        ["CSV indir"] = "Download CSV", ["Başlangıç"] = "Start", ["Bitiş"] = "End", ["Görüntüle"] = "View", ["Dönem"] = "Period",
        ["Giriş-çıkış kayıtları"] = "Check-in/out records", ["Vardiya"] = "Shift", ["Giriş"] = "Check-in", ["Çıkış"] = "Check-out",
        ["Çalışma"] = "Work", ["Durum"] = "Status", ["Molalar"] = "Breaks", ["Mola geçmişim"] = "My break history",
        ["Süre"] = "Duration", ["Kapanış"] = "Closure", ["Devam ediyor"] = "In progress", ["Çıkışta otomatik"] = "Automatic at check-out",
        ["Normal"] = "Normal", ["Talepler"] = "Requests", ["Puantaj düzeltmelerim"] = "My attendance corrections",
        ["Talep edilen"] = "Requested", ["Gerekçe"] = "Reason", ["Bekliyor"] = "Pending", ["Onaylandı"] = "Approved", ["Reddedildi"] = "Rejected",
        ["Eksik veya hatalı kayıt"] = "Missing or incorrect record", ["Düzeltme talep et"] = "Request correction", ["Çalışma tarihi"] = "Work date",
        ["Giriş saati"] = "Check-in time", ["Çıkış saati"] = "Check-out time", ["En az 10 karakter girin."] = "Enter at least 10 characters.",
        ["Onaya gönder"] = "Submit for approval", ["Yeni izin talebi oluşturun ve mevcut taleplerinizin durumunu takip edin."] = "Create a new leave request and track existing requests.",
        ["Geçmiş"] = "History", ["İzin taleplerim"] = "My leave requests", ["Tür"] = "Type", ["Açıklama"] = "Description",
        ["takvim günü"] = "calendar days", ["iş günü"] = "work days", ["İptal"] = "Cancel", ["Yeni talep"] = "New request",
        ["İzin talep et"] = "Request leave", ["İzin türü"] = "Leave type", ["İzin süresi"] = "Leave duration",
        ["Yarım gün seçilirse başlangıç ve bitiş aynı tarih olmalıdır."] = "For half-day leave, start and end dates must be the same.",
        ["İnsan kaynakları"] = "Human resources", ["İzin talepleri"] = "Leave requests", ["bekleyen talep"] = "pending requests",
        ["Personel taleplerini inceleyin ve kararları kayıt altına alın."] = "Review employee requests and record decisions.",
        ["Karar notu"] = "Decision note", ["isteğe bağlı"] = "optional", ["Onayla"] = "Approve", ["Reddet"] = "Reject",
        ["Onay merkezi"] = "Approval center", ["Puantaj düzeltmeleri"] = "Attendance corrections", ["Düzeltme listesi"] = "Correction list",
        ["Henüz düzeltme talebi bulunmuyor."] = "There are no correction requests yet.", ["İstenen saatler"] = "Requested times",
        ["Durum / işlem"] = "Status / action", ["Gece çalışması"] = "Night work", ["Gündüz çalışması"] = "Day work",
        ["Personel Yönetimi"] = "Employee Management", ["Personel yönetimi"] = "Employee management", ["Organizasyon"] = "Organization",
        ["Personel hesaplarını, rollerini ve çalışma bilgilerini yönetin."] = "Manage employee accounts, roles and work information.",
        ["Mobil kayıt onayları"] = "Mobile registration approvals", ["personel"] = "employees", ["Kayıtlar"] = "Records", ["Personel listesi"] = "Employee list",
        ["Henüz personel bulunmuyor."] = "No employees found yet.", ["İlk personeli sağdaki formdan oluşturun."] = "Create the first employee using the form on the right.",
        ["Bölüm"] = "Department", ["Rol"] = "Role", ["İşlem"] = "Action", ["İşyeri belirtilmemiş"] = "Workplace not specified",
        ["İşe giriş yok"] = "No hire date", ["Aktif"] = "Active", ["Pasif"] = "Inactive", ["Düzenle"] = "Edit", ["Pasife al"] = "Deactivate",
        ["Aktifleştir"] = "Activate", ["Yeni kayıt"] = "New record", ["Personel ekle"] = "Add employee", ["Sicil numarası"] = "Employee number",
        ["Ad soyad"] = "Full name", ["Kurumsal e-posta"] = "Corporate email", ["İşyeri / bölüm"] = "Workplace / department",
        ["Bölüm seçilmedi"] = "No department selected", ["İşe giriş tarihi"] = "Hire date", ["Rol seçin"] = "Select role",
        ["Geçici parola"] = "Temporary password", ["En az 8 karakter. Güvenli kanaldan personele iletin."] = "At least 8 characters. Share it with the employee through a secure channel.",
        ["Personel oluştur"] = "Create employee", ["Personeli düzenle"] = "Edit employee", ["Kimlik, organizasyon ve rol bilgilerini güncelleyin."] = "Update identity, organization and role information.",
        ["Listeye dön"] = "Back to list", ["Vazgeç"] = "Cancel", ["Değişiklikleri kaydet"] = "Save changes",
        ["Mobil Kayıt Onayları"] = "Mobile Registration Approvals", ["Telefon ve parolayla kayıt olan yeni çalışanları personel bilgileriyle eşleştirin."] = "Match employees who registered with a phone number and password to their personnel information.",
        ["Bekleyen kayıt yok"] = "No pending registrations", ["Yeni mobil kayıtlar burada görünecek."] = "New mobile registrations will appear here.",
        ["Telefon"] = "Phone", ["Personel no"] = "Employee no.", ["Departman"] = "Department", ["Departman seçilmedi"] = "No department selected",
        ["Bu kaydı reddetmek istiyor musunuz?"] = "Do you want to reject this registration?", ["Örn."] = "E.g.",
        ["Giriş-Çıkış Yönetimi"] = "Check-in/out Management", ["Mobil QR kodlarını yönetin ve personel geçişlerini tek ekrandan izleyin."] = "Manage mobile QR codes and monitor employee transitions from one screen.",
        ["aktif QR"] = "active QR", ["QR yenilendi"] = "QR renewed", ["QR oluşturuldu"] = "QR created", ["QR'ı indir"] = "Download QR", ["Yazdır"] = "Print",
        ["QR Kodları"] = "QR Codes", ["Ayarlar"] = "Settings", ["Manuel yönetim"] = "Manual management", ["QR kodları"] = "QR codes",
        ["Henüz QR kodu yok."] = "There are no QR codes yet.", ["Yeni QR"] = "New QR", ["QR oluştur"] = "Create QR", ["QR adı"] = "QR name",
        ["İşyeri"] = "Workplace", ["İşyeri seçin"] = "Select workplace", ["Bölge seçin"] = "Select zone", ["İşlem türü"] = "Action type",
        ["Canlı kayıtlar"] = "Live records", ["Geçiş kayıtları"] = "Transition records", ["Henüz geçiş kaydı yok."] = "There are no transition records yet.",
        ["Bölge"] = "Zone", ["Kaynak"] = "Source", ["Zaman"] = "Time", ["Firma yapılandırması"] = "Company configuration",
        ["Giriş-çıkış yöntemleri"] = "Check-in/out methods", ["Açık"] = "Enabled", ["Kapalı"] = "Disabled", ["Manuel"] = "Manual",
        ["Güvenlik ve izlenebilirlik"] = "Security and traceability", ["Denetim kayıtları"] = "Audit logs", ["İşlem geçmişi"] = "Action history",
        ["Henüz denetim kaydı bulunmuyor."] = "There are no audit logs yet.", ["Aktör"] = "Actor", ["Varlık"] = "Entity", ["Değişiklik"] = "Change",
        ["İstek kimliği yok"] = "No request identifier", ["Değerleri göster"] = "Show values", ["Önce"] = "Before", ["Sonra"] = "After",
        ["Çalışma takvimi"] = "Work calendar", ["Puantaj kuralları"] = "Attendance rules", ["özel gün"] = "special days", ["Özel günler"] = "Special days",
        ["Takvim kayıtları"] = "Calendar records", ["Özel gün tanımlanmamış."] = "No special days have been defined.", ["Gün"] = "Day", ["Kapsam"] = "Scope",
        ["Tatil"] = "Holiday", ["Çalışma günü"] = "Working day", ["Yeni kural"] = "New rule", ["Özel gün ekle"] = "Add special day",
        ["Gün adı / açıklama"] = "Day name / description", ["Gün tipi"] = "Day type", ["Tüm işyerleri"] = "All workplaces", ["Takvime ekle"] = "Add to calendar",
        ["Tema ve arayüz dilini seçin."] = "Choose the theme and interface language.", ["Tercihler"] = "Preferences", ["Arayüz ayarlarım"] = "Interface settings",
        ["Dil seçimi"] = "Language", ["Hesap arayüzü dili"] = "Account interface language", ["Güvenlik"] = "Security", ["Hesap koruması"] = "Account protection",
        ["Parola değiştir"] = "Change password", ["En az 8 karakter kullanın"] = "Use at least 8 characters", ["Mevcut parola"] = "Current password",
        ["Yeni parola"] = "New password", ["Yeni parolayı doğrulayın"] = "Confirm new password", ["Parolayı göster"] = "Show password",
        ["Tercihleriniz kaydedildi."] = "Preferences saved.", ["Aydınlık"] = "Light", ["Karanlık"] = "Dark",
        ["Türkçe"] = "Turkish", ["Faydam PDKS ana sayfa"] = "Faydam PDKS home", ["Genel arama"] = "Global search",
        ["Güvenli ve izlenebilir iş gücü yönetimi"] = "Secure and traceable workforce management",
        ["İşe giriş yok"] = "No hire date", ["En az 6 karakter. Güvenli kanaldan personele iletin."] = "At least 8 characters. Share it with the employee through a secure channel.",
        ["Yonetici"] = "Administrator", ["Merkez İşyeri"] = "Head Office"
    };

    private static readonly Regex TextNode = new(@"(?<=>)(?<text>[^<>]+)(?=<)", RegexOptions.Compiled);
    private static readonly Regex TranslatableAttribute = new(@"(?<name>aria-label|title|placeholder|alt)=(?<quote>[\""'])(?<value>.*?)(\k<quote>)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public async Task InvokeAsync(HttpContext context)
    {
        if (!string.Equals(context.Request.Cookies["Faydam.Language"], "en", StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        var originalBody = context.Response.Body;
        await using var buffer = new MemoryStream();
        context.Response.Body = buffer;
        await next(context);
        context.Response.Body = originalBody;
        buffer.Position = 0;
        if (context.Response.ContentType?.StartsWith("text/html", StringComparison.OrdinalIgnoreCase) != true)
        {
            await buffer.CopyToAsync(originalBody, context.RequestAborted);
            return;
        }

        var html = await new StreamReader(buffer, Encoding.UTF8).ReadToEndAsync(context.RequestAborted);
        html = TextNode.Replace(html, match => ReplacePreservingWhitespace(match.Groups["text"].Value));
        html = TranslatableAttribute.Replace(html, match =>
        {
            var translated = Translate(match.Groups["value"].Value);
            return $"{match.Groups["name"].Value}={match.Groups["quote"].Value}{translated}{match.Groups["quote"].Value}";
        });
        context.Response.ContentLength = null;
        await context.Response.WriteAsync(html, Encoding.UTF8, context.RequestAborted);
    }

    private static string ReplacePreservingWhitespace(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.Length == 0) return value;
        var translated = Translate(trimmed);
        return translated == trimmed ? value : value.Replace(trimmed, translated, StringComparison.Ordinal);
    }

    private static string Translate(string value)
    {
        if (English.TryGetValue(value, out var translated)) return translated;
        if (value.Contains(" · ", StringComparison.Ordinal))
            return string.Join(" · ", value.Split(" · ", StringSplitOptions.None).Select(Translate));
        var suffixes = new (string Turkish, string English)[]
        {
            (" personel", " employees"), (" bekleyen talep", " pending requests"), (" bekleyen", " pending"),
            (" aktif QR", " active QR"), (" özel gün", " special days"), (" takvim günü", " calendar days"),
            (" iş günü", " work days"), (" bekleyen olay", " pending events"), (" okunmamış bildirim", " unread notifications")
        };
        foreach (var suffix in suffixes)
            if (value.EndsWith(suffix.Turkish, StringComparison.Ordinal)
                && decimal.TryParse(value[..^suffix.Turkish.Length], NumberStyles.Number, CultureInfo.CurrentCulture, out _))
                return value[..^suffix.Turkish.Length] + suffix.English;
        return Regex.Replace(value, @"^(\d+) sa (\d+) dk$", "$1 h $2 min", RegexOptions.CultureInvariant);
    }
}
