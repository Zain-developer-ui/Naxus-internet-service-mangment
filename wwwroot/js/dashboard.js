/* =========================================================
   NEXUS — dashboard.js
   Stat card counter animation for [data-count] figures.
   ========================================================= */

(function () {
    'use strict';
    const NEXUS = window.NEXUS;

    function finalText(el) {
        const prefix = el.getAttribute('data-prefix') || '';
        const suffix = el.getAttribute('data-suffix') || '';
        const target = Number(el.getAttribute('data-count')) || 0;
        return prefix + target.toLocaleString() + suffix;
    }

    function animateCounters() {
        const nums = NEXUS.$$('[data-count]');
        if (!nums.length) return;

        // With reduced motion the count-up is decoration, not information.
        // Print the final value and skip the ramp.
        if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
            nums.forEach(el => { el.textContent = finalText(el); });
            return;
        }

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

    NEXUS.ready(animateCounters);

})();
