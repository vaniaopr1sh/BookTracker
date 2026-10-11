/* ==========================================================================
   BookWings — особистий кабінет
   Статистика читача завантажується із захищеного endpoint /api/profile/stats.
   Запит іде через BookWingsAuth.apiFetch, тому JWT додається автоматично.
   ========================================================================== */
(function (window, document) {
    "use strict";

    const Auth = window.BookWingsAuth;
    const statsBox = document.getElementById("bwProfileStats");
    const recentBox = document.getElementById("bwProfileRecent");
    const adminBox = document.getElementById("bwAdminStats");
    const sessionInfo = document.getElementById("bwSessionInfo");
    const script = document.currentScript;
    if (!statsBox || !Auth) return;

    // Безпечне створення елементів (дані з сервера не вставляються як HTML)
    function el(tag, className, text) {
        const node = document.createElement(tag);
        if (className) node.className = className;
        if (text !== undefined && text !== null) node.textContent = text;
        return node;
    }

    function tile(value, label) {
        const box = el("div", "bw-stat-tile");
        box.append(el("div", "bw-stat-tile-value", value), el("div", "bw-stat-tile-label", label));
        return box;
    }

    function renderStats(stats) {
        const tiles = el("div", "bw-stat-tiles");
        tiles.append(
            tile(stats.total, "книг на полиці"),
            tile(stats.reading, "читаю зараз"),
            tile(stats.read, "прочитано"),
            tile(stats.averageRating != null ? stats.averageRating.toFixed(1) : "—", "середня оцінка")
        );

        const progress = el("div", "bw-progress-block");
        const percent = stats.total > 0 ? Math.round((stats.read / stats.total) * 100) : 0;
        const caption = el("div", "d-flex justify-content-between small mb-1");
        caption.append(el("span", "", "Прочитано з полиці"), el("strong", "", percent + "%"));
        const bar = el("div", "bw-progress");
        bar.setAttribute("role", "progressbar");
        bar.setAttribute("aria-valuenow", String(percent));
        bar.setAttribute("aria-valuemin", "0");
        bar.setAttribute("aria-valuemax", "100");
        const fill = el("span");
        bar.append(fill);
        progress.append(caption, bar);

        const facts = el("p", "small text-secondary mt-3 mb-0");
        facts.textContent = `Хочу прочитати: ${stats.wantToRead} · Оцінок поставлено: ${stats.ratingsCount}` +
            (stats.favoriteGenre ? ` · Улюблений жанр: ${stats.favoriteGenre}` : "");

        statsBox.replaceChildren(tiles, progress, facts);
        // Анімація заповнення шкали
        window.requestAnimationFrame(() => { fill.style.width = percent + "%"; });
    }

    function renderRecent(books) {
        if (!books.length) {
            const empty = el("div", "text-center py-3");
            empty.append(el("p", "text-secondary mb-2", "Ваша полиця поки порожня."));
            const link = el("a", "btn btn-sm btn-olive", "Знайти книгу");
            link.href = "/Books";
            empty.append(link);
            recentBox.replaceChildren(empty);
            return;
        }

        const list = el("ul", "bw-recent-list");
        books.forEach((book) => {
            const item = el("li");
            const link = el("a", "bw-recent-item");
            link.href = `/Books/Details/${encodeURIComponent(book.id)}`;

            const thumb = el("span", "bw-recent-thumb");
            if (book.coverImageUrl) {
                const img = el("img");
                img.src = book.coverImageUrl;
                img.alt = "";
                img.loading = "lazy";
                img.addEventListener("error", () => img.remove(), { once: true });
                thumb.append(img);
            }

            const info = el("span", "bw-recent-info");
            info.append(el("strong", "", book.title), el("small", "", book.author));

            const status = el("span", "bw-recent-status bw-status-" + book.status.toLowerCase(), book.statusName);
            link.append(thumb, info, status);
            if (book.myRating) {
                const stars = el("span", "bw-stars ms-2");
                stars.style.setProperty("--rating", book.myRating);
                stars.setAttribute("role", "img");
                stars.setAttribute("aria-label", `Моя оцінка ${book.myRating} з 5`);
                link.append(stars);
            }
            item.append(link);
            list.append(item);
        });
        recentBox.replaceChildren(list);
    }

    function renderAdmin(admin) {
        if (!adminBox || !admin) return;
        adminBox.replaceChildren(
            tile(admin.books, "книг у каталозі"),
            tile(admin.users, "користувачів"),
            tile(admin.ratings, "оцінок")
        );
    }

    function renderError(message) {
        const box = el("div", "bw-empty py-4");
        box.append(el("h3", "", "Не вдалося завантажити статистику"), el("p", "", message));
        const retry = el("button", "btn btn-primary btn-sm", "Спробувати ще раз");
        retry.type = "button";
        retry.addEventListener("click", load);
        box.append(retry);
        statsBox.replaceChildren(box);
        recentBox.replaceChildren();
    }

    async function load() {
        statsBox.setAttribute("aria-busy", "true");
        statsBox.classList.add("opacity-50");
        try {
            const stats = await Auth.apiFetch("/api/profile/stats");
            renderStats(stats);
            renderRecent(stats.recent || []);
            renderAdmin(stats.admin);
        } catch (error) {
            // 401 обробляє auth.js (popup входу), тут показуємо інші помилки
            if (error.status !== 401) renderError(error.message);
        } finally {
            statsBox.setAttribute("aria-busy", "false");
            statsBox.classList.remove("opacity-50");
        }
    }

    function updateSessionInfo() {
        if (!sessionInfo) return;
        const token = Auth.getToken();
        const raw = window.localStorage ? window.localStorage.getItem("bw.expiresAt") : null;
        if (!token || !raw) {
            sessionInfo.textContent = "Сесія відновлюється…";
            return;
        }
        const minutes = Math.max(0, Math.round((Date.parse(raw) - Date.now()) / 60000));
        sessionInfo.textContent = `Токен доступу дійсний ще ${minutes} хв (оновлюється автоматично)`;
    }

    document.querySelector("[data-bw-stats-reload]")?.addEventListener("click", load);
    document.addEventListener("bw:session-changed", updateSessionInfo);

    document.addEventListener("DOMContentLoaded", async () => {
        // Після зміни імені отримуємо новий токен, щоб у ньому було актуальне ім'я
        if (script && script.dataset.refreshToken === "true") {
            await Auth.refresh();
        }
        await load();
        updateSessionInfo();
        window.setInterval(updateSessionInfo, 30000);
    });
})(window, document);
