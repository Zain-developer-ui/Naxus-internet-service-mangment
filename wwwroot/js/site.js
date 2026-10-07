/* =========================================================
   NEXUS — site.js
   Global utilities + Luxury Toast System
   ========================================================= */

(function () {
    'use strict';

    /* ---------------------------------------------------------
       GLOBAL NAMESPACE
       --------------------------------------------------------- */
    window.NEXUS = window.NEXUS || {};

    NEXUS.$ = (sel, ctx = document) => ctx.querySelector(sel);
    NEXUS.$$ = (sel, ctx = document) => Array.from(ctx.querySelectorAll(sel));

    NEXUS.ready = function (fn) {
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', fn);
        } else {
            fn();
        }
    };

    NEXUS.debounce = function (fn, delay = 200) {
        let t;
        return function (...args) {
            clearTimeout(t);
            t = setTimeout(() => fn.apply(this, args), delay);
        };
    };

    NEXUS.formatMoney = function (value) {
        const n = Number(value) || 0;
        return '$' + n.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    };

    NEXUS.formatDate = function (input) {
        const d = input instanceof Date ? input : new Date(input);
        if (isNaN(d)) return '';
        return d.toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' });
    };

    /* ---------------------------------------------------------
       TOAST SYSTEM — Luxury
       ---------------------------------------------------------
       Usage:
         NEXUS.toast('Message')
         NEXUS.toast('Message', 'success')
         NEXUS.toast('Message', 'warning', 5000)
         NEXUS.toast({ title: 'Saved', message: 'All good.', type: 'success' })
       --------------------------------------------------------- */
    const TOAST_ICONS = {
        success: 'fa-circle-check',
        warning: 'fa-triangle-exclamation',
        danger: 'fa-circle-xmark',
        info: 'fa-circle-info'
    };
    const TOAST_TITLES = {
        success: 'Success',
        warning: 'Warning',
        danger: 'Error',
        info: 'Info'
    };

    function getContainer() {
        let c = document.getElementById('nexus-toast-container');
        if (!c) {
            c = document.createElement('div');
            c.id = 'nexus-toast-container';
            c.className = 'nexus-toast-container';
            c.setAttribute('role', 'status');
            c.setAttribute('aria-live', 'polite');
            document.body.appendChild(c);
        }
        return c;
    }

    NEXUS.toast = function (messageOrOptions, type = 'info', duration = 4000) {
        // Support both calling styles
        let title, message, toastType;

        if (typeof messageOrOptions === 'object' && messageOrOptions !== null) {
            title = messageOrOptions.title || TOAST_TITLES[messageOrOptions.type || 'info'];
            message = messageOrOptions.message || '';
            toastType = messageOrOptions.type || 'info';
            duration = messageOrOptions.duration || duration;
        } else {
            toastType = type;
            title = TOAST_TITLES[toastType] || 'Info';
            message = messageOrOptions || '';
        }

        const container = getContainer();

        // Cap at 4 toasts on screen
        const existing = container.querySelectorAll('.nexus-toast');
        if (existing.length >= 4) {
            existing[0].remove();
        }

        const toast = document.createElement('div');
        toast.className = `nexus-toast ${toastType}`;
        toast.setAttribute('role', 'alert');

        toast.innerHTML = `
            <span class="toast-icon"><i class="fa-solid ${TOAST_ICONS[toastType] || TOAST_ICONS.info}"></i></span>
            <div class="toast-body">
                <p class="toast-title">${escapeHtml(title)}</p>
                <p class="toast-message">${escapeHtml(message)}</p> </div>
            <button type="button" class="toast-close" aria-label="Close notification">
                <i class="fa-solid fa-xmark"></i> </button>
            <span class="toast-progress" style="animation-duration:${duration}ms;"></span>
        `;

        container.appendChild(toast);

        // Trigger animation next frame
        requestAnimationFrame(() => toast.classList.add('show'));

        // Dismiss handler
        let dismissed = false;
        function dismiss() {
            if (dismissed) return;
            dismissed = true;
            toast.classList.remove('show');
            toast.classList.add('hide');
            setTimeout(() => toast.remove(), 420);
        }

        // Auto-dismiss
        const timer = setTimeout(dismiss, duration);

        // Manual close
        toast.querySelector('.toast-close').addEventListener('click', () => {
            clearTimeout(timer);
            dismiss();
        });

        // Pause on hover
        toast.addEventListener('mouseenter', () => {
            toast.querySelector('.toast-progress').style.animationPlayState = 'paused';
            clearTimeout(timer);
        });
        toast.addEventListener('mouseleave', () => {
            toast.querySelector('.toast-progress').style.animationPlayState = 'running';
            // Give remaining 1.5s to read
            setTimeout(dismiss, 1500);
        });

        return toast;
    };

    function escapeHtml(str) {
        return String(str)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');
    }

    /* ---------------------------------------------------------
       SCROLL REVEAL
       --------------------------------------------------------- */
    function initScrollReveal() {
        const items = NEXUS.$$('.reveal, .reveal-left, .reveal-right');
        if (!items.length) return;

        if (!('IntersectionObserver' in window)) {
            items.forEach(i => i.classList.add('in-view'));
            return;
        }

        const io = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    entry.target.classList.add('in-view');
                    io.unobserve(entry.target);
                }
            });
        }, { threshold: 0.12, rootMargin: '0px 0px -40px 0px' });

        items.forEach(i => io.observe(i));
    }

    /* ---------------------------------------------------------
       SMOOTH ANCHOR SCROLL
       --------------------------------------------------------- */
    function initSmoothScroll() {
        document.addEventListener('click', (e) => {
            const a = e.target.closest('a[href^="#"]');
            if (!a) return;
            const id = a.getAttribute('href');
            if (id === '#' || id.length < 2) return;
            const target = document.querySelector(id);
            if (!target) return;
            e.preventDefault();
            target.scrollIntoView({ behavior: 'smooth', block: 'start' });
        });
    }

    /* ---------------------------------------------------------
       NAVBAR SCROLL SHADOW
       --------------------------------------------------------- */
    function initNavbarScroll() {
        const nav = NEXUS.$('.nexus-navbar');
        if (!nav) return;
        const onScroll = () => {
            nav.classList.toggle('scrolled', window.scrollY > 8);
        };
        window.addEventListener('scroll', onScroll, { passive: true });
        onScroll();
    }

    /* ---------------------------------------------------------
       ALERT DISMISSAL
       --------------------------------------------------------- */
    function initAlerts() {
        document.addEventListener('click', (e) => {
            const btn = e.target.closest('[data-alert-close]');
            if (!btn) return;
            const alert = btn.closest('.alert');
            if (!alert) return;
            alert.style.transition = 'opacity .25s ease, transform .25s ease';
            alert.style.opacity = '0';
            alert.style.transform = 'translateY(-6px)';
            setTimeout(() => alert.remove(), 260);
        });
    }

    /* ---------------------------------------------------------
       RELATIVE TIME
       ---------------------------------------------------------
       Used by dashboards for "2 hours ago" style stamps. Kept here
       rather than in each page so the wording stays consistent.
       --------------------------------------------------------- */
    NEXUS.timeAgo = function (value) {
        const then = new Date(value);
        if (Number.isNaN(then.getTime())) return '';

        const seconds = Math.floor((Date.now() - then.getTime()) / 1000);
        if (seconds < 60) return 'just now';

        const minutes = Math.floor(seconds / 60);
        if (minutes < 60) return minutes + ' min ago';

        const hours = Math.floor(minutes / 60);
        if (hours < 24) return hours + (hours === 1 ? ' hour ago' : ' hours ago');

        const days = Math.floor(hours / 24);
        if (days < 30) return days + (days === 1 ? ' day ago' : ' days ago');

        return then.toLocaleDateString();
    };

    /* ---------------------------------------------------------
       USER MENU
       ---------------------------------------------------------
       The account menu on the dashboard topbar. Opens on click,
       closes on outside click, on Escape, and returns focus to the
       trigger so keyboard users are not stranded.
       --------------------------------------------------------- */
    function initUserMenu() {
        document.querySelectorAll('[data-user-menu]').forEach(function (wrapper) {
            const trigger = wrapper.querySelector('.user-chip-trigger');
            const menu = wrapper.querySelector('.user-menu');
            if (!trigger || !menu) return;

            function close() {
                wrapper.classList.remove('open');
                trigger.setAttribute('aria-expanded', 'false');
            }

            trigger.addEventListener('click', function (event) {
                event.stopPropagation();
                const isOpen = wrapper.classList.toggle('open');
                trigger.setAttribute('aria-expanded', String(isOpen));
            });

            menu.addEventListener('click', function (event) {
                event.stopPropagation();
            });

            document.addEventListener('click', close);
            document.addEventListener('keydown', function (event) {
                if (event.key !== 'Escape') return;
                if (!wrapper.classList.contains('open')) return;
                close();
                trigger.focus();
            });
        });
    }

    /* ---------------------------------------------------------
       BOOTSTRAP
       --------------------------------------------------------- */
    NEXUS.ready(function () {
        initScrollReveal();
        initSmoothScroll();
        initNavbarScroll();
        initAlerts();
        initUserMenu();
    });

    NEXUS.refreshReveal = initScrollReveal;

})();