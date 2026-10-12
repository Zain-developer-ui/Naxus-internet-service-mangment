/*
 * Plan edit form behaviour. The pricing table is the only non-obvious part -
 * an operator wants to see a yearly figure broken down per month while they
 * type, which is the number the plan is actually compared on.
 */
(function () {
    'use strict';

    var MONTHS = { monthly: 1, halfyearly: 6, yearly: 12 };

    function cycleKey(select) {
        var v = (select.value || '').toLowerCase();
        return MONTHS[v] ? v : null;
    }

    function initUnlimitedToggle() {
        var toggle = document.querySelector('[data-unlimited-toggle]');
        var hoursField = document.querySelector('[data-hours-field]');
        if (!toggle || !hoursField) return;

        var hoursInput = hoursField.querySelector('input');

        function sync() {
            var unlimited = toggle.checked;
            hoursField.hidden = unlimited;
            if (unlimited && hoursInput) hoursInput.value = '';
        }

        toggle.addEventListener('change', sync);
        sync();
    }

    function initPricing() {
        var rows = document.querySelectorAll('[data-rate-input]');
        if (!rows.length) return;

        function recalc() {
            rows.forEach(function (input) {
                var row = input.closest('tr');
                if (!row) return;

                var cycleInput = row.querySelector('input[name$=".Cycle"]');
                var offered = row.querySelector('[data-cycle-toggle]');
                var target = row.querySelector('[data-per-month]');
                if (!cycleInput || !target) return;

                var key = cycleKey(cycleInput);
                var amount = parseFloat(input.value);
                var on = offered ? offered.checked : true;

                if (!on || isNaN(amount) || amount <= 0) {
                    target.textContent = '—';
                    return;
                }

                var divisor = key ? MONTHS[key] : 1;
                var perMonth = amount / divisor;
                target.textContent = '$' + perMonth.toFixed(2);
            });
        }

        rows.forEach(function (input) {
            input.addEventListener('input', recalc);
            var row = input.closest('tr');
            var offered = row ? row.querySelector('[data-cycle-toggle]') : null;
            if (offered) offered.addEventListener('change', recalc);
        });

        recalc();
    }

    function initTariffField() {
        var group = document.querySelector('[data-tariff-group]');
        var radios = document.querySelectorAll('[data-conn-type]');
        if (!group || !radios.length) return;

        var select = group.querySelector('select');

        // Mirrors the server rule: only the exchange-metered line types carry a
        // call tariff, so the control is hidden rather than left to confuse.
        var METERED = ['Telephone', 'DialUp'];

        function sync() {
            var checked = document.querySelector('[data-conn-type]:checked');
            var metered = checked && METERED.indexOf(checked.value) !== -1;

            group.hidden = !metered;
            if (!metered && select) select.value = '';
        }

        radios.forEach(function (radio) {
            radio.addEventListener('change', sync);
        });

        sync();
    }

    document.addEventListener('DOMContentLoaded', function () {
        initUnlimitedToggle();
        initPricing();
        initTariffField();
    });
})();
