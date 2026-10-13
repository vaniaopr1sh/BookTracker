/* ==========================================================================
   BookWings — адміністративна панель
   Зміна ролі користувача через захищений endpoint POST /api/admin/users/{id}/role.
   Запит іде з JWT адміністратора (BookWingsAuth.apiFetch).
   ========================================================================== */
(function (window, document) {
    "use strict";

    const Auth = window.BookWingsAuth;
    const BookWings = window.BookWings;
    if (!Auth || !BookWings) return;

    function renderRoles(cell, roles) {
        cell.replaceChildren();
        roles.forEach((role) => {
            const badge = document.createElement("span");
            badge.className = "bw-role-badge" + (role === "Адміністратор" ? "" : " bw-role-badge--user");
            badge.textContent = role;
            cell.append(badge, document.createTextNode(" "));
        });
    }

    document.addEventListener("click", async (e) => {
        const button = e.target.closest("[data-bw-role-toggle]");
        if (!button) return;

        const makeAdmin = button.dataset.makeAdmin === "true";
        const name = button.dataset.name || "користувач";
        const confirmed = await BookWings.confirm({
            title: makeAdmin ? "Надати права адміністратора?" : "Забрати права адміністратора?",
            text: makeAdmin
                ? `${name} зможе додавати, редагувати й видаляти книги та керувати користувачами.`
                : `${name} втратить доступ до адміністративної панелі.`,
            okText: makeAdmin ? "Так, надати" : "Так, забрати"
        });
        if (!confirmed) return;

        BookWings.setLoading(button, true, "");
        try {
            const result = await Auth.apiFetch(`/api/admin/users/${encodeURIComponent(button.dataset.userId)}/role`, {
                method: "POST",
                body: { isAdmin: makeAdmin }
            });

            const row = button.closest("[data-bw-user]");
            const rolesCell = row && row.querySelector("[data-bw-user-roles]");
            if (rolesCell) renderRoles(rolesCell, result.roles || []);

            BookWings.setLoading(button, false);
            button.dataset.makeAdmin = makeAdmin ? "false" : "true";
            button.textContent = makeAdmin ? "Забрати права адміна" : "Зробити адміном";
            button.classList.toggle("btn-outline-danger", makeAdmin);
            button.classList.toggle("btn-outline-bw", !makeAdmin);
            BookWings.toast(result.message || "Роль змінено");
        } catch (error) {
            BookWings.setLoading(button, false);
            if (error.status !== 401) BookWings.toast(error.message, "error");
        }
    });
})(window, document);
