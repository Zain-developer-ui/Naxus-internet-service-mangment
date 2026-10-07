/* =========================================================
   NEXUS — modal.js
   Open/close for .modal-backdrop panels. The markup contract
   is defined in css/components.css; this only handles the
   open class, focus and the escape/backdrop dismissal.
   ========================================================= */

(function () {
    'use strict';

    const NEXUS = window.NEXUS = window.NEXUS || {};

    let lastFocused = null;

    function open(backdrop) {
        if (!backdrop) return;

        lastFocused = document.activeElement;
        backdrop.classList.add('open');

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
        if (e.key === 'Escape') closeAll();
    });
})();
