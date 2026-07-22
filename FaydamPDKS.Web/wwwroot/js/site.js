// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

document.addEventListener("click", function (event) {
    const toggle = event.target.closest("[data-password-toggle]");
    if (!toggle) return;

    const input = toggle.closest(".password-field")?.querySelector("input");
    if (!input) return;

    const showPassword = input.type === "password";
    input.type = showPassword ? "text" : "password";
    toggle.setAttribute("aria-pressed", showPassword ? "true" : "false");
    toggle.setAttribute("aria-label", showPassword ? "Parolayı gizle" : "Parolayı göster");
});

document.addEventListener("DOMContentLoaded", function () {
    document.querySelectorAll("[data-field-work-request-form]").forEach(function (form) {
        const startDate = form.querySelector("[name='StartDate']");
        const endDate = form.querySelector("[name='EndDate']");
        const recurrence = form.querySelector("[data-recurrence-type]");
        const weekdayFields = form.querySelector("[data-weekday-fields]");
        const locationType = form.querySelector("[data-request-location-type]");
        const fieldFields = form.querySelector("[data-request-field-fields]");
        if (!startDate || !endDate || !recurrence || !weekdayFields) return;

        function updateFieldWorkRecurrence() {
            const isSingleDay = startDate.value && endDate.value && startDate.value === endDate.value;
            if (isSingleDay) recurrence.value = "3";
            const selectedWeekdays = !isSingleDay && recurrence.value === "2";
            weekdayFields.hidden = !selectedWeekdays;
            weekdayFields.querySelectorAll("input").forEach(function (control) {
                control.disabled = !selectedWeekdays;
                if (!selectedWeekdays) control.checked = false;
            });
            recurrence.querySelector("option[value='3']").hidden = !isSingleDay;
            if (!isSingleDay && recurrence.value === "3") recurrence.value = "1";
        }

        startDate.addEventListener("change", function () {
            if (!endDate.value) endDate.value = startDate.value;
            if (endDate.value < startDate.value) endDate.value = startDate.value;
            endDate.min = startDate.value;
            updateFieldWorkRecurrence();
        });
        endDate.addEventListener("change", updateFieldWorkRecurrence);
        recurrence.addEventListener("change", updateFieldWorkRecurrence);
        function updateRequestLocation() {
            if (!locationType || !fieldFields) return;
            const isField = locationType.value === "2";
            fieldFields.hidden = !isField;
            fieldFields.querySelectorAll("input").forEach(function (control) {
                control.disabled = !isField;
                control.required = isField && control.name === "ProjectName";
                if (!isField) control.value = "";
            });
        }
        locationType?.addEventListener("change", updateRequestLocation);
        updateRequestLocation();
        updateFieldWorkRecurrence();
    });

    document.querySelectorAll("[data-work-location-form]").forEach(function (form) {
        const type = form.querySelector("[data-work-location-type]");
        const fieldFields = form.querySelector("[data-field-work-fields]");
        const recurrence = form.querySelector("[data-recurrence-type]");
        const weekdayFields = form.querySelector("[data-weekday-fields]");
        const startDate = form.querySelector("[name='StartDate']");
        const endDate = form.querySelector("[name='EndDate']");
        if (!type || !fieldFields) return;

        function updateLocationFields() {
            const isFieldWork = type.value === "2";
            fieldFields.hidden = !isFieldWork;
            fieldFields.querySelectorAll("input, textarea, select").forEach(function (control) {
                control.disabled = !isFieldWork;
                if (!isFieldWork) control.value = "";
            });
        }

        type.addEventListener("change", updateLocationFields);
        updateLocationFields();

        function updateRecurrenceFields() {
            if (!recurrence || !weekdayFields) return;
            const selectedWeekdays = recurrence.value === "2";
            weekdayFields.hidden = !selectedWeekdays;
            weekdayFields.querySelectorAll("input").forEach(function (control) {
                control.disabled = !selectedWeekdays;
                if (!selectedWeekdays) control.checked = false;
            });
            if (recurrence.value === "3" && startDate && endDate && startDate.value) endDate.value = startDate.value;
        }
        recurrence?.addEventListener("change", updateRecurrenceFields);
        startDate?.addEventListener("change", function () {
            if (recurrence?.value === "3" && endDate) endDate.value = startDate.value;
        });
        updateRecurrenceFields();
    });
});

document.querySelectorAll("[data-global-search]").forEach(function (search) {
    const input = search.querySelector("input[type='search']");
    const results = search.querySelector(".global-search-results");
    const endpoint = search.dataset.searchUrl;
    const scope = search.dataset.searchScope;
    let timer;
    let request;
    const managerPages = [
        ["Kontrol Paneli", "Günlük özet, grafikler ve bekleyen işlemler", "/", "ana sayfa dashboard"],
        ["PDKS Yönetim Merkezi", "Giriş-çıkış, izin, vardiya ve takvim", "/Modules/Pdks"],
        ["QR Kodları", "Giriş ve çıkış QR kodlarını yönetin", "/AttendanceQr#qr-codes", "qr yenile indir pasifleştir"],
        ["QR oluştur", "Yeni giriş veya çıkış QR kodu oluşturun", "/AttendanceQr#qr-codes", "karekod ekle"],
        ["Geçiş Kayıtları", "Mobil QR giriş-çıkış hareketleri", "/AttendanceQr#transitions", "hareketler puantaj"],
        ["Personel yönetimi", "Personel listesi ve hesaplar", "/Employees", "çalışan kullanıcı liste"],
        ["Personel ekle", "Yeni personel hesabı oluşturun", "/Employees#create-employee", "çalışan ekle kullanıcı oluştur yeni personel"],
        ["Mobil kayıt onayları", "Yeni çalışan kayıtlarını onaylayın", "/Registrations", "kullanıcı kayıt kabul red"],
        ["İzinler ve Talepler", "Personel izin taleplerini onaylayın veya reddedin", "/LeaveRequests", "izin onayı"],
        ["Vardiyalar", "Vardiya şablonları ve çalışma planları", "/Shifts"],
        ["Vardiya oluştur", "Yeni vardiya şablonu tanımlayın", "/Shifts#create-shift", "vardiya ekle"],
        ["Vardiya ata", "Personele vardiya ve geçerlilik dönemi atayın", "/Shifts#assign-shift", "personel vardiya atama"],
        ["Çalışma Takvimi", "Tatil ve özel çalışma günlerini yönetin", "/WorkCalendar"],
        ["Özel gün ekle", "Takvime tatil veya çalışma günü ekleyin", "/WorkCalendar#create-calendar-title", "tatil ekle takvim kuralı"],
        ["Çalışma Konumu Planları", "Uzaktan çalışma ve saha görevlerini yönetin", "/WorkLocations", "çalışma yeri konum"],
        ["Çalışma konumu talebini onayla", "Uzaktan çalışma ve saha görevi talepleri", "/WorkLocations", "uzaktan çalışma onay saha görevi"],
        ["Organizasyon", "İşyeri ve bölüm tanımlarını yönetin", "/Organization", "işyeri bölüm ekle"],
        ["Raporlama Merkezi", "PDKS ve yönetim raporları", "/Modules/Reporting"],
        ["Puantaj raporu", "Günlük çalışma sonuçlarını görüntüleyin", "/Reports", "rapor getir"],
        ["Rapor indir", "Puantaj raporunu CSV, Excel veya PDF indirin", "/Reports", "csv excel pdf dışa aktar"],
        ["Denetim kayıtları", "Kritik işlem ve değişiklik geçmişi", "/Audit", "audit log güvenlik"],
        ["İşlemler Merkezi", "Onay ve düzeltme işlemleri", "/Modules/Operations"],
        ["Puantaj Düzeltmeleri", "Eksik veya hatalı kayıt taleplerini değerlendirin", "/AttendanceCorrections", "düzeltme onayla reddet"],
        ["Terminal yönetimi", "Fiziksel PDKS cihazlarını yönetin", "/Terminals", "cihaz okuyucu terminal kaydet"],
        ["Bildirimler", "Güncel sistem bildirimlerini görüntüleyin", "/Home/Account?section=notifications"],
        ["Hesabım", "Profil ve hesap ayarları", "/Home/Account", "şifre parola tema dil"],
        ["Yardım", "Destek ve kullanım bilgileri", "/Home/Help"]
    ];
    const personalPages = [
        ["Kontrol Paneli", "Günlük çalışma ve puantaj özeti", "/MyWork"],
        ["Çalışma Konumlarım", "Uzaktan çalışma ve saha görevi talepleri", "/MyWork/FieldWork", "çalışma yeri konum"],
        ["Uzaktan çalışma talebi", "Yeni uzaktan çalışma talebi gönderin", "/MyWork/FieldWork", "evden çalışma"],
        ["Saha görevi talebi", "Yeni saha görevi talebi gönderin", "/MyWork/FieldWork"],
        ["Çalışma Kayıtlarım", "Puantaj, giriş-çıkış ve düzeltmeler", "/MyWork/Records", "mesai"],
        ["Puantaj düzeltmesi talep et", "Eksik veya hatalı çalışma kaydını bildirin", "/MyWork/Records", "giriş çıkış düzelt"],
        ["Puantaj raporu indir", "Çalışma kayıtlarını CSV, Excel veya PDF indirin", "/MyWork/Records"],
        ["İzinlerim", "İzin talepleri ve geçmişi", "/MyWork/Leaves", "izin oluştur talep gönder"],
        ["Bildirimlerim", "Hesap bildirimleri", "/Home/Account?section=notifications"],
        ["Hesabım", "Profil ve hesap ayarları", "/Home/Account"],
        ["Yardım", "Destek ve kullanım bilgileri", "/Home/Help"]
    ];
    const pages = scope === "manager" ? managerPages : personalPages;

    function normalizeSearch(value) {
        return value.toLocaleLowerCase("tr-TR").normalize("NFD").replace(/[\u0300-\u036f]/g, "")
            .replaceAll("ı", "i").replaceAll("ş", "s").replaceAll("ğ", "g").replaceAll("ç", "c").replaceAll("ö", "o").replaceAll("ü", "u")
            .replace(/[^a-z0-9\s]/g, " ").replace(/\s+/g, " ").trim();
    }

    function editDistance(left, right) {
        const row = Array.from({ length: right.length + 1 }, (_, index) => index);
        for (let i = 1; i <= left.length; i++) {
            let previous = row[0]; row[0] = i;
            for (let j = 1; j <= right.length; j++) {
                const current = row[j];
                row[j] = Math.min(row[j] + 1, row[j - 1] + 1, previous + (left[i - 1] === right[j - 1] ? 0 : 1));
                previous = current;
            }
        }
        return row[right.length];
    }

    function pageScore(page, query) {
        const haystack = normalizeSearch(`${page[0]} ${page[1]} ${page[3] || ""}`);
        if (haystack.includes(query)) return 100 + (normalizeSearch(page[0]).includes(query) ? 20 : 0);
        const words = haystack.split(" ");
        const queryWords = query.split(" ");
        let score = 0;
        for (const queryWord of queryWords) {
            let best = 0;
            for (const word of words) {
                if (word === queryWord) best = Math.max(best, 20);
                else if (word.startsWith(queryWord) || queryWord.startsWith(word)) best = Math.max(best, 14);
                else {
                    const distance = editDistance(queryWord, word);
                    const limit = Math.max(1, Math.floor(Math.max(queryWord.length, word.length) * .34));
                    if (distance <= limit) best = Math.max(best, 11 - distance * 2);
                }
            }
            score += best;
        }
        return score >= queryWords.length * 7 ? score : 0;
    }

    function closeResults() {
        results.hidden = true;
        results.replaceChildren();
        input.setAttribute("aria-expanded", "false");
    }

    function render(items, query) {
        results.replaceChildren();
        if (!items.length) {
            const empty = document.createElement("div");
            empty.className = "global-search-empty";
            empty.textContent = `“${query}” için sonuç bulunamadı.`;
            results.append(empty);
        } else {
            items.slice(0, 9).forEach(function (item) {
                const link = document.createElement("a");
                link.href = item.url;
                link.setAttribute("role", "option");
                const copy = document.createElement("span");
                const title = document.createElement("strong");
                const description = document.createElement("small");
                const category = document.createElement("em");
                title.textContent = item.title;
                description.textContent = item.description;
                category.textContent = item.category;
                copy.append(title, description);
                link.append(copy, category);
                results.append(link);
            });
        }
        results.hidden = false;
        input.setAttribute("aria-expanded", "true");
    }

    input.addEventListener("input", function () {
        clearTimeout(timer);
        request?.abort();
        const query = input.value.trim();
        if (query.length < 2) return closeResults();
        timer = setTimeout(async function () {
            const normalized = normalizeSearch(query);
            const pageMatches = pages
                .map(page => ({ page, score: pageScore(page, normalized) }))
                .filter(match => match.score > 0)
                .sort((left, right) => right.score - left.score)
                .map(match => ({ title: match.page[0], description: match.page[1], category: match.score >= 100 ? "Sonuç" : "Önerilen", url: match.page[2] }));
            try {
                request = new AbortController();
                const response = await fetch(`${endpoint}?q=${encodeURIComponent(query)}`, { signal: request.signal, headers: { "X-Requested-With": "XMLHttpRequest" } });
                const people = response.ok ? await response.json() : [];
                render([...pageMatches, ...people], query);
            } catch (error) {
                if (error.name !== "AbortError") render(pageMatches, query);
            }
        }, 220);
    });

    input.addEventListener("keydown", function (event) {
        if (event.key === "Escape") closeResults();
        if (event.key === "Enter") {
            const first = results.querySelector("a");
            if (first) { event.preventDefault(); first.click(); }
        }
    });
    document.addEventListener("click", event => { if (!search.contains(event.target)) closeResults(); });
});
