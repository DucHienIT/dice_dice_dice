# THEME MAPPING — Cosmic Critter Quest → One Tap Immortal (Nhất Niệm Thành Tiên)

Bảng tra cứu một-chạm cho đợt chuyển theme tu tiên (theo `docs/THEME_Immortal.md`).
Cơ chế gameplay và số liệu cân bằng KHÔNG đổi — spec `docs/COSMIC_CRITTER_QUEST_Game_Design_Spec.md`
vẫn là nguồn sự thật về luật chơi; đọc spec với bảng ánh xạ thuật ngữ dưới đây.

## Danh tính sản phẩm

| | Cũ | Mới |
|---|---|---|
| productName | Cosmic Critter Quest | **One Tap Immortal** |
| Tựa hiển thị VI | — | **Nhất Niệm Thành Tiên** |
| companyName | DefaultCompany | **Imba** |
| Scene | `Assets/Scenes/CosmicCritterQuest.unity` | `Assets/Scenes/Main.unity` |
| WebGL template | `CosmicCritterQuestPortrait` | `PortraitGame` |
| CSV localization | `Assets/Localization/CCQ_Localization.csv` | `Assets/Localization/Localization.csv` |
| Save keys | `ccq_run/ccq_best/ccq_meta/ccq_music/ccq_sfx` | `run/best/meta/music/sfx` |

⚠️ productName + companyName dẫn xuất đường lưu save WebGL — đã chốt TRƯỚC bản deploy public đầu tiên.

## Code (generic — dùng lại được cho mọi theme)

| Cũ | Mới |
|---|---|
| namespace `CCQ.*` | `Game.*` |
| `CCQ.Planets` / `Scripts/Planets/` | `Game.Worlds` / `Scripts/Worlds/` |
| `Planet` (class) | `World` |
| `PlanetBackgroundRenderer` | `WorldBackgroundRenderer` |
| `CritterView` / `CritterLook` | `EnemyView` / `EnemyLook` |
| `StarForgeView` | `MetaPathView` |
| `RunState.StarCycle` | `RunState.Cycle` |
| `RoundsPerPlanet` / `PlanetAt` / `ShardsPerPlanetClear` / `PlanetClearHeal` | `RoundsPerWorld` / `WorldAt` / `ShardsPerWorldClear` / `WorldClearHeal` |
| `CritterColors` | `EnemyColors` |
| `CcqGameBuilder/CcqSpriteBaker/CcqWebGLBuilder/CcqLocalizationImporter/CcqBuilderUtil` | bỏ tiền tố `Ccq` |
| menu `Tools/CCQ/*` | `Tools/Game/*` |
| `CreateAssetMenu("CCQ/…")` | `"Game/…"` |
| (mới) | `TribulationFxView` — sét thiên kiếp, `World._melodyDegrees` — giai điệu ngũ cung, `NarrativeConfig._realmNameKeys` — tên cảnh giới theo level |

GameConfig giữ `[FormerlySerializedAs]` vĩnh viễn trên 5 field đổi tên (asset numeric không được builder tái sinh).

## Content id (asset + CSV key, GUID giữ nguyên qua git mv)

**5 Cõi** (`Assets/Data/Worlds/World_*.asset`, key `World/*`):
Verdania→**AzureCloud** (Thanh Vân Sơn / Azure Cloud Peaks) · Pyros→**Emberfall** (Hỏa Diệm Cốc) · Glacius→**Frostmoon** (Hàn Nguyệt Phong) · Fungaria→**Gloomfen** (U Minh Trạch) · Voidreach→**HollowDeep** (Hư Không Uyên)

**8 Công pháp** (`Upgrade/*`):
PlasmaCell→**FlameArt** (Liệt Hỏa Quyết) · GeneSplice→**UndyingBody** (Bất Diệt Thể) · OrbitalPlating→**GoldenBell** (Kim Chung Tráo) · SymbioteFangs→**EssenceDrain** (Hấp Tinh Đại Pháp) · TargetingVisor→**SpiritEye** (Linh Nhãn Thông) · SpikeMembrane→**ReboundForce** (Phản Chấn Công) · UnstableCore→**QiDeviation** (Tẩu Hỏa Nhập Ma) · NanoSerum→**RejuvenationPill** (Hồi Xuân Đan)

**6 bậc meta = thang cảnh giới** (`Meta/*`, id cũng là save id — save meta cũ đã reset):
StarHull→**BodyTempering** (Luyện Thể, HP) · IonEdge→**QiRefining** (Luyện Khí, ATK) · AegisWeave→**Foundation** (Trúc Cơ, DEF) · VoidFangs→**GoldenCore** (Kim Đan, hút máu) · QuillPlate→**NascentSoul** (Nguyên Anh, phản đòn) · LuckyNova→**SpiritSevering** (Hóa Thần, chí mạng)

**4 Pháp bảo** (id save `blob/medic/shield/spark` GIỮ NGUYÊN; display đổi):
Gloop→**FlyingSword** (Phi Kiếm) · Sporeling→**Lingzhi** (Linh Chi) · Orbit→**ShellWard** (Quy Giáp) · Zappy→**ThunderPearl** (Lôi Châu)

**Key pool đổi prefix**: `Narrative/CritterName/*`→`Narrative/BeastName/*` · `Narrative/PlanetClear/*`→`Narrative/WorldClear/*` · `Planet/*`→`World/*` · `UI/Forge/*`→`UI/Path/*` · `UI/Nav/Forge`→`UI/Nav/Path` · `UI/Console/StarCycle`→`UI/Console/Cycle` · `UI/Engage/NewVoyage`→`UI/Engage/NewRun` · `UI/Death/BestVoyage`→`UI/Death/BestRun` · `UI/Banner/PlanetCleared`→`UI/Banner/WorldCleared` · (mới) `Realm/*` ×6

## Thuật ngữ player-facing (đọc spec bằng bảng này)

| Spec / cũ | VI mới | EN mới |
|---|---|---|
| ENGAGE | TU LUYỆN | CULTIVATE |
| Star Cycle | Chu Thiên | Cycle |
| Planet (30 round → warp) | Cõi | Realm |
| Critter | Yêu thú | Demon beast |
| Elite "Irradiated" | Thành Tinh | Awakened |
| Boss | Yêu vương (+ VFX thiên kiếp) | Beast Lord |
| Sidekick | Pháp bảo | Artifact |
| Upgrade card | Công pháp | Technique |
| Fortune | Cơ duyên | Fortuitous encounter |
| Spring / Trap / Treasure | Linh tuyền / Độc chướng / Bí tàng | Spirit spring / Miasma trap / Hidden cache |
| XP / Level / LEVEL UP! | Tu vi / Tầng / ĐỘT PHÁ! | Qi / Stage / BREAKTHROUGH! |
| Enrage | Sát khí | Killing intent |
| Star shards | Linh thạch | Spirit stones |
| Star Forge | Con Đường Tu Luyện | Cultivation Path |
| Death / New voyage | Kiếp này khép lại / CHUYỂN THẾ | This Life Ends / REINCARNATE |
| Warp | Phi thăng | Ascension |
| HUD XP/HP/ATK/DEF | TU VI / KHÍ HUYẾT / KIẾM KHÍ / HỘ THỂ | QI / VITALITY / SWORD QI / WARD |
