# Addressables — chính sách & quy trình

Addressables (`com.unity.addressables` 2.8.1) được dùng **có chọn lọc và hoàn toàn bằng code** — không ai chỉnh tay trong cửa sổ Groups. File này giải thích cái gì vào bundle, cái gì không, và vì sao.

## Nguyên tắc phân vùng (đọc trước khi thêm entry)

`Main.unity` là scene duy nhất và **luôn được load** — mọi asset nó reference (trực tiếp hoặc qua `GameConfig` → SO con) đã ship trong player build. Đánh dấu một asset như vậy là addressable **không** tách nó khỏi build mà **nhân đôi** nó: một bản trong scene data, một bản trong bundle. Đây chính là anti-pattern mà guard bên dưới chặn.

Chỉ nội dung thỏa **cả hai** điều kiện sau mới vào bundle:

1. **Loại trừ lẫn nhau lúc runtime** — tại một thời điểm chỉ một biến thể được dùng (backdrop realm: chơi ở Frostmoon thì 4 realm kia là bộ nhớ chết).
2. **Có thời điểm swap xác định** để load async không làm khựng gameplay (`EnterWorld` chạy sau banner realm-clear ~3 s — đủ che một lượt tải).

Hiện tại chỉ có **sky backdrop của realm** đạt chuẩn đó — và từ khi bỏ sky baked per-realm (đọc không ra chất tu tiên), cả 5 `World` cùng trỏ về một sprite vẽ tay `azure_side_scroll_v3`, tức group chỉ còn **một entry**. Config SO, prefab, sprite nhỏ, font ở lại player build — chúng cần lúc boot, nhỏ, hoặc bị scene giữ trực tiếp.

## Cấu trúc group (sinh bởi `AddressablesConfigurator`)

| Group | Entry | Address | Label | Đóng gói |
|---|---|---|---|---|
| `WorldBackdrops` | sprite sky lấy từ `World.SkyLayerRef` (hiện cả 5 world chung một sprite ⇒ một entry; address/label mang tên world cuối được duyệt — chỉ là tên hiển thị, runtime load bằng GUID) | `World_X/sky` | `World_X` | `PackTogetherByLabel` → bundle riêng, LZ4, local path |
| `Default Local Group` | *(luôn rỗng)* | — | — | group mặc định Addressables bắt buộc phải có |

- `AddressablesConfigurator.Sync(config)` là **declarative**: tính desired-set từ `GameConfig.Worlds` rồi ép groups khớp — entry lạ (kể cả kéo tay vào) bị xóa ở lần sync sau.
- Sync cũng chốt: play mode = `Use Asset Database` (chơi trong editor không cần build content), player builder = packed, CRC tắt (bundle local, check chỉ tốn thời gian load).
- **Duplication guard**: sau khi sync, mọi entry addressable đồng thời là dependency của `Main.unity` → lỗi đỏ, WebGL build dừng. Nếu gặp lỗi này: bỏ reference trong scene (hoặc bỏ entry), đừng tắt guard.

## Luồng runtime

- `World` không giữ `Sprite` nữa mà giữ `AssetReferenceSprite SkyLayerRef` (SpriteBaker ghi GUID + sub-object mỗi lần build).
- `WorldBackgroundRenderer.Enter(index, world)` stream bundle của realm: backdrop **cũ vẫn hiển thị** đến khi sprite mới về, apply xong mới `Release` handle cũ → texture không bao giờ bị rút giữa frame, và bộ nhớ realm cũ được trả lại (quan trọng trên WebGL).
- Gọi `Enter` đè lên lượt load đang bay là an toàn (lượt cũ bị release và bỏ qua khi hoàn tất).
- `GameManager.ValidateReferences` fail sớm nếu World nào thiếu `SkyLayerRef` hợp lệ.
- **Không dùng `WaitForCompletion()`** — WebGL không hỗ trợ load đồng bộ; mọi chỗ tiêu thụ asset addressable phải chịu được một-vài-frame trễ.

## Quy trình build

- **Tools ▸ Game ▸ Build Game (Full)** — bước gần cuối gọi `AddressablesConfigurator.Sync` nên groups không bao giờ lệch content. Có thể chạy riêng: **Tools ▸ Game ▸ Addressables ▸ Sync Groups**.
- **Tools ▸ Game ▸ Build WebGL (…)** — sync lại, `CleanPlayerContent` + `BuildPlayerContent` (bundle build mới mỗi lần, tránh catalog lệch bundle), rồi mới build player. Content build lỗi ⇒ dừng, không ship build hỏng. Bảng payload trong log có mục riêng `addressables (on demand)` — phần đó tải khi cần, không tính vào first load.

## Thêm nội dung addressable mới

1. Tự hỏi hai điều kiện ở trên — nếu asset luôn cần lúc boot thì **đừng** (cứ wire qua Inspector như cũ).
2. Consumer runtime giữ `AssetReference*` (trên SO/scene object, builder ghi giá trị), load async + giữ handle + `Release` khi thay — theo mẫu `WorldBackgroundRenderer`.
3. Mở rộng desired-set trong `AddressablesConfigurator` (group/address/label mới nếu cần) — **không** kéo tay vào Groups window.
4. Chạy Build Game (Full), xác nhận không có lỗi `[Addressables]` trong console.

## Ghi chú payload liên quan

- `RobotoVN SDF` (7.4 MB trên đĩa) là font dynamic với `m_ClearDynamicDataOnBuild: 1` — atlas bị xóa khi build, glyph raster lại lúc runtime. GameBuilder enforce flag này mỗi lần build (property là internal trong uGUI 2.0 nên ghi qua `SerializedObject`).
- `World.GroundLayer` đã bị gỡ (không có consumer runtime từ khi rework side-scroll); toàn bộ pipeline ground (PaintGround + các PNG `bg_*_ground`) cũng đã xóa khỏi SpriteBaker — mỗi realm giờ chỉ còn một layer sky.
- Sprite sky Azure authored (`azure_side_scroll_v3.png`, ~2 MB) trước đây ship **hai lần** (seed trong scene + qua World SO). Scene giờ để renderer `Sky` sprite = null; backdrop chỉ tồn tại trong bundle của realm đó.
