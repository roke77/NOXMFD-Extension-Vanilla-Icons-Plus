// VANILLA ICONS PLUS settings page. Entirely generic over the .vip-row markup in
// vanilla-icons-plus.html — each row's data-key/data-kind is the only place a setting is named;
// this file never mentions one by name. Polls state.json every REFRESH_MS so a change made
// elsewhere (ConfigurationManager's own F9 menu) shows up here too, without fighting whatever
// control the pilot is actively dragging (see applyRow's activeElement guard).
(function () {
  var REFRESH_MS = 2000;

  var empty = document.getElementById('vip-empty');
  var scroll = document.getElementById('vip-scroll');
  var rows = Array.prototype.slice.call(document.querySelectorAll('.vip-row'));

  function postCommand(body) {
    fetch('/ext/vanilla-icons-plus/command', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
    }).catch(function () {});
  }

  // Applies a freshly-fetched value to one row's control, unless the pilot is mid-interaction
  // with it right now — a poll landing while a slider is being dragged or a color picker is open
  // would otherwise yank the control back to the last-known-server value under their cursor.
  function applyRow(row, kind, value) {
    if (kind === 'bool') {
      var btn = row.querySelector('.vip-toggle');
      btn.classList.toggle('on', !!value);
      btn.textContent = value ? 'ON' : 'OFF';
    } else if (kind === 'color') {
      var color = row.querySelector('.vip-color');
      if (document.activeElement !== color) color.value = value;
    } else {
      var slider = row.querySelector('.vip-slider');
      var num = row.querySelector('.vip-num');
      if (document.activeElement !== slider) slider.value = value;
      num.textContent = kind === 'int' ? Math.round(value) : Number(value).toFixed(1);
    }
  }

  function refresh() {
    fetch('/ext/vanilla-icons-plus/state.json').then(function (r) {
      if (!r.ok) throw new Error('bad status ' + r.status);
      return r.json();
    }).then(function (state) {
      empty.hidden = true;
      scroll.hidden = false;
      rows.forEach(function (row) {
        var key = row.dataset.key;
        if (key in state) applyRow(row, row.dataset.kind, state[key]);
      });
    }).catch(function () {
      empty.hidden = false;
      scroll.hidden = true;
    });
  }

  rows.forEach(function (row) {
    var key = row.dataset.key;
    var kind = row.dataset.kind;

    if (kind === 'bool') {
      var btn = row.querySelector('.vip-toggle');
      btn.addEventListener('click', function () {
        var next = !btn.classList.contains('on');
        btn.classList.toggle('on', next);
        btn.textContent = next ? 'ON' : 'OFF';
        postCommand({ key: key, boolValue: next });
      });
    } else if (kind === 'color') {
      var color = row.querySelector('.vip-color');
      color.addEventListener('input', function () {
        postCommand({ key: key, hexValue: color.value });
      });
    } else {
      var slider = row.querySelector('.vip-slider');
      var num = row.querySelector('.vip-num');
      slider.addEventListener('input', function () {
        num.textContent = kind === 'int' ? Math.round(slider.value) : Number(slider.value).toFixed(1);
      });
      slider.addEventListener('change', function () {
        postCommand({ key: key, numberValue: parseFloat(slider.value) });
      });
    }
  });

  refresh();
  setInterval(refresh, REFRESH_MS);
})();
