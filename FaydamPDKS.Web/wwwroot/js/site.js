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
    let timer;
    let request;
    const pages = [
        ["Kontrol Paneli", "Günlük özet ve grafikler", "/"],
        ["PDKS", "Giriş-çıkış, izin, vardiya ve takvim", "/Modules/Pdks"],
        ["QR Kodları", "Giriş-çıkış QR yönetimi", "/AttendanceQr#qr-codes"],
        ["Geçiş Kayıtları", "Mobil QR hareketleri", "/AttendanceQr#transitions"],
        ["Personel", "Personel listesi ve hesaplar", "/Employees"],
        ["İzinler ve Talepler", "İzin yönetimi", "/LeaveRequests"],
        ["Vardiyalar", "Vardiya ve çalışma planları", "/Shifts"],
        ["Çalışma Takvimi", "Takvim ve özel günler", "/WorkCalendar"],
        ["Raporlama", "PDKS ve yönetim raporları", "/Modules/Reporting"],
        ["Raporlar", "PDKS raporları", "/Reports"],
        ["İşlemler", "Onay ve düzeltme merkezi", "/Modules/Operations"],
        ["Puantaj Düzeltmeleri", "Düzeltme talepleri", "/AttendanceCorrections"]
    ];

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
            const normalized = query.toLocaleLowerCase("tr-TR");
            const pageMatches = pages
                .filter(page => `${page[0]} ${page[1]}`.toLocaleLowerCase("tr-TR").includes(normalized))
                .map(page => ({ title: page[0], description: page[1], category: "Sayfa", url: page[2] }));
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
