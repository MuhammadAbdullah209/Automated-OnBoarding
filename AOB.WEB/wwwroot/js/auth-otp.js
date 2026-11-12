(function () {
    var group = document.querySelector('[data-otp-group]');
    var hidden = document.getElementById('otp-hidden');
    if (!group || !hidden) return;

    var inputs = Array.prototype.slice.call(group.querySelectorAll('input[data-otp-index]'));

    function syncHidden() {
        hidden.value = inputs.map(function (i) { return i.value; }).join('');
    }

    inputs.forEach(function (input, idx) {
        input.addEventListener('input', function () {
            input.value = input.value.replace(/\D/g, '').slice(-1);
            syncHidden();
            if (input.value && idx < inputs.length - 1) {
                inputs[idx + 1].focus();
            }
        });
        input.addEventListener('keydown', function (e) {
            if (e.key === 'Backspace' && !input.value && idx > 0) {
                inputs[idx - 1].focus();
            }
        });
        input.addEventListener('paste', function (e) {
            e.preventDefault();
            var text = (e.clipboardData || window.clipboardData).getData('text').replace(/\D/g, '').slice(0, 6);
            text.split('').forEach(function (ch, i) {
                if (inputs[i]) inputs[i].value = ch;
            });
            syncHidden();
            (inputs[Math.min(text.length, 5)] || inputs[0]).focus();
        });
    });

    var existing = hidden.value.replace(/\D/g, '');
    if (existing) {
        existing.split('').forEach(function (ch, i) {
            if (inputs[i]) inputs[i].value = ch;
        });
    }
})();
