/* =========================================================
   NEXUS — modal.js
   Open/close for .modal-backdrop panels. The markup contract
   is defined in css/components.css; this only handles the
   open class, focus and the escape/backdrop dismissal.
   ========================================================= */

(function () {
    'use strict';

    const NEXUS = window.NEXUS = window.NEXUS || {};

    const FOCUSABLE = 'a[href], button:not([disabled]), input:not([disabled]), ' +
        'select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])';

    let lastFocused = null;

    function focusableWithin(panel) {
        return Array.from(panel.querySelectorAll(FOCUSABLE)).filter(function (el) {
            return el.offsetParent !== null || el === document.activeElement;
        });
    }

    function open(backdrop) {
        if (!backdrop) return;

        lastFocused = document.activeElement;
        backdrop.classList.add('open');

        // Panels carry role="dialog"; if a hand-written one forgot the
        // attributes the screen reader would read the page behind it.
        const panel = backdrop.querySelector('.modal');
        if (panel) {
            if (!panel.hasAttribute('role')) panel.setAttribute('role', 'dialog');
            panel.setAttribute('aria-modal', 'true');
        }

        // Focus the first control so keyboard users are not stranded behind
        // the overlay.
        const target = backdrop.querySelector(
            '[autofocus], .modal-close, button, [href], input, select, textarea'
        );
        if (target) target.focus();

        document.body.style.overflow = 'hidden';
    }

    function close(backdrop) {
        if (!backdrop) return;

        backdrop.classList.remove('open');
        document.body.style.overflow = '';

        if (lastFocused && typeof lastFocused.focus === 'function') {
            lastFocused.focus();
        }
        lastFocused = null;
    }

    function closeAll() {
        document.querySelectorAll('.modal-backdrop.open').forEach(close);
    }

    /* Keeps Tab inside the topmost open dialog. Without this the focus walks
       out onto the page behind the overlay, which is aria-hidden in effect
       but still reachable by keyboard. */
    function trapFocus(event) {
        const open = document.querySelectorAll('.modal-backdrop.open');
        if (!open.length) return;

        const backdrop = open[open.length - 1];
        const panel = backdrop.querySelector('.modal') || backdrop;
        const items = focusableWithin(panel);
        if (!items.length) return;

        const first = items[0];
        const last = items[items.length - 1];
        const active = document.activeElement;

        if (!panel.contains(active)) {
            event.preventDefault();
            first.focus();
            return;
        }

        if (event.shiftKey && active === first) {
            event.preventDefault();
            last.focus();
        } else if (!event.shiftKey && active === last) {
            event.preventDefault();
            first.focus();
        }
    }

    NEXUS.modal = { open, close, closeAll };

    document.addEventListener('click', function (e) {
        const trigger = e.target.closest('[data-modal-open]');
        if (trigger) {
            e.preventDefault();
            open(document.getElementById(trigger.getAttribute('data-modal-open')));
            return;
        }

        const dismiss = e.target.closest('[data-modal-close]');
        if (dismiss) {
            e.preventDefault();
            close(dismiss.closest('.modal-backdrop'));
            return;
        }

        // A click on the backdrop itself (not the panel) dismisses.
        if (e.target.classList.contains('modal-backdrop')) {
            close(e.target);
        }
    });

    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') {
            closeAll();
            return;
        }
        if (e.key === 'Tab') trapFocus(e);
    });
})();
