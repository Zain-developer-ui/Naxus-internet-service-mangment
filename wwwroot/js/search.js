/* =========================================================
   NEXUS — search.js
   Global client-side search / filter / sort / pagination
   for tables. Reads data from the DOM only (no backend).
   ========================================================= */

(function () {
    'use strict';
    const NEXUS = window.NEXUS;

    /* ---------------------------------------------------------
       Attach a search box to a table
       Markup:
         <input data-table-search="#tableId">
         <table id="tableId" data-searchable>
           <tr><td data-col="name">…</td></tr>
       --------------------------------------------------------- */
    function initTableSearch() {
        document.addEventListener('input', NEXUS.debounce((e) => {
            const input = e.target.closest('[data-table-search]');
            if (!input) return;
            const sel = input.getAttribute('data-table-search');
            const table = document.querySelector(sel);
            if (!table) return;

            const q = input.value.trim().toLowerCase();
            const rows = table.querySelectorAll('tbody tr');
            let visible = 0;

            rows.forEach(row => {
                const match = !q || row.textContent.toLowerCase().includes(q);
                row.style.display = match ? '' : 'none';
                if (match) visible++;
            });

            const empty = document.querySelector(table.getAttribute('data-empty-target') || '');
            if (empty) empty.style.display = visible === 0 ? 'block' : 'none';
        }, 180));
    }

    /* ---------------------------------------------------------
       Multi-select filter chips
       Markup: <select data-table-filter="#tableId" data-col="status">
       --------------------------------------------------------- */
    function initTableFilters() {
        document.addEventListener('change', (e) => {
            const sel = e.target.closest('[data-table-filter]');
            if (!sel) return;
            const table = document.querySelector(sel.getAttribute('data-table-filter'));
            if (!table) return;
            const col = sel.getAttribute('data-col');
            const val = sel.value.trim().toLowerCase();
            const rows = table.querySelectorAll('tbody tr');

            rows.forEach(row => {
                const cell = row.querySelector(`[data-col="${col}"]`);
                if (!cell) return;
                const match = !val || cell.textContent.trim().toLowerCase() === val;
                row.style.display = match ? '' : 'none';
            });
        });
    }

    /* ---------------------------------------------------------
       Sortable table headers
       Markup: <th data-sortable="name">Name</th>
       --------------------------------------------------------- */
    function initSortableTables() {
        document.addEventListener('click', (e) => {
            const th = e.target.closest('th[data-sortable]');
            if (!th) return;
            const table = th.closest('table');
            if (!table) return;
            const key = th.getAttribute('data-sortable');
            const tbody = table.querySelector('tbody');
            const rows = Array.from(tbody.querySelectorAll('tr'));

            const asc = th.dataset.order !== 'asc';
            th.dataset.order = asc ? 'asc' : 'desc';

            rows.sort((a, b) => {
                const av = a.querySelector(`[data-col="${key}"]`)?.textContent.trim() || '';
                const bv = b.querySelector(`[data-col="${key}"]`)?.textContent.trim() || '';
                const an = Number(av), bn = Number(bv);
                if (!isNaN(an) && !isNaN(bn) && av !== '' && bv !== '') {
                    return asc ? an - bn : bn - an;
                }
                return asc ? av.localeCompare(bv) : bv.localeCompare(av);
            });
            rows.forEach(r => tbody.appendChild(r));

            // Update visual indicator
            table.querySelectorAll('th[data-sortable]').forEach(t => {
                t.classList.remove('sorted-asc', 'sorted-desc');
            });
            th.classList.add(asc ? 'sorted-asc' : 'sorted-desc');
        });
    }

    /* ---------------------------------------------------------
       Client-side pagination
       Markup: <table data-paginate="10"> … plus
               <div class="pagination" data-pagination-target="#tableId"></div>
       --------------------------------------------------------- */
    function initPagination() {
        NEXUS.$$('table[data-paginate]').forEach(table => {
            const perPage = Number(table.getAttribute('data-paginate')) || 10;
            const targetSel = table.getAttribute('data-pagination-target');
            const container = targetSel ? document.querySelector(targetSel) : null;
            if (!container) return;

            const rows = Array.from(table.querySelectorAll('tbody tr'));
            const pages = Math.max(1, Math.ceil(rows.length / perPage));
            let page = 1;

            function render() {
                rows.forEach((row, i) => {
                    const visible = i >= (page - 1) * perPage && i < page * perPage;
                    row.style.display = visible ? '' : 'none';
                });

                container.innerHTML = '';
                const prev = document.createElement('a');
                prev.href = '#';
                prev.className = page === 1 ? 'disabled' : '';
                prev.innerHTML = '<i class="fa-solid fa-chevron-left"></i>';
                prev.addEventListener('click', e => { e.preventDefault(); if (page > 1) { page--; render(); } });
                container.appendChild(prev);

                for (let p = 1; p <= pages; p++) {
                    const a = document.createElement('a');
                    a.href = '#';
                    a.textContent = p;
                    a.className = p === page ? 'active' : '';
                    a.addEventListener('click', e => { e.preventDefault(); page = p; render(); });
                    container.appendChild(a);
                }

                const next = document.createElement('a');
                next.href = '#';
                next.className = page === pages ? 'disabled' : '';
                next.innerHTML = '<i class="fa-solid fa-chevron-right"></i>';
                next.addEventListener('click', e => { e.preventDefault(); if (page < pages) { page++; render(); } });
                container.appendChild(next);
            }

            render();
        });
    }

    /* ---------------------------------------------------------
       Read ?q= param and prefill the first search box
       --------------------------------------------------------- */
    function prefillFromQuery() {
        const params = new URLSearchParams(window.location.search);
        const q = params.get('q');
        if (!q) return;
        const input = NEXUS.$('[data-table-search]');
        if (input) {
            input.value = q;
            input.dispatchEvent(new Event('input', { bubbles: true }));
        }
    }

    NEXUS.ready(() => {
        initTableSearch();
        initTableFilters();
        initSortableTables();
        initPagination();
        prefillFromQuery();
    });

})();