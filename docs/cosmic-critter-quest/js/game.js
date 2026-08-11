// ================= game: state machine + combat loop + event + save =================

(function () {
  var SAVE_RUN = CFG.SAVE_PREFIX + 'run';
  var SAVE_BEST = CFG.SAVE_PREFIX + 'best';

  // ---------- run state ----------
  var player, enemy = null;
  var day, round, planet, hits, nonBattleStreak, speedIdx;
  var state = 'ready';          // ready | battling | choosing | dead
  var anim = { who: null, t: 0 };
  var time = 0;

  // combat nội bộ
  var beatTimer = 0, turn = 'hero', pendingHit = null, hitApplied = false, winTimer = -1, battleBeats = 0;

  function speed() { return CFG.SPEEDS[speedIdx]; }
  function fmt(tpl, map) {
    return tpl.replace(/\{(\w+)\}/g, function (_, k) { return map[k] != null ? map[k] : '{' + k + '}'; });
  }
  function planetName() { return CFG.PLANETS[planet % CFG.PLANETS.length].name; }
  function globalRound(r) { return planet * CFG.ROUNDS_PER_PLANET + r; }

  // ---------- save ----------
  function saveRun() {
    try {
      localStorage.setItem(SAVE_RUN, JSON.stringify({
        player: player, day: day, round: round, planet: planet,
        hits: hits, streak: nonBattleStreak, speedIdx: speedIdx,
      }));
    } catch (e) { }
  }
  function clearRun() { try { localStorage.removeItem(SAVE_RUN); } catch (e) { } }
  function loadBest() {
    try { return JSON.parse(localStorage.getItem(SAVE_BEST)) || null; } catch (e) { return null; }
  }
  function saveBest() {
    var best = loadBest();
    var score = globalRound(round);
    if (!best || score > best.score) {
      try {
        localStorage.setItem(SAVE_BEST, JSON.stringify({
          score: score, planet: planet + 1, round: round, lv: player.lv,
        }));
      } catch (e) { }
    }
  }

  // ---------- vòng đời run ----------
  function newRun(keepSpeed) {
    player = makePlayer();
    enemy = null;
    day = 0; round = 0; planet = 0; hits = 0; nonBattleStreak = 0;
    if (!keepSpeed) speedIdx = 0;
    state = 'ready';
    anim.who = null;
    UI.hideOverlay(); UI.hideChoices();
    UI.setEvent('You beam down onto *' + planetName() + '* — bio-luminescent flora hums in the dusk. Tap <b>ENGAGE</b> to explore.');
    refreshHud();
    UI.setButton('engage');
    UI.scrambleGlyphs();
    saveRun();
  }

  function tryResume() {
    var raw = null;
    try { raw = JSON.parse(localStorage.getItem(SAVE_RUN)); } catch (e) { }
    if (!raw || !raw.player) return false;
    player = raw.player;
    day = raw.day || 0; round = raw.round || 0; planet = raw.planet || 0;
    hits = raw.hits || 0; nonBattleStreak = raw.streak || 0;
    speedIdx = raw.speedIdx || 0;
    state = 'ready';
    UI.setEvent('Signal restored — your voyage on *' + planetName() + '* continues. Tap <b>ENGAGE</b>.');
    refreshHud();
    UI.setButton('engage');
    UI.scrambleGlyphs();
    return true;
  }

  function refreshHud() {
    UI.setStats(player);
    UI.setRound(Math.max(1, round), CFG.ROUNDS_PER_PLANET);
    UI.setHits(hits);
    UI.setPlanet(planetName());
    UI.setDay(Math.max(1, day));
    UI.setSpeed(speed());
    UI.setSidekicks(player.sidekicks);
  }

  // ---------- event mỗi Star Cycle ----------
  function pickEventKind() {
    if (nonBattleStreak >= CFG.MAX_NONBATTLE_STREAK) return 'battle';
    var w = CFG.EVENT_WEIGHTS, total = 0, k;
    for (k in w) total += w[k];
    var r = Math.random() * total;
    for (k in w) { r -= w[k]; if (r < 0) return k; }
    return 'battle';
  }

  function nextEvent() {
    day++;
    UI.setDay(day);
    UI.scrambleGlyphs();
    var kind = pickEventKind();
    if (kind === 'battle') { nonBattleStreak = 0; startBattle(); }
    else { nonBattleStreak++; resolvePeaceful(kind); }
  }

  function resolvePeaceful(kind) {
    var T = CFG.TEXT, g = globalRound(round + 1);
    switch (kind) {
      case 'fortune': {
        var f = pick(CFG.FORTUNES);
        applyMod(player, f.stat, f.val);
        UI.showFortune(f.ico, f.name);
        UI.setEvent(pick(T.fortune));
        break;
      }
      case 'choice': {
        var opts = CFG.UPGRADES.slice().sort(function () { return Math.random() - 0.5; }).slice(0, 3);
        UI.setEvent(pick(T.choice));
        state = 'choosing';
        UI.setButton('choosing');
        UI.showChoices(opts, function (i) {
          applyMod(player, opts[i].stat, opts[i].val);
          UI.showFortune(opts[i].ico, opts[i].name, 'Acquired');
          state = 'ready';
          UI.setButton('engage');
          refreshHud(); saveRun();
        });
        refreshHud();
        return; // save khi chọn xong
      }
      case 'spring': {
        var heal = Math.round(player.maxHp * rand(CFG.SPRING_HEAL[0], CFG.SPRING_HEAL[1]));
        player.hp = Math.min(player.maxHp, player.hp + heal);
        UI.setEvent(fmt(pick(T.spring), { heal: heal }));
        break;
      }
      case 'sidekick': {
        var owned = player.sidekicks;
        var avail = CFG.SIDEKICKS.filter(function (s) { return owned.indexOf(s.id) < 0; });
        if (owned.length >= CFG.MAX_SIDEKICKS || avail.length === 0) {
          var snack = Math.round(player.maxHp * CFG.SNACK_HEAL);
          player.hp = Math.min(player.maxHp, player.hp + snack);
          UI.setEvent(fmt(pick(T.sidekickFull), { heal: snack }));
        } else {
          var sk = pick(avail);
          owned.push(sk.id);
          UI.setEvent(fmt(pick(T.sidekick), { s: sk.ico + ' ' + sk.name }) +
            ' <b>' + sk.name + '</b> ' + sk.desc + '.');
          UI.showFortune(sk.ico, sk.name + ' joins!', 'New sidekick');
        }
        break;
      }
      case 'trap': {
        var dmg = Math.round(player.maxHp * rand(CFG.TRAP_DMG[0], CFG.TRAP_DMG[1]));
        player.hp = Math.max(1, player.hp - dmg);
        UI.setEvent(fmt(pick(T.trap), { dmg: dmg }));
        R.addFloater(R.HERO_X, R.BASE_Y - 60, '-' + dmg, '#ff5c7a');
        break;
      }
      case 'treasure': {
        var xp = Math.round(CFG.ENEMY.xp(g) * rand(CFG.TREASURE_XP[0], CFG.TREASURE_XP[1]));
        var txt = fmt(pick(T.treasure), { xp: xp });
        var ups = grantXp(player, xp);
        if (ups > 0) txt += fmt(CFG.TEXT.levelup, { lv: player.lv });
        UI.setEvent(txt);
        break;
      }
    }
    state = 'ready';
    UI.setButton('engage');
    refreshHud(); saveRun();
  }

  // ---------- battle ----------
  function startBattle() {
    round++;
    var g = globalRound(round);
    var kind = 'normal';
    if (round % CFG.BOSS_EVERY === 0) kind = 'boss';
    else if (Math.random() < CFG.ELITE_CHANCE) kind = 'elite';
    enemy = makeEnemy(g, kind, planet);

    var T = CFG.TEXT;
    var tpl = kind === 'boss' ? pick(T.boss) : kind === 'elite' ? pick(T.elite) : pick(T.battle);
    UI.setEvent(fmt(tpl, { e: enemy.name }));

    state = 'battling';
    turn = 'hero'; beatTimer = 0; anim.who = null; pendingHit = null; winTimer = -1; battleBeats = 0;
    UI.setButton('battling');
    refreshHud();
  }

  function startBeat() {
    hitApplied = false;
    anim.t = 0;
    battleBeats++;
    if (turn === 'hero') {
      anim.who = 'hero';
      var effAtk = player.atk * (1 + skBonus(player, 'dmg'));
      pendingHit = rollDamage(effAtk, enemy.def, player.crit + skBonus(player, 'crit'), player.critMult);
    } else {
      anim.who = 'enemy';
      var roll = rollDamage(enemy.atk, player.def, CFG.ENEMY_CRIT.chance, CFG.ENEMY_CRIT.mult);
      var enrage = 1 + Math.max(0, battleBeats - CFG.ENRAGE.after) * CFG.ENRAGE.ramp;
      roll.dmg = Math.max(1, Math.round(roll.dmg * enrage * (1 - skBonus(player, 'block'))));
      pendingHit = roll;
    }
  }

  function applyHit() {
    hitApplied = true;
    if (anim.who === 'hero') {
      enemy.hp -= pendingHit.dmg;
      hits++;
      UI.setHits(hits);
      R.addFloater(R.ENEMY_X, R.BASE_Y - 60 * enemy.look.size,
        pendingHit.dmg + (pendingHit.crit ? '!' : ''),
        pendingHit.crit ? '#ffab3d' : '#fff', pendingHit.crit);
      // lifesteal + medic
      var healed = Math.round(pendingHit.dmg * player.lifesteal + player.maxHp * skBonus(player, 'heal'));
      if (healed > 0 && player.hp < player.maxHp) {
        player.hp = Math.min(player.maxHp, player.hp + healed);
        R.addFloater(R.HERO_X, R.BASE_Y - 70, '+' + healed, '#8bf07a');
      }
      if (enemy.hp <= 0) { enemy.hp = 0; winTimer = 0.45; }
    } else {
      player.hp -= pendingHit.dmg;
      R.addFloater(R.HERO_X, R.BASE_Y - 60,
        pendingHit.dmg + (pendingHit.crit ? '!' : ''),
        pendingHit.crit ? '#ff3d5c' : '#ff8fa5', pendingHit.crit);
      // thorns phản dame
      if (player.thorns > 0 && enemy.hp > 0) {
        var ref = Math.max(1, Math.round(pendingHit.dmg * player.thorns));
        enemy.hp -= ref;
        R.addFloater(R.ENEMY_X, R.BASE_Y - 60 * enemy.look.size, ref + '', '#c79bff');
        if (enemy.hp <= 0) { enemy.hp = 0; winTimer = 0.45; }
      }
      if (player.hp <= 0) { player.hp = 0; die(); return; }
    }
    UI.setStats(player);
    turn = (turn === 'hero') ? 'enemy' : 'hero';
  }

  function winBattle() {
    var T = CFG.TEXT;
    var txt = fmt(pick(T.win), { e: enemy.name, xp: enemy.xp });
    var ups = grantXp(player, enemy.xp);
    if (ups > 0) {
      txt += fmt(T.levelup, { lv: player.lv });
      R.addFloater(R.HERO_X, R.BASE_Y - 85, 'LEVEL UP!', '#ffd35c', true);
    }
    // hồi nhẹ sau trận
    var regen = Math.round(player.maxHp * CFG.PLAYER.regenPct);
    player.hp = Math.min(player.maxHp, player.hp + regen);
    enemy = null;

    if (round >= CFG.ROUNDS_PER_PLANET) {
      // dọn xong hành tinh
      txt += '<br>' + fmt(pick(T.planetClear), { p: planetName() });
      planet++; round = 0;
      player.hp = Math.min(player.maxHp, player.hp + Math.round(player.maxHp * CFG.PLANET_CLEAR_HEAL));
      UI.showFortune('🪐', 'Warping to ' + planetName() + '!', 'Planet cleared');
    }
    UI.setEvent(txt);
    state = 'ready';
    UI.setButton('engage');
    refreshHud(); saveRun();
  }

  function die() {
    state = 'dead';
    saveBest(); clearRun();
    var best = loadBest();
    UI.setEvent('Your suit\'s life support fades… the *' + enemy.name + '* was too much. The mothership retrieves your escape pod.');
    UI.setButton('dead');
    UI.showOverlay('☠️ Voyage Ended',
      'You fell on <b>' + planetName() + '</b><span class="big">Planet ' + (planet + 1) +
      ' · Round ' + round + '</span>Level ' + player.lv + ' · Star Cycle ' + day +
      (best ? '<br><br>Best voyage: Planet ' + best.planet + ' · Round ' + best.round + ' (Lv.' + best.lv + ')' : ''),
      [{ label: '🚀 NEW VOYAGE', onClick: function () { newRun(true); } }]);
  }

  // ---------- update / render loop ----------
  var lastTs = 0;
  function loop(ts) {
    var dt = Math.min(0.05, (ts - lastTs) / 1000 || 0);
    lastTs = ts;
    time += dt;

    var paused = !UI.els.overlay.classList.contains('hidden');
    if (state === 'battling' && !paused) {
      var sp = speed();
      if (winTimer >= 0) {
        // chờ xác quái tan rồi kết thúc trận
        winTimer -= dt * sp;
        if (anim.who !== null) { anim.t = Math.min(1, anim.t + dt * sp / 0.35); if (anim.t >= 1) anim.who = null; }
        if (winTimer <= 0) { anim.who = null; winBattle(); }
      } else if (anim.who !== null) {
        anim.t += dt * sp / 0.35;
        if (!hitApplied && anim.t >= 0.45) applyHit();
        if (anim.t >= 1) { anim.t = 1; anim.who = null; }
      } else {
        beatTimer += dt * 1000 * sp;
        if (beatTimer >= CFG.BEAT_MS) { beatTimer = 0; startBeat(); }
      }
    } else if (anim.who !== null) {
      anim.t += dt / 0.35;
      if (anim.t >= 1) anim.who = null;
    }

    R.frame({ planet: planet, hero: player, enemy: enemy, anim: anim, time: time }, dt);
    requestAnimationFrame(loop);
  }

  // ---------- input ----------
  function onEngage() {
    if (state === 'ready') nextEvent();
    else if (state === 'dead') newRun(true);
  }

  function onSpeed() {
    speedIdx = (speedIdx + 1) % CFG.SPEEDS.length;
    UI.setSpeed(speed());
    saveRun();
  }

  function onGear() {
    var best = loadBest();
    UI.showOverlay('⚙️ Settings',
      'Cosmic Critter Quest — a tap-to-advance<br>auto-battle voyage.' +
      (best ? '<br><br>Best voyage: Planet ' + best.planet + ' · Round ' + best.round : ''),
      [
        { label: 'Resume', onClick: UI.hideOverlay },
        {
          label: 'Restart voyage', style: 'warn', onClick: function () {
            if (state === 'battling') { enemy = null; anim.who = null; }
            newRun(true);
          }
        },
        {
          label: 'Reset all data', style: 'secondary', onClick: function () {
            clearRun();
            try { localStorage.removeItem(SAVE_BEST); } catch (e) { }
            newRun(false);
          }
        },
      ]);
  }

  // ---------- init ----------
  function init() {
    R.init(document.getElementById('cv'));
    UI.els.engage.addEventListener('click', onEngage);
    UI.els.speed.addEventListener('click', onSpeed);
    document.getElementById('btn-gear').addEventListener('click', onGear);

    speedIdx = 0;
    if (!tryResume()) newRun(false);
    requestAnimationFrame(loop);
  }

  init();
})();
