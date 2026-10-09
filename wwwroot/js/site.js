/* ==========================================================================
   BookWings — загальна логіка інтерфейсу
   • toast-сповіщення та вікно підтвердження
   • клієнтська валідація форм
   • popup Login / Registration
   • захист посилань, що потребують авторизації
   • видалення книг через захищений JWT endpoint
   ========================================================================== */
(function (window, document) {
    "use strict";

    const Auth = window.BookWingsAuth;
    const BookWings = window.BookWings = window.BookWings || {};

    /* ======================= Toast ======================= */
    BookWings.toast = function (message, type = "success", timeout = 4500) {
        const container = document.getElementById("bwToasts");
        if (!container || !message) return;

        const toast = document.createElement("div");
        toast.className = "bw-toast" + (type === "error" ? " is-error" : "");
        toast.setAttribute("role", type === "error" ? "alert" : "status");

        const text = document.createElement("span");
        text.textContent = message;
        const close = document.createElement("button");
        close.type = "button";
        close.setAttribute("aria-label", "Закрити");
        close.innerHTML = "&times;";

        toast.append(text, close);
        container.appendChild(toast);

        const hide = () => {
            toast.classList.add("is-hiding");
            toast.addEventListener("animationend", () => toast.remove(), { once: true });
        };
        close.addEventListener("click", hide);
        window.setTimeout(hide, timeout);
    };

    /* ======================= Підтвердження ======================= */
    BookWings.confirm = function ({ title = "Підтвердіть дію", text = "Ви впевнені?", okText = "Підтвердити" } = {}) {
        const el = document.getElementById("bwConfirmModal");
        if (!el || !window.bootstrap) {
            return Promise.resolve(window.confirm(text));
        }

        el.querySelector("#bwConfirmTitle").textContent = title;
        el.querySelector("[data-bw-confirm-text]").textContent = text;
        const okButton = el.querySelector("[data-bw-confirm-ok]");
        okButton.textContent = okText;

        const modal = bootstrap.Modal.getOrCreateInstance(el);
        return new Promise((resolve) => {
            let confirmed = false;
            const onOk = () => { confirmed = true; modal.hide(); };
            okButton.addEventListener("click", onOk, { once: true });
            el.addEventListener("hidden.bs.modal", () => {
                okButton.removeEventListener("click", onOk);
                resolve(confirmed);
            }, { once: true });
            modal.show();
        });
    };

    /* ======================= Кнопка у стані завантаження ======================= */
    BookWings.setLoading = function (button, loading, loadingText = "Зачекайте…") {
        if (!button) return;
        if (loading) {
            button.dataset.originalHtml = button.innerHTML;
            button.disabled = true;
            button.innerHTML = `<span class="spinner-border me-2" aria-hidden="true"></span>${loadingText}`;
        } else {
            button.disabled = false;
            if (button.dataset.originalHtml) button.innerHTML = button.dataset.originalHtml;
        }
    };

    /* ======================= Валідація форм ======================= */
    const EMAIL_RE = /^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/;

    function feedbackFor(input) {
        const group = input.closest(".mb-2, .mb-3, .mb-4") || input.parentElement;
        return group ? group.querySelector(".invalid-feedback") : null;
    }

    function validateField(input) {
        const value = input.value.trim();
        const msg = (name, fallback) => input.dataset["msg" + name] || fallback;
        let error = "";

        if (input.required && !value) {
            error = msg("Required", "Це поле обов'язкове");
        } else if (value) {
            if (input.type === "email" && !EMAIL_RE.test(value)) {
                error = msg("Email", "Некоректний email");
            } else if (input.minLength > 0 && value.length < input.minLength) {
                error = msg("Minlength", `Мінімум ${input.minLength} символів`);
            } else if (input.maxLength > 0 && value.length > input.maxLength) {
                error = msg("Maxlength", `Максимум ${input.maxLength} символів`);
            } else if (input.dataset.match) {
                const other = document.getElementById(input.dataset.match);
                if (other && other.value !== input.value) error = msg("Match", "Значення не збігаються");
            }
        }

        input.classList.toggle("is-invalid", !!error);
        input.classList.toggle("is-valid", !error && !!value);
        input.setAttribute("aria-invalid", error ? "true" : "false");
        const feedback = feedbackFor(input);
        if (feedback) feedback.textContent = error;
        return !error;
    }

    BookWings.validateForm = function (form) {
        const inputs = Array.from(form.querySelectorAll("input:not([type=checkbox]):not([type=hidden]), textarea, select"));
        const results = inputs.map(validateField);
        const firstInvalid = inputs.find((_, i) => !results[i]);
        if (firstInvalid) firstInvalid.focus();
        form.dataset.validated = "true";
        return !firstInvalid;
    };

    BookWings.showServerErrors = function (form, error) {
        const alert = form.querySelector("[data-bw-alert]");
        const general = [];
        Object.entries(error.fieldErrors || {}).forEach(([key, messages]) => {
            const name = key.replace(/^\$\./, "").toLowerCase();
            const input = Array.from(form.elements).find((el) => el.name && el.name.toLowerCase() === name);
            const text = [].concat(messages).join(" ");
            if (input) {
                input.classList.add("is-invalid");
                const feedback = feedbackFor(input);
                if (feedback) feedback.textContent = text;
            } else {
                general.push(text);
            }
        });
        if (!general.length && !Object.keys(error.fieldErrors || {}).length) general.push(error.message);
        if (alert) {
            alert.textContent = general.join(" ");
            alert.classList.toggle("d-none", general.length === 0);
        }
    };

    function resetForm(form) {
        form.reset();
        delete form.dataset.validated;
        form.querySelectorAll(".is-invalid, .is-valid").forEach((el) => el.classList.remove("is-invalid", "is-valid"));
        form.querySelectorAll(".invalid-feedback").forEach((el) => { el.textContent = ""; });
        const alert = form.querySelector("[data-bw-alert]");
        if (alert) alert.classList.add("d-none");
        const strength = form.querySelector("[data-bw-strength]");
        if (strength) strength.style.width = "0";
    }

    // Жива перевірка після першої спроби відправлення
    document.addEventListener("input", (e) => {
        const form = e.target.form;
        if (form && form.dataset.validated === "true" && form.hasAttribute("novalidate")) {
            validateField(e.target);
            const matchTarget = form.querySelector(`[data-match="${e.target.id}"]`);
            if (matchTarget && matchTarget.value) validateField(matchTarget);
        }
    });

    /* ======================= Popup Login / Registration ======================= */
    const authModalEl = document.getElementById("bwAuthModal");
    let authReturnUrl = null;

    function switchAuthTab(tab) {
        if (!authModalEl) return;
        authModalEl.querySelectorAll(".bw-auth-tabs [data-bw-tab]").forEach((btn) => {
            const active = btn.dataset.bwTab === tab;
            btn.classList.toggle("active", active);
            btn.setAttribute("aria-selected", active ? "true" : "false");
        });
        authModalEl.querySelectorAll("[data-bw-pane]").forEach((pane) => {
            pane.classList.toggle("active", pane.dataset.bwPane === tab);
        });
        authModalEl.querySelector("#bwAuthTitle").textContent =
            tab === "register" ? "Створіть обліковий запис" : "Ласкаво просимо!";
        const firstInput = authModalEl.querySelector(`[data-bw-pane="${tab}"] input`);
        if (firstInput && authModalEl.classList.contains("show")) firstInput.focus();
    }

    BookWings.openAuth = function (tab = "login", returnUrl = null) {
        if (!authModalEl || !window.bootstrap) {
            window.location.href = "/Identity/Account/" + (tab === "register" ? "Register" : "Login");
            return;
        }
        authReturnUrl = returnUrl;
        switchAuthTab(tab);
        bootstrap.Modal.getOrCreateInstance(authModalEl).show();
    };

    function afterAuthSuccess(user, message) {
        BookWings.toast(message.replace("{name}", user.displayName || ""));
        const target = authReturnUrl && authReturnUrl.startsWith("/") && !authReturnUrl.startsWith("//")
            ? authReturnUrl
            : null;
        window.setTimeout(() => {
            if (target) window.location.href = target;
            else if (/\/Home\/Login/i.test(window.location.pathname)) window.location.href = "/";
            else window.location.reload();
        }, 600);
    }

    if (authModalEl) {
        authModalEl.querySelectorAll("[data-bw-tab]").forEach((el) => {
            el.addEventListener("click", (e) => { e.preventDefault(); switchAuthTab(el.dataset.bwTab); });
        });

        authModalEl.addEventListener("shown.bs.modal", () => {
            const pane = authModalEl.querySelector("[data-bw-pane].active input");
            if (pane) pane.focus();
        });

        authModalEl.addEventListener("hidden.bs.modal", () => {
            authModalEl.querySelectorAll("form").forEach(resetForm);
        });

        const loginForm = document.getElementById("bwLoginForm");
        loginForm.addEventListener("submit", async (e) => {
            e.preventDefault();
            loginForm.querySelector("[data-bw-alert]").classList.add("d-none");
            if (!BookWings.validateForm(loginForm)) return;

            const button = loginForm.querySelector("[type=submit]");
            BookWings.setLoading(button, true, "Вхід…");
            try {
                const user = await Auth.login(
                    loginForm.email.value.trim(),
                    loginForm.password.value,
                    loginForm.rememberMe.checked);
                afterAuthSuccess(user, "Раді бачити вас знову, {name}!");
            } catch (error) {
                BookWings.showServerErrors(loginForm, error);
                BookWings.setLoading(button, false);
            }
        });

        const registerForm = document.getElementById("bwRegisterForm");
        registerForm.addEventListener("submit", async (e) => {
            e.preventDefault();
            registerForm.querySelector("[data-bw-alert]").classList.add("d-none");
            if (!BookWings.validateForm(registerForm)) return;

            const button = registerForm.querySelector("[type=submit]");
            BookWings.setLoading(button, true, "Створюємо…");
            try {
                const user = await Auth.register({
                    displayName: registerForm.displayName.value.trim(),
                    email: registerForm.email.value.trim(),
                    password: registerForm.password.value,
                    confirmPassword: registerForm.confirmPassword.value
                });
                afterAuthSuccess(user, "Вітаємо в BookWings, {name}! Обліковий запис створено.");
            } catch (error) {
                BookWings.showServerErrors(registerForm, error);
                BookWings.setLoading(button, false);
            }
        });

        // Індикатор надійності пароля
        const strengthBar = registerForm.querySelector("[data-bw-strength]");
        registerForm.password.addEventListener("input", () => {
            const v = registerForm.password.value;
            let score = 0;
            if (v.length >= 6) score++;
            if (v.length >= 10) score++;
            if (/[a-zа-яії]/.test(v) && /[A-ZА-ЯІЇ]/.test(v)) score++;
            if (/\d/.test(v)) score++;
            if (/[^A-Za-z0-9А-Яа-яІіЇїЄєҐґ]/.test(v)) score++;
            const colors = ["#9B3B2E", "#9B3B2E", "#C08A3E", "#ACAC85", "#8E8E66", "#6B7B3E"];
            strengthBar.style.width = `${(score / 5) * 100}%`;
            strengthBar.style.backgroundColor = colors[score];
        });
    }

    // Кнопки «Увійти» / «Реєстрація» у шапці та будь-де на сторінці
    document.addEventListener("click", (e) => {
        const trigger = e.target.closest("[data-bw-auth]");
        if (trigger && authModalEl) {
            e.preventDefault();
            BookWings.openAuth(trigger.dataset.bwAuth, trigger.dataset.returnUrl || null);
            return;
        }

        // Захищені маршрути: неавторизованому користувачу одразу показуємо popup входу
        const protectedLink = e.target.closest("a[data-requires-auth]");
        if (protectedLink && document.body.dataset.authenticated !== "true" && authModalEl) {
            e.preventDefault();
            BookWings.openAuth("login", protectedLink.getAttribute("href"));
            BookWings.toast("Увійдіть, щоб відкрити цей розділ", "error", 3000);
        }
    });

    // Показати / приховати пароль
    document.addEventListener("click", (e) => {
        const toggle = e.target.closest("[data-bw-toggle-password]");
        if (!toggle) return;
        const input = toggle.parentElement.querySelector("input");
        const show = input.type === "password";
        input.type = show ? "text" : "password";
        toggle.setAttribute("aria-label", show ? "Сховати пароль" : "Показати пароль");
    });

    // Вихід: окрім cookie-сесії очищаємо і JWT
    document.addEventListener("submit", (e) => {
        if (e.target.matches("form[data-bw-logout]")) {
            Auth.clearSession();
        }
    });

    // Сесію втрачено (токен прострочено / відкликано)
    document.addEventListener("bw:session-lost", (e) => {
        BookWings.toast(e.detail.message, "error", 6000);
        const here = window.location.pathname + window.location.search;
        if (e.detail.reload) {
            window.setTimeout(() => {
                window.location.href = "/Home/Login?returnUrl=" + encodeURIComponent(here);
            }, 1200);
        } else {
            BookWings.openAuth("login", here);
        }
    });

    /* ======================= Підтвердження звичайних форм ======================= */
    document.addEventListener("submit", async (e) => {
        const form = e.target;
        if (!form.matches("form[data-bw-confirm]") || form.dataset.bwConfirmed === "true") return;
        e.preventDefault();
        const ok = await BookWings.confirm({ title: "Підтвердіть дію", text: form.dataset.bwConfirm, okText: "Так" });
        if (ok) {
            form.dataset.bwConfirmed = "true";
            form.submit();
        }
    });

    /* ======================= Видалення книги (JWT + підтвердження) ======================= */
    document.addEventListener("click", async (e) => {
        const button = e.target.closest("[data-bw-delete-book]");
        if (!button) return;
        e.preventDefault();

        const id = button.dataset.id;
        const title = button.dataset.title || "цю книгу";
        const confirmed = await BookWings.confirm({
            title: "Видалити книгу?",
            text: `Книгу «${title}» буде остаточно видалено з каталогу разом з оцінками та записами читачів. Цю дію не можна скасувати.`,
            okText: "Так, видалити"
        });
        if (!confirmed) return;

        BookWings.setLoading(button, true, "");
        try {
            const result = await Auth.apiFetch(`/api/books/${encodeURIComponent(id)}`, { method: "DELETE" });
            BookWings.toast(result && result.message ? result.message : "Книгу видалено");

            if (button.dataset.redirect) {
                window.setTimeout(() => { window.location.href = button.dataset.redirect; }, 700);
                return;
            }

            const card = button.closest("[data-bw-book]");
            if (card) {
                card.querySelector(".bw-book-card").classList.add("is-removing");
                window.setTimeout(() => {
                    card.remove();
                    document.dispatchEvent(new CustomEvent("bw:catalog-changed"));
                }, 350);
            }
        } catch (error) {
            BookWings.setLoading(button, false);
            if (error.status === 404) {
                // Книгу вже видалили (наприклад, в іншій вкладці) — синхронізуємо інтерфейс
                const card = button.closest("[data-bw-book]");
                if (card) card.remove();
                document.dispatchEvent(new CustomEvent("bw:catalog-changed"));
            }
            if (error.status !== 401) BookWings.toast(error.message, "error");
        }
    });

    /* ======================= Дрібниці інтерфейсу ======================= */
    // Зламане посилання на обкладинку → фірмова заглушка (працює і для карток, підвантажених через AJAX)
    function showCoverFallback(img) {
        const holder = img.parentElement && img.parentElement.querySelector(".bw-cover-placeholder");
        if (holder) holder.hidden = false;
        img.remove();
    }
    document.addEventListener("error", (e) => {
        if (e.target instanceof HTMLImageElement && e.target.matches("img[data-bw-cover]")) {
            showCoverFallback(e.target);
        }
    }, true);

    document.addEventListener("DOMContentLoaded", () => {
        // Повідомлення з TempData (після редиректу)
        document.querySelectorAll("[data-bw-toast]").forEach((el) => BookWings.toast(el.dataset.bwToast));

        // Автоматичне відкриття popup (переадресація з захищеного маршруту)
        if (document.body.dataset.openLogin === "true") {
            BookWings.openAuth("login", document.body.dataset.returnUrl || null);
        }

        // Тінь шапки під час прокручування
        const header = document.getElementById("bwHeader");
        if (header) {
            const onScroll = () => header.classList.toggle("is-scrolled", window.scrollY > 8);
            window.addEventListener("scroll", onScroll, { passive: true });
            onScroll();
        }

        // Плавна поява секцій
        const revealItems = document.querySelectorAll(".bw-reveal");
        if ("IntersectionObserver" in window) {
            const observer = new IntersectionObserver((entries) => {
                entries.forEach((entry) => {
                    if (entry.isIntersecting) {
                        entry.target.classList.add("is-visible");
                        observer.unobserve(entry.target);
                    }
                });
            }, { threshold: 0.12 });
            revealItems.forEach((el) => observer.observe(el));
        } else {
            revealItems.forEach((el) => el.classList.add("is-visible"));
        }

        // Обкладинки, що не завантажилися ще до підключення скрипта
        document.querySelectorAll("img[data-bw-cover]").forEach((img) => {
            if (img.complete && img.naturalWidth === 0) showCoverFallback(img);
        });
    });
})(window, document);
