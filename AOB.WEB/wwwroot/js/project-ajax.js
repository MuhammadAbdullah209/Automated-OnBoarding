// Project overview: actions that save without reloading the page.
// A form with data-ajax="..." is sent with fetch; the server answers with JSON and the page moves on to the page
// the server names (data-ajax="advance": move the project along its workflow).
// Document request reviews (data-info-review) ask for a note in a dialog, then post the form normally.
(function () {
    function toast(message, isError) {
        // The shared toast stack from site.js; the fallback below only runs if site.js did not load.
        if (typeof window.aobToast === 'function') {
            window.aobToast(message, isError ? 'bad' : 'ok');
            return;
        }
        var host = document.getElementById('ajax-toasts');
        if (!host) {
            host = document.createElement('div');
            host.id = 'ajax-toasts';
            host.className = 'ajax-toasts';
            host.setAttribute('aria-live', 'polite');
            document.body.appendChild(host);
        }
        var item = document.createElement('div');
        item.className = 'ajax-toast' + (isError ? ' is-error' : '');
        item.setAttribute('role', isError ? 'alert' : 'status');
        item.textContent = message;
        host.appendChild(item);
        setTimeout(function () { item.remove(); }, 4500);
    }

    var handlers = {};

    function postAjaxForm(form, kind, data) {
        var buttons = form.querySelectorAll('button');
        buttons.forEach(function (b) { b.disabled = true; });

        fetch(form.action, {
            method: 'POST',
            body: data,
            credentials: 'same-origin',
            headers: { 'X-Requested-With': 'XMLHttpRequest', 'Accept': 'application/json' }
        })
            .then(function (response) {
                return response.json().catch(function () { return { success: false, error: 'Something went wrong. Please try again.' }; });
            })
            .then(function (json) {
                if (!json.success) {
                    toast(json.error || 'Something went wrong. Please try again.', true);
                    buttons.forEach(function (b) { b.disabled = false; });
                    return;
                }
                if (json.redirect) {
                    window.location.assign(json.redirect);
                    return;
                }
                if (handlers[kind]) handlers[kind](json, form);
                if (json.message) toast(json.message, false);
            })
            .catch(function () {
                toast('Could not reach the server. Please try again.', true);
                buttons.forEach(function (b) { b.disabled = false; });
            });
    }

    // Reject needs a reason (shown to the customer); Resolve takes an optional note. Approve submits directly.
    document.addEventListener('click', function (e) {
        var btn = e.target.closest('[data-info-review]');
        if (!btn) return;
        e.preventDefault();

        var form = btn.closest('form[data-info-review-form]');
        if (!form) return;
        var decision = btn.getAttribute('data-info-review');
        var isReject = decision === 'Reject';
        var title = form.getAttribute('data-item-title') || 'this document';

        function send(note) {
            form.querySelector('[data-review-note]').value = note;
            var decisionInput = form.querySelector('[data-review-decision]');
            decisionInput.value = decision;
            decisionInput.disabled = false;
            form.querySelectorAll('button').forEach(function (b) { b.disabled = true; });
            form.submit();
        }

        if (typeof Swal === 'undefined') {
            var typed = window.prompt(isReject ? 'Reason for rejecting ' + title : 'Note for resolving ' + title + ' (optional)', '');
            if (typed === null || (isReject && !typed.trim())) return;
            send(typed.trim());
            return;
        }

        Swal.fire({
            title: isReject ? 'Reject response' : 'Resolve request',
            text: isReject
                ? 'The customer sees this reason and can resubmit on the same request.'
                : 'Close this request without approving a document.',
            icon: isReject ? 'warning' : 'question',
            input: 'textarea',
            inputLabel: isReject ? 'Reason for rejecting ' + title : 'Note (optional)',
            inputPlaceholder: isReject ? 'Say what is wrong and what to send instead…' : 'Add a note for the record…',
            inputAttributes: { maxlength: '1000', rows: '4' },
            showCancelButton: true,
            reverseButtons: true,
            confirmButtonText: isReject ? 'Reject' : 'Resolve',
            cancelButtonText: 'Cancel',
            focusCancel: true,
            customClass: {
                popup: 'request-again-modal',
                input: 'tinput',
                confirmButton: 'swal2-confirm-brand',
                cancelButton: 'swal2-cancel-brand'
            },
            inputValidator: function (value) {
                if (isReject && (!value || !value.trim())) return 'Give a reason for rejecting this response.';
            }
        }).then(function (result) {
            if (!result.isConfirmed) return;
            send((result.value || '').trim());
        });
    });

    document.addEventListener('submit', function (e) {
        var form = e.target;
        var kind = form.getAttribute && form.getAttribute('data-ajax');
        if (!kind) return;
        e.preventDefault();

        var data;
        try { data = new FormData(form, e.submitter); }
        catch (err) {
            data = new FormData(form);
            if (e.submitter && e.submitter.name) data.append(e.submitter.name, e.submitter.value);
        }

        postAjaxForm(form, kind, data);
    });
})();
