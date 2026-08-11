// ================= entities: player / enemy / sidekick / damage =================

function rand(a, b) { return a + Math.random() * (b - a); }
function randInt(a, b) { return Math.floor(rand(a, b + 1)); }
function pick(arr) { return arr[Math.floor(Math.random() * arr.length)]; }

// ---------- player ----------
function makePlayer() {
  var P = CFG.PLAYER;
  return {
    lv: 1, xp: 0,
    maxHp: P.hp, hp: P.hp,
    atk: P.atk, def: P.def,
    crit: P.crit, critMult: P.critMult,
    lifesteal: P.lifesteal, thorns: P.thorns,
    sidekicks: [],            // mảng id trong CFG.SIDEKICKS
  };
}

function xpNeed(p) { return CFG.LEVEL.xpNeed(p.lv); }

// trả về số level tăng được
function grantXp(p, amount) {
  p.xp += amount;
  var ups = 0;
  while (p.xp >= xpNeed(p)) {
    p.xp -= xpNeed(p);
    p.lv++; ups++;
    var L = CFG.LEVEL;
    p.maxHp = Math.round(p.maxHp * (1 + L.hpPct));
    p.atk = Math.round(p.atk * (1 + L.atkPct) * 10) / 10;
    p.def += L.defFlat;
    p.hp = Math.min(p.maxHp, p.hp + Math.round(p.maxHp * L.healPct));
  }
  return ups;
}

// áp một buff dạng {stat, val} (dùng chung cho fortune + upgrade)
function applyMod(p, stat, val) {
  switch (stat) {
    case 'maxHpPct': {
      var add = Math.round(p.maxHp * val);
      p.maxHp += add; p.hp += add; break;
    }
    case 'atkPct':  p.atk = Math.round(p.atk * (1 + val) * 10) / 10; break;
    case 'defFlat': p.def += val; break;
    case 'crit':    p.crit += val; break;
    case 'lifesteal': p.lifesteal += val; break;
    case 'thorns':  p.thorns += val; break;
    case 'healPct': p.hp = Math.min(p.maxHp, p.hp + Math.round(p.maxHp * val)); break;
    case 'glass': { // Unstable Core: ATK +30%, MaxHP -10%
      p.atk = Math.round(p.atk * 1.3 * 10) / 10;
      p.maxHp = Math.round(p.maxHp * 0.9);
      p.hp = Math.min(p.hp, p.maxHp); break;
    }
  }
}

// tổng hợp hiệu ứng sidekick
function skBonus(p, type) {
  var total = 0;
  for (var i = 0; i < p.sidekicks.length; i++) {
    var sk = CFG.SIDEKICKS.find(function (s) { return s.id === p.sidekicks[i]; });
    if (sk && sk.type === type) total += sk.val;
  }
  return total;
}

// ---------- enemy ----------
// g = global round; kind = 'normal' | 'elite' | 'boss'
function makeEnemy(g, kind, planetIdx) {
  var E = CFG.ENEMY;
  var hp = E.hp(g), atk = E.atk(g), def = E.def(g), xp = E.xp(g);
  var name;
  if (kind === 'boss') {
    var m = E.bossMult;
    hp = Math.round(hp * m.hp); atk = Math.round(atk * m.atk); xp = Math.round(xp * m.xp);
    name = CFG.BOSS_NAMES[planetIdx % CFG.BOSS_NAMES.length];
  } else if (kind === 'elite') {
    var m2 = E.eliteMult;
    hp = Math.round(hp * m2.hp); atk = Math.round(atk * m2.atk); xp = Math.round(xp * m2.xp);
    name = 'Irradiated ' + pick(CFG.CRITTER_NAMES);
  } else {
    name = pick(CFG.CRITTER_NAMES);
  }
  return {
    name: name, kind: kind,
    maxHp: hp, hp: hp, atk: atk, def: def, xp: xp,
    // tham số hình dáng để render vẽ procedural
    look: {
      color: pick(CFG.CRITTER_COLORS),
      eyes: kind === 'boss' ? 3 : randInt(1, 3),
      horns: Math.random() < 0.5,
      spots: Math.random() < 0.6,
      size: kind === 'boss' ? 1.45 : (kind === 'elite' ? 1.2 : rand(0.85, 1.05)),
    },
  };
}

// ---------- damage ----------
// trả {dmg, crit}
function rollDamage(atk, def, critChance, critMult) {
  var v = CFG.DMG_VARIANCE;
  var raw = atk * rand(1 - v, 1 + v);
  var isCrit = Math.random() < critChance;
  if (isCrit) raw *= critMult;
  return { dmg: Math.max(1, Math.round(raw - def)), crit: isCrit };
}
