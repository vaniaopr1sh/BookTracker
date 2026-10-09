/* ==========================================================================
   BookWings — централізована робота з JWT на клієнті
   --------------------------------------------------------------------------
   • зберігання / відновлення токена (localStorage)
   • автоматичне додавання заголовка Authorization до запитів на захищені endpoint
   • відновлення токена за дійсною cookie-сесією (/api/auth/token)
   • реакція на завершення або втрату сесії (подія "bw:session-lost")
   ========================================================================== */
(function (window, document) {
    "use strict";

    const KEYS = {
        token: "bw.token",
        expiresAt: "bw.expiresAt",
        user: "bw.user"
    };

    const ENDPOINTS = {
        login: "/api/auth/login",
        register: "/api/auth/register",
        logout: "/api/auth/logout",
        refresh: "/api/auth/token",
        me: "/api/auth/me"
    };

    // Оновлюємо токен трохи раніше, ніж він фактично спливе
    const REFRESH_BEFORE_MS = 60 * 1000;

    let expiryTimer = null;

    /* ---------- Безпечний доступ до localStorage ---------- */
    const storage = {
        get(key) {
            try { return window.localStorage.getItem(key); } catch { return null; }
        },
        set(key, value) {
            try { window.localStorage.setItem(key, value); } catch { /* приватний режим */ }
        },
        remove(key) {
            try { window.localStorage.removeItem(key); } catch { /* ignore */ }
        }
    };

    /* ---------- Помилка API з кодом статусу та деталями ---------- */
    class ApiError extends Error {
        constructor(message, status, data) {
            super(message);
            this.name = "ApiError";
            this.status = status;
            this.data = data;
        }

        /** Помилки валідації ASP.NET (ValidationProblemDetails) у вигляді { поле: [повідомлення] } */
        get fieldErrors() {
            return (this.data && this.data.errors) || {};
        }
    }

    /* ---------- Сесія ---------- */
    function getExpiresAt() {
        const value = Date.parse(storage.get(KEYS.expiresAt) || "");
        return Number.isNaN(value) ? 0 : value;
    }

    function getToken() {
        const token = storage.get(KEYS.token);
        if (!token || getExpiresAt() <= Date.now()) {
            return null;
        }
        return token;
    }

    function getUser() {
        try {
            return JSON.parse(storage.get(KEYS.user) || "null");
        } catch {
            return null;
        }
    }

    function isAuthenticated() {
        return getToken() !== null;
    }

    function isInRole(role) {
        const user = getUser();
        return !!user && Array.isArray(user.roles) && user.roles.includes(role);
    }

    function saveSession(response) {
        storage.set(KEYS.token, response.token);
        storage.set(KEYS.expiresAt, response.expiresAt);
        storage.set(KEYS.user, JSON.stringify(response.user));
        scheduleRefresh();
        document.dispatchEvent(new CustomEvent("bw:session-changed", { detail: { user: response.user } }));
    }

    function clearSession() {
        window.clearTimeout(expiryTimer);
        storage.remove(KEYS.token);
        storage.remove(KEYS.expiresAt);
        storage.remove(KEYS.user);
        document.dispatchEvent(new CustomEvent("bw:session-changed", { detail: { user: null } }));
    }

    function scheduleRefresh() {
        window.clearTimeout(expiryTimer);
        const msLeft = getExpiresAt() - Date.now() - REFRESH_BEFORE_MS;
        // setTimeout не підтримує затримки понад ~24.8 доби
        const delay = Math.min(Math.max(msLeft, 0), 2147483647);
        expiryTimer = window.setTimeout(async () => {
            const ok = await refresh();
            if (!ok) {
                await handleSessionLost("Час сесії вичерпано. Будь ласка, увійдіть знову.");
            }
        }, delay);
    }

    /* ---------- Базовий HTTP-клієнт ---------- */
    async function parseBody(response) {
        if (response.status === 204) return null;
        const type = response.headers.get("content-type") || "";
        if (type.includes("json")) {
            try { return await response.json(); } catch { return null; }
        }
        const text = await response.text();
        return text ? { message: text } : null;
    }

    function extractMessage(data, status) {
        if (data) {
            if (typeof data.message === "string") return data.message;
            if (data.errors) {
                const first = Object.values(data.errors).flat()[0];
                if (first) return first;
            }
            if (typeof data.title === "string" && status !== 400) return data.title;
        }
        switch (status) {
            case 0: return "Немає з'єднання із сервером. Перевірте інтернет.";
            case 400: return "Перевірте правильність введених даних.";
            case 401: return "Потрібна авторизація.";
            case 403: return "Недостатньо прав для виконання цієї дії.";
            case 404: return "Запитаний ресурс не знайдено.";
            default: return "Сталася помилка сервера. Спробуйте пізніше.";
        }
    }

    /**
     * fetch-обгортка: автоматично додає JWT, серіалізує JSON, обробляє помилки.
     * options.auth === false — не додавати токен і не реагувати на 401 як на втрату сесії.
     */
    async function apiFetch(url, options = {}) {
        const { auth = true, body, headers, ...rest } = options;
        const finalHeaders = new Headers(headers || {});
        finalHeaders.set("Accept", "application/json");
        finalHeaders.set("X-Requested-With", "XMLHttpRequest");

        let finalBody = body;
        if (body && !(body instanceof FormData) && typeof body === "object") {
            finalHeaders.set("Content-Type", "application/json");
            finalBody = JSON.stringify(body);
        }

        if (auth) {
            let token = getToken();
            // Токен прострочено, але cookie-сесія може бути ще дійсною — пробуємо відновити
            if (!token && document.body.dataset.authenticated === "true") {
                if (await refresh()) token = getToken();
            }
            if (token) finalHeaders.set("Authorization", `Bearer ${token}`);
        }

        let response;
        try {
            response = await fetch(url, { credentials: "same-origin", ...rest, headers: finalHeaders, body: finalBody });
        } catch {
            throw new ApiError(extractMessage(null, 0), 0, null);
        }

        const data = await parseBody(response);

        if (response.status === 401 && auth) {
            await handleSessionLost("Сесію завершено. Увійдіть, щоб продовжити.");
        }

        if (!response.ok) {
            throw new ApiError(extractMessage(data, response.status), response.status, data);
        }

        return data;
    }

    /* ---------- Операції автентифікації ---------- */
    async function login(email, password, rememberMe) {
        const data = await apiFetch(ENDPOINTS.login, {
            method: "POST",
            auth: false,
            body: { email, password, rememberMe: !!rememberMe }
        });
        saveSession(data);
        return data.user;
    }

    async function register(model) {
        const data = await apiFetch(ENDPOINTS.register, { method: "POST", auth: false, body: model });
        saveSession(data);
        return data.user;
    }

    async function logout() {
        clearSession();
        try {
            await fetch(ENDPOINTS.logout, { method: "POST", credentials: "same-origin" });
        } catch { /* навіть без мережі локальну сесію вже очищено */ }
    }

    async function refresh() {
        try {
            const data = await apiFetch(ENDPOINTS.refresh, { method: "POST", auth: false });
            saveSession(data);
            return true;
        } catch {
            return false;
        }
    }

    let sessionLostHandled = false;
    async function handleSessionLost(message) {
        if (sessionLostHandled) return;
        sessionLostHandled = true;
        const wasServerAuthenticated = document.body.dataset.authenticated === "true";
        await logout();
        document.dispatchEvent(new CustomEvent("bw:session-lost", {
            detail: { message, reload: wasServerAuthenticated }
        }));
        window.setTimeout(() => { sessionLostHandled = false; }, 1000);
    }

    /**
     * Відновлення стану під час завантаження сторінки:
     *  - сервер вважає користувача авторизованим, а токена немає → отримуємо новий за cookie;
     *  - токен є, а cookie-сесії вже немає (вихід в іншій вкладці) → очищаємо токен.
     */
    async function restore() {
        const serverAuthenticated = document.body.dataset.authenticated === "true";
        const token = getToken();

        if (serverAuthenticated && !token) {
            if (!(await refresh())) {
                await handleSessionLost("Сесію завершено. Увійдіть знову.");
            }
            return;
        }

        if (!serverAuthenticated && storage.get(KEYS.token)) {
            clearSession();
            return;
        }

        if (token) scheduleRefresh();
    }

    // Синхронізація між вкладками: вихід в одній вкладці завершує сесію в інших
    window.addEventListener("storage", (event) => {
        if (event.key === KEYS.token && !event.newValue && document.body.dataset.authenticated === "true") {
            window.location.reload();
        }
    });

    window.BookWingsAuth = {
        ApiError,
        apiFetch,
        login,
        register,
        logout,
        refresh,
        restore,
        getToken,
        getUser,
        isAuthenticated,
        isInRole,
        clearSession
    };

    document.addEventListener("DOMContentLoaded", restore);
})(window, document);
