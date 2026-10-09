/* ==========================================================================
   BookWings — каталог: живий пошук, фільтр за жанром і сортування без
   перезавантаження сторінки. Сервер повертає готову розмітку сітки (_BookGrid).
   ========================================================================== */
(function (window, document) {
    "use strict";

    const form = document.getElementById("bwCatalogFilters");
    const results = document.getElementById("bwCatalogResults");
    if (!form || !results) return;

    const searchInput = form.querySelector("#bwSearch");
    const sortSelect = form.querySelector("#bwSort");
    const genreInput = form.querySelector("#bwGenre");
    const skeleton = document.getElementById("bwSkeletonTemplate");
    const chips = document.querySelectorAll("[data-bw-genre]");

    let debounceTimer = null;
    let controller = null;
    let skeletonTimer = null;

    function buildQuery() {
        const params = new URLSearchParams();
        const search = searchInput.value.trim();
        if (search) params.set("search", search);
        if (genreInput.value) params.set("genre", genreInput.value);
        if (sortSelect.value && sortSelect.value !== "title") params.set("sort", sortSelect.value);
        return params.toString();
    }

    function setLoading(loading) {
        results.classList.toggle("is-loading", loading);
        results.setAttribute("aria-busy", loading ? "true" : "false");
        window.clearTimeout(skeletonTimer);
        // Скелетон показуємо лише якщо відповідь затримується, щоб не блимав
        if (loading && skeleton) {
            skeletonTimer = window.setTimeout(() => {
                results.replaceChildren(skeleton.content.cloneNode(true));
            }, 350);
        }
    }

    function showError(message) {
        results.innerHTML = "";
        const box = document.createElement("div");
        box.className = "bw-empty";
        box.innerHTML = '<h3>Не вдалося завантажити книги</h3><p></p><button type="button" class="btn btn-primary">Спробувати ще раз</button>';
        box.querySelector("p").textContent = message;
        box.querySelector("button").addEventListener("click", () => load());
        results.appendChild(box);
    }

    async function load() {
        const query = buildQuery();
        const url = form.action.split("?")[0] + (query ? "?" + query : "");

        if (controller) controller.abort();
        controller = new AbortController();
        setLoading(true);

        try {
            const response = await fetch(url, {
                headers: { "X-Requested-With": "XMLHttpRequest" },
                credentials: "same-origin",
                signal: controller.signal
            });
            if (!response.ok) throw new Error("Сервер повернув помилку " + response.status);
            const html = await response.text();
            window.clearTimeout(skeletonTimer);
            results.innerHTML = html;
            window.history.replaceState(null, "", url);
        } catch (error) {
            if (error.name === "AbortError") return;
            window.clearTimeout(skeletonTimer);
            showError(navigator.onLine === false
                ? "Немає з'єднання з інтернетом. Перевірте мережу."
                : "Сталася помилка під час завантаження каталогу.");
        } finally {
            setLoading(false);
        }
    }

    function updateChips() {
        const current = genreInput.value;
        chips.forEach((chip) => {
            chip.classList.toggle("active", chip.dataset.bwGenre === current);
            chip.setAttribute("aria-pressed", chip.dataset.bwGenre === current ? "true" : "false");
        });
    }

    searchInput.addEventListener("input", () => {
        window.clearTimeout(debounceTimer);
        debounceTimer = window.setTimeout(load, 400);
    });

    sortSelect.addEventListener("change", load);

    form.addEventListener("submit", (e) => {
        e.preventDefault();
        window.clearTimeout(debounceTimer);
        load();
    });

    chips.forEach((chip) => {
        chip.addEventListener("click", (e) => {
            e.preventDefault();
            genreInput.value = chip.dataset.bwGenre;
            updateChips();
            load();
        });
    });

    results.addEventListener("click", (e) => {
        if (e.target.closest("[data-bw-reset-filters]")) {
            e.preventDefault();
            searchInput.value = "";
            genreInput.value = "";
            updateChips();
            load();
        }
    });

    // Після видалення книги оновлюємо лічильник і стан «порожньо»
    document.addEventListener("bw:catalog-changed", load);

    updateChips();
})(window, document);
