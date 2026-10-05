/* =========================================================
   NEXUS — bills.js
   Bill search, filters, payment modal, mock payment flow,
   invoice print/download stubs.
   ========================================================= */

(function () {
    'use strict';
    const NEXUS = window.NEXUS;

    /* ---------------------------------------------------------
       Bill filter (status + search text)
       --------------------------------------------------------- */
    function initBillFilters() {
        const filterForm = NEXUS.$('[data-bill-filter]');
        if (!filterForm) return;

        const apply = () => {
            const q = (filterForm.querySelector('[name="q"]')?.value || '').toLowerCase();
            const status = (filterForm.querySelector('[name="status"]')?.value || '').toLowerCase();
            const rows = document.querySelectorAll('[data-bills-table] tbody tr');

            rows.forEach(row => {
                const text = row.textContent.toLowerCase();
                const rowStatus = (row.getAttribute('data-bill-status') || '').toLowerCase();
                const matchQ = !q || text.includes(q);
                const matchStatus = !status || rowStatus === status;
                row.style.display = (matchQ && matchStatus) ? '' : 'none';
            });

            // Toggle empty state
            const empty = document.querySelector('[data-bills-empty]');
            const anyVisible = Array.from(rows).some(r => r.style.display !== 'none');
            if (empty) empty.style.display = anyVisible ? 'none' : 'block';
        };

        filterForm.addEventListener('submit', e => { e.preventDefault(); apply(); });
        filterForm.addEventListener('input', NEXUS.debounce(apply, 200));
        filterForm.addEventListener('change', apply);
    }

    /* ---------------------------------------------------------
       Payment form inside modal
       Markup: form[data-payment-form] with [name=amount] etc.
       --------------------------------------------------------- */
    function initPaymentForm() {
        const form = NEXUS.$('form[data-payment-form]');
        if (!form) return;

        const amount = form.querySelector('[name="amount"]');
        const method = form.querySelector('[name="method"]');
        const btn = form.querySelector('button[type=submit]');

        form.addEventListener('submit', (e) => {
            e.preventDefault();

            const v = Number(amount?.value || 0);
            if (!v || v <= 0) {
                NEXUS.toast('Please enter a valid payment amount.', 'warning');
                amount?.focus();
                return;
            }
            if (!method?.value) {
                NEXUS.toast('Please choose a payment method.', 'warning');
                method?.focus();
                return;
            }

            // Loading state
            const original = btn?.innerHTML;
            if (btn) {
                btn.disabled = true;
                btn.innerHTML = '<span class="loading-spinner" style="width:16px;height:16px;border-width:2px;"></span> Processing…';
            }

            // Simulate processing delay then success toast + close
            setTimeout(() => {
                if (btn) { btn.disabled = false; btn.innerHTML = original; }
                NEXUS.toast('Payment processed successfully (demo).', 'success');
                const backdrop = form.closest('.modal-backdrop');
                if (backdrop) NEXUS.modal.close(backdrop);
            }, 1200);
        });
    }

    /* ---------------------------------------------------------
       Print / download invoice stubs
       --------------------------------------------------------- */
    function initInvoiceActions() {
        document.addEventListener('click', (e) => {
            const print = e.target.closest('[data-print-invoice]');
            const dl = e.target.closest('[data-download-invoice]');
            if (print) {
                e.preventDefault();
                window.print();
            }
            if (dl) {
                e.preventDefault();
                NEXUS.toast('Invoice downloaded (demo).', 'success');
            }
        });
    }

    /* ---------------------------------------------------------
       Mark "Pay Now" buttons — copy invoice amount into the
       payment modal, then open it.
       --------------------------------------------------------- */
    function initPayButtons() {
        document.addEventListener('click', (e) => {
            const btn = e.target.closest('[data-pay-now]');
            if (!btn) return;
            const amount = btn.getAttribute('data-amount');
            const billNo = btn.getAttribute('data-bill-no');

            const amountInput = document.querySelector('form[data-payment-form] [name="amount"]');
            const billInput = document.querySelector('form[data-payment-form] [name="billNo"]');
            if (amountInput && amount) amountInput.value = amount;
            if (billInput && billNo) billInput.value = billNo;
        });
    }

    /* ---------------------------------------------------------
       Status badge helper for bill tables
       --------------------------------------------------------- */
    function initBillStatusBadges() {
        document.querySelectorAll('tr[data-bill-status]').forEach(row => {
            const status = (row.getAttribute('data-bill-status') || '').toLowerCase();
            const cell = row.querySelector('[data-col="status"]');
            if (!cell || cell.querySelector('.badge')) return;

            const cls = status === 'paid' ? 'badge-success'
                : status === 'due' ? 'badge-warning'
                    : status === 'overdue' ? 'badge-danger'
                        : 'badge-muted';
            const label = status.charAt(0).toUpperCase() + status.slice(1);
            cell.innerHTML = `<span class="badge ${cls}">${label}</span>`;
        });
    }

    NEXUS.ready(() => {
        initBillFilters();
        initPaymentForm();
        initInvoiceActions();
        initPayButtons();
        initBillStatusBadges();
    });

})();