/* =========================================================
   NEXUS — validation.js
   Lightweight client-side validation.
   Works alongside ASP.NET Core MVC validation attributes.
   Uses `.field-error` spans and `.input-error` class from forms.css.
   ========================================================= */

(function () {
    'use strict';
    const NEXUS = window.NEXUS;

    // Regular expressions
    const RE = {
        email: /^[^\s@]+@[^\s@]+\.[^\s@]+$/,
        phonePK: /^03\d{9}$/,
        cnic: /^\d{5}-\d{7}-\d$/,
        postal: /^\d{5}$/
    };

    // ---------------------------------------------------------
    // Show / clear inline error on an input
    // ---------------------------------------------------------
    function setError(input, message) {
        input.classList.add('input-error');
        let err = input.parentElement.querySelector('.field-error');
        if (!err) {
            err = document.createElement('span');
            err.className = 'field-error';
            input.parentElement.appendChild(err);
        }
        err.textContent = message;
    }

    function clearError(input) {
        input.classList.remove('input-error');
        const err = input.parentElement.querySelector('.field-error');
        if (err) err.textContent = '';
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
        phone(input) {
            if (!input.value) return null;
            return RE.phonePK.test(input.value) ? null : 'Format: 03001234567';
        },
        cnic(input) {
            if (!input.value) return null;
            return RE.cnic.test(input.value) ? null : 'Format: 35202-1234567-1';
        },
        postal(input) {
            if (!input.value) return null;
            return RE.postal.test(input.value) ? null : 'Enter a 5-digit postal code';
        },
        minlength(input) {
            const min = Number(input.getAttribute('minlength')) || 0;
            if (!input.value) return null;
            return input.value.length >= min ? null : `Must be at least ${min} characters`;
        },
        password(input) {
            if (!input.value) return null;
            if (input.value.length < 6) return 'Password must be at least 6 characters';
            return null;
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

        if (type === 'email' || name.includes('email')) checks.push('email');
        if (name.includes('phone') || name.includes('mobile') || name.includes('contact')) checks.push('phone');
        if (name.includes('cnic')) checks.push('cnic');
        if (name.includes('postal') || name.includes('zip')) checks.push('postal');
        if (input.hasAttribute('minlength')) checks.push('minlength');
        if (type === 'password' && name !== 'confirmpassword') checks.push('password');
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