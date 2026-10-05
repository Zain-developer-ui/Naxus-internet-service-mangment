/* =========================================================
   NEXUS — charts.js
   Chart.js wrappers. Every function defensively checks that
   the target canvas exists before creating a chart.
   Uses global NEXUS_CHART_DATA (defined on dashboard pages)
   and falls back to built-in demo data.
   ========================================================= */

(function () {
    'use strict';
    const NEXUS = window.NEXUS;

    /* Shared theme */
    const THEME = {
        primary: '#0a2540',
        secondary: '#1e88e5',
        accent: '#38bdf8',
        success: '#16a34a',
        warning: '#d97706',
        danger: '#dc2626',
        muted: '#64748b',
        grid: 'rgba(100,116,139,.12)'
    };

    // Register sensible defaults
    if (window.Chart) {
        Chart.defaults.font.family = "'Inter', sans-serif";
        Chart.defaults.color = THEME.muted;
        Chart.defaults.borderColor = THEME.grid;
        Chart.defaults.responsive = true;
        Chart.defaults.maintainAspectRatio = false;
    }

    function has(canvasId) {
        return !!document.getElementById(canvasId) && !!window.Chart;
    }

    // Read data source: prefer window.NEXUS_CHART_DATA
    function data(key, fallback) {
        const src = window.NEXUS_CHART_DATA || {};
        return src[key] || fallback;
    }

    /* ---------------------------------------------------------
       Line chart (revenue, customers, usage)
       --------------------------------------------------------- */
    NEXUS.lineChart = function (canvasId, opts = {}) {
        if (!has(canvasId)) return null;
        const ctx = document.getElementById(canvasId);

        const labels = opts.labels || data('months', ['Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep']);
        const values = opts.values || data(opts.dataKey || 'revenue', [180000, 195000, 210000, 225000, 240000, 258000]);
        const color = opts.color || THEME.secondary;

        return new Chart(ctx, {
            type: 'line',
            data: {
                labels,
                datasets: [{
                    label: opts.label || 'Value',
                    data: values,
                    borderColor: color,
                    backgroundColor: color + '22',
                    fill: true,
                    tension: 0.35,
                    pointRadius: 4,
                    pointBackgroundColor: '#fff',
                    pointBorderColor: color,
                    pointBorderWidth: 2,
                    borderWidth: 3
                }]
            },
            options: {
                plugins: {
                    legend: { display: opts.showLegend || false },
                    tooltip: {
                        backgroundColor: THEME.primary,
                        titleColor: '#fff',
                        bodyColor: '#fff',
                        padding: 12,
                        cornerRadius: 8,
                        displayColors: false
                    }
                },
                scales: {
                    x: { grid: { display: false }, ticks: { color: THEME.muted } },
                    y: {
                        grid: { color: THEME.grid },
                        ticks: {
                            color: THEME.muted,
                            callback: v => opts.yPrefix ? opts.yPrefix + v.toLocaleString() : v
                        }
                    }
                }
            }
        });
    };

    /* ---------------------------------------------------------
       Bar chart (orders, bills per month)
       --------------------------------------------------------- */
    NEXUS.barChart = function (canvasId, opts = {}) {
        if (!has(canvasId)) return null;
        const ctx = document.getElementById(canvasId);

        const labels = opts.labels || data('months', ['Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep']);
        const values = opts.values || data(opts.dataKey || 'orders', [22, 28, 32, 26, 35, 42]);
        const colors = opts.colors || values.map((_, i) =>
            i === values.length - 1 ? THEME.secondary : THEME.accent
        );

        return new Chart(ctx, {
            type: 'bar',
            data: {
                labels,
                datasets: [{
                    label: opts.label || 'Value',
                    data: values,
                    backgroundColor: colors,
                    borderRadius: 8,
                    maxBarThickness: 42
                }]
            },
            options: {
                plugins: {
                    legend: { display: false },
                    tooltip: { backgroundColor: THEME.primary, padding: 12, cornerRadius: 8, displayColors: false }
                },
                scales: {
                    x: { grid: { display: false } },
                    y: { beginAtZero: true, grid: { color: THEME.grid } }
                }
            }
        });
    };

    /* ---------------------------------------------------------
       Doughnut (service distribution, payment status)
       --------------------------------------------------------- */
    NEXUS.doughnutChart = function (canvasId, opts = {}) {
        if (!has(canvasId)) return null;
        const ctx = document.getElementById(canvasId);

        const labels = opts.labels || ['Broadband', 'Fiber', 'Wireless', 'Dedicated'];
        const values = opts.values || data(opts.dataKey || 'distribution', [540, 260, 120, 65]);
        const colors = opts.colors || [THEME.secondary, THEME.accent, THEME.success, THEME.warning];

        return new Chart(ctx, {
            type: 'doughnut',
            data: {
                labels,
                datasets: [{
                    data: values,
                    backgroundColor: colors,
                    borderWidth: 0,
                    hoverOffset: 6
                }]
            },
            options: {
                cutout: '68%',
                plugins: {
                    legend: {
                        position: 'bottom',
                        labels: { padding: 16, usePointStyle: true, pointStyle: 'circle' }
                    },
                    tooltip: { backgroundColor: THEME.primary, padding: 12, cornerRadius: 8 }
                }
            }
        });
    };

    /* ---------------------------------------------------------
       Area chart (usage)
       --------------------------------------------------------- */
    NEXUS.areaChart = function (canvasId, opts = {}) {
        if (!has(canvasId)) return null;
        const ctx = document.getElementById(canvasId);
        const labels = opts.labels || data('months', ['Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep']);
        const values = opts.values || data('usage', [120, 145, 138, 172, 190, 205]);

        const g = ctx.getContext('2d');
        const gradient = g.createLinearGradient(0, 0, 0, 300);
        gradient.addColorStop(0, 'rgba(56,189,248,.5)');
        gradient.addColorStop(1, 'rgba(56,189,248,0)');

        return new Chart(ctx, {
            type: 'line',
            data: {
                labels,
                datasets: [{
                    label: opts.label || 'Usage (GB)',
                    data: values,
                    borderColor: THEME.accent,
                    backgroundColor: gradient,
                    fill: true,
                    tension: 0.4,
                    pointRadius: 0,
                    borderWidth: 3
                }]
            },
            options: {
                plugins: {
                    legend: { display: false },
                    tooltip: { backgroundColor: THEME.primary, padding: 12, cornerRadius: 8, displayColors: false }
                },
                scales: {
                    x: { grid: { display: false } },
                    y: { beginAtZero: true, grid: { color: THEME.grid } }
                }
            }
        });
    };

    /* ---------------------------------------------------------
       Pie chart
       --------------------------------------------------------- */
    NEXUS.pieChart = function (canvasId, opts = {}) {
        if (!has(canvasId)) return null;
        const ctx = document.getElementById(canvasId);
        const labels = opts.labels || ['Paid', 'Due', 'Overdue'];
        const values = opts.values || data(opts.dataKey || 'paymentStatus', [720, 60, 20]);
        const colors = opts.colors || [THEME.success, THEME.warning, THEME.danger];

        return new Chart(ctx, {
            type: 'pie',
            data: { labels, datasets: [{ data: values, backgroundColor: colors, borderWidth: 0 }] },
            options: {
                plugins: {
                    legend: { position: 'bottom', labels: { padding: 16, usePointStyle: true, pointStyle: 'circle' } },
                    tooltip: { backgroundColor: THEME.primary, padding: 12, cornerRadius: 8 }
                }
            }
        });
    };

    /* ---------------------------------------------------------
       Auto-init based on [data-chart] attribute
       ---------------------------------------------------------
       Markup example:
       <canvas id="revenueChart" data-chart="line"
               data-label="Revenue" data-key="revenue" data-prefix="Rs. "></canvas>
       --------------------------------------------------------- */
    function autoInit() {
        if (!window.Chart) return;

        NEXUS.$$('canvas[data-chart]').forEach(canvas => {
            const type = canvas.getAttribute('data-chart');
            const id = canvas.id;
            const opts = {
                label: canvas.getAttribute('data-label') || undefined,
                dataKey: canvas.getAttribute('data-key') || undefined,
                yPrefix: canvas.getAttribute('data-prefix') || '',
                showLegend: canvas.hasAttribute('data-legend')
            };
            switch (type) {
                case 'line': NEXUS.lineChart(id, opts); break;
                case 'area': NEXUS.areaChart(id, opts); break;
                case 'bar': NEXUS.barChart(id, opts); break;
                case 'doughnut': NEXUS.doughnutChart(id, opts); break;
                case 'pie': NEXUS.pieChart(id, opts); break;
            }
        });
    }

    NEXUS.ready(autoInit);

})();