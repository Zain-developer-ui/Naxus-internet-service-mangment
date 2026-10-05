/* =========================================================
   NEXUS — orders.js
   Order form price calculation, order summary update,
   order tracking timeline, order lookup.
   All demo — no backend calls.
   ========================================================= */

(function () {
    'use strict';
    const NEXUS = window.NEXUS;

    // Demo plan catalogue mirrors ApiService mock values
    const PLANS = {
        'Basic': { speed: '5 Mbps', monthly: 1200, deposit: 450, install: 0 },
        'Standard': { speed: '15 Mbps', monthly: 2000, deposit: 450, install: 0 },
        'Premium': { speed: '30 Mbps', monthly: 3500, deposit: 450, install: 0 }
    };

    /* ---------------------------------------------------------
       Order form: recalc summary when plan/type changes
       --------------------------------------------------------- */
    function initOrderSummary() {
        const form = NEXUS.$('form[data-order-form]');
        if (!form) return;

        const planSelect = form.querySelector('[name="PlanName"]');
        const speedField = form.querySelector('[name="Speed"]');
        const serviceSel = form.querySelector('[name="ServiceType"]');

        const sumPlan = document.querySelector('[data-summary="plan"]');
        const sumSpeed = document.querySelector('[data-summary="speed"]');
        const sumMonthly = document.querySelector('[data-summary="monthly"]');
        const sumDeposit = document.querySelector('[data-summary="deposit"]');
        const sumInstall = document.querySelector('[data-summary="install"]');
        const sumTotal = document.querySelector('[data-summary="total"]');
        const sumService = document.querySelector('[data-summary="service"]');

        function recalc() {
            const planName = planSelect?.value || 'Standard';
            const plan = PLANS[planName] || PLANS['Standard'];

            if (speedField && !speedField.value) speedField.value = plan.speed;

            const total = plan.monthly + plan.deposit + plan.install;

            sumPlan && (sumPlan.textContent = planName + ' Plan');
            sumSpeed && (sumSpeed.textContent = plan.speed);
            sumMonthly && (sumMonthly.textContent = NEXUS.formatPKR(plan.monthly));
            sumDeposit && (sumDeposit.textContent = NEXUS.formatPKR(plan.deposit));
            sumInstall && (sumInstall.textContent = plan.install === 0 ? 'Free' : NEXUS.formatPKR(plan.install));
            sumTotal && (sumTotal.textContent = NEXUS.formatPKR(total));
            sumService && (sumService.textContent = serviceSel?.value || 'Broadband');
        }

        ['change', 'input'].forEach(evt =>
            form.addEventListener(evt, recalc)
        );
        recalc();
    }

    /* ---------------------------------------------------------
       Auto-fill speed when user picks a plan
       --------------------------------------------------------- */
    function initPlanSpeedSync() {
        document.addEventListener('change', (e) => {
            const sel = e.target.closest('[data-plan-select]');
            if (!sel) return;
            const plan = PLANS[sel.value];
            const speedTarget = document.querySelector(sel.getAttribute('data-speed-target') || '[name="Speed"]');
            if (plan && speedTarget) speedTarget.value = plan.speed;
        });
    }

    /* ---------------------------------------------------------
       Order tracking — search simulation
       --------------------------------------------------------- */
    function initTracking() {
        const form = NEXUS.$('form[data-tracking-form]');
        if (!form) return;

        form.addEventListener('submit', (e) => {
            e.preventDefault();
            const input = form.querySelector('input[name="orderId"], input[name="id"]');
            const id = input?.value.trim();
            if (!id) {
                NEXUS.toast('Please enter an Order or Account ID.', 'warning');
                input?.focus();
                return;
            }

            // Show loading spinner on the submit button
            const btn = form.querySelector('button[type=submit]');
            const original = btn?.innerHTML;
            if (btn) {
                btn.disabled = true;
                btn.innerHTML = '<span class="loading-spinner" style="width:16px;height:16px;border-width:2px;"></span> Searching…';
            }

            // Simulate network delay then redirect (demo)
            setTimeout(() => {
                window.location.href = `/Orders/Tracking?id=${encodeURIComponent(id)}`;
            }, 700);
        });
    }

    /* ---------------------------------------------------------
       Timeline animation — highlights the current active step
       --------------------------------------------------------- */
    function initTimelineAnimation() {
        const timeline = NEXUS.$('[data-order-timeline]');
        if (!timeline) return;

        const items = Array.from(timeline.querySelectorAll('.timeline-item'));
        const currentIndex = items.findIndex(i => i.classList.contains('active'));

        // Animate items one by one
        items.forEach((item, i) => {
            item.style.opacity = '0';
            item.style.transform = 'translateY(10px)';
            setTimeout(() => {
                item.style.transition = 'opacity .5s ease, transform .5s ease';
                item.style.opacity = '1';
                item.style.transform = 'translateY(0)';
            }, 120 * i + 100);
        });

        // Progressively fill the bar if the page has one
        const bar = document.querySelector('[data-order-progress]');
        if (bar && items.length > 1) {
            const pct = currentIndex >= 0
                ? ((currentIndex) / (items.length - 1)) * 100
                : 0;
            bar.style.width = '0%';
            setTimeout(() => { bar.style.width = pct + '%'; }, 300);
        }
    }

    /* ---------------------------------------------------------
       Quick order status pill — turn status text into a class
       --------------------------------------------------------- */
    function initStatusClasses() {
        document.querySelectorAll('[data-status]').forEach(el => {
            const raw = (el.getAttribute('data-status') || '').toLowerCase();
            const cls = ['active', 'completed', 'paid'].includes(raw) ? 'badge-success'
                : ['pending', 'due', 'review'].includes(raw) ? 'badge-warning'
                    : ['overdue', 'failed', 'suspended'].includes(raw) ? 'badge-danger'
                        : 'badge-muted';
            el.classList.add('badge', cls);
        });
    }

    NEXUS.ready(() => {
        initOrderSummary();
        initPlanSpeedSync();
        initTracking();
        initTimelineAnimation();
        initStatusClasses();
    });

})();