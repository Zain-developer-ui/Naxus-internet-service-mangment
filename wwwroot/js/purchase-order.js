/**
 * Purchase order line builder. The server re-validates every row, so this
 * only handles the conveniences: add a line, pull the cost from the product
 * catalogue, and keep the running total honest.
 */
(function () {
    'use strict';

    var form = document.getElementById('po-form');
    if (!form) return;

    var body = document.getElementById('po-line-body');
    var template = document.getElementById('po-line-template');
    var totalCell = document.getElementById('po-total');
    var currency = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' });

    function reindex() {
        var rows = body.querySelectorAll('.po-line');
        rows.forEach(function (row, index) {
            row.querySelectorAll('[name]').forEach(function (field) {
                field.name = field.name.replace(/Lines\[\d+\]/, 'Lines[' + index + ']');
            });
        });
    }

    function lineAmount(row) {
        var qty = parseFloat(row.querySelector('.po-qty').value);
        var cost = parseFloat(row.querySelector('.po-cost').value);
        if (isNaN(qty) || isNaN(cost)) return 0;
        return qty * cost;
    }

    function recalc() {
        var total = 0;
        body.querySelectorAll('.po-line').forEach(function (row) {
            var amount = lineAmount(row);
            total += amount;
            row.querySelector('.po-amount').textContent = currency.format(amount);
        });
        totalCell.textContent = currency.format(total);
    }

    function syncCost(row) {
        var select = row.querySelector('.po-product');
        var cost = row.querySelector('.po-cost');
        var option = select.options[select.selectedIndex];
        var suggested = option ? option.getAttribute('data-cost') : null;

        if (suggested !== null && (!cost.value || cost.dataset.auto !== 'false')) {
            cost.value = parseFloat(suggested).toFixed(2);
        }
    }

    function addLine() {
        var row = template.content.firstElementChild.cloneNode(true);
        body.appendChild(row);
        reindex();
        recalc();
    }

    form.addEventListener('click', function (event) {
        var add = event.target.closest('#po-add-line');
        if (add) {
            event.preventDefault();
            addLine();
            return;
        }

        var remove = event.target.closest('.po-remove');
        if (remove) {
            event.preventDefault();
            var row = remove.closest('.po-line');
            if (body.querySelectorAll('.po-line').length > 1) {
                row.remove();
                reindex();
                recalc();
            }
        }
    });

    form.addEventListener('change', function (event) {
        var select = event.target.closest('.po-product');
        if (!select) return;
        syncCost(select.closest('.po-line'));
        recalc();
    });

    form.addEventListener('input', function (event) {
        var cost = event.target.closest('.po-cost');
        if (cost) cost.dataset.auto = 'false';
        if (event.target.closest('.po-qty') || cost) recalc();
    });

    if (!body.querySelector('.po-line')) {
        addLine();
    } else {
        recalc();
    }
})();
