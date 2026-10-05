/* =========================================================
   NEXUS — dashboard.js
   Topbar interactions (notifications, user menu),
   stat card counter animation, quick actions routing.
   ========================================================= */

(function () {
    'use strict';
    const NEXUS = window.NEXUS;

    /* ---------------------------------------------------------
       Stat card counter animation (uses .stat-value[data-count])
       --------------------------------------------------------- */
    function animateCounters() {
        const nums = NEXUS.$$('[data-count]');
        if (!nums.length) return;

        const io = new IntersectionObserver((entries) => {
            entries.forEach(e => {
                if (!e.isIntersecting) return;
                const el = e.target;
                io.unobserve(el);

                const target = Number(el.getAttribute('data-count')) || 0;
                const prefix = el.getAttribute('data-prefix') || '';
                const suffix = el.getAttribute('data-suffix') || '';
                const duration = 900;
                const start = performance.now();

                function step(now) {
                    const p = Math.min((now - start) / duration, 1);
                    const eased = 1 - Math.pow(1 - p, 3);
                    const val = Math.round(target * eased);
                    el.textContent = prefix + val.toLocaleString() + suffix;
                    if (p < 1) requestAnimationFrame(step);
                }
                requestAnimationFrame(step);
            });
        }, { threshold: 0.4 });

        nums.forEach(n => io.observe(n));
    }

    /* ---------------------------------------------------------
       Notifications panel
       Markup: <button data-notifications-toggle>
               <div class="notifications-panel" data-notifications-panel>
       --------------------------------------------------------- */
    function initNotifications() {
        const btn = NEXUS.$('[data-notifications-toggle]');
        const panel = NEXUS.$('[data-notifications-panel]');
        if (!btn || !panel) return;

        btn.setAttribute('aria-expanded', 'false');

        btn.addEventListener('click', (e) => {
            e.stopPropagation();
            const open = panel.classList.toggle('open');
            btn.setAttribute('aria-expanded', open ? 'true' : 'false');
        });

        document.addEventListener('click', (e) => {
            if (!panel.contains(e.target) && !btn.contains(e.target)) {
                panel.classList.remove('open');
                btn.setAttribute('aria-expanded', 'false');
            }
        });
    }

    /* ---------------------------------------------------------
       User chip dropdown
       Markup: <div class="user-chip" data-user-chip>
               <div class="user-dropdown">…</div>
       --------------------------------------------------------- */
    function initUserMenu() {
        const chip = NEXUS.$('[data-user-chip]');
        if (!chip) return;
        const menu = chip.querySelector('.user-dropdown');
        if (!menu) return;

        chip.addEventListener('click', (e) => {
            // avoid toggling when a menu link is clicked
            if (e.target.closest('a')) return;
            const open = menu.classList.toggle('open');
            chip.setAttribute('aria-expanded', open ? 'true' : 'false');
        });

        document.addEventListener('click', (e) => {
            if (!chip.contains(e.target)) menu.classList.remove('open');
        });
    }

    /* ---------------------------------------------------------
       Quick actions
       Markup: <button class="quick-action" data-quick-action="new-order">
       --------------------------------------------------------- */
    const QUICK_ROUTES = {
        'new-order': '/Orders/New',
        'search': '/Retail/Search',
        'order-status': '/Orders/Tracking',
        'payment': '/Bills',
        'new-customer': '/Retail/NewOrder',
        'reports': '/Admin/Reports'
    };

    function initQuickActions() {
        document.addEventListener('click', (e) => {
            const qa = e.target.closest('[data-quick-action]');
            if (!qa) return;
            const key = qa.getAttribute('data-quick-action');
            const route = QUICK_ROUTES[key];
            if (route) {
                window.location.href = route;
            } else {
                NEXUS.toast('That action is not available in this demo.', 'info');
            }
        });
    }

    /* ---------------------------------------------------------
       Dashboard filters (dropdown selects on tables)
       Markup: <select data-dashboard-filter="status">
       --------------------------------------------------------- */
    function initDashboardFilters() {
        document.addEventListener('change', (e) => {
            const sel = e.target.closest('[data-dashboard-filter]');
            if (!sel) return;
            const key = sel.getAttribute('data-dashboard-filter');
            const value = sel.value.toLowerCase();
            const targetSel = sel.getAttribute('data-target');
            const table = targetSel ? document.querySelector(targetSel) : document.querySelector('table tbody');
            if (!table) return;

            Array.from(table.rows).forEach(row => {
                const cell = row.querySelector(`[data-col="${key}"]`);
                if (!cell) return;
                const text = cell.textContent.trim().toLowerCase();
                row.style.display = (!value || text.includes(value)) ? '' : 'none';
            });
        });
    }

    /* ---------------------------------------------------------
       Topbar search — routes to global search page
       --------------------------------------------------------- */
    function initTopbarSearch() {
        const form = NEXUS.$('[data-topbar-search]');
        if (!form) return;
        form.addEventListener('submit', (e) => {
            e.preventDefault();
            const q = form.querySelector('input')?.value.trim();
            if (!q) return;
            window.location.href = `/Retail/Search?q=${encodeURIComponent(q)}`;
        });
    }

    /* ---------------------------------------------------------
       Activity feed "show more" (progressive disclosure)
       --------------------------------------------------------- */
    function initActivityToggle() {
        const toggles = NEXUS.$$('[data-activity-toggle]');
        toggles.forEach(btn => {
            btn.addEventListener('click', () => {
                const feed = document.querySelector(btn.getAttribute('data-activity-toggle'));
                if (!feed) return;
                const hidden = feed.querySelectorAll('.activity-item[hidden]');
                if (hidden.length) {
                    hidden.forEach(h => h.removeAttribute('hidden'));
                    btn.textContent = 'Show less';
                } else {
                    Array.from(feed.querySelectorAll('.activity-item')).slice(4).forEach(a => a.setAttribute('hidden', ''));
                    btn.textContent = 'Show more';
                }
            });
        });
    }

    NEXUS.ready(() => {
        animateCounters();
        initNotifications();
        initUserMenu();
        initQuickActions();
        initDashboardFilters();
        initTopbarSearch();
        initActivityToggle();
    });

})();