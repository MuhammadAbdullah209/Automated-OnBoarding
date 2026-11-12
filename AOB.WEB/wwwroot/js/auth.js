// Auth pages: show a loading state on the submit button once a form passes client-side validation.
(function () {
    document.querySelectorAll('form[data-auth-form]').forEach(function (form) {
        form.addEventListener('submit', function () {
            // jquery-validation blocks invalid submits itself; only flag valid ones as loading.
            if (window.jQuery && typeof jQuery(form).valid === 'function' && !jQuery(form).valid()) {
                return;
            }
            var button = form.querySelector('button[type="submit"]');
            if (button) {
                button.disabled = true;
                button.classList.add('is-loading');
                button.setAttribute('aria-busy', 'true');
            }
        });
    });

    // Restore buttons when the page comes back from the back/forward cache.
    window.addEventListener('pageshow', function (e) {
        if (!e.persisted) return;
        document.querySelectorAll('form[data-auth-form] button.is-loading').forEach(function (button) {
            button.disabled = false;
            button.classList.remove('is-loading');
            button.removeAttribute('aria-busy');
        });
    });

    // Show/hide password. The icon follows the state: an eye while the password is hidden (click to show),
    // an eye with a line through it while it is visible (click to hide).
    var eyeIcon = '<path d="M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7-10-7-10-7z" /><circle cx="12" cy="12" r="3" />';
    var eyeOffIcon = '<path d="M9.9 4.24A9.1 9.1 0 0 1 12 4c6.5 0 10 7 10 7a18.5 18.5 0 0 1-2.16 3.19" />'
        + '<path d="M6.61 6.61A18.4 18.4 0 0 0 2 12s3.5 7 10 7a9.7 9.7 0 0 0 5.39-1.61" />'
        + '<path d="M14.12 14.12a3 3 0 1 1-4.24-4.24" /><path d="M2 2l20 20" />';

    document.querySelectorAll('[data-password-toggle]').forEach(function (btn) {
        var id = btn.getAttribute('data-target');
        var input = id ? document.getElementById(id) : null;
        if (!input) return;

        function sync() {
            var visible = input.type === 'text';
            var svg = btn.querySelector('svg');
            if (svg) svg.innerHTML = visible ? eyeOffIcon : eyeIcon;
            btn.setAttribute('aria-label', visible ? 'Hide password' : 'Show password');
            btn.setAttribute('aria-pressed', visible ? 'true' : 'false');
        }

        btn.addEventListener('click', function () {
            input.type = input.type === 'password' ? 'text' : 'password';
            sync();
        });
        sync();
    });
})();
