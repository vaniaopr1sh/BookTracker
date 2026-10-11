/* ==========================================================================
   BookWings — єдина клієнтська валідація
   • українські повідомлення jQuery Validation
   • дробові числа з комою
   • фокус на першому полі з помилкою та захист від повторного надсилання
   Підключається через _ValidationScriptsPartial після jquery.validate.
   ========================================================================== */
(function ($) {
    "use strict";
    if (!$ || !$.validator) return;

    $.extend($.validator.messages, {
        required: "Це поле обов'язкове",
        remote: "Виправте це поле",
        email: "Вкажіть коректний email",
        url: "Вкажіть коректне посилання (http:// або https://)",
        date: "Вкажіть коректну дату",
        dateISO: "Вкажіть дату у форматі РРРР-ММ-ДД",
        number: "Вкажіть число",
        digits: "Можна вводити лише цифри",
        equalTo: "Значення не збігаються",
        maxlength: $.validator.format("Не більше {0} символів"),
        minlength: $.validator.format("Не менше {0} символів"),
        rangelength: $.validator.format("Від {0} до {1} символів"),
        range: $.validator.format("Значення має бути від {0} до {1}"),
        max: $.validator.format("Значення не може перевищувати {0}"),
        min: $.validator.format("Значення не може бути меншим за {0}"),
        step: $.validator.format("Значення має бути кратним {0}")
    });

    // Дозволяємо кому як десятковий роздільник (українська розкладка)
    const baseNumber = $.validator.methods.number;
    $.validator.methods.number = function (value, element) {
        return baseNumber.call(this, value.replace(",", "."), element);
    };

    // Обрізаємо пробіли на початку й у кінці перед перевіркою
    $(document).on("blur", "input[type=text], input[type=email], input:not([type])", function () {
        if (this.value !== this.value.trim()) this.value = this.value.trim();
    });

    $(document).on("submit", "form", function (e) {
        const $form = $(this);
        if (!$form.data("validator")) return; // форма без unobtrusive-валідації

        if (!$form.valid()) {
            const first = $form.find(".input-validation-error").first();
            if (first.length) first.trigger("focus");
            return;
        }

        // Блокуємо кнопку, щоб дані не надіслалися двічі
        const button = this.querySelector("[type=submit][data-loading-text]");
        if (button && this.id !== "bwBookForm" && window.BookWings) {
            if (button.disabled) {
                e.preventDefault();
                return;
            }
            window.BookWings.setLoading(button, true, button.dataset.loadingText);
        }
    });
})(window.jQuery);
