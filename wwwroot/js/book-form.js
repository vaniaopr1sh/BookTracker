/* ==========================================================================
   BookWings — форма створення / редагування книги
   • живий попередній перегляд обкладинки
   • лічильники символів
   • підказки жанрів з Backend (/api/books/genres)
   • захист від повторного надсилання
   ========================================================================== */
(function (window, document) {
    "use strict";

    const form = document.getElementById("bwBookForm");
    if (!form) return;

    const titleInput = form.querySelector("#Title");
    const authorInput = form.querySelector("#Author");
    const coverInput = form.querySelector("#CoverImageUrl");
    const preview = document.getElementById("bwCoverPreview");
    const previewImg = preview.querySelector("img.bw-details-img");
    const placeholder = preview.querySelector(".bw-cover-placeholder");
    const previewTitle = preview.querySelector("[data-bw-preview-title]");
    const previewAuthor = preview.querySelector("[data-bw-preview-author]");

    /* ---------- Попередній перегляд ---------- */
    function syncText() {
        previewTitle.textContent = titleInput.value.trim() || "Назва книги";
        previewAuthor.textContent = authorInput.value.trim() || "Автор";
    }

    function showPlaceholder() {
        previewImg.hidden = true;
        placeholder.hidden = false;
    }

    let coverTimer = null;
    function syncCover() {
        const url = coverInput.value.trim();
        if (!/^https?:\/\/\S+$/i.test(url)) {
            showPlaceholder();
            return;
        }
        previewImg.onload = () => { previewImg.hidden = false; placeholder.hidden = true; };
        previewImg.onerror = showPlaceholder;
        previewImg.src = url;
    }

    titleInput.addEventListener("input", syncText);
    authorInput.addEventListener("input", syncText);
    coverInput.addEventListener("input", () => {
        window.clearTimeout(coverTimer);
        coverTimer = window.setTimeout(syncCover, 400);
    });
    syncText();
    syncCover();

    /* ---------- Лічильники символів ---------- */
    form.querySelectorAll("[data-bw-counter]").forEach((input) => {
        const max = parseInt(input.getAttribute("maxlength") || input.dataset.valLengthMax || "0", 10);
        if (!max) return;
        const counter = document.createElement("div");
        counter.className = "bw-char-counter";
        input.insertAdjacentElement("afterend", counter);
        const update = () => {
            const length = input.value.length;
            counter.textContent = `${length} / ${max}`;
            counter.classList.toggle("is-over", length > max);
        };
        input.addEventListener("input", update);
        update();
    });

    /* ---------- Підказки жанрів ---------- */
    const datalist = document.getElementById("bwGenresList");
    if (datalist && window.BookWingsAuth) {
        window.BookWingsAuth.apiFetch("/api/books/genres", { auth: false })
            .then((genres) => {
                (genres || []).forEach((genre) => {
                    const option = document.createElement("option");
                    option.value = genre;
                    datalist.appendChild(option);
                });
            })
            .catch(() => { /* підказки необов'язкові */ });
    }

    /* ---------- Надсилання ---------- */
    let dirty = false;
    form.addEventListener("input", () => { dirty = true; });

    form.addEventListener("submit", (e) => {
        // jquery.validate.unobtrusive перевіряє форму; якщо є помилки — не блокуємо кнопку
        if (window.jQuery && window.jQuery(form).valid && !window.jQuery(form).valid()) {
            const firstError = form.querySelector(".input-validation-error");
            if (firstError) firstError.focus();
            return;
        }
        const button = form.querySelector("[type=submit]");
        if (button.disabled) {
            e.preventDefault();
            return;
        }
        dirty = false;
        window.BookWings.setLoading(button, true, button.dataset.loadingText || "Зберігаємо…");
    });

    // Попередження про незбережені зміни
    window.addEventListener("beforeunload", (e) => {
        if (dirty) {
            e.preventDefault();
            e.returnValue = "";
        }
    });
})(window, document);
