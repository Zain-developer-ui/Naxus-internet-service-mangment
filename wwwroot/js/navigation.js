/* =========================================================
   NEXUS — navigation.js
   Public navbar toggle, dashboard sidebar toggle,
   overlay, escape-key, aria state.
   ========================================================= */

(function () {
    'use strict';
    const NEXUS = window.NEXUS;

    /* ---------------------------------------------------------
       Public navbar toggle
       --------------------------------------------------------- */
    function initNavbarToggle() {
        const toggle = NEXUS.$('[data-nav-toggle]');
        const links = NEXUS.$('[data-nav-links]');
        if (!toggle || !links) return;

        toggle.setAttribute('aria-expanded', 'false');

        toggle.addEventListener('click', () => {
            const open = links.classList.toggle('open');
            toggle.setAttribute('aria-expanded', open ? 'true' : 'false');
            toggle.innerHTML = open
                ? '<i class="fa-solid fa-xmark"></i>'
                : '<i class="fa-solid fa-bars"></i>';
        });

        // Close when a link is clicked
        links.addEventListener('click', (e) => {
            if (e.target.closest('a')) {
                links.classList.remove('open');
                toggle.setAttribute('aria-expanded', 'false');
                toggle.innerHTML = '<i class="fa-solid fa-bars"></i>';
            }
        });
    }

    /* ---------------------------------------------------------
       Dashboard sidebar toggle (mobile) + overlay
       --------------------------------------------------------- */
    function initSidebar() {
        const sidebar = NEXUS.$('[data-sidebar]');
        const openBtn = NEXUS.$('[data-sidebar-toggle]');
        const closeBtn = NEXUS.$('[data-sidebar-close]');
        if (!sidebar) return;

        // Create overlay once
        let overlay = NEXUS.$('.sidebar-overlay');
        if (!overlay) {
            overlay = document.createElement('div');
            overlay.className = 'sidebar-overlay';
            document.body.appendChild(overlay);
        }

        function openSidebar() {
            sidebar.classList.add('open');
            overlay.classList.add('show');
            document.body.style.overflow = 'hidden';
            if (openBtn) openBtn.setAttribute('aria-expanded', 'true');
        }
        function closeSidebar() {
            sidebar.classList.remove('open');
            overlay.classList.remove('show');
            document.body.style.overflow = '';
            if (openBtn) openBtn.setAttribute('aria-expanded', 'false');
        }

        openBtn && openBtn.addEventListener('click', openSidebar);
        closeBtn && closeBtn.addEventListener('click', closeSidebar);
        overlay.addEventListener('click', closeSidebar);

        // Close on sidebar link click (mobile)
        sidebar.addEventListener('click', (e) => {
            if (e.target.closest('a') && window.innerWidth <= 992) closeSidebar();
        });

        // Auto-close when resizing up to desktop
        window.addEventListener('resize', NEXUS.debounce(() => {
            if (window.innerWidth > 992) closeSidebar();
        }, 150));
    }

    /* ---------------------------------------------------------
       Escape key closes everything open
       --------------------------------------------------------- */
    function initEscape() {
        document.addEventListener('keydown', (e) => {
            if (e.key !== 'Escape') return;

            // Navbar
            const links = NEXUS.$('[data-nav-links].open');
            const navToggle = NEXUS.$('[data-nav-toggle]');
            if (links) {
                links.classList.remove('open');
                if (navToggle) {
                    navToggle.setAttribute('aria-expanded', 'false');
                    navToggle.innerHTML = '<i class="fa-solid fa-bars"></i>';
                }
            }

            // Sidebar
            const sidebar = NEXUS.$('[data-sidebar].open');
            if (sidebar) {
                sidebar.classList.remove('open');
                const overlay = NEXUS.$('.sidebar-overlay');
                overlay && overlay.classList.remove('show');
                document.body.style.overflow = '';
            }

            // Notifications
            const notif = NEXUS.$('.notifications-panel.open');
            notif && notif.classList.remove('open');
        });
    }

    /* ---------------------------------------------------------
       Mark active nav link based on current path
       --------------------------------------------------------- */
    function initActiveLink() {
        const path = window.location.pathname.toLowerCase();
        NEXUS.$$('.nav-links a, .sidebar-nav a').forEach(a => {
            const href = (a.getAttribute('href') || '').toLowerCase();
            if (!href || href === '#') return;
            // Match if path equals href or starts with href (for nested routes)
            const isActive = path === href || (href !== '/' && path.startsWith(href));
            if (isActive) a.classList.add('active');
        });
    }

    NEXUS.ready(() => {
        initNavbarToggle();
        initSidebar();
        initEscape();
        initActiveLink();
    });

})();