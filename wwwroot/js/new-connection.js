/* =========================================================
   NEXUS — new-connection.js
   Multi-step form for /Orders/New
   Step 1,2,3: [Previous] [Cancel] [Next]
   Step 4    : [Previous] [Cancel] [Submit Order]
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

        var total = panels.length;
        var index = 0;

        console.log('[NEXUS] Form loaded with ' + total + ' steps');

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
                if (isLastStep) {
                    nextBtn.style.display = 'none';
                    nextBtn.setAttribute('hidden', 'hidden');
                } else {
                    nextBtn.style.display = '';
                    nextBtn.removeAttribute('hidden');
                }
            }

            if (submitBtn) {
                if (isLastStep) {
                    submitBtn.style.display = '';
                    submitBtn.removeAttribute('hidden');
                } else {
                    submitBtn.style.display = 'none';
                    submitBtn.setAttribute('hidden', 'hidden');
                }
            }

            if (prevBtn) {
                prevBtn.disabled = (index === 0);
            }

            form.setAttribute('data-step', String(index + 1));
            console.log('[NEXUS] Switched to step ' + (index + 1));

            var rect = form.getBoundingClientRect();
            var scrollTop = window.scrollY || document.documentElement.scrollTop || 0;
            var top = rect.top + scrollTop - 100;
            window.scrollTo({ top: top, behavior: 'smooth' });
        }

        function currentPanelValid() {
            var panel = panels[index];
            var fields = panel.querySelectorAll('input, select, textarea');
            var valid = true;

            for (var i = 0; i < fields.length; i++) {
                var f = fields[i];
                if (f.type === 'button' || f.type === 'submit' || f.type === 'radio') continue;

                f.classList.remove('input-error');
                var err = f.parentElement.querySelector('.field-error');
                if (err) err.textContent = '';

                if (f.hasAttribute('required') && f.value.trim() === '') {
                    f.classList.add('input-error');
                    if (err) err.textContent = 'This field is required';
                    valid = false;
                    continue;
                }

                if (f.type === 'email' && f.value) {
                    var emailPattern = /^[^\s]+@[^\s]+\.[^\s]+$/;
                    if (!emailPattern.test(f.value)) {
                        f.classList.add('input-error');
                        if (err) err.textContent = 'Enter a valid email';
                        valid = false;
                    }
                }

                if (f.type === 'tel' && f.value) {
                    var phonePattern = /^03\d{9}$/;
                    if (!phonePattern.test(f.value)) {
                        f.classList.add('input-error');
                        if (err) err.textContent = 'Format: 03001234567';
                        valid = false;
                    }
                }

                if (f.name === 'Cnic' && f.value) {
                    var cnicPattern = /^\d{5}-\d{7}-\d$/;
                    if (!cnicPattern.test(f.value)) {
                        f.classList.add('input-error');
                        if (err) err.textContent = 'Format: 35202-1234567-1';
                        valid = false;
                    }
                }
            }

            if (index === 0) {
                var planSelected = panel.querySelector('input[name=PlanName]:checked');
                if (!planSelected) {
                    valid = false;
                    if (window.NEXUS && NEXUS.toast) {
                        NEXUS.toast('Please select a plan', 'warning', 2500);
                    }
                }
            }

            return valid;
        }

        form.addEventListener('click', function (e) {
            var nextClicked = e.target.closest('[data-step-next]');
            var prevClicked = e.target.closest('[data-step-prev]');

            if (nextClicked) {
                e.preventDefault();
                if (!currentPanelValid()) {
                    if (window.NEXUS && NEXUS.toast) {
                        NEXUS.toast('Please complete the current step', 'warning', 2500);
                    }
                    return;
                }
                if (index < total - 1) {
                    index++;
                    update();
                }
            }

            if (prevClicked) {
                e.preventDefault();
                if (index > 0) {
                    index--;
                    update();
                }
            }
        });

        form.addEventListener('submit', function (e) {
            e.preventDefault();
            e.stopImmediatePropagation();

            if (!currentPanelValid()) {
                if (window.NEXUS && NEXUS.toast) {
                    NEXUS.toast('Please complete all required fields', 'warning', 2500);
                }
                return;
            }

            if (window.NEXUS && NEXUS.saveSubmission) {
                var data = {};
                new FormData(form).forEach(function (value, key) {
                    data[key] = value;
                });
                NEXUS.saveSubmission('New Connection', data);
            }

            var modal = document.getElementById('orderSuccess');
            if (modal) {
                if (window.NEXUS && NEXUS.modal) {
                    NEXUS.modal.open(modal);
                } else {
                    modal.classList.add('open');
                    document.body.style.overflow = 'hidden';
                }
            }

            if (window.NEXUS && NEXUS.toast) {
                NEXUS.toast({
                    title: 'Order Submitted',
                    message: 'Your NEXUS connection request has been received.',
                    type: 'success',
                    duration: 4000
                });
            }
        }, true);

        function syncReview() {
            var reviews = document.querySelectorAll('[data-review]');
            for (var i = 0; i < reviews.length; i++) {
                var el = reviews[i];
                var name = el.getAttribute('data-review');
                var field = form.querySelector('[name=' + name + ']');
                if (field) {
                    var val = (field.value || '').trim();
                    el.textContent = val || '—';
                }
            }
        }
        form.addEventListener('input', syncReview);
        form.addEventListener('change', syncReview);
        syncReview();

        var planRadios = form.querySelectorAll('input[name=PlanName]');
        for (var r = 0; r < planRadios.length; r++) {
            (function (radio) {
                radio.addEventListener('change', function () {
                    var speedField = document.getElementById('Speed');
                    if (speedField) {
                        speedField.value = '15 Mbps';
                    }

                    var planSummaries = document.querySelectorAll('[data-summary=plan]');
                    for (var k = 0; k < planSummaries.length; k++) {
                        planSummaries[k].textContent = radio.value + ' Plan';
                    }
                });
            })(planRadios[r]);
        }

        update();

        setTimeout(function () {
            if (window.NEXUS && NEXUS.toast) {
                NEXUS.toast({
                    title: 'Ready to Get Connected',
                    message: 'Complete the form to place your order.',
                    type: 'info',
                    duration: 3500
                });
            }
        }, 800);

    });

})();