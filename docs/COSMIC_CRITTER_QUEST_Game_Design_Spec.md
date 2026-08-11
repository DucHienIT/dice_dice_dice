# SPEC — Cosmic Critter Quest

Design document. Mọi con số cân bằng nằm trong **config trung tâm** — trong project Unity này là ScriptableObject config (class định nghĩa ở `Assets/Scripts/Data/`, file `.asset` ở `Assets/Data/`, theo CODE_RULES). Các tên viết hoa trong tài liệu (`EVENT_WEIGHTS`, `ENRAGE`, `PLAYER`…) là tên nhóm/field trong config đó — **chỉnh cân bằng ở đó, không sửa rải rác trong code**.

## Core loop

```
tap ENGAGE → Star Cycle +1 → roll event
   ├─ battle (46%) ──→ auto-battle theo nhịp → thắng: XP/level, Round +1 → quay về READY
   ├─ fortune (13%) ─→ buff nhỏ tự áp, banner "Small fortune"
   ├─ choice (12%) ──→ 3 thẻ upgrade, người chơi chọn 1 (điểm quyết định duy nhất)
   ├─ spring/trap/treasure ─→ hồi máu / mất máu / XP
   └─ sidekick (8%) ─→ đồng đội passive (tối đa 3)
30 Round / hành tinh → warp, palette + độ khó mới. Chết = hết run, lưu best.
```

Nhịp thiết kế: người chơi chỉ có **một nút** + thi thoảng một lựa chọn 3 thẻ — mọi độ sâu nằm ở build (upgrade + sidekick stack) và quản lý HP giữa các trận (biết khi nào mình "còn cửa" gặp boss).

## Các hệ thống

### 1. Event roll (`EVENT_WEIGHTS`)
Weighted random; chống chuỗi nhàm: quá `MAX_NONBATTLE_STREAK` (2) event không-battle liên tiếp thì ép battle — giữ tỉ lệ battle thực tế ~55-60%, cảm giác "đi tới đâu đánh tới đó" như game gốc.

### 2. Combat
- Turn-based theo nhịp `BEAT_MS` (550ms ÷ speed), hero đánh trước, luân phiên.
- Damage = `atk × rand(0.85–1.15) − def`, tối thiểu 1. Crit hero mặc định 8% ×1.6; quái 5% ×1.5 (`ENEMY_CRIT`).
- Animation lunge 0.35s, damage áp tại t≈0.45 (điểm chạm), số damage bay lên bằng floater (pooled).
- Hero hồi `regenPct` (12% maxHP) sau mỗi trận thắng — sustain cơ bản để không bắt buộc build hồi máu.
- **Enrage** (`ENRAGE`): quá 40 nhịp trong một trận, dmg quái +5%/nhịp — chặn build hồi máu hòa vô hạn với quái.

### 3. Scaling quái — `g = planet×30 + round`
| chỉ số | công thức | g=1 | g=15 | g=30 |
|---|---|---|---|---|
| HP  | `(26 + 9g + 0.15g²) × 1.015^g` | 36 | 243 | 674 |
| ATK | `(6 + 0.9g) × 1.013^g` | 7 | 24 | 49 |
| DEF | `1 + 0.35g` | 1 | 6 | 12 |
| XP  | `8 + 3g`    | 11 | 53 | 98 |

- Boss (round % 10 == 0): HP ×1.8, ATK ×1.15, XP ×3.
- Elite (12% chance): HP ×1.7, ATK ×1.25, XP ×1.8.
- Phần đa thức quyết định cảm giác early game; phần mũ `1.015^g` bảo đảm mọi run **có kết thúc** — player tăng trưởng theo level sẽ thua hàm mũ về lâu dài.

### 4. Người chơi
- Base: 160 HP / 15 ATK / 3 DEF (`PLAYER`).
- XP cần: `16 × lv^1.55` — lên cấp nhanh đầu run rồi chậm dần. Mỗi cấp: HP +14%, ATK +11%, DEF +1, hồi 35% maxHP.
- Level là trục tăng trưởng "đảm bảo"; upgrade/fortune là trục "may rủi + lựa chọn" chồng lên.

### Kết quả sim cân bằng (200 run, tap liên tục, pick ngẫu nhiên — sim chạy lúc thiết kế, ngoài project)
- Chết trước boss 1: ~0.5-2% · median chết: g≈18 (hành tinh 1, round ~18) · p75: g≈24.
- ~10% lên hành tinh 2, ~3% lên hành tinh 3; hiếm (≈0.5%) build sustain hoàn hảo đi rất xa — chấp nhận như "god run" của thể loại idle.
- Người chơi thật pick thẻ có chủ đích nên kỳ vọng đi xa hơn sim một chút.

### 5. Upgrade & sidekick
- 8 thẻ upgrade (`UPGRADES`) — cố tình có thẻ trade-off (Unstable Core: ATK +30% / HP −10%) và thẻ tình huống (Nano Serum hồi 45% chỉ đáng giá khi sắp gặp boss).
- 4 sidekick (`SIDEKICKS`), giữ tối đa 3 → luôn phải bỏ 1 loại: dmg (+15% ATK/hit), heal (2% maxHP/nhịp đánh của hero), block (−12% damage nhận), crit (+6%).
- Sidekick vẽ thành orb lơ lửng sau lưng hero + chip ở panel dưới.

### 6. Hành tinh & presentation
- 5 palette (`PLANETS`) xoay vòng, mỗi cái đổi mood: Verdania (teal/hồng) → Pyros (lửa) → Glacius (băng) → Fungaria (nấm) → Voidreach (void).
- Background render một lần và cache theo planet (sao, trăng, đá, hồ phát quang, flora — seeded RNG nên cố định trong 1 hành tinh).
- Quái sinh hình procedural: màu / 1-3 mắt / sừng / đốm / size từ `look` — cùng data, khác mặt.
- Glyph console: chuỗi ký tự alien random mỗi event, thuần trang trí (bán "mood phi thuyền dịch tín hiệu").

### 7. Save
- `ccq_run`: toàn bộ player + day/round/planet/hits/speed — ghi sau mỗi event kết thúc (không ghi giữa trận; reload giữa trận sẽ về trạng thái trước trận).
- `ccq_best`: điểm = tổng round toàn cục, chỉ ghi đè khi cao hơn.
- Prefix `ccq_` làm namespace cho key save.

## Hướng tuning

- Game quá dễ/khó → chỉnh cặp `ENEMY.hp/atk` trước, giữ nguyên player.
- Trận kéo dài lê thê → giảm `ENEMY.hp` hệ số g², hoặc tăng `LEVEL.atkPct`.
- Chết vặt vì trap/elite đầu game → giảm `TRAP_DMG` max, `ELITE_CHANCE`.
- Muốn "roguelike" hơn → tăng weight `choice`, giảm `fortune` (chuyển power từ ngẫu nhiên sang lựa chọn).
- Boss phải là "cửa ải build-check": nếu ai cũng qua được boss 10 mà không cần upgrade nào → tăng `bossMult.hp` lên ~2.2 (lưu ý: từng để 2.5 thì 75% run chết đúng boss 1 — chỉnh từng nấc 0.2).
