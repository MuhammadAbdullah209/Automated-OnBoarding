// App shell: mobile nav sheet and user menu.
(function () {
    var app = document.getElementById('app');
    if (!app) return;

    var toggle = document.querySelector('[data-nav-toggle]');
    var scrim = document.querySelector('.nav-scrim');

    function setNavOpen(open) {
        app.classList.toggle('nav-open', open);
        if (toggle) toggle.setAttribute('aria-expanded', open ? 'true' : 'false');
    }

    if (toggle) {
        toggle.addEventListener('click', function () { setNavOpen(!app.classList.contains('nav-open')); });
    }
    document.querySelectorAll('[data-nav-close]').forEach(function (el) {
        el.addEventListener('click', function () { setNavOpen(false); });
    });

    var userBtn = document.querySelector('[data-user-menu]');
    var userMenu = document.getElementById('user-menu');

    function setMenuOpen(open) {
        if (!userMenu || !userBtn) return;
        userMenu.hidden = !open;
        userBtn.setAttribute('aria-expanded', open ? 'true' : 'false');
    }

    if (userBtn && userMenu) {
        userMenu.hidden = true;
        userBtn.addEventListener('click', function (e) {
            e.stopPropagation();
            setMenuOpen(userMenu.hidden);
        });
        document.addEventListener('click', function (e) {
            if (!userMenu.contains(e.target) && e.target !== userBtn && !userBtn.contains(e.target)) {
                setMenuOpen(false);
            }
        });
    }

    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') {
            setNavOpen(false);
            setMenuOpen(false);
        }
    });
})();
// Multi-select dropdowns (rendered by Html.MultiSelect / Html.EnumMultiSelect).
// The tick boxes inside the dropdown are ordinary checkboxes, so the form posts exactly as before;
// this only keeps the button text ("N selected") and the removable chips in step with them.
(function () {
    function init(root) {
        var button = root.querySelector('[data-multi-select-label]');
        var chips = root.querySelector('[data-multi-select-chips]');
        var placeholder = root.getAttribute('data-placeholder') || 'Select…';
        // Disabled boxes are listed for information only (e.g. "Coming soon"); they can't be ticked.
        var boxes = Array.prototype.slice.call(root.querySelectorAll('input[type="checkbox"]:not(:disabled)'));

        function textOf(box) {
            var label = root.querySelector('label[for="' + box.id + '"]');
            return label ? (label.getAttribute('data-text') || label.textContent.trim()) : box.value;
        }

        function refresh() {
            var ticked = boxes.filter(function (box) { return box.checked; });
            button.textContent = ticked.length === 0 ? placeholder : ticked.length + ' selected';
            button.classList.toggle('is-empty', ticked.length === 0);

            chips.textContent = '';
            ticked.forEach(function (box) {
                var chip = document.createElement('span');
                chip.className = 'ms-chip';
                chip.appendChild(document.createTextNode(textOf(box) + ' '));

                var remove = document.createElement('button');
                remove.type = 'button';
                remove.textContent = '×';
                remove.setAttribute('aria-label', 'Remove ' + textOf(box));
                remove.addEventListener('click', function () {
                    box.checked = false;
                    // Let other scripts on the page (e.g. "show the redirect URL box") react as if it was unticked by hand.
                    box.dispatchEvent(new Event('change', { bubbles: true }));
                });

                chip.appendChild(remove);
                chips.appendChild(chip);
            });
        }

        boxes.forEach(function (box) { box.addEventListener('change', refresh); });
        refresh();

        // Open / close the list. This is self-contained (no Bootstrap script needed): the list is hidden
        // until the button is clicked, stays open while ticking several options, and closes on a click
        // outside it or on Escape. The "show" class is toggled as well so it also works where Bootstrap's
        // stylesheet is present.
        var menu = root.querySelector('.ms-menu');
        if (menu) {
            function setOpen(open) {
                menu.hidden = !open;
                menu.classList.toggle('show', open);
                button.setAttribute('aria-expanded', open ? 'true' : 'false');
            }

            setOpen(false);
            button.addEventListener('click', function () { setOpen(menu.hidden); });
            document.addEventListener('click', function (e) { if (!root.contains(e.target)) setOpen(false); });
            document.addEventListener('keydown', function (e) { if (e.key === 'Escape') setOpen(false); });
        }
    }

    document.querySelectorAll('[data-multi-select]').forEach(init);
})();

// SweetAlert2 confirmations for forms with data-confirm (replaces window.confirm).
(function () {
    if (typeof Swal === 'undefined') return;

    document.querySelectorAll('form[data-confirm]').forEach(function (form) {
        form.addEventListener('submit', function (e) {
            if (form.dataset.confirmBypass === '1') {
                delete form.dataset.confirmBypass;
                return;
            }

            e.preventDefault();
            e.stopPropagation();

            var text = form.getAttribute('data-confirm') || 'Are you sure?';
            var title = form.getAttribute('data-confirm-title') || 'Please confirm';
            var confirmText = form.getAttribute('data-confirm-ok') || 'Yes, continue';
            var cancelText = form.getAttribute('data-confirm-cancel') || 'Cancel';
            var icon = form.getAttribute('data-confirm-icon') || 'warning';
            // data-confirm-tone="danger" gives the destructive (red) action, as in the prototype's delete dialogs.
            var danger = form.getAttribute('data-confirm-tone') === 'danger';
            // Named icons drawn in the prototype's blue icon tile (e.g. data-confirm-icon="logout" for Sign out).
            var namedIcons = {
                logout: '<svg viewBox="0 0 24 24" width="24" height="24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"><path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"/><path d="m16 17 5-5-5-5"/><path d="M21 12H9"/></svg>'
            };
            var iconHtml = namedIcons[icon];

            // A confirmation started from the user menu (Sign out) should not leave the menu open behind it.
            document.querySelectorAll('.menu:not([hidden])').forEach(function (menu) { menu.hidden = true; });
            document.querySelectorAll('[data-user-menu][aria-expanded="true"]').forEach(function (btn) {
                btn.setAttribute('aria-expanded', 'false');
            });

            Swal.fire({
                title: title,
                text: text,
                icon: iconHtml ? 'info' : icon,
                iconHtml: iconHtml,
                showCancelButton: true,
                reverseButtons: true,
                confirmButtonText: confirmText,
                cancelButtonText: cancelText,
                focusCancel: true,
                customClass: {
                    confirmButton: danger ? 'swal2-confirm-danger' : 'swal2-confirm-brand',
                    cancelButton: 'swal2-cancel-brand'
                }
            }).then(function (result) {
                if (result.isConfirmed) {
                    form.dataset.confirmBypass = '1';
                    if (typeof form.requestSubmit === 'function') {
                        form.requestSubmit();
                    } else {
                        form.submit();
                    }
                }
            });
        });
    });
})();

// Dashboard: dismiss incomplete-profile callout for this browser session.
(function () {
    var storageKey = 'aob_profile_callout_dismissed';
    var callout = document.querySelector('[data-profile-callout]');
    if (!callout) return;
    if (sessionStorage.getItem(storageKey) === '1') {
        callout.hidden = true;
        return;
    }
    var dismiss = callout.querySelector('[data-profile-callout-dismiss]');
    if (!dismiss) return;
    dismiss.addEventListener('click', function () {
        sessionStorage.setItem(storageKey, '1');
        callout.hidden = true;
    });
})();

// Project details: expand review rows (details hidden until chevron is clicked).
(function () {
    document.querySelectorAll('[data-review-toggle]').forEach(function (btn) {
        btn.addEventListener('click', function () {
            var panel = document.getElementById(btn.getAttribute('aria-controls'));
            if (!panel) return;
            var open = panel.hidden;
            panel.hidden = !open;
            btn.setAttribute('aria-expanded', open ? 'true' : 'false');
            btn.classList.toggle('is-open', open);
            var row = btn.closest('[data-review-row]');
            if (row) row.classList.toggle('is-open', open);
        });
    });
})();

// Organisation profile: sections open for editing in place (no page reload), save in the background and
// refresh the page content, TOC highlight and in-app scrolling. Without script the Edit / Cancel links reload as before.
(function () {
    if (!document.querySelector('.org-grid')) return;

    var scrollRoot = document.querySelector('.app-scroll');
    var margin = 24;
    var smoothScroll = !window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    var observer = null;
    var formSnapshots = new WeakMap();
    var viewSnapshots = new WeakMap();

    function orgGrid() { return document.querySelector('.org-grid'); }

    function scrollToEl(el, smooth) {
        if (!el) return;
        var behavior = smooth && smoothScroll ? 'smooth' : 'auto';
        if (scrollRoot) {
            var scRect = scrollRoot.getBoundingClientRect();
            var elRect = el.getBoundingClientRect();
            var top = scrollRoot.scrollTop + (elRect.top - scRect.top) - margin;
            scrollRoot.scrollTo({ top: Math.max(0, top), behavior: behavior });
            return;
        }
        var y = el.getBoundingClientRect().top + window.scrollY - margin;
        window.scrollTo({ top: Math.max(0, y), behavior: behavior });
    }

    // Scroll only when the element is not already comfortably in view.
    function revealEl(el) {
        if (!el) return;
        var view = scrollRoot ? scrollRoot.getBoundingClientRect() : { top: 0, bottom: window.innerHeight };
        var r = el.getBoundingClientRect();
        if (r.top < view.top + margin || r.top > view.bottom - 160) scrollToEl(el, true);
    }

    function setActiveToc(sectionId) {
        document.querySelectorAll('.org-grid a.toc-link').forEach(function (link) {
            var on = link.getAttribute('data-toc-for') === sectionId;
            link.classList.toggle('is-active', on);
            if (on) link.setAttribute('aria-current', 'location');
            else link.removeAttribute('aria-current');
        });
    }

    function focusField(section, preferError) {
        var field = preferError ? section.querySelector('.input-validation-error') : null;
        field = field || section.querySelector('[data-org-form] input:not([type="hidden"]), [data-org-form] select, [data-org-form] textarea');
        if (!field) return;
        try { field.focus({ preventScroll: true }); } catch (_) { field.focus(); }
    }

    function sectionParts(section) {
        return {
            edit: section.querySelector('[data-org-edit]'),
            view: section.querySelector('[data-org-view]'),
            formWrap: section.querySelector('[data-org-form]'),
            form: section.querySelector('form[data-org-profile-form]'),
            error: section.querySelector('[data-org-error]')
        };
    }

    function captureFormState(form) {
        var state = {};
        Array.prototype.forEach.call(form.elements, function (el) {
            if (!el.name || el.disabled || el.type === 'file') return;
            if (el.type === 'checkbox') {
                state[el.name] = { t: 'c', v: el.checked };
            } else if (el.type === 'radio') {
                if (el.checked) state[el.name] = { t: 'r', v: el.value };
            } else {
                state[el.name] = { t: 'v', v: el.value };
            }
        });
        return state;
    }

    function applyFormState(form, state) {
        if (!state) {
            form.reset();
            return;
        }
        Array.prototype.forEach.call(form.elements, function (el) {
            if (!el.name || el.disabled || el.type === 'file') return;
            var s = state[el.name];
            if (!s) return;
            if (s.t === 'c' && el.type === 'checkbox') {
                el.checked = s.v;
            } else if (s.t === 'r' && el.type === 'radio') {
                el.checked = el.value === s.v;
            } else if (s.t === 'v' && el.type !== 'checkbox' && el.type !== 'radio') {
                el.value = s.v;
            }
        });
    }

    function syncFormDefaults(form) {
        Array.prototype.forEach.call(form.elements, function (el) {
            if (el.disabled || el.type === 'file') return;
            if (el.type === 'checkbox' || el.type === 'radio') {
                el.defaultChecked = el.checked;
            } else if (el.tagName === 'SELECT' || el.tagName === 'TEXTAREA' || el.type === 'hidden' || el.type === 'text'
                || el.type === 'url' || el.type === 'email' || el.type === 'tel' || el.type === 'search' || el.type === 'number') {
                el.defaultValue = el.value;
            }
        });
    }

    function snapshotSectionForm(section) {
        var parts = sectionParts(section);
        if (parts.view) viewSnapshots.set(section, parts.view.innerHTML);
        if (!parts.form) return;
        var state = captureFormState(parts.form);
        formSnapshots.set(section, state);
        syncFormDefaults(parts.form);
    }

    function restoreSectionView(section) {
        var view = sectionParts(section).view;
        var html = viewSnapshots.get(section);
        if (view && html != null) view.innerHTML = html;
    }

    function attachOrgFormValidator(form) {
        if (!window.jQuery || !jQuery.validator || !jQuery.validator.unobtrusive) return null;
        var $form = jQuery(form);
        if (!$form.data('validator')) {
            jQuery.validator.unobtrusive.parse(form);
            var validator = $form.data('validator');
            if (validator) {
                validator.settings.onfocusout = function (element) {
                    if (element.name || element.id) this.element(element);
                };
                validator.settings.onkeyup = function (element, event) {
                    if (event.which === 9 && this.elementValue(element) === '') return;
                    this.element(element);
                };
            }
        }
        return $form.data('validator') || null;
    }

    function orgFormIsValid(form) {
        var validator = attachOrgFormValidator(form);
        if (!validator) return true;
        return jQuery(form).valid();
    }

    function clearOrgFormValidation(form) {
        if (window.jQuery && jQuery(form).data('validator')) {
            jQuery(form).data('validator').resetForm();
            return;
        }
        form.querySelectorAll('.input-validation-error').forEach(function (el) { el.classList.remove('input-validation-error'); });
        form.querySelectorAll('.field-validation-valid.field-validation-error').forEach(function (el) {
            el.classList.remove('field-validation-error');
            el.classList.add('field-validation-valid');
        });
        form.querySelectorAll('[data-valmsg-for]').forEach(function (el) {
            el.textContent = '';
            el.classList.remove('field-validation-error');
            el.classList.add('field-validation-valid');
        });
        form.querySelectorAll('.is-invalid').forEach(function (el) { el.classList.remove('is-invalid'); });
    }

    // Restores the last saved values (snapshot taken when edit opened) and clears validation UI.
    function resetForm(section) {
        var parts = sectionParts(section);
        if (!parts.form) return;
        var snap = formSnapshots.get(section);
        if (snap) applyFormState(parts.form, snap);
        else parts.form.reset();
        restoreSectionView(section);
        clearOrgFormValidation(parts.form);
        if (parts.error) { parts.error.hidden = true; parts.error.textContent = ''; }
    }

    function closeSection(section, restoreFocus) {
        if (!section || !section.classList.contains('editing')) return;
        var parts = sectionParts(section);
        resetForm(section);
        section.classList.remove('editing');
        if (parts.formWrap) parts.formWrap.hidden = true;
        if (parts.view) parts.view.hidden = false;
        if (parts.edit) {
            parts.edit.hidden = false;
            parts.edit.setAttribute('aria-expanded', 'false');
            if (restoreFocus) parts.edit.focus({ preventScroll: true });
        }
    }

    function openSection(section) {
        document.querySelectorAll('.org-grid .osec.editing').forEach(function (other) {
            if (other !== section) closeSection(other, false);
        });
        snapshotSectionForm(section);
        var parts = sectionParts(section);
        section.classList.add('editing');
        if (parts.view) parts.view.hidden = true;
        if (parts.formWrap) parts.formWrap.hidden = false;
        if (parts.edit) {
            parts.edit.hidden = true;
            parts.edit.setAttribute('aria-expanded', 'true');
        }
        if (parts.form) attachOrgFormValidator(parts.form);
        history.replaceState(null, '', location.pathname + '#' + section.id);
        setActiveToc(section.id);
        revealEl(section);
        requestAnimationFrame(function () { focusField(section, false); });
    }

    function setBusy(form, busy) {
        var btn = form.querySelector('[data-org-save]');
        if (!btn) return;
        btn.classList.toggle('is-loading', busy);
        btn.disabled = busy;
        btn.setAttribute('aria-busy', busy ? 'true' : 'false');
    }

    function showSaveError(section, message) {
        var parts = sectionParts(section);
        if (parts.error) {
            parts.error.textContent = message;
            parts.error.hidden = false;
        }
    }

    // Shows the fetched page's messages in the live toast stack (which closes them by itself), then drops the
    // server-rendered copies: they come inside the swapped-in content and would otherwise stay on screen.
    function showToastsFrom(doc) {
        if (typeof window.aobToast === 'function') {
            doc.querySelectorAll('[data-toast]').forEach(function (t) {
                var title = t.querySelector('.toast-t');
                var text = t.querySelector('.toast-d');
                window.aobToast(text ? text.textContent.trim() : '', t.getAttribute('data-kind') || 'ok', title ? title.textContent.trim() : null);
            });
        }
        doc.querySelectorAll('[data-toast-host]').forEach(function (h) { h.remove(); });
        document.querySelectorAll('[data-toast-host]').forEach(function (h) { h.remove(); });
    }

    function observeSections() {
        if (observer) observer.disconnect();
        if (!scrollRoot || !('IntersectionObserver' in window)) return;
        observer = new IntersectionObserver(function (entries) {
            if (document.querySelector('.org-grid .osec.editing')) return;
            var visible = entries.filter(function (en) { return en.isIntersecting; })
                .sort(function (a, b) { return b.intersectionRatio - a.intersectionRatio; });
            if (visible[0]) setActiveToc(visible[0].target.id);
        }, { root: scrollRoot, rootMargin: '-20% 0px -55% 0px', threshold: [0, 0.25, 0.5] });
        document.querySelectorAll('.org-grid .osec[id^="os-"]').forEach(function (sec) { observer.observe(sec); });
    }

    function save(form) {
        var section = form.closest('.osec');
        var sectionId = section.id;
        var parts = sectionParts(section);
        if (parts.error) parts.error.hidden = true;
        if (!orgFormIsValid(form)) {
            focusField(section, true);
            return;
        }
        setBusy(form, true);

        fetch(form.action, {
            method: 'POST',
            body: new FormData(form),
            credentials: 'same-origin'
        }).then(function (res) {
            if (!res.ok) throw new Error('HTTP ' + res.status);
            return res.text();
        }).then(function (html) {
            var doc = new DOMParser().parseFromString(html, 'text/html');
            var newWrap = doc.querySelector('.org-profile-wrap');
            if (!newWrap) {
                // Not the profile page (e.g. the session ended): post the normal way.
                form.dataset.orgNative = '1';
                form.submit();
                return;
            }

            // Read the messages first: swapping the new content in moves its nodes out of the fetched page.
            showToastsFrom(doc);

            var failed = doc.querySelector('.osec.editing');
            if (failed && failed.id === sectionId) {
                // The server kept the section open with its messages: show them in place.
                section.replaceWith(failed);
                var failedForm = failed.querySelector('form[data-org-profile-form]');
                if (failedForm) attachOrgFormValidator(failedForm);
                setBusy(failed.querySelector('form'), false);
                setActiveToc(sectionId);
                revealEl(failed);
                focusField(failed, true);
                return;
            }

            // Saved: refresh the band (completion counts) and the profile with what the server now has.
            var band = document.querySelector('.org-band');
            var newBand = doc.querySelector('.org-band');
            if (band && newBand) band.replaceWith(newBand);
            document.querySelector('.org-profile-wrap').replaceWith(newWrap);
            observeSections();
            if (window.aobOrgProfileAttachAllForms) window.aobOrgProfileAttachAllForms();
            var saved = document.getElementById(sectionId);
            setActiveToc(sectionId);
            history.replaceState(null, '', location.pathname + '#' + sectionId);
            if (saved) {
                revealEl(saved);
                var edit = saved.querySelector('[data-org-edit]');
                if (edit) edit.focus({ preventScroll: true });
            }
        }).catch(function () {
            setBusy(form, false);
            showSaveError(section, 'Your changes weren’t saved. Check your connection and try again.');
        });
    }

    document.addEventListener('click', function (e) {
        var edit = e.target.closest('.org-grid [data-org-edit]');
        if (edit) {
            e.preventDefault();
            openSection(edit.closest('.osec'));
            return;
        }
        var cancel = e.target.closest('.org-grid [data-org-cancel]');
        if (cancel) {
            e.preventDefault();
            closeSection(cancel.closest('.osec'), true);
            return;
        }
        var toc = e.target.closest('.org-grid a.toc-link[href^="#os-"]');
        if (toc) {
            var target = document.getElementById(toc.getAttribute('href').slice(1));
            if (!target) return;
            e.preventDefault();
            history.replaceState(null, '', location.pathname + toc.getAttribute('href'));
            setActiveToc(target.id);
            scrollToEl(target, true);
        }
    });

    document.addEventListener('submit', function (e) {
        var form = e.target.closest('.org-grid form[data-org-profile-form]');
        if (!form || form.dataset.orgNative === '1' || typeof fetch !== 'function') return;
        e.preventDefault();
        if (form.querySelector('[data-org-save]')?.disabled) return;
        save(form);
    });

    // Escape inside an open section cancels it.
    document.addEventListener('keydown', function (e) {
        if (e.key !== 'Escape') return;
        var section = e.target.closest && e.target.closest('.org-grid .osec.editing');
        if (section) closeSection(section, true);
    });

    // First paint: go to the section in the address (#os-… or ?edit=…), opening it if the server did.
    requestAnimationFrame(function () {
        var editing = document.querySelector('.org-grid .osec.editing');
        var hash = location.hash.indexOf('#os-') === 0 ? document.getElementById(location.hash.slice(1)) : null;
        var target = editing || hash;
        if (target) {
            scrollToEl(target, false);
            setActiveToc(target.id);
            if (editing) focusField(editing, true);
        }
        if (new URLSearchParams(location.search).has('edit') && editing) {
            history.replaceState(null, '', location.pathname + '#' + editing.id);
        }
        if (editing) snapshotSectionForm(editing);
        observeSections();
    });

    window.aobOrgProfileAttachAllForms = function () {
        document.querySelectorAll('.org-grid form[data-org-profile-form]').forEach(attachOrgFormValidator);
    };
})();

// Toasts (prototype .toast): page messages float at the top, below the menu bar, instead of pushing the page down.
// Server-rendered ones ([data-toast], from _FlashMessages) are gathered into one stack; window.aobToast adds more.
// Success, info, and error toasts close after AUTO_CLOSE_MS (paused while hovered or focused).
(function () {
    var host = null;
    var AUTO_CLOSE_MS = 6000;

    function getHost() {
        if (!host) {
            host = document.createElement('div');
            host.className = 'toasts';
            host.setAttribute('aria-live', 'polite');
            document.body.appendChild(host);
        }
        return host;
    }

    function dismiss(item) {
        if (item.classList.contains('is-leaving')) return;
        item.classList.add('is-leaving');
        setTimeout(function () { item.remove(); }, 220);
    }

    function arm(item) {
        getHost().appendChild(item);
        var close = item.querySelector('[data-toast-close]');
        if (close) close.addEventListener('click', function () { dismiss(item); });

        var timer = null;
        function start() { timer = setTimeout(function () { dismiss(item); }, AUTO_CLOSE_MS); }
        function stop() { clearTimeout(timer); }
        item.addEventListener('mouseenter', stop);
        item.addEventListener('mouseleave', start);
        item.addEventListener('focusin', stop);
        item.addEventListener('focusout', start);
        start();
    }

    window.aobToast = function (message, kind, title) {
        kind = kind || 'ok';
        var item = document.createElement('div');
        item.className = 'toast-item toast-' + kind;
        item.setAttribute('data-toast', '');
        item.setAttribute('data-kind', kind);
        item.setAttribute('role', kind === 'bad' ? 'alert' : 'status');
        var icon = kind === 'ok'
            ? '<path d="M20 6 9 17l-5-5"/>'
            : kind === 'bad' ? '<path d="M12 8v4M12 16h.01"/><circle cx="12" cy="12" r="9"/>' : '<circle cx="12" cy="12" r="9"/><path d="M12 16v-4M12 8h.01"/>';
        item.innerHTML = '<span class="toast-ic" aria-hidden="true"><svg class="i sm" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.4">' + icon + '</svg></span>'
            + '<div class="toast-body"></div>'
            + '<button type="button" class="toast-x" data-toast-close aria-label="Dismiss message"><svg class="i xs" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.4" aria-hidden="true"><path d="M18 6 6 18M6 6l12 12"/></svg></button>';
        var body = item.querySelector('.toast-body');
        if (title) {
            var t = document.createElement('div');
            t.className = 'toast-t';
            t.textContent = title;
            body.appendChild(t);
        }
        var d = document.createElement('div');
        d.className = 'toast-d';
        d.textContent = message;
        body.appendChild(d);
        arm(item);
    };

    document.querySelectorAll('[data-toast-host]').forEach(function (serverHost) {
        serverHost.querySelectorAll('[data-toast]').forEach(arm);
        serverHost.remove();
    });
})();
// File size guard: any <input type="file" data-max-bytes="..."> refuses a file over the limit before the form posts,
// so an oversized upload shows a message next to the field instead of the server's "request too large" error page.
(function () {
    function formatMb(bytes) {
        var mb = bytes / (1024 * 1024);
        return (mb >= 10 ? Math.round(mb) : Math.round(mb * 10) / 10) + ' MB';
    }

    function errorFor(input) {
        var id = input.getAttribute('data-size-error');
        var el = id ? document.getElementById(id) : null;
        if (!el) {
            el = input.parentNode.querySelector('[data-file-size-error]');
        }
        if (!el) {
            el = document.createElement('span');
            el.className = 'text-danger field-validation-error';
            el.setAttribute('data-file-size-error', '');
            el.setAttribute('role', 'alert');
            var host = input.closest('[data-drop]') || input;
            host.insertAdjacentElement('afterend', el);
        }
        return el;
    }

    function tooLarge(input) {
        var max = parseInt(input.getAttribute('data-max-bytes'), 10);
        if (!max || !input.files) return null;
        for (var i = 0; i < input.files.length; i++) {
            if (input.files[i].size > max) {
                return input.files[i];
            }
        }
        return null;
    }

    function check(input) {
        var file = tooLarge(input);
        var el = errorFor(input);
        if (file) {
            var max = parseInt(input.getAttribute('data-max-bytes'), 10);
            el.textContent = 'The selected file exceeds the maximum upload size of ' + formatMb(max) + '. Please upload a smaller file.';
            el.hidden = false;
            input.value = '';
            input.dispatchEvent(new Event('aob:file-cleared', { bubbles: true }));
            return false;
        }
        el.textContent = '';
        el.hidden = true;
        return true;
    }

    // Capture phase, so the check runs before any other change handler (e.g. one that auto-submits the form).
    document.addEventListener('change', function (e) {
        var input = e.target;
        if (input && input.matches && input.matches('input[type="file"][data-max-bytes]') && !check(input)) {
            e.stopImmediatePropagation();
        }
    }, true);

    document.addEventListener('submit', function (e) {
        var inputs = e.target.querySelectorAll('input[type="file"][data-max-bytes]');
        for (var i = 0; i < inputs.length; i++) {
            if (!check(inputs[i])) {
                e.preventDefault();
                e.stopImmediatePropagation();
                inputs[i].focus();
                return;
            }
        }
    }, true);
})();
