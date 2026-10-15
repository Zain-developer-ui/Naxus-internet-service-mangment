/* =========================================================
   NEXUS — forms.js
   Multi-step forms, password toggle, file drop, submit state,
   radio/checkbox group interactions, feedback stars.
   ========================================================= */

(function () {
    'use strict';
    const NEXUS = window.NEXUS;

    /* ---------------------------------------------------------
       Multi-step forms live in new-connection.js. It owns the
       order wizard on its own, including the plan filtering and
       the landline and organisation rules. A second copy of the
       step machine here drove the same form from a second set of
       indices, so the two disagreed about which step was open.
       --------------------------------------------------------- */

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
                 <input type="file" hidden> </label>
       --------------------------------------------------------- */
    /* ---------------------------------------------------------
       Input masks
       Phone, CNIC and the landline all have to carry their dashes
       and the shape is fixed, so the separators are inserted as the
       digits are typed. The group sizes are read out of the field's
       own data-val-regex-pattern, which is the same pattern the
       server validates against, so the two cannot disagree.
       --------------------------------------------------------- */
    function digitGroups(pattern) {
        const groups = [];
        let run = 0;
        let i = 0;

        while (i < pattern.length) {
            const ch = pattern[i];

            if (ch === '\\' && pattern[i + 1] === 'd') {
                const counted = /^\\d\{(\d+)\}/.exec(pattern.slice(i));
                if (counted) {
                    run += Number(counted[1]);
                    i += counted[0].length;
                    continue;
                }
                run += 1;
                i += 2;
                continue;
            }

            if (ch >= '0' && ch <= '9') { run += 1; i += 1; continue; }
            if (ch === '-') { groups.push(run); run = 0; i += 1; continue; }

            i += 1;
        }

        groups.push(run);
        return groups.filter(n => n > 0);
    }

    function applyMask(input, groups) {
        const max = groups.reduce((a, b) => a + b, 0);

        input.addEventListener('input', () => {
            const digits = input.value.replace(/\D/g, '').slice(0, max);
            let out = '';
            let at = 0;

            groups.forEach((size, index) => {
                if (at >= digits.length) return;
                if (index > 0) out += '-';
                out += digits.slice(at, at + size);
                at += size;
            });

            if (out !== input.value) input.value = out;
        });
    }

    function initInputMasks() {
        NEXUS.$$('input[data-val-regex-pattern]').forEach(input => {
            const groups = digitGroups(input.getAttribute('data-val-regex-pattern'));
            if (groups.length > 1) applyMask(input, groups);
        });
    }

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
        initInputMasks();
        initPasswordToggle();
        initFileDrop();
        initSubmitLoading();
        initDuplicateGuard();
        initStarRatings();
        initChoiceCards();
        initInputFocusHighlight();
    });

})();