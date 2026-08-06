# CODE RULES — DICE DICE DICE!

Quy tắc code bắt buộc cho toàn bộ code first-party trong project. Đọc file này trước khi viết hoặc sửa bất kỳ script nào.

## 1. Cấu trúc thư mục

Toàn bộ code first-party nằm trong `Assets/Scripts/`, chia theo hệ thống:

```
Assets/Scripts/
├── Core/          # GameManager, phase (shopping/wave), vòng đời run
├── Board/         # Bảng 8 ô, slot, đặt/di chuyển/merge item
├── Items/         # Item base + Dice, Weapon, Magic, Support, Defense
├── Combat/        # Projectile, targeting, damage, hiệu ứng khống chế
├── Enemies/       # Quái, wave spawner, boss
├── Economy/       # Vàng, XP, level up
├── Shop/          # Shop logic: mua, reroll, lock, bán
├── Roguelike/     # Upgrade choices và hiệu lực của chúng
├── UI/            # Toàn bộ uGUI code
├── Data/          # Class định nghĩa ScriptableObject (.cs)
└── Utils/         # Helper dùng chung, object pool
```

File `.asset` (instance của ScriptableObject — số liệu cân bằng thật) nằm ở `Assets/Data/`, tách khỏi code.

- **Editor script** đặt trong thư mục `Editor/` (con của thư mục liên quan).
- **KHÔNG** đặt code mới vào thư mục third-party (`JMO Assets`, `Plugins`, `Layer Lab`, `TextMesh Pro`).

## 2. Naming convention (C# chuẩn Unity)

- Class / struct / enum / method / property: `PascalCase`.
- Field private: `_camelCase` (prefix `_`). Field được serialize: `[SerializeField] private Type _name;` — không dùng field public chỉ để hiện trong Inspector.
- Local variable / parameter: `camelCase`.
- Constant: `PascalCase` (không `ALL_CAPS`).
- Enum không dùng prefix (viết `ItemRarity.Common`, không `ItemRarity.RARITY_COMMON`).
- Một class chính mỗi file, tên file trùng tên class.
- Tên theo domain của spec: `Dice`, `ItemRarity` (Common/Rare/Epic/Legendary), `BoardSlot`, `WavePhase`, `ShoppingPhase` — không tự đặt tên khác cho khái niệm đã có trong spec.

## 3. Kiến trúc

- **Data-driven bằng ScriptableObject:** mọi chỉ số cân bằng (giá item, damage, tốc độ roll, vàng theo mặt số, cấu trúc wave, nội dung upgrade...) nằm trong file `.asset` dưới `Assets/Data/` (class định nghĩa ở `Assets/Scripts/Data/`) — KHÔNG hardcode số cân bằng trong logic. Designer phải chỉnh được số mà không sửa code.
- **Tách logic khỏi presentation:** logic gameplay (roll, merge, damage, kinh tế) không phụ thuộc vào UI/VFX. UI lắng nghe qua C# `event`/`Action`; logic không gọi thẳng vào UI.
- **Phase là state machine trung tâm:** trạng thái ShoppingPhase/WavePhase quản lý ở một nơi (GameManager hoặc tương đương). Mọi hành vi phụ thuộc phase (Dice roll, Shop mở/đóng, mua/bán) hỏi state này — không tự track phase riêng lẻ ở từng component.
- **Không Singleton tràn lan:** tối đa một entry point (GameManager). Các hệ thống khác nhận reference qua Inspector hoặc được truyền vào, không `static Instance` khắp nơi.
- **Object pooling bắt buộc** cho projectile, quái, hiệu ứng, số bay (damage/gold popup) — không `Instantiate`/`Destroy` liên tục trong wave.

## 4. Quy tắc Unity cụ thể

### Code phải tường minh — mọi thứ khai báo sẵn trong scene/prefab

- **CẤM bootstrap runtime:** không tạo GameObject hay gắn script lúc runtime mà không tồn tại sẵn trong scene hoặc prefab. Cụ thể:
  - Không `new GameObject(...)` + `AddComponent<T>()` để dựng hệ thống lúc chạy.
  - Không dùng `[RuntimeInitializeOnLoadMethod]` để tự khởi tạo manager/singleton ẩn.
  - Không có pattern "tự tạo Instance nếu chưa có" (`if (Instance == null) new GameObject(...)`).
  - Mọi manager, hệ thống, UI đều phải là object đặt sẵn trong scene hoặc prefab được instantiate tường minh — nhìn Hierarchy/Project là thấy được toàn bộ hệ thống.
- **Không `AddComponent`:** component phải được gắn sẵn trên prefab/scene object. Object động (projectile, quái, popup) là prefab đầy đủ component, lấy ra từ pool.
- **Không `GetComponent`/`GetComponentInChildren`/`TryGetComponent`** để tìm reference lúc runtime — mọi reference khai báo `[SerializeField]` và kéo thả trong Inspector (hoặc gán sẵn trong prefab). Ngoại lệ duy nhất: đọc component từ kết quả physics (collision/trigger/raycast/overlap — không có cách nào khác). Ưu tiên hơn nữa: manager đăng ký sẵn map `Collider → Enemy` khi spawn (tra dictionary thay vì `GetComponent` mỗi lần va chạm).
- Không dùng `GameObject.Find`, `FindObjectOfType`, tìm theo tag/tên — reference qua Inspector.
- Không dùng chuỗi (`Invoke("MethodName")`, `SendMessage`) — dùng gọi trực tiếp hoặc event.
- `Update` chỉ ở nơi thật sự cần chạy mỗi frame; timer đơn giản (roll cooldown, attack cooldown) dùng cộng dồn `Time.deltaTime`, không tạo coroutine tràn lan.
- Tween dùng **DOTween** (đã có sẵn), đặt `SetLink(gameObject)` để tween tự hủy theo object.
- Không dùng `Resources.Load` cho asset gameplay — reference qua Inspector/ScriptableObject.
- Text dùng **TextMeshPro**, không dùng `UnityEngine.UI.Text` cũ.
- Input dùng **Input System mới** (`InputSystem_Actions.inputactions`), không dùng `Input.GetKey` cũ.

## 5. Performance — ưu tiên hàng đầu

Game chạy mobile với nhiều quái + projectile + popup mỗi wave. Mọi code trong gameplay loop phải viết theo tư duy "frame budget".

### 5.1. Zero allocation trong hot path

Hot path = mọi code chạy mỗi frame hoặc nhiều lần mỗi giây trong wave (`Update`, targeting, di chuyển quái, projectile, damage). Trong hot path:

- **Không tạo garbage:** không `new` class/array/List, không LINQ (`Where`, `Select`, `OrderBy`, `Any`...), không `foreach` trên interface (`IEnumerable`), không string concat/`$"..."`, không boxing (đưa struct/enum vào `object`).
- Không lambda/closure bắt biến ngoài trong hot path (mỗi lần tạo là một allocation) — cache delegate một lần ở `Awake` nếu cần.
- Collection dùng lại: khai báo `List`/mảng làm field, `Clear()` rồi dùng lại thay vì tạo mới. Khởi tạo với capacity dự kiến (`new List<Enemy>(64)`).
- Physics query dùng bản không cấp phát: overload nhận buffer/`List` cấp sẵn (Unity 6 khuyến nghị overload nhận `ContactFilter2D` + `List<T>`; các API `*NonAlloc` cũ một phần đã obsolete). Lưu ý: targeting quái KHÔNG dùng physics query — dùng list của manager (mục 5.2); physics chỉ dành cho hit detection của projectile/AoE nếu cần.
- So khoảng cách dùng `sqrMagnitude` (so với bình phương range), không `Vector3.Distance`/`magnitude` (tránh sqrt).
- Coroutine: cache `WaitForSeconds` thành field nếu lặp lại; ưu tiên timer cộng `Time.deltaTime` cho cooldown (đã quy định ở mục 4).
- Text cập nhật thường xuyên (vàng, HP): chỉ set khi giá trị đổi, không set mỗi frame; số format dùng cache chuỗi cho giá trị nhỏ lặp lại nếu profiler chỉ ra vấn đề.

### 5.2. Cấu trúc update hiệu quả

- **Update manager thay vì trăm `Update()`:** quái, projectile, Dice roll timer... không tự có `Update()` riêng. Mỗi hệ thống có một manager giữ danh sách active object và tick chúng trong một `Update()` duy nhất (ví dụ `EnemyManager.Update()` lặp qua list quái). Vài trăm `MonoBehaviour.Update()` riêng lẻ có overhead interop đáng kể.
- Targeting/scan mục tiêu không cần chạy mỗi frame — chạy theo interval (ví dụ 0.1–0.2s) hoặc khi danh sách quái thay đổi; giữa các lần scan thì dùng target đã cache.
- Quái/projectile active track bằng danh sách trong manager — **không** `FindObjectsOfType` hay physics scan toàn màn để "tìm quái"; manager là source of truth (quái spawn thì add vào list, chết thì remove).
- Cache mọi thứ truy cập lặp lại: `Transform` (field, không gọi `.transform` lặp trong loop), `Time.deltaTime` đọc một lần mỗi frame nếu dùng nhiều lần. Camera reference qua `[SerializeField]` trong Inspector — không dùng `Camera.main` (bản chất là tìm theo tag, vi phạm mục 4).
- `transform.position`/`rotation`: đọc một lần vào biến local, tính toán, ghi lại một lần — không đọc/ghi property nhiều lần trong một phép tính.

### 5.3. Object pooling (chi tiết hóa mục 3)

- Pool cho: projectile, quái, VFX, popup số (damage/gold), audio one-shot.
- **Prewarm** pool trong loading/trước wave — không để `Instantiate` xảy ra giữa wave gây spike.
- Trả về pool bằng `SetActive(false)` + reset state trong method `OnDespawn` tường minh; không dựa vào `OnDisable` để chứa logic quan trọng.
- Pool hết thì mở rộng có kiểm soát (log warning để tune size), không âm thầm `Instantiate` từng cái.

### 5.4. uGUI performance

- **Chia Canvas theo tần suất thay đổi:** một element thay đổi làm rebuild cả Canvas chứa nó. Tách riêng: Canvas tĩnh (khung, nền), Canvas HUD thay đổi thường xuyên (vàng, HP, XP bar), Canvas popup/hiệu ứng. Không dồn tất cả vào một Canvas.
- Tắt `Raycast Target` trên mọi Image/Text không cần nhận input (đa số là không cần).
- Không dùng Layout Group (`HorizontalLayoutGroup`...) + `ContentSizeFitter` cho UI thay đổi trong gameplay — chúng rebuild đắt; layout động thì set vị trí bằng code. Layout Group chỉ chấp nhận được cho UI tĩnh dựng một lần (Shop list, upgrade panel).
- Ẩn UI bằng cách disable `Canvas` component (hoặc `CanvasGroup.alpha = 0` + tắt interactable/raycast), không `SetActive(false)` cả hierarchy lớn nếu sẽ bật lại thường xuyên (tránh chi phí rebuild khi bật).
- Sprite UI đóng gói vào Sprite Atlas để giảm draw call.

### 5.5. Rendering & mobile

- `Application.targetFrameRate = 60` set tường minh ở khởi động (mặc định mobile là 30).
- Hạn chế overdraw: không xếp chồng nhiều layer sprite/UI trong suốt full-screen; particle giới hạn max particles và không dùng particle trong suốt phủ màn hình.
- Material dùng chung (shared) cho object cùng loại để batch được; **không** `renderer.material` (tạo instance material — phá batching và leak) — đổi màu per-object thì dùng `MaterialPropertyBlock` hoặc màu vertex/sprite color.
- Shadow, post-processing đắt tiền tắt trên mobile trừ khi art direction yêu cầu và đã profile.

### 5.6. Đo trước khi tối ưu thêm

- Các quy tắc trên là mặc định (rẻ để tuân thủ ngay từ đầu). Tối ưu **sâu hơn** (job system, burst, cache-friendly layout...) chỉ làm khi Profiler chỉ ra bottleneck thật — không phức tạp hóa code vì vấn đề chưa đo được.
- Khi nghi ngờ performance: dùng Unity Profiler (qua MCP `manage_profiler`) trên thiết bị/target thật, nhìn GC Alloc và CPU time, sửa đúng chỗ nóng nhất.

## 6. Clean code

- **Mỗi class một trách nhiệm**, đặt tên nói rõ trách nhiệm đó (`DiceRollTimer`, `ShopRerollService` — không `GameHelper`, `Utils2`, `ManagerManager`).
- Method ngắn, làm một việc; ưu tiên **guard clause / early return** thay vì if lồng sâu (tối đa ~2 tầng lồng).
- Không viết trước cho tương lai (YAGNI): không interface/abstraction cho thứ mới có một implementation, không tham số "để sau này dùng". Thêm abstraction khi có nhu cầu thứ hai thật sự.
- Không copy-paste logic — lặp đến lần thứ hai thì tách method/class chung.
- State phơi bày tối thiểu: field `private` mặc định, property chỉ có setter khi thật sự cần bên ngoài ghi; dùng `readonly` cho reference gán một lần.
- Điều kiện phức tạp tách thành biến/method có tên (`bool canBuy = ...` hoặc `CanAfford(item)`) thay vì biểu thức dài trong `if`.
- Event đặt tên theo chuyện đã xảy ra (`OnWaveEnded`, `OnDiceRolled`), subscriber tự lo việc của mình — publisher không biết ai nghe.
- Không comment giải thích "code làm gì" (code tự nói); chỉ comment "tại sao" khi có ràng buộc không hiển nhiên (ví dụ liên quan luật trong spec).
- Xử lý null tường minh: reference Inspector bắt buộc thì validate trong `Awake` (fail sớm, log rõ thiếu gì) thay vì NullReference giữa game.

## 7. OOP & SOLID

Áp dụng SOLID theo hướng thực dụng cho Unity — mục tiêu là code dễ mở rộng nội dung game (thêm item, quái, upgrade mới) mà không sửa code cũ.

### 7.1. Nguyên tắc OOP nền tảng

- **Composition hơn inheritance:** Unity là component-based — ghép hành vi bằng nhiều component/class nhỏ thay vì cây kế thừa sâu. Cây kế thừa tối đa 2 tầng (ví dụ `ItemBase` → `BowItem`), sâu hơn là dấu hiệu cần tách composition.
- **Encapsulation triệt để:** state là `private`, bên ngoài tương tác qua method/event có tên theo hành vi (`TakeDamage(int)`, không `set Hp`). Không có class nào "thò tay" chỉnh field của class khác.
- **Không God object:** GameManager chỉ điều phối phase và vòng đời run — không chứa logic shop, combat, kinh tế. Mỗi hệ thống trong mục 1 tự quản logic của mình.

### 7.2. SOLID áp dụng cụ thể

- **S — Single Responsibility:** một class, một lý do để thay đổi. Ví dụ Dice tách thành: logic roll/timer, logic tạo vàng, và view (animation, hiển thị mặt số) — sửa hiệu ứng roll không đụng vào logic kinh tế.
- **O — Open/Closed:** thêm nội dung mới bằng cách **thêm** asset/class, không **sửa** code cũ. Cấm switch-case/if-else theo loại item hay loại quái rải rác trong code (`switch (itemType)`) — hành vi riêng của từng loại nằm trong class/ScriptableObject của loại đó (polymorphism hoặc data-driven). Thêm item thứ 12 không được phép sửa file nào ngoài file của chính nó + data.
- **L — Liskov Substitution:** class con phải dùng được ở mọi nơi class cha được dùng. Không override method thành no-op hay throw để "vô hiệu hóa" hành vi thừa kế — nếu phải làm vậy nghĩa là kế thừa sai chỗ (ví dụ: Dice không kế thừa từ class item chiến đấu rồi vô hiệu hóa attack; tách hẳn nhánh economy và combat).
- **I — Interface Segregation:** interface nhỏ theo vai trò: `IDamageable`, `IMergeable`, `ITickable`, `ISellable`... — không một interface `IItem` khổng lồ bắt mọi item implement cả những gì nó không có. Hệ thống chỉ phụ thuộc đúng interface nó cần (damage system chỉ biết `IDamageable`, không biết đó là quái hay boss).
- **D — Dependency Inversion:** hệ thống cấp cao phụ thuộc abstraction, không phụ thuộc chi tiết. Trong Unity project này nghĩa là: logic gameplay phát `event`/gọi interface, không reference trực tiếp class UI/VFX cụ thể (đã quy định ở mục 3); wiring cụ thể diễn ra qua Inspector — Inspector chính là composition root của project.

### 7.3. Giới hạn — SOLID không phải mục tiêu tự thân

- Tuân thủ YAGNI ở mục 6: **không** tạo interface/abstraction khi mới có một implementation và chưa có nhu cầu thay thế thật (interface có từ 2 implementer trở lên, hoặc là ranh giới hệ thống như `IDamageable`). SOLID dùng để code dễ thêm nội dung, không phải để tăng số file.

## 8. Chất lượng & quy trình

- Sau mỗi lần thêm/sửa script: compile-check qua Unity MCP (`validate_script` + `read_console`) — không được để lại compile error hoặc warning mới.
- Không commit code chết (method/field không dùng), không để `Debug.Log` rác trong code hoàn thiện (log quan trọng thì có prefix hệ thống, ví dụ `[Shop]`).
- Magic number trong logic phải thành constant có tên hoặc field trong ScriptableObject config.
- Logic gameplay phải tôn trọng tuyệt đối luật trong `docs/DICE_DICE_DICE_Game_Design_Spec.md` (đặc biệt mục 32) — code không được "sáng tạo" thêm cơ chế.
