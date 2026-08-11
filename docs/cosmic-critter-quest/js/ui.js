// ================= ui: DOM panel dưới + overlay =================

var UI = (function () {
  function $(id) { return document.getElementById(id); }

  var els = {
    round: $('round-label'), hits: $('hits-num'), planet: $('planet-name'),
    speed: $('btn-speed'),
    lv: $('st-lv'), hp: $('st-hp'), atk: $('st-atk'), def: $('st-def'),
    xpFill: $('xp-fill'),
    day: $('day-label'), eventText: $('event-text'),
    choiceBox: $('choice-box'),
    fortune: $('fortune-banner'), fortuneText: $('fortune-text'),
    skRow: $('sidekick-row'),
    engage: $('btn-engage'), engageLabel: $('engage-label'),
    overlay: $('overlay'), ovTitle: $('ov-title'), ovBody: $('ov-body'), ovButtons: $('ov-buttons'),
    glyphs: [$('gl-1'), $('gl-2'), $('gl-3'), $('gl-4')],
  };

  // ---- stats bar ----
  function setStats(p) {
    els.lv.textContent = 'Lv.' + p.lv;
    var pct = Math.round(p.hp / p.maxHp * 100);
    els.hp.innerHTML = '<span style="color:' + (pct <= 30 ? '#ff5c7a' : '#8bf07a') + '">' +
      Math.ceil(p.hp) + '</span>/' + p.maxHp;
    els.atk.textContent = Math.round(p.atk);
    els.def.textContent = p.def;
    els.xpFill.style.width = Math.min(100, Math.round(p.xp / xpNeed(p) * 100)) + '%';
  }

  function setRound(round, total) { els.round.textContent = 'Round ' + round + '/' + total; }
  function setHits(n) { els.hits.textContent = n; }
  function setPlanet(name) { els.planet.textContent = name; }
  function setDay(d) { els.day.textContent = 'Star Cycle ' + d; }
  function setSpeed(mult) { els.speed.textContent = 'x' + mult; }

  // ---- event text: *cam* và <b> tím giữ nguyên ----
  function setEvent(html) {
    els.eventText.innerHTML = html.replace(/\*(.+?)\*/g, '<em>$1</em>');
  }

  // ---- glyph console: chữ alien trang trí, đổi mỗi event ----
  function scrambleGlyphs() {
    var G = CFG.GLYPHS;
    for (var l = 0; l < els.glyphs.length; l++) {
      var n = 22 + Math.floor(Math.random() * 8) - l * 2;
      var s = '';
      for (var i = 0; i < n; i++) {
        s += Math.random() < 0.18 ? ' ' : G[Math.floor(Math.random() * G.length)];
      }
      els.glyphs[l].textContent = s;
    }
  }

  // ---- fortune banner ----
  var fortuneTimer = null;
  function showFortune(ico, name, tag) {
    els.fortuneText.textContent = ico + ' ' + name;
    els.fortune.querySelector('.fortune-tag').textContent = tag || 'Small fortune';
    els.fortune.classList.remove('hidden');
    clearTimeout(fortuneTimer);
    fortuneTimer = setTimeout(function () { els.fortune.classList.add('hidden'); }, 3200);
  }

  // ---- choice cards ----
  function showChoices(options, onPick) {
    els.choiceBox.innerHTML = '';
    options.forEach(function (opt, i) {
      var btn = document.createElement('button');
      btn.className = 'choice-card';
      btn.innerHTML = '<span class="choice-ico">' + opt.ico + '</span>' +
        '<span><div class="choice-name">' + opt.name + '</div>' +
        '<div class="choice-desc">' + opt.desc + '</div></span>';
      btn.onclick = function () { hideChoices(); onPick(i); };
      els.choiceBox.appendChild(btn);
    });
    els.choiceBox.classList.remove('hidden');
  }
  function hideChoices() {
    els.choiceBox.classList.add('hidden');
    els.choiceBox.innerHTML = '';
  }

  // ---- sidekick chips ----
  function setSidekicks(ids) {
    els.skRow.innerHTML = '';
    ids.forEach(function (id) {
      var sk = CFG.SIDEKICKS.find(function (s) { return s.id === id; });
      if (!sk) return;
      var chip = document.createElement('span');
      chip.className = 'sk-chip';
      chip.innerHTML = '<span class="sk-ico">' + sk.ico + '</span>' + sk.name;
      chip.title = sk.desc;
      els.skRow.appendChild(chip);
    });
  }

  // ---- nút chính ----
  // mode: 'engage' | 'battling' | 'choosing' | 'next' | 'dead'
  function setButton(mode) {
    var b = els.engage;
    b.classList.remove('battling', 'danger');
    b.disabled = false;
    if (mode === 'engage') { els.engageLabel.textContent = 'ENGAGE'; }
    else if (mode === 'next') { els.engageLabel.textContent = 'CONTINUE'; }
    else if (mode === 'battling') {
      els.engageLabel.textContent = 'Battling';
      b.classList.add('battling'); b.disabled = true;
    }
    else if (mode === 'choosing') { els.engageLabel.textContent = 'Choose…'; b.disabled = true; }
    else if (mode === 'dead') { els.engageLabel.textContent = 'NEW VOYAGE'; b.classList.add('danger'); }
  }

  // ---- overlay ----
  function showOverlay(title, bodyHtml, buttons) {
    els.ovTitle.textContent = title;
    els.ovBody.innerHTML = bodyHtml;
    els.ovButtons.innerHTML = '';
    buttons.forEach(function (bt) {
      var btn = document.createElement('button');
      btn.className = 'ov-btn' + (bt.style ? ' ' + bt.style : '');
      btn.textContent = bt.label;
      btn.onclick = bt.onClick;
      els.ovButtons.appendChild(btn);
    });
    els.overlay.classList.remove('hidden');
  }
  function hideOverlay() { els.overlay.classList.add('hidden'); }

  return {
    els: els,
    setStats: setStats, setRound: setRound, setHits: setHits, setPlanet: setPlanet,
    setDay: setDay, setSpeed: setSpeed, setEvent: setEvent,
    scrambleGlyphs: scrambleGlyphs, showFortune: showFortune,
    showChoices: showChoices, hideChoices: hideChoices,
    setSidekicks: setSidekicks, setButton: setButton,
    showOverlay: showOverlay, hideOverlay: hideOverlay,
  };
})();
