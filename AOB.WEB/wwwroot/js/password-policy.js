// New-password rules in the browser. Mirrors AOB.WEB.Validation.PasswordPolicyAttribute (keep the rule keys
// and patterns in sync). Registers the "passwordpolicy" jquery-validation rule so a failing password
// blocks submit, and drives the live checklist (_PasswordChecklist) and confirm-password match line.
(function () {
    var tests = {
        length: function (v, min, max) { return v.length >= min && v.length <= max; },
        upper: function (v) { return /[A-Z]/.test(v); },
        lower: function (v) { return /[a-z]/.test(v); },
        number: function (v) { return /[0-9]/.test(v); },
        special: function (v) { return /[^A-Za-z0-9\s]/.test(v); }
    };

    function meetsAll(value, min, max) {
        return Object.keys(tests).every(function (key) { return tests[key](value, min, max); });
    }

    if (window.jQuery && jQuery.validator && jQuery.validator.unobtrusive) {
        jQuery.validator.addMethod('passwordpolicy', function (value, element, params) {
            return this.optional(element) || meetsAll(value, params.min, params.max);
        });
        jQuery.validator.unobtrusive.adapters.add('passwordpolicy', ['min', 'max'], function (options) {
            options.rules.passwordpolicy = { min: +options.params.min, max: +options.params.max };
            options.messages.passwordpolicy = options.message;
        });
    }

    // Only call this from input events: calling .valid() before DOM-ready would create a validator
    // without the unobtrusive rules.
    // Server-rendered errors are never cleared by jquery-validation-unobtrusive (it only clears messages it
    // placed itself), so re-validate while typing and clear the server message once the field is valid.
    function revalidateIfFlagged(input) {
        if (!(window.jQuery && jQuery.fn.valid && input.form)) return;
        var message = input.form.querySelector('[data-valmsg-for="' + input.name + '"]');
        var flagged = input.classList.contains('input-validation-error') ||
            (message && message.classList.contains('field-validation-error'));
        if (!flagged) return;

        if (jQuery(input).valid() && message) {
            message.textContent = '';
            message.classList.remove('field-validation-error');
            message.classList.add('field-validation-valid');
        }
    }

    // Live checklist under the new-password field.
    document.querySelectorAll('[data-password-checklist]').forEach(function (list) {
        var input = document.getElementById(list.getAttribute('data-for'));
        if (!input) return;
        var min = +list.getAttribute('data-min');
        var max = +list.getAttribute('data-max');

        function update(event) {
            var value = input.value;
            var metCount = 0;
            list.querySelectorAll('[data-rule]').forEach(function (item) {
                var met = tests[item.getAttribute('data-rule')](value, min, max);
                item.classList.toggle('met', met);
                item.classList.toggle('ok', met);
                if (met) metCount++;
                item.querySelector('[data-rule-state]').textContent = met ? 'met' : 'not met';
            });
            var meter = document.getElementById(list.getAttribute('data-for') + '-meter');
            if (meter) {
                meter.querySelectorAll('i').forEach(function (seg, i) {
                    seg.classList.toggle('on', i < metCount);
                });
            }
            if (event) revalidateIfFlagged(input);
        }

        input.addEventListener('input', update);
        update();
    });

    // "Passwords match" line under the confirm field; empty until something is typed.
    document.querySelectorAll('[data-password-match]').forEach(function (line) {
        var confirm = document.getElementById(line.getAttribute('data-for'));
        var original = document.getElementById(line.getAttribute('data-compare'));
        if (!confirm || !original) return;

        function update(event) {
            if (!confirm.value) {
                line.className = 'pw-match';
                line.textContent = '';
                return;
            }
            var match = confirm.value === original.value;
            line.className = 'pw-match ' + (match ? 'is-match' : 'is-mismatch');
            line.textContent = match ? 'Passwords match' : 'Passwords do not match';
            if (event) revalidateIfFlagged(confirm);
        }

        confirm.addEventListener('input', update);
        original.addEventListener('input', update);
        update();
    });
})();
