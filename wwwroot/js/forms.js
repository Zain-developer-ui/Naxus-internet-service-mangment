/* =========================================================
   NEXUS — forms.js
   Multi-step forms, password toggle, file drop, submit state,
   radio/checkbox group interactions, feedback stars.
   ========================================================= */

(function () {
    'use strict';
    const NEXUS = window.NEXUS;

    /* ---------------------------------------------------------
       Multi-step form
       Markup: <form data-step-form> with .step-panel and
       .step-progress containing .step elements + .step-bar-fill.
       --------------------------------------------------------- */
    function initStepForms() {
        NEXUS.$$('form[data-step-form]').forEach(form => {
            const panels = Array.from(form.querySelectorAll('.step-panel'));
            const steps = Array.from(form.querySelectorAll('.step-progress .step'));
            const fill = form.querySelector('.step-bar-fill');
            if (!panels.length) return;

            let index = 0;
            const total = panels.length;

            function update() {
                panels.forEach((p, i) => p.classList.toggle('active', i === index));
                steps.forEach((s, i) => {
                    s.classList.toggle('active', i === index);
                    s.classList.toggle('done', i < index);
                });
                if (fill) {
                    // 5% margin on each side matches .step-progress::before
                    const pct = total === 1 ? 0 : (index / (total - 1)) * 90;
                    fill.style.width = pct + '%';
                }
                form.setAttribute('data-step', String(index + 1));
                window.scrollTo({ top: form.offsetTop - 90, behavior: 'smooth' });
            }

            function currentPanelValid() {
                const panel = panels[index];
                const fields = Array.from(panel.querySelectorAll('input, select, textarea'))
                    .filter(f => f.type !== 'button' && f.type !== 'submit');
                let ok = true;
                fields.forEach(f => {
                    const v = NEXUS.validation.validateField(f);
                    if (!v) ok = false;
                });
                return ok;
            }

            // Buttons
            form.addEventListener('click', (e) => {
                const next = e.target.closest('[data-step-next]');
                const prev = e.target.closest('[data-step-prev]');

                if (next) {
                    e.preventDefault();
                    if (!currentPanelValid()) {
                        NEXUS.toast('Please complete the current step.', 'warning');
                        return;
                    }
                    if (index < total - 1) { index++; update(); }
                }
                if (prev) {
                    e.preventDefault();
                    if (index > 0) { index--; update(); }
                }
            });

            update();
        });
    }

    /* ---------------------------------------------------------
       Password visibility toggle
       Markup: <button data-password-toggle="#inputId">
       --------------------------------------------------------- */
    function initPasswordToggle() {
        document.addEventListener('click', (e) => {
            const btn = e.target.closest('[data-password-toggle]');
            if (!btn) return;
            e.preventDefault();
            const sel = btn.getAttribute('data-password-toggle');
            const input = document.querySelector(sel);
            if (!input) return;
            const show = input.type === 'password';
            input.type = show ? 'text' : 'password';
            btn.innerHTML = show
                ? '<i class="fa-solid fa-eye-slash"></i>'
                : '<i class="fa-solid fa-eye"></i>';
        });
    }

    /* ---------------------------------------------------------
       File drop zones
       Markup: <label class="file-drop" data-file-drop>
                 <input type="file" hidden>
               </label>
       --------------------------------------------------------- */
    function initFileDrop() {
        NEXUS.$$('[data-file-drop]').forEach(drop => {
            const input = drop.querySelector('input[type=file]');
            if (!input) return;

            const label = drop.querySelector('[data-file-label]');

            ['dragenter', 'dragover'].forEach(evt =>
                drop.addEventListener(evt, (e) => {
                    e.preventDefault();
                    drop.classList.add('dragover');
                })
            );
            ['dragleave', 'drop'].forEach(evt =>
                drop.addEventListener(evt, (e) => {
                    e.preventDefault();
                    drop.classList.remove('dragover');
                })
            );
            drop.addEventListener('drop', (e) => {
                if (e.dataTransfer && e.dataTransfer.files.length) {
                    input.files = e.dataTransfer.files;
                    updateLabel();
                }
            });
            input.addEventListener('change', updateLabel);

            function updateLabel() {
                const f = input.files[0];
                if (label) label.textContent = f ? f.name : 'No file chosen';
            }
        });
    }

    /* ---------------------------------------------------------
       Submit button loading state
       Markup: <form data-loading-submit>
       --------------------------------------------------------- */
    function initSubmitLoading() {
        document.addEventListener('submit', (e) => {
            const form = e.target;
            if (!form.hasAttribute('data-loading-submit')) return;
            const btn = form.querySelector('button[type=submit], input[type=submit]');
            if (!btn) return;
            btn.dataset.originalText = btn.innerHTML;
            btn.disabled = true;
            btn.innerHTML = '<span class="loading-spinner" style="width:18px;height:18px;border-width:2px;"></span> Please wait…';
        }, true);
    }

    /* ---------------------------------------------------------
       Prevent duplicate submit if user navigates away / back
       --------------------------------------------------------- */
    function initDuplicateGuard() {
        document.addEventListener('submit', (e) => {
            const form = e.target;
            if (form.dataset.submitted === 'true') {
                e.preventDefault();
                NEXUS.toast('This form was already submitted.', 'info');
                return;
            }
            form.dataset.submitted = 'true';
            // Release lock if user hits validation and stays on page
            setTimeout(() => { form.dataset.submitted = 'false'; }, 8000);
        }, true);
    }

    /* ---------------------------------------------------------
       Star rating widgets
       Markup: <div class="star-rating" data-star-rating="overallRating">
               <i data-star="1"></i> … <i data-star="5"></i>
               <input type="hidden" name="OverallRating">
       --------------------------------------------------------- */
    function initStarRatings() {
        NEXUS.$$('[data-star-rating]').forEach(widget => {
            const stars = widget.querySelectorAll('[data-star]');
            const input = widget.querySelector('input[type=hidden]');

            function render(n) {
                stars.forEach(s => {
                    const v = Number(s.getAttribute('data-star'));
                    s.classList.toggle('fa-solid', v <= n);
                    s.classList.toggle('fa-regular', v > n);
                    s.style.color = v <= n ? '#f59e0b' : '#cbd5e1';
                });
            }

            stars.forEach(s => {
                const val = Number(s.getAttribute('data-star'));
                s.style.cursor = 'pointer';
                s.style.fontSize = '1.6rem';
                s.style.transition = 'color .15s ease, transform .15s ease';

                s.addEventListener('mouseenter', () => render(val));
                s.addEventListener('mouseleave', () => render(Number(input?.value) || 0));
                s.addEventListener('click', () => {
                    if (input) input.value = val;
                    render(val);
                });
            });

            render(Number(input?.value) || 0);
        });
    }

    /* ---------------------------------------------------------
       Radio-based cards (tabs to update plan choice)
       --------------------------------------------------------- */
    function initChoiceCards() {
        document.addEventListener('change', (e) => {
            const r = e.target;
            if (r.type !== 'radio') return;
            const name = r.name;
            NEXUS.$$(`input[name="${name}"]`).forEach(i => {
                const card = i.closest('.choice-card');
                if (card) card.classList.toggle('selected', i.checked);
            });
        });
    }

    /* ---------------------------------------------------------
       Simple select/input enhancements for touch
       --------------------------------------------------------- */
    function initInputFocusHighlight() {
        document.addEventListener('focusin', (e) => {
            const f = e.target.closest('.form-group');
            if (f) f.classList.add('focused');
        });
        document.addEventListener('focusout', (e) => {
            const f = e.target.closest('.form-group');
            if (f) f.classList.remove('focused');
        });
    }

    NEXUS.ready(() => {
        initStepForms();
        initPasswordToggle();
        initFileDrop();
        initSubmitLoading();
        initDuplicateGuard();
        initStarRatings();
        initChoiceCards();
        initInputFocusHighlight();
    });

})();