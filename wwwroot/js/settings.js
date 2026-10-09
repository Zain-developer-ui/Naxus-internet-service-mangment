/*
 * Settings screen. The only behaviour here is the colour swatch: an admin
 * picking a hex value should see it before saving, not after.
 */
(function () {
    'use strict';

    var HEX = /^#([0-9a-f]{3}|[0-9a-f]{6})$/i;

    function initSwatches() {
        document.querySelectorAll('[data-color-input]').forEach(function (input) {
            var swatch = input.closest('.color-field')?.querySelector('[data-swatch]');
            if (!swatch) return;

            input.addEventListener('input', function () {
                var value = input.value.trim();
                if (!HEX.test(value)) return;
                swatch.style.background = value;
            });
        });
    }

    document.addEventListener('DOMContentLoaded', initSwatches);
})();
