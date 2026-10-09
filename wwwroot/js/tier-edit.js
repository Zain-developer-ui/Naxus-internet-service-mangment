(function () {
    'use strict';

    var toggle = document.querySelector('[data-open-ended-toggle]');
    var maxInput = document.querySelector('[data-max-input]');
    var maxField = document.querySelector('[data-open-ended-field]');
    var percentInput = document.querySelector('[data-percent-input]');
    var preview = document.querySelector('[data-percent-preview]');

    if (toggle && maxInput && maxField) {
        var syncOpenEnded = function () {
            var open = toggle.checked;
            maxInput.disabled = open;
            maxField.style.opacity = open ? '0.45' : '1';
            if (open) {
                maxInput.value = '';
                maxInput.removeAttribute('required');
            }
        };

        toggle.addEventListener('change', syncOpenEnded);
        syncOpenEnded();
    }

    if (percentInput && preview) {
        var syncPercent = function () {
            var rate = parseInt(percentInput.value, 10);
            if (isNaN(rate) || rate < 1 || rate > 100) {
                preview.textContent = 'Enter a whole percent between 1 and 100.';
                return;
            }
            preview.textContent = 'A $100 order in this band pays $' + (100 - rate).toFixed(2) + '.';
        };

        percentInput.addEventListener('input', syncPercent);
        syncPercent();
    }
})();
