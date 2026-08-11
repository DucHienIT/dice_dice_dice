// ================= render: canvas panel trên (background + nhân vật + hiệu ứng) =================

var R = (function () {
  var cv, ctx, W = 460, H = 330;
  var bgCache = null, bgPlanet = -1;

  var HERO_X = 140, ENEMY_X = 330, BASE_Y = 240;

  // seeded rng cho background cố định theo hành tinh
  function mulberry32(a) {
    return function () {
      a |= 0; a = a + 0x6D2B79F5 | 0;
      var t = Math.imul(a ^ a >>> 15, 1 | a);
      t = t + Math.imul(t ^ t >>> 7, 61 | t) ^ t;
      return ((t ^ t >>> 14) >>> 0) / 4294967296;
    };
  }

  function init(canvas) { cv = canvas; ctx = cv.getContext('2d'); }

  // ---------- background (cache offscreen theo planet) ----------
  function buildBg(planetIdx) {
    var P = CFG.PLANETS[planetIdx % CFG.PLANETS.length];
    var rng = mulberry32(planetIdx * 1337 + 7);
    var oc = document.createElement('canvas');
    oc.width = W; oc.height = H;
    var c = oc.getContext('2d');

    // bầu trời
    var sky = c.createLinearGradient(0, 0, 0, H);
    sky.addColorStop(0, P.sky1); sky.addColorStop(1, P.sky2);
    c.fillStyle = sky; c.fillRect(0, 0, W, H);

    // sao
    for (var i = 0; i < 60; i++) {
      c.globalAlpha = 0.25 + rng() * 0.6;
      c.fillStyle = '#fff';
      var sx = rng() * W, sy = rng() * H * 0.55, sr = rng() * 1.4 + 0.4;
      c.fillRect(sx, sy, sr, sr);
    }
    c.globalAlpha = 1;

    // mặt trăng
    c.fillStyle = P.moon;
    c.beginPath(); c.arc(W * 0.82, 52, 22, 0, 7); c.fill();
    c.globalAlpha = 0.25; c.fillStyle = '#000';
    c.beginPath(); c.arc(W * 0.82 + 7, 48, 5, 0, 7); c.fill();
    c.beginPath(); c.arc(W * 0.82 - 6, 58, 3.5, 0, 7); c.fill();
    c.globalAlpha = 1;

    // đá nền xa
    c.fillStyle = P.rock;
    rockCluster(c, rng, 40, 150, 1.2);
    rockCluster(c, rng, 390, 145, 1.4);

    // mặt đất
    c.fillStyle = P.ground;
    c.fillRect(0, 195, W, H - 195);

    // hồ phát sáng
    var lake = c.createRadialGradient(W / 2, 250, 20, W / 2, 250, 190);
    lake.addColorStop(0, P.lake); lake.addColorStop(1, P.lakeDeep);
    c.fillStyle = lake;
    c.beginPath(); c.ellipse(W / 2, 252, 205, 52, 0, 0, 7); c.fill();
    // viền hồ
    c.strokeStyle = 'rgba(255,255,255,.25)'; c.lineWidth = 3;
    c.beginPath(); c.ellipse(W / 2, 252, 205, 52, 0, 0, 7); c.stroke();

    // flora phát quang hai bên
    for (var f = 0; f < 7; f++) {
      var fx = f < 4 ? 12 + rng() * 80 : W - 12 - rng() * 80;
      var fy = 215 + rng() * 90;
      drawFlora(c, fx, fy, P.flora[f % P.flora.length], 0.5 + rng() * 0.7, rng);
    }
    return oc;
  }

  function rockCluster(c, rng, x, y, s) {
    c.beginPath();
    c.moveTo(x - 45 * s, y + 50);
    c.lineTo(x - 30 * s, y - 20 * s);
    c.lineTo(x - 10 * s, y);
    c.lineTo(x + 8 * s, y - 38 * s);
    c.lineTo(x + 28 * s, y - 5 * s);
    c.lineTo(x + 45 * s, y + 50);
    c.closePath(); c.fill();
  }

  function drawFlora(c, x, y, color, s, rng) {
    // nấm/cây phát sáng đơn giản
    c.save();
    c.translate(x, y); c.scale(s, s);
    c.shadowColor = color; c.shadowBlur = 12;
    c.fillStyle = color;
    if (rng() < 0.5) { // nấm
      c.fillRect(-3, -8, 6, 14);
      c.beginPath(); c.ellipse(0, -12, 14, 9, 0, 0, 7); c.fill();
      c.fillStyle = 'rgba(255,255,255,.5)';
      c.beginPath(); c.arc(-4, -14, 2, 0, 7); c.fill();
      c.beginPath(); c.arc(5, -11, 1.5, 0, 7); c.fill();
    } else { // cỏ tinh thể
      for (var k = -1; k <= 1; k++) {
        c.beginPath();
        c.moveTo(k * 6 - 3, 6); c.lineTo(k * 6, -18 - Math.abs(k) * -6); c.lineTo(k * 6 + 3, 6);
        c.closePath(); c.fill();
      }
    }
    c.restore();
  }

  // ---------- nhân vật ----------
  function drawHero(t, anim, hero) {
    var lunge = 0, flash = 0;
    if (anim.who === 'hero') lunge = Math.sin(anim.t * Math.PI) * 46;
    if (anim.who === 'enemy' && anim.t > 0.3 && anim.t < 0.7) flash = 1;
    var x = HERO_X + lunge;
    var y = BASE_Y + Math.sin(t * 2.2) * 3;

    var c = ctx;
    c.save(); c.translate(x, y);

    // bóng
    c.fillStyle = 'rgba(0,0,0,.3)';
    c.beginPath(); c.ellipse(0, 26, 26, 7, 0, 0, 7); c.fill();

    // kiếm laser (phía sau thân, chĩa về địch)
    var swing = anim.who === 'hero' ? Math.sin(anim.t * Math.PI) * 0.9 : 0;
    c.save();
    c.translate(16, -2); c.rotate(-0.5 + swing);
    c.strokeStyle = '#9be8ff'; c.lineWidth = 5; c.lineCap = 'round';
    c.shadowColor = '#4ad4ff'; c.shadowBlur = 10;
    c.beginPath(); c.moveTo(0, 0); c.lineTo(0, -34); c.stroke();
    c.shadowBlur = 0;
    c.strokeStyle = '#fff'; c.lineWidth = 2;
    c.beginPath(); c.moveTo(0, -2); c.lineTo(0, -32); c.stroke();
    c.fillStyle = '#555e70'; c.fillRect(-3, 0, 6, 9);
    c.restore();

    // thân bộ đồ phi hành trắng
    c.fillStyle = '#f2f4f8';
    c.beginPath(); c.ellipse(0, 8, 20, 19, 0, 0, 7); c.fill();
    c.fillStyle = '#c9d2e0';
    c.beginPath(); c.ellipse(0, 14, 12, 8, 0, 0, 7); c.fill();
    // đai
    c.fillStyle = '#4ad4ff';
    c.fillRect(-14, 6, 28, 5);

    // chân
    c.fillStyle = '#dfe5ee';
    c.beginPath(); c.ellipse(-9, 25, 7, 5, 0, 0, 7); c.fill();
    c.beginPath(); c.ellipse(9, 25, 7, 5, 0, 0, 7); c.fill();

    // đầu alien xanh trong mũ kính
    c.fillStyle = '#7ede6a';
    c.beginPath(); c.arc(0, -14, 15, 0, 7); c.fill();
    // mắt to đen
    c.fillStyle = '#1c2430';
    c.beginPath(); c.ellipse(-6, -15, 3.6, 5, -0.15, 0, 7); c.fill();
    c.beginPath(); c.ellipse(6, -15, 3.6, 5, 0.15, 0, 7); c.fill();
    c.fillStyle = '#fff';
    c.beginPath(); c.arc(-7, -17, 1.3, 0, 7); c.fill();
    c.beginPath(); c.arc(5, -17, 1.3, 0, 7); c.fill();
    // miệng
    c.strokeStyle = '#2e5c24'; c.lineWidth = 1.6; c.lineCap = 'round';
    c.beginPath(); c.arc(0, -9, 3.4, 0.25 * Math.PI, 0.75 * Math.PI); c.stroke();
    // ăng-ten
    c.strokeStyle = '#7ede6a'; c.lineWidth = 2;
    c.beginPath(); c.moveTo(0, -28); c.quadraticCurveTo(2, -35, 6, -37); c.stroke();
    c.fillStyle = '#ffd35c';
    c.shadowColor = '#ffd35c'; c.shadowBlur = 8;
    c.beginPath(); c.arc(6.5, -38, 3, 0, 7); c.fill();
    c.shadowBlur = 0;
    // kính mũ
    c.strokeStyle = 'rgba(180,230,255,.8)'; c.lineWidth = 2.5;
    c.beginPath(); c.arc(0, -14, 18, 0, 7); c.stroke();
    c.fillStyle = 'rgba(160,220,255,.14)';
    c.beginPath(); c.arc(0, -14, 18, 0, 7); c.fill();
    c.fillStyle = 'rgba(255,255,255,.45)';
    c.beginPath(); c.ellipse(-8, -22, 5, 3, -0.6, 0, 7); c.fill();

    if (flash) { // bị đánh: chớp trắng
      c.globalAlpha = 0.55; c.fillStyle = '#fff';
      c.beginPath(); c.arc(0, -2, 30, 0, 7); c.fill();
      c.globalAlpha = 1;
    }
    c.restore();

    // sidekick lơ lửng sau lưng
    for (var i = 0; i < hero.sidekicks.length; i++) {
      var sk = CFG.SIDEKICKS.find(function (s) { return s.id === hero.sidekicks[i]; });
      if (!sk) continue;
      var ox = HERO_X - 52 - i * 26;
      var oy = BASE_Y - 30 + Math.sin(t * 2.5 + i * 1.7) * 5;
      c.save();
      c.fillStyle = sk.color; c.shadowColor = sk.color; c.shadowBlur = 8;
      c.beginPath(); c.arc(ox, oy, 9, 0, 7); c.fill();
      c.shadowBlur = 0;
      c.fillStyle = '#1c2430';
      c.beginPath(); c.arc(ox - 2.5, oy - 1, 1.6, 0, 7); c.fill();
      c.beginPath(); c.arc(ox + 2.5, oy - 1, 1.6, 0, 7); c.fill();
      c.strokeStyle = '#1c2430'; c.lineWidth = 1.2;
      c.beginPath(); c.arc(ox, oy + 2.5, 2, 0.2 * Math.PI, 0.8 * Math.PI); c.stroke();
      c.restore();
    }
  }

  function drawEnemy(t, anim, e) {
    if (!e || e.hp <= 0) return;
    var lunge = 0, flash = 0;
    if (anim.who === 'enemy') lunge = Math.sin(anim.t * Math.PI) * -46;
    if (anim.who === 'hero' && anim.t > 0.3 && anim.t < 0.7) flash = 1;
    var s = e.look.size;
    var x = ENEMY_X + lunge;
    var y = BASE_Y + Math.sin(t * 2.6 + 1) * 3;
    var c = ctx;

    c.save(); c.translate(x, y); c.scale(s, s);

    // bóng
    c.fillStyle = 'rgba(0,0,0,.3)';
    c.beginPath(); c.ellipse(0, 27, 26, 7, 0, 0, 7); c.fill();

    // thân blob
    var squish = 1 + Math.sin(t * 5) * 0.03;
    c.fillStyle = e.look.color;
    c.beginPath(); c.ellipse(0, 2, 26 * squish, 26 / squish, 0, 0, 7); c.fill();
    // bụng sáng
    c.fillStyle = 'rgba(255,255,255,.35)';
    c.beginPath(); c.ellipse(0, 12, 15, 10, 0, 0, 7); c.fill();
    // đốm
    if (e.look.spots) {
      c.fillStyle = 'rgba(0,0,0,.15)';
      c.beginPath(); c.arc(-14, -6, 4, 0, 7); c.fill();
      c.beginPath(); c.arc(12, -12, 3, 0, 7); c.fill();
      c.beginPath(); c.arc(16, 4, 2.5, 0, 7); c.fill();
    }
    // sừng
    if (e.look.horns) {
      c.fillStyle = '#ffe9c9';
      c.beginPath(); c.moveTo(-14, -20); c.lineTo(-20, -34); c.lineTo(-7, -24); c.closePath(); c.fill();
      c.beginPath(); c.moveTo(14, -20); c.lineTo(20, -34); c.lineTo(7, -24); c.closePath(); c.fill();
    }
    // gai boss
    if (e.kind === 'boss') {
      c.fillStyle = '#ffd35c';
      for (var k = -2; k <= 2; k++) {
        c.beginPath();
        c.moveTo(k * 10 - 4, -22); c.lineTo(k * 10, -36 - (2 - Math.abs(k)) * 4); c.lineTo(k * 10 + 4, -22);
        c.closePath(); c.fill();
      }
    }
    // mắt (1–3)
    var n = e.look.eyes;
    for (var i = 0; i < n; i++) {
      var ex = (i - (n - 1) / 2) * 13;
      var ey = -6 - (i % 2) * 3;
      c.fillStyle = '#fff';
      c.beginPath(); c.arc(ex, ey, 6.5, 0, 7); c.fill();
      c.fillStyle = '#1c2430';
      c.beginPath(); c.arc(ex - 1.5, ey, 3, 0, 7); c.fill();
    }
    // miệng dữ
    c.strokeStyle = '#7a1c2e'; c.lineWidth = 2; c.lineCap = 'round';
    c.beginPath(); c.arc(0, 9, 6, 1.15 * Math.PI, 1.85 * Math.PI); c.stroke();
    // răng nanh
    c.fillStyle = '#fff';
    c.beginPath(); c.moveTo(-5, 6); c.lineTo(-3, 11); c.lineTo(-1, 6); c.closePath(); c.fill();
    c.beginPath(); c.moveTo(1, 6); c.lineTo(3, 11); c.lineTo(5, 6); c.closePath(); c.fill();

    if (flash) {
      c.globalAlpha = 0.6; c.fillStyle = '#fff';
      c.beginPath(); c.arc(0, 0, 32, 0, 7); c.fill();
      c.globalAlpha = 1;
    }
    c.restore();
  }

  // ---------- HP bar + số ----------
  function drawHpBar(x, cur, max, isHero, yOffset) {
    var c = ctx, w = 52, h = 8;
    var y = BASE_Y + 34 + (yOffset || 0);
    c.save();
    // số HP trên đầu
    c.font = '900 15px "Segoe UI",sans-serif';
    c.textAlign = 'center';
    c.lineWidth = 3.5; c.strokeStyle = 'rgba(0,0,0,.65)';
    c.strokeText(Math.ceil(cur), x, y - 4);
    c.fillStyle = '#fff'; c.fillText(Math.ceil(cur), x, y - 4);
    // thanh
    c.fillStyle = 'rgba(0,0,0,.55)';
    roundRect(c, x - w / 2 - 1.5, y - 1.5, w + 3, h + 3, 5); c.fill();
    var ratio = Math.max(0, cur / max);
    var grad = c.createLinearGradient(0, y, 0, y + h);
    if (isHero) { grad.addColorStop(0, '#ffe95c'); grad.addColorStop(1, '#8fd41e'); }
    else { grad.addColorStop(0, '#ff7a6b'); grad.addColorStop(1, '#e02e4a'); }
    c.fillStyle = grad;
    if (ratio > 0) { roundRect(c, x - w / 2, y, w * ratio, h, 4); c.fill(); }
    c.restore();
  }

  function roundRect(c, x, y, w, h, r) {
    r = Math.min(r, w / 2, h / 2);
    c.beginPath();
    c.moveTo(x + r, y);
    c.arcTo(x + w, y, x + w, y + h, r);
    c.arcTo(x + w, y + h, x, y + h, r);
    c.arcTo(x, y + h, x, y, r);
    c.arcTo(x, y, x + w, y, r);
    c.closePath();
  }

  // ---------- damage floaters ----------
  var floaters = [];
  function addFloater(x, y, txt, color, big) {
    floaters.push({ x: x + rand(-12, 12), y: y, txt: txt, color: color, big: !!big, age: 0 });
  }
  function drawFloaters(dt) {
    var c = ctx;
    for (var i = floaters.length - 1; i >= 0; i--) {
      var f = floaters[i];
      f.age += dt; f.y -= dt * 38;
      if (f.age > 1.1) { floaters.splice(i, 1); continue; }
      var a = f.age < 0.8 ? 1 : (1.1 - f.age) / 0.3;
      c.save();
      c.globalAlpha = a;
      c.font = '900 ' + (f.big ? 26 : 19) + 'px "Segoe UI",sans-serif';
      c.textAlign = 'center';
      c.lineWidth = 4; c.strokeStyle = 'rgba(0,0,0,.7)';
      c.strokeText(f.txt, f.x, f.y);
      c.fillStyle = f.color;
      c.fillText(f.txt, f.x, f.y);
      c.restore();
    }
  }

  // ---------- frame ----------
  // state: {planet, hero, enemy, anim:{who,t}, time}
  function frame(state, dt) {
    if (bgPlanet !== state.planet) { bgCache = buildBg(state.planet); bgPlanet = state.planet; }
    ctx.clearRect(0, 0, W, H);
    ctx.drawImage(bgCache, 0, 0);

    drawHero(state.time, state.anim, state.hero);
    drawEnemy(state.time, state.anim, state.enemy);

    drawHpBar(HERO_X, state.hero.hp, state.hero.maxHp, true);
    if (state.enemy && state.enemy.hp > 0) {
      drawHpBar(ENEMY_X, state.enemy.hp, state.enemy.maxHp, false);
      // tên quái
      ctx.save();
      ctx.font = '700 11px "Segoe UI",sans-serif'; ctx.textAlign = 'center';
      ctx.lineWidth = 3; ctx.strokeStyle = 'rgba(0,0,0,.6)';
      var label = (state.enemy.kind === 'boss' ? '👑 ' : '') + state.enemy.name;
      ctx.strokeText(label, ENEMY_X, BASE_Y - 52 * state.enemy.look.size - 8);
      ctx.fillStyle = state.enemy.kind === 'boss' ? '#ffd35c' : '#fff';
      ctx.fillText(label, ENEMY_X, BASE_Y - 52 * state.enemy.look.size - 8);
      ctx.restore();
    }
    drawFloaters(dt);
  }

  return {
    init: init, frame: frame, addFloater: addFloater,
    HERO_X: HERO_X, ENEMY_X: ENEMY_X, BASE_Y: BASE_Y,
  };
})();
