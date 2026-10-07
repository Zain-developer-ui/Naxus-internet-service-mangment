/* =========================================================
   NEXUS — new-connection.js
   Multi-step application for /Orders/New.
   Steps 1-3: [Previous] [Cancel] [Next]
   Step 4   : [Previous] [Cancel] [Submit Application]
   ========================================================= */

(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {

        var form = document.querySelector('form[data-step-form]');
        if (!form) return;

        var panels = form.querySelectorAll('.step-panel');
        var steps = form.querySelectorAll('.step-progress .step');
        var fill = form.querySelector('.step-bar-fill');
        var nextBtn = form.querySelector('[data-step-next]');
        var submitBtn = form.querySelector('[data-step-submit]');
        var prevBtn = form.querySelector('[data-step-prev]');

        var planGrid = form.querySelector('[data-plan-grid]');
        var planEmpty = form.querySelector('[data-plan-empty]');
        var landlineField = form.querySelector('[data-landline-field]');
        var landlineInput = landlineField ? landlineField.querySelector('input') : null;
        var orgField = form.querySelector('[data-org-field]');
        var orgInput = orgField ? orgField.querySelector('input') : null;
        var corporateToggle = form.querySelector('[data-corporate-toggle]');
        var termsBox = form.querySelector('[data-terms]');
        var passwordInput = document.getElementById('Password');

        var total = panels.length;
        var index = 0;

        /* ---------------- Step navigation ---------------- */

        function update() {
            panels.forEach(function (p, i) {
                p.classList.toggle('active', i === index);
            });

            steps.forEach(function (s, i) {
                s.classList.toggle('active', i === index);
                s.classList.toggle('done', i < index);
            });

            if (fill) {
                var pct = total === 1 ? 0 : (index / (total - 1)) * 90;
                fill.style.width = pct + '%';
            }

            var isLastStep = (index === total - 1);

            if (nextBtn) {
                nextBtn.hidden = isLastStep;
            }
            if (submitBtn) {
                submitBtn.hidden = !isLastStep;
            }

            // Focus the first real input so keyboard users are not dropped at
            // the top of the document on every step change.
            var panel = panels[index];
            if (panel) {
                var firstInput = panel.querySelector('input:not([type=hidden]):not([disabled]), select, textarea');
                if (firstInput) firstInput.focus({ preventScroll: true });
                panel.scrollIntoView({ behavior: 'smooth', block: 'start' });
            }
        }

        /* The steps are gated on the fields inside them, using the same
           validation helper the rest of the site uses. */
        function currentPanelValid() {
            var panel = panels[index];
            if (!panel) return true;

            var fields = panel.querySelectorAll('input, select, textarea');
            var ok = true;

            fields.forEach(function (field) {
                if (field.type === 'hidden' || field.disabled) return;
                if (field.closest('[hidden]')) return;
                if (!NEXUS.validation || !NEXUS.validation.validateField) return;
                if (!NEXUS.validation.validateField(field)) ok = false;
            });

            return ok;
        }

        form.addEventListener('click', function (e) {
            if (e.target.closest('[data-step-next]')) {
                e.preventDefault();
                if (!currentPanelValid()) {
                    NEXUS.toast('Please complete this step first.', 'warning', 2500);
                    return;
                }
                if (index < total - 1) { index++; update(); }
                return;
            }

            if (e.target.closest('[data-step-prev]')) {
                e.preventDefault();
                if (index > 0) { index--; update(); }
            }
        });

        /* ---------------- Plan filtering + summary ---------------- */

        function selectedPlanOption() {
            if (!planGrid) return null;
            var radio = planGrid.querySelector('[data-plan-radio]:checked');
            return radio ? radio.closest('[data-plan-option]') : null;
        }

        function money(value) {
            return '$' + Number(value || 0).toFixed(2);
        }

        /* The order summary sits outside the form - it is a sibling <aside>, so
           it can stay pinned while the steps advance. Searching only within the
           form found nothing and the panel silently never updated. */
        function setSummary(name, value) {
            document.querySelectorAll('[data-summary=' + name + ']').forEach(function (el) {
                el.textContent = value;
            });
        }

        /* The chosen card is marked with .selected rather than relying on
           :checked, because the radio itself is visually hidden. Without this
           the user gets no feedback at all about which plan is active. */
        function paintPlanSelection() {
            if (!planGrid) return;

            planGrid.querySelectorAll('[data-plan-option]').forEach(function (option) {
                var radio = option.querySelector('[data-plan-radio]');
                option.classList.toggle('selected', !!(radio && radio.checked));
            });
        }

        /* A plan belongs to one connection type, so switching type re-filters
           the grid and clears a plan that no longer applies. The first plan of
           the new type is auto-selected so the form is never left without a
           plan, and so the summary has something to show before the user
           touches anything. */
        function syncPlans() {
            if (!planGrid) return;

            var typeRadio = form.querySelector('[data-conn-type]:checked');
            var type = typeRadio ? typeRadio.value : '';
            var anyVisible = false;
            var checkedVisible = false;
            var firstVisibleRadio = null;

            var options = planGrid.querySelectorAll('[data-plan-option]');
            options.forEach(function (option) {
                var matches = !type || option.getAttribute('data-type') === type;
                option.hidden = !matches;

                var radio = option.querySelector('[data-plan-radio]');
                if (radio) {
                    radio.disabled = !matches;
                    if (!matches) radio.checked = false;
                    if (matches && radio.checked) checkedVisible = true;
                    if (matches && !firstVisibleRadio) firstVisibleRadio = radio;
                }

                if (matches) anyVisible = true;
            });

            if (planEmpty) planEmpty.hidden = anyVisible;

            // Keep the customer's own pick when it still applies; otherwise
            // fall back to the first plan of the selected type.
            if (!checkedVisible && firstVisibleRadio) {
                firstVisibleRadio.checked = true;
                checkedVisible = true;
            }

            paintPlanSelection();
            syncSummary();
        }

        function syncSummary() {
            var typeRadio = form.querySelector('[data-conn-type]:checked');
            setSummary('type', typeRadio ? typeRadio.value : '—');

            var option = selectedPlanOption();
            if (!option) {
                setSummary('monthly', money(0));
                setSummary('deposit', money(0));
                setSummary('total', money(0));
                return;
            }

            var monthly = parseFloat(option.getAttribute('data-monthly')) || 0;
            var depositAmount = parseFloat(option.getAttribute('data-deposit')) || 0;

            setSummary('plan', option.querySelector('.choice-name').textContent.trim());
            setSummary('monthly', money(monthly));
            setSummary('deposit', money(depositAmount));
            setSummary('total', money(monthly + depositAmount));
        }

        /* The radio values are the raw cycle names, so "HalfYearly" would show
           up verbatim in the summary without this. */
        var cycleLabels = {
            Monthly: 'Monthly',
            HalfYearly: 'Half-Yearly',
            Yearly: 'Yearly'
        };

        function syncCycle() {
            var checked = form.querySelector('input[name=BillingCycle]:checked');
            var value = checked ? checked.value : 'Monthly';
            setSummary('cycle', cycleLabels[value] || value);
        }

        /* ---------------- Conditional fields ---------------- */

        function hasVisibleError(field) {
            var span = form.querySelector('[data-valmsg-for="' + field + '"]');
            return !!span && span.textContent.trim().length > 0;
        }

        function syncLandline() {
            if (!landlineField) return;

            var typeRadio = form.querySelector('[data-conn-type]:checked');
            var type = typeRadio ? typeRadio.value : '';
            var needed = type === 'DialUp' || type === 'Telephone';

            // A server-side rejection has to stay visible even if the current
            // type would normally hide the field.
            if (hasVisibleError('LandlineNumber')) needed = true;

            landlineField.hidden = !needed;
            if (landlineInput) landlineInput.required = needed;
        }

        function syncOrganisation() {
            if (!orgField) return;

            var on = (corporateToggle && corporateToggle.checked) || hasVisibleError('OrganisationName');
            orgField.hidden = !on;
            if (orgInput) orgInput.required = !!on;
        }

        /* ---------------- Password strength ---------------- */

        function strengthScore(password) {
            if (password.length < 8) return 1;

            var variety = 0;
            if (/[a-z]/.test(password)) variety++;
            if (/[A-Z]/.test(password)) variety++;
            if (/\d/.test(password)) variety++;
            if (/[^A-Za-z0-9]/.test(password)) variety++;

            if (variety >= 4 && password.length >= 12) return 4;
            if (variety >= 3) return 3;
            return 2;
        }

        function syncStrength() {
            if (!passwordInput) return;

            var meter = form.querySelector('[data-strength]');
            var barFill = form.querySelector('[data-strength-fill]');
            var label = form.querySelector('[data-strength-label]');
            if (!meter || !barFill || !label) return;

            if (!passwordInput.value) {
                meter.hidden = true;
                return;
            }

            var levels = {
                1: { width: '25%', colour: 'var(--nexus-danger)', text: 'Weak' },
                2: { width: '50%', colour: 'var(--nexus-warning)', text: 'Fair' },
                3: { width: '75%', colour: 'var(--nexus-blue)', text: 'Good' },
                4: { width: '100%', colour: 'var(--nexus-success)', text: 'Strong' }
            };

            var level = levels[strengthScore(passwordInput.value)];

            meter.hidden = false;
            barFill.style.width = level.width;
            barFill.style.background = level.colour;
            label.textContent = level.text;
            label.style.color = level.colour;
        }

        function syncSubmit() {
            if (!submitBtn || !termsBox) return;
            submitBtn.disabled = !termsBox.checked;
        }

        /* ---------------- Wiring ---------------- */

        form.addEventListener('change', function (e) {
            if (e.target.matches('[data-conn-type]')) {
                syncPlans();
                syncLandline();
                return;
            }

            if (e.target.matches('[data-plan-radio]')) {
                paintPlanSelection();
                syncSummary();
                return;
            }

            if (e.target.matches('input[name=BillingCycle]')) {
                syncCycle();
                return;
            }

            if (e.target.matches('[data-corporate-toggle]')) {
                syncOrganisation();
                return;
            }

            if (e.target.matches('[data-terms]')) {
                syncSubmit();
            }
        });

        if (passwordInput) passwordInput.addEventListener('input', syncStrength);

        syncPlans();
        syncLandline();
        syncOrganisation();
        syncCycle();
        syncStrength();
        syncSubmit();
        update();
    });

})();
