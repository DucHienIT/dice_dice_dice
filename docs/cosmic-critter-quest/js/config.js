// ================= Cosmic Critter Quest — BALANCE & DATA =================
// Toàn bộ hằng số cân bằng + data content gom về đây. Chỉnh game feel ở file này.

var CFG = {

  SAVE_PREFIX: 'ccq_',          // localStorage prefix (file:// dùng chung origin)

  ROUNDS_PER_PLANET: 30,
  BOSS_EVERY: 10,               // round chia hết cho số này -> boss
  ELITE_CHANCE: 0.12,

  SPEEDS: [1, 2, 4],
  BEAT_MS: 550,                 // nhịp đánh cơ bản (chia cho speed)

  // ---- người chơi ----
  PLAYER: {
    hp: 160, atk: 15, def: 3,
    crit: 0.08, critMult: 1.6,
    lifesteal: 0, thorns: 0, regenPct: 0.12,   // hồi % maxHP sau mỗi trận thắng
  },
  LEVEL: {
    xpNeed: function (lv) { return Math.round(16 * Math.pow(lv, 1.55)); },
    hpPct: 0.14, atkPct: 0.11, defFlat: 1,
    healPct: 0.35,              // hồi % maxHP khi lên cấp
  },

  // ---- quái: g = (planet-1)*30 + round ----
  ENEMY: {
    hp:  function (g) { return Math.round((26 + 9 * g + 0.15 * g * g) * Math.pow(1.015, g)); },
    atk: function (g) { return Math.round((6 + 0.9 * g) * Math.pow(1.013, g)); },
    def: function (g) { return Math.round(1 + 0.35 * g); },
    xp:  function (g) { return Math.round(8 + 3 * g); },
    bossMult:  { hp: 1.8, atk: 1.15, xp: 3 },
    eliteMult: { hp: 1.7, atk: 1.25, xp: 1.8 },
  },
  DMG_VARIANCE: 0.15,           // ±15%
  ENEMY_CRIT: { chance: 0.05, mult: 1.5 },
  ENRAGE: { after: 40, ramp: 0.05 },  // quá N nhịp trận đấu, dmg quái +5%/nhịp (chặn hòa vô hạn)
  SNACK_HEAL: 0.15,             // hồi khi pod sidekick đầy
  PLANET_CLEAR_HEAL: 0.5,       // hồi khi dọn xong hành tinh

  // ---- event mỗi Star Cycle ----
  EVENT_WEIGHTS: {
    battle: 46, fortune: 13, choice: 12, spring: 8, sidekick: 8, trap: 6, treasure: 7,
  },
  MAX_NONBATTLE_STREAK: 2,      // quá số này thì ép battle
  SPRING_HEAL: [0.30, 0.50],    // hồi % maxHP ngẫu nhiên trong khoảng
  TRAP_DMG: [0.08, 0.15],       // mất % maxHP
  TREASURE_XP: [0.5, 1.2],      // × xp của quái cùng g
  MAX_SIDEKICKS: 3,

  // ---- fortune: buff nhỏ tự áp ----
  FORTUNES: [
    { ico: '❤️', name: 'Max HP +7%',   stat: 'maxHpPct', val: 0.07 },
    { ico: '⚔️', name: 'ATK +5%',      stat: 'atkPct',   val: 0.05 },
    { ico: '🛡️', name: 'DEF +2',       stat: 'defFlat',  val: 2 },
    { ico: '🍀', name: 'Crit +3%',     stat: 'crit',     val: 0.03 },
    { ico: '💧', name: 'Heal 20% HP',  stat: 'healPct',  val: 0.20 },
  ],

  // ---- choice: 3 thẻ công nghệ / đột biến ----
  UPGRADES: [
    { ico: '🔋', name: 'Plasma Cell',      desc: 'Cosmic ATK +18%',            stat: 'atkPct',    val: 0.18 },
    { ico: '🧬', name: 'Gene Splice',      desc: 'Max HP +22%',                stat: 'maxHpPct',  val: 0.22 },
    { ico: '🛰️', name: 'Orbital Plating', desc: 'Shield DEF +4',              stat: 'defFlat',   val: 4 },
    { ico: '🩸', name: 'Symbiote Fangs',   desc: 'Lifesteal +8% of damage',    stat: 'lifesteal', val: 0.08 },
    { ico: '🎯', name: 'Targeting Visor',  desc: 'Crit chance +8%',            stat: 'crit',      val: 0.08 },
    { ico: '🌵', name: 'Spike Membrane',   desc: 'Thorns: reflect 20% dmg',    stat: 'thorns',    val: 0.20 },
    { ico: '☢️', name: 'Unstable Core',    desc: 'ATK +30% but Max HP −10%',   stat: 'glass',     val: 0 },
    { ico: '🧪', name: 'Nano Serum',       desc: 'Heal 45% HP instantly',      stat: 'healPct',   val: 0.45 },
  ],

  // ---- sidekick vũ trụ ----
  SIDEKICKS: [
    { id: 'blob',  ico: '🫧', name: 'Gloop',   color: '#6ee7ff', desc: 'adds +15% of your ATK each strike', type: 'dmg',   val: 0.15 },
    { id: 'medic', ico: '💚', name: 'Sporeling', color: '#8bf07a', desc: 'heals 2% Max HP each beat',       type: 'heal',  val: 0.02 },
    { id: 'shield',ico: '🔮', name: 'Orbit',   color: '#c79bff', desc: 'blocks 12% incoming damage',        type: 'block', val: 0.12 },
    { id: 'spark', ico: '⚡', name: 'Zappy',   color: '#ffd35c', desc: 'crit chance +6%',                   type: 'crit',  val: 0.06 },
  ],

  // ---- hành tinh: palette + tên ----
  PLANETS: [
    { name: 'Verdania', sky1: '#2b1b5e', sky2: '#123a6b', lake: '#1fa8a0', lakeDeep: '#0f7c86',
      ground: '#1b6d68', flora: ['#ff5fae', '#c95fff', '#37e0b8'], rock: '#274b7a', moon: '#ffd98a' },
    { name: 'Pyros',    sky1: '#4a1035', sky2: '#7a2410', lake: '#e86a28', lakeDeep: '#a83c0f',
      ground: '#6b2a14', flora: ['#ffd35c', '#ff7a3d', '#ff4a6b'], rock: '#5c2020', moon: '#ffe9c9' },
    { name: 'Glacius',  sky1: '#101a4a', sky2: '#1f5a8f', lake: '#6fd8ff', lakeDeep: '#2e9ad4',
      ground: '#3a6ea8', flora: ['#aef2ff', '#7a9bff', '#e2c9ff'], rock: '#2a4a8a', moon: '#eaf6ff' },
    { name: 'Fungaria', sky1: '#1c0f3a', sky2: '#3a1a5e', lake: '#8a4ad4', lakeDeep: '#5c24a0',
      ground: '#4a2a7a', flora: ['#5cff8f', '#c9ff5c', '#ff9bdd'], rock: '#38205e', moon: '#d3ffb8' },
    { name: 'Voidreach',sky1: '#05030f', sky2: '#1a0a2e', lake: '#e83d8f', lakeDeep: '#8f1458',
      ground: '#2a0f35', flora: ['#ff2e7a', '#8f2eff', '#2effd8'], rock: '#1c0f2e', moon: '#ff9bce' },
  ],

  // ---- bảng màu / hình dáng quái ----
  CRITTER_COLORS: ['#ff6b8f', '#ffa53d', '#8f6bff', '#4ad48f', '#ff5c5c', '#5cb8ff', '#e8d43d'],
  CRITTER_NAMES: ['Wobblor', 'Zorp', 'Muncher', 'Blinkoid', 'Squishex', 'Gnarp', 'Floob', 'Krellik'],
  BOSS_NAMES: ['Overmind Gluttox', 'Warden Xal', 'The Devourer', 'Empress Vex', 'Null Titan'],

  // ---- flavor text ({e}=tên quái, chữ trong *..* tô cam) ----
  TEXT: {
    battle: [
      '*Cosmic Critters* emerge from the bio-luminescent flora; engage defensive protocols!',
      'A wild *{e}* blocks the trail, hissing static; you have no choice but to strike first.',
      'Sensors ping — *{e}* burrows out of the glowshroom bed, fangs first!',
      'The lake bubbles… a hungry *{e}* surfaces and charges!',
      'Your translator crackles: "*{e}* claims this territory. Prepare!"',
    ],
    elite: [
      'An *irradiated {e}* looms ahead, crackling with unstable energy!',
      'This *{e}* has feasted on starlight — larger, meaner, hungrier.',
    ],
    boss: [
      'The ground trembles. *{e}*, tyrant of this world, descends!',
      'All flora dims. *{e}* has found you.',
    ],
    win: [
      'The *{e}* dissolves into stardust. You absorb <b>+{xp} XP</b>.',
      '*{e}* flees into the flora, defeated. <b>+{xp} XP</b> gathered.',
      'Threat neutralized. Cosmic residue grants <b>+{xp} XP</b>.',
    ],
    fortune: [
      'A drifting *spore of luck* settles on your antenna.',
      'You sip glowing dew from a crystal leaf. Refreshing!',
      'A tiny *star fragment* fuses with your suit.',
    ],
    choice: [
      'A derelict *supply pod* cracks open, offering strange tech…',
      'The flora whispers of *mutation*. Choose your evolution:',
      'An ancient vending machine hums to life. *Pick one:*',
    ],
    spring: [
      'You discover a *bio-luminescent spring* and soak your weary tentacles. <b>+{heal} HP</b>.',
      'Friendly micro-critters knit your wounds. <b>+{heal} HP</b>.',
    ],
    sidekick: [
      'A curious *{s}* bobs out of the flora and decides you are its best friend!',
      '*{s}* the cosmic critter joins your voyage!',
    ],
    sidekickFull: [
      'Another critter wants to join, but your pod is full. It gifts you a *snack* instead. <b>+{heal} HP</b>.',
    ],
    trap: [
      'You step on a *snapvine*! It lashes out before you break free. <b>−{dmg} HP</b>.',
      'A gas bloom bursts — *toxic spores*! <b>−{dmg} HP</b>.',
    ],
    treasure: [
      'Half-buried in the moss: an *alien artifact*! Analyzing grants <b>+{xp} XP</b>.',
      'You crack open a *meteor geode* full of knowledge crystals. <b>+{xp} XP</b>.',
    ],
    planetClear: [
      'The skies calm. *{p}* is pacified — your ship beams you to the next world.',
    ],
    levelup: ' <b>Level up! Lv.{lv}</b>',
  },

  GLYPHS: 'ΛΨΦΞΩ⌖⌬⍟⟒⟟⏃⏚⋔☌⍜⎅⍾ϟ∇⊑⋏⌰',
};
