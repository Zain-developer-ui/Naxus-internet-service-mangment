/* =========================================================
   NEXUS — validation.js
   Client-side validation helper. MVC attribute validation
   is still the source of truth; this only gives faster
   feedback and uses .field-error / .input-error from forms.css.
   ========================================================= */

(function () {
    'use strict';
    const NEXUS = window.NEXUS;

    const RE = {
        email: /^[^\s@]+@[^\s@]+\.[^\s@]+$/
    };

    // ---------------------------------------------------------
    // Show / clear inline error on an input
    // ---------------------------------------------------------
    let errorSeq = 0;

    function errorFor(input) {
        // One error node per field, created lazily and then reused. Keyed off
        // the input id so a field that already has a server-rendered
        // validation span does not end up with two messages under it.
        let err = input.parentElement.querySelector('.field-error');
        if (!err) {
            err = document.createElement('span');
            err.className = 'field-error';
            err.setAttribute('role', 'alert');
            input.parentElement.appendChild(err);
        }
        if (!err.id) err.id = 'err-' + (input.id || 'field-' + (++errorSeq));
        return err;
    }

    function setError(input, message) {
        input.classList.add('input-error');
        input.setAttribute('aria-invalid', 'true');

        const err = errorFor(input);
        err.textContent = message;
        input.setAttribute('aria-describedby', err.id);
    }

    function clearError(input) {
        input.classList.remove('input-error');
        input.removeAttribute('aria-invalid');

        const err = input.parentElement.querySelector('.field-error');
        if (err) {
            err.textContent = '';
            if (input.getAttribute('aria-describedby') === err.id) {
                input.removeAttribute('aria-describedby');
            }
        }
    }

    // ---------------------------------------------------------
    // Field-level validators
    // ---------------------------------------------------------
    const rules = {
        required(input) {
            const v = (input.value || '').trim();
            if (!v) return 'This field is required';
            return null;
        },
        email(input) {
            if (!input.value) return null;
            return RE.email.test(input.value) ? null : 'Enter a valid email address';
        },

        // The server renders its own pattern and its own wording into data-val-*
        // attributes. Enforcing those keeps the two from drifting apart, which
        // is exactly how the mobile format used to disagree with the server.
        regex(input) {
            const pattern = input.getAttribute('data-val-regex-pattern');
            if (!pattern || !input.value) return null;

            let matches;
            try {
                matches = new RegExp(pattern).test(input.value);
            } catch (e) {
                return null; // a pattern we cannot parse is not ours to enforce
            }
            return matches ? null : (input.getAttribute('data-val-regex') || 'Invalid format');
        },

        length(input) {
            const min = Number(input.getAttribute('data-val-length-min')) || 0;
            if (!min || !input.value) return null;
            return input.value.length >= min
                ? null
                : (input.getAttribute('data-val-length') || `Must be at least ${min} characters`);
        },

        minlength(input) {
            const min = Number(input.getAttribute('minlength')) || 0;
            if (!input.value) return null;
            return input.value.length >= min ? null : `Must be at least ${min} characters`;
        },
        confirm(input) {
            const targetId = input.getAttribute('data-confirm-target');
            if (!targetId) return null;
            const target = document.getElementById(targetId);
            if (!target) return null;
            return input.value === target.value ? null : 'Passwords do not match';
        }
    };

    // ---------------------------------------------------------
    // Determine which rules to apply based on input attributes
    // ---------------------------------------------------------
    function validateField(input) {
        const type = (input.getAttribute('type') || '').toLowerCase();
        const name = (input.getAttribute('name') || '').toLowerCase();
        const explicit = input.getAttribute('data-validate');

        // Skip hidden / disabled / non-form fields
        if (input.disabled || type === 'hidden') return true;

        const checks = [];

        if (input.hasAttribute('required') || input.hasAttribute('data-required')) checks.push('required');

        // A name match is only a guess. An explicit type or a data-validate
        // list wins, and the sign-in identifier - which takes either an
        // account id or an email - is exempt from the guess entirely.
        const guessable = name.includes('email') && !name.includes('account');

        if (type === 'email' || guessable) checks.push('email');
        if (input.hasAttribute('data-val-regex-pattern')) checks.push('regex');
        if (input.hasAttribute('data-val-length-min')) checks.push('length');
        if (input.hasAttribute('minlength')) checks.push('minlength');
        if (input.hasAttribute('data-confirm-target')) checks.push('confirm');

        // Explicit override: space-separated list in data-validate
        if (explicit) explicit.split(/\s+/).forEach(r => rules[r] && checks.push(r));

        for (const ruleName of checks) {
            const fn = rules[ruleName];
            if (!fn) continue;
            const err = fn(input);
            if (err) {
                setError(input, err);
                return false;
            }
        }
        clearError(input);
        return true;
    }

    // ---------------------------------------------------------
    // Validate entire form (only [data-validate-form] opt-in)
    // ---------------------------------------------------------
    function validateForm(form) {
        const fields = Array.from(
            form.querySelectorAll('input, select, textarea')
        ).filter(f => f.type !== 'submit' && f.type !== 'button' && f.type !== 'hidden');

        let firstInvalid = null;
        fields.forEach(f => {
            const ok = validateField(f);
            if (!ok && !firstInvalid) firstInvalid = f;
        });

        if (firstInvalid) {
            firstInvalid.focus();
            firstInvalid.scrollIntoView({ behavior: 'smooth', block: 'center' });
            return false;
        }
        return true;
    }

    // ---------------------------------------------------------
    // Wire up
    // ---------------------------------------------------------
    function init() {
        // Live validation on blur
        document.addEventListener('blur', (e) => {
            const t = e.target;
            if (!t.matches || !t.matches('input, select, textarea')) return;
            const form = t.closest('form');
            if (!form || !form.hasAttribute('data-validate-form')) return;
            validateField(t);
        }, true);

        // Clear on input
        document.addEventListener('input', (e) => {
            const t = e.target;
            if (!t.matches || !t.matches('input, select, textarea')) return;
            if (t.classList.contains('input-error')) clearError(t);
        });

        // Block submit if invalid
        document.addEventListener('submit', (e) => {
            const form = e.target;
            if (!form.hasAttribute('data-validate-form')) return;
            if (!validateForm(form)) {
                e.preventDefault();
                e.stopPropagation();
                NEXUS.toast('Please correct the highlighted fields.', 'warning');
            }
        }, true);
    }

    NEXUS.validation = { validateForm, validateField, setError, clearError };

    NEXUS.ready(init);

})();