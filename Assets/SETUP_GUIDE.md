# Match3Core — Hướng dẫn setup trên Unity 6

Bộ script này implement phần **core** của một board Match-3: sinh bàn cờ (generation), kéo-thả để swap (drag), phát hiện match, rơi tile (collapse/gravity), refill và xử lý cascade (combo liên hoàn) — toàn bộ animation dùng **DOTween**.

---

## 1. Yêu cầu

- **Unity 6** (6000.x)
- **DOTween** (bản Free trên Asset Store, hoặc HOTween/DOTween qua Package Manager)

---

## 2. Cài đặt DOTween

1. Mở **Asset Store** trong Unity (hoặc Package Manager nếu bạn dùng bản UPM), tìm **"DOTween (HOTween v2)"** của Demigiant và import vào project.
2. Sau khi import xong, vào **Tools > Demigiant > DOTween Utility Panel**, bấm **"Setup DOTween..."** rồi để mặc định và bấm **Apply**. Bước này chỉ cần làm 1 lần cho cả project.

---

## 3. Import Scripts

Copy toàn bộ thư mục `Scripts/` (trong file zip này) vào trong `Assets/` của project, ví dụ: `Assets/Match3Core/Scripts/`.

Cấu trúc thư mục (mỗi thư mục tương ứng 1 namespace `Match3.<TênThưMục>`):

```
Scripts/
├── Data/            Match3.Data        (ScriptableObject config)
├── Model/           Match3.Model       (dữ liệu bàn cờ thuần, không phụ thuộc Unity)
├── Generation/      Match3.Generation  (sinh bàn cờ / xáo trộn)
├── Matching/        Match3.Matching    (tìm match)
├── Swap/            Match3.Swap        (kiểm tra swap hợp lệ, phát hiện bí nước đi)
├── Collapse/        Match3.Collapse    (rơi tile + refill)
├── StateMachine/    Match3.StateMachine (Idle / Swapping / Resolving)
├── View/            Match3.View        (TileView, BoardView, object pool)
├── InputHandling/   Match3.InputHandling (kéo-thả)
├── Gameplay/        Match3.Gameplay    (BoardController — orchestrator chính)
└── Utilities/       Match3.Utilities   (cầu nối DOTween ↔ Coroutine)
```

Nếu project của bạn đặt DOTween trong một **Assembly Definition** riêng và bạn cũng bọc các script này trong 1 `.asmdef`, nhớ thêm reference tới asmdef của DOTween.

---

## 4. Tạo ScriptableObject assets

### 4.1. Tile Type Data (mỗi loại gem/tile 1 asset)

Trong Project window: **Right click > Create > Match3 > Tile Type Data**.

Tạo ít nhất **4–6 asset** (ví dụ `TileType_Red`, `TileType_Blue`, `TileType_Green`, `TileType_Yellow`, `TileType_Purple`...). Với mỗi asset, gán:
- **Sprite**: sprite hình vuông/tròn/gem của bạn (nếu chưa có art, dùng tạm sprite built-in `Knob` hoặc `UISprite` của Unity để test).
- **Color**: màu để phân biệt các loại (đặc biệt hữu ích nếu tạm dùng chung 1 sprite trắng cho mọi loại).

> ⚠️ Số lượng Tile Type Data tối thiểu phải ≥ **Min Match Length** (mặc định 3), nếu không `BoardController` sẽ báo lỗi và tự tắt.

### 4.2. Board Config

**Right click > Create > Match3 > Board Config**, đặt tên ví dụ `BoardConfig_Main`. Cấu hình:
- **Width / Height**: kích thước lưới (ví dụ 8x8).
- **Cell Size**: khoảng cách giữa các tile theo world unit (ví dụ 1).
- **Tile Types**: kéo toàn bộ asset `TileTypeData` vừa tạo vào list này. **Thứ tự trong list chính là TypeId** (index 0, 1, 2...), không có field Id riêng nên không thể bị lệch.
- **Min Match Length**: số tile tối thiểu để tính là 1 match (mặc định 3).
- **Swap / Fall / Spawn / Destroy Duration**: thời lượng các animation (giây).

---

## 5. Tạo Tile Prefab

1. Tạo 1 GameObject mới (**Create Empty**), đặt tên `Tile`.
2. Add Component **Sprite Renderer**.
3. Add Component **Tile View** (script `Match3.View.TileView`) — script này tự `[RequireComponent]` Sprite Renderer nên sẽ tự thêm nếu bạn quên.
4. Kéo GameObject này vào thư mục Project để tạo **Prefab**, rồi xoá khỏi Scene (prefab sẽ được instantiate lúc runtime qua object pool, không cần để sẵn trong scene).

> Lưu ý: `BoardInputController` xác định ô lưới bằng toán học tọa độ (screen → world → grid), **không dùng Collider/Raycast**, nên Tile prefab **không cần Collider2D**.

---

## 6. Setup Scene

### 6.1. Camera

- Chọn **Main Camera**, set **Projection = Orthographic**.
- Đặt camera nhìn thẳng theo trục Z vào mặt phẳng board (ví dụ camera ở `(x, y, -10)`, board ở Z = 0).
- Chỉnh **Size** đủ để nhìn thấy toàn bộ board. Công thức tham khảo: `Size ≈ Height / 2 + 1` (theo chiều dọc), rồi kiểm tra lại theo chiều ngang dựa trên aspect ratio màn hình.

### 6.2. Tạo GameObject cho board

Tạo 1 GameObject trống tên `Match3Board`. Board tự động **căn giữa GameObject này** (không còn tính theo góc dưới-trái nữa), nên cứ để Position `(0,0,0)` là board sẽ nằm giữa camera. Muốn dịch board sang một bên (ví dụ chừa chỗ cho UI), sửa **`Board Offset`** trong asset `BoardConfig` thay vì di chuyển GameObject — cách này "dynamic" hơn vì board tự tính lại đúng vị trí căn giữa mỗi khi Width/Height/CellSize đổi.

Add 3 component sau lên **cùng GameObject** `Match3Board` (hoặc tách ra các GameObject con tuỳ bạn, miễn là gán đúng reference ở bước 6.3):
- `Board View` (`Match3.View.BoardView`)
- `Board Input Controller` (`Match3.InputHandling.BoardInputController`)
- `Board Controller` (`Match3.Gameplay.BoardController`)

### 6.3. Gán reference trong Inspector

**Board View**:
- `Config` → kéo asset `BoardConfig_Main` vào.
- `Tile Prefab` → kéo prefab `Tile` vào.
- `Tile Container` → để trống cũng được (tự tạo child `"Tiles"` lúc Awake), hoặc gán 1 Transform con nếu muốn tự quản lý.

**Board Input Controller**:
- `Board View` → kéo chính GameObject `Match3Board` (component BoardView) vào.
- `Input Camera` → kéo Main Camera vào (nếu để trống sẽ tự dùng `Camera.main`).

**Board Controller**:
- `Config` → kéo asset `BoardConfig_Main` vào.
- `Board View` → kéo BoardView vào.
- `Input Controller` → kéo BoardInputController vào.

---

## 7. Chạy thử

Bấm **Play**. Board sẽ tự sinh (không có match sẵn từ đầu), sau đó bạn có thể kéo 1 tile sang ô kề (trái/phải/trên/dưới):
- Nếu swap tạo ra match → tile hoán đổi, match biến mất, các tile phía trên rơi xuống, tile mới rơi vào lấp chỗ trống, và tự động kiểm tra combo (cascade) cho đến khi bàn cờ ổn định.
- Nếu swap không tạo match → 2 tile tự động hoán đổi trở lại vị trí cũ.
- Nếu sau khi giải xong mà bàn cờ **hết nước đi hợp lệ**, hệ thống tự xáo trộn (reshuffle) lại toàn bộ tile.

---

## 8. Kiến trúc & điểm mở rộng

`BoardController` bắn ra 4 sự kiện C# (`System.Action`) để bạn gắn thêm tính năng (điểm số, âm thanh, UI...) mà **không cần sửa code core**:

```csharp
boardController.BoardGenerated   += () => { /* bàn cờ vừa sinh xong */ };
boardController.TilesSwapped     += (from, to) => { /* swap hợp lệ vừa xảy ra */ };
boardController.MatchesResolved  += (matchedTileCount) => { /* vừa xoá bao nhiêu tile */ };
boardController.CascadeCompleted += () => { /* toàn bộ chuỗi combo đã xử lý xong, board trở lại Idle */ };
```

Tổng quan các pattern đã áp dụng (đối chiếu tài liệu Game Programming Patterns bạn cung cấp):
- **Object Pool** (`UnityEngine.Pool.ObjectPool<TileView>` built-in của Unity 6) — tái sử dụng TileView thay vì Instantiate/Destroy liên tục.
- **State Machine** (`IBoardState` / `BoardStateMachine`) — 3 trạng thái Idle / Swapping / Resolving, chặn input khi board đang animate.
- **Observer** (C# `event Action`) — BoardStateMachine, BoardInputController và BoardController đều giao tiếp qua event thay vì gọi trực tiếp lẫn nhau.
- **Dependency Inversion / SOLID** — mọi service (`IMatchFinder`, `IBoardGenerator`, `ISwapValidator`, `ICollapseResolver`) đều đứng sau interface, `BoardController` chỉ điều phối (Presenter), không chứa rule logic.
- **MVP-ish tách lớp** — `Model` (BoardModel/Tile) hoàn toàn thuần C#, `View` (TileView/BoardView) chỉ lo hiển thị + animation, `Gameplay` (BoardController) là lớp điều phối ở giữa.

Muốn thêm tính năng mới (điểm số, tile đặc biệt/power-up, giới hạn số nước đi...) thì nên tạo class/service mới rồi cắm vào qua các event trên hoặc mở rộng `BoardController`, tránh sửa trực tiếp vào `Matching`/`Collapse`/`Generation` để giữ các lớp đó gọn và dễ test.

---

## 9. Về Input System

`BoardInputController.cs` tự nhận diện backend đang bật qua compiler directive `ENABLE_INPUT_SYSTEM` (cờ Unity tự set dựa trên **Player Settings > Active Input Handling**):
- **Input System Package (New)** → dùng `Mouse.current` / `Touchscreen.current`.
- **Input Manager (Old)** hoặc **Both** → dùng `UnityEngine.Input` (legacy).

Nghĩa là bạn **không cần đổi Project Settings gì cả** — file này compile và chạy đúng với bất kỳ lựa chọn Active Input Handling nào. Toàn bộ phần phụ thuộc backend nằm gọn trong 2 hàm private `TryGetPointerDownThisFrame` / `TryGetPointerUpThisFrame` ở cuối file, nên nếu sau này muốn tuỳ biến thêm (multi-touch, pen, v.v.) thì chỉ cần sửa đúng 2 chỗ đó.

> Nếu vẫn gặp lỗi `InvalidOperationException: ... Input System package` sau khi cập nhật file, khả năng cao là bạn đang dùng bản `BoardInputController.cs` cũ từ lần tải trước — hãy thay bằng bản mới nhất trong `Scripts/InputHandling/`.

---

## 10. Tuỳ chỉnh nhanh

| Muốn thay đổi | Sửa ở đâu |
|---|---|
| Kích thước bàn cờ | `BoardConfig.Width` / `Height` |
| Độ khó (nhiều màu hơn = khó hơn) | Thêm/bớt asset trong `BoardConfig.TileTypes` |
| Match 4/5 mới tính | `BoardConfig.MinMatchLength` |
| Tốc độ animation | `BoardConfig.SwapDuration` / `FallDuration` / `SpawnDuration` / `DestroyDuration` |
| Ngưỡng để tính là "kéo" (drag) | `BoardInputController._dragThresholdPixels` (Inspector) |
| Ease/kiểu animation | Các hàm `AnimateMoveTo` / `AnimateSpawn` / `AnimateRemoval` trong `TileView.cs` |

---

Chúc bạn build game vui vẻ! Nếu cần mở rộng thêm (score system, power-up tile, giới hạn moves/objectives, UI...), phần core này đã được thiết kế theo interface + event nên có thể cắm thêm mà không cần đụng vào logic gen/match/collapse hiện tại.

---

# Phần 2 — RPG Battle Layer (Phase 1)

Phần này build trên nền Core ở Phần 1, thêm hệ thống chỉ số + resolve theo `harder.txt`. Đây là **Phase 1**: data/stat, tile-kind resolve, turn/cycle, damage/shield. AI "Thông minh", timer, tốc độ game, UI đều là các phase sau.

## 2.1. Core đã thay đổi gì (để bạn biết nếu có tuỳ biến riêng)

- `BoardController.MatchesResolved` đổi kiểu từ `Action<int>` → `Action<MatchSearchResult>` (cần dữ liệu chi tiết loại tile đã ăn, không chỉ số lượng).
- `BoardController` có thêm 2 method public: `RequestSwap(from, to)` (tách từ logic input cũ, để AI cũng gọi được) và `GetAllValidMoves()`.
- `BoardModel` implement thêm interface `IReadOnlyBoardModel` (không đổi behavior, chỉ thêm cách expose an toàn ra ngoài).
- `BoardInputController` có thêm `SetExternalGate(bool)` — 1 cổng khoá input độc lập với cổng Idle/Busy cũ, để BattleController khoá input theo lượt mà không đụng vào cơ chế cũ.
- `ISwapValidator` có thêm `GetAllValidMoves(...)` — AI Random dùng cái này để chọn nước đi.

Tất cả đều là bổ sung thuần (additive), không phá hành vi cũ của Core.

## 2.2. Tạo ScriptableObject assets mới

### a. Gán TileKind cho 6 loại tile hiện có
**Create > Match3 > Battle > Battle Tile Config**. Gán `Board Config` (asset cũ), rồi trong list `Tile Kinds`, thêm 6 entry — mỗi entry kéo 1 asset `TileTypeData` bạn đã tạo ở Phần 1 vào, và chọn `Kind` tương ứng: HP, VHP, Mana, Sword, Slash, Shield (đúng 6 loại theo `harder.txt`, nên bạn cần đúng 6 `TileTypeData` — nếu trước đó tạo nhiều/ít hơn, chỉnh lại `BoardConfig.TileTypes` cho khớp).

### b. Character Config (2 asset — 1 cho Player, 1 cho Enemy)
**Create > Match3 > Battle > Character Config**. Đặt Max HP, Swordrain Damage, Slash Damage, Max VHP (tự kẹp ≤ MaxHP/2), Attack Type (Melee/Ranged), Ranged Object Count.

### c. Battle Tuning Config
**Create > Match3 > Battle > Battle Tuning Config**. Toàn bộ % trong `harder.txt` đều nằm ở đây: HP/VHP heal %, Slash Ranged %, Sword %, Sword-triggered-Slash %, Shield Count per Stack, Extra Turn Match Length.

## 2.3. Thêm GameObject BattleController

Trên GameObject `Match3Board` (hoặc GameObject riêng), add component **Battle Controller**. Gán:
- `Board Controller` → chính BoardController trên board.
- `Input Controller` → chính BoardInputController trên board.
- `Tile Config` → asset Battle Tile Config vừa tạo.
- `Tuning Config` → asset Battle Tuning Config.
- `Player Config` / `Enemy Config` → 2 asset Character Config tương ứng.

Bấm Play: Player kéo tile như bình thường; sau khi hết lượt (không ăn match-4+ nữa), Enemy sẽ tự động chọn ngẫu nhiên 1 nước đi hợp lệ và đánh (có delay ngắn cho dễ theo dõi). Máu/mana/shield cập nhật ở tầng dữ liệu — chưa có thanh HP hiển thị (đó là Phase 3).

## 2.4. Đọc chỉ số qua code (tạm thời, trước khi có UI)

```csharp
battleController.PlayerState.CurrentHp
battleController.EnemyState.ShieldStack
battleController.TurnTracker.CurrentSide   // đang là lượt của ai
battleController.TurnTracker.CycleCount
battleController.CharacterStatsChanged += (side) => { /* refresh UI sau này */ };
battleController.BattleEnded += (winningSide) => { /* end game */ };
```

## 2.5. Các giả định đã áp dụng (dễ đổi, chỉ 1 dòng code mỗi chỗ)

- Current VHP bắt đầu ở 0 (giống Mana), không phải đầy — file gốc không nói rõ giá trị khởi đầu.
- VHP hồi theo % của **Max VHP** (không phải Max HP).
- Mana/VHP hiện chưa có nơi tiêu — model đã có sẵn chỗ chứa, sẵn sàng cho tính năng skill sau này.
- Shield Stack chặn **đòn sắp tới** (chưa áp dụng cho damage phạt do hết giờ — vì Timer là Phase 2, sẽ quyết định lúc đó có cho né được không).

---

# Phần 3 — AI Thông minh, Auto-play, Timer, Tốc độ game, UI (Phase 2 + 3)

Không cần tạo thêm ScriptableObject nào mới — toàn bộ dùng lại `BattleTuningConfig`/`CharacterConfig` đã có.

## 3.1. Những gì mới trong code

- **AI "Thông minh"** (`SmartMoveSelector`): ưu tiên ngẫu nhiên 1 nước tạo match ≥ `Extra Turn Match Length` (trong `BattleTuningConfig`), không có thì random trong toàn bộ nước hợp lệ. `BoardController.GetAllValidMoves()` giờ trả về `EvaluatedMove` (kèm độ dài match tốt nhất mỗi nước) thay vì chỉ vị trí.
- **Random AI difficulty**: `BattleController.Awake()` random 1 lần giữa Random/Smart cho Enemy, giữ nguyên cả trận — đọc qua `battleController.EnemyAiDifficulty`.
- **Auto-play Player**: gọi `battleController.SetPlayerAutoPlay(true/false)`. Bật lên thì Player luôn dùng AI Random (đúng theo `harder.txt`), tắt input kéo-thả, tắt luôn timer.
- **Timer**: `BattleController` tự chạy đếm ngược (`MoveTimer`, tick bằng `Time.unscaledDeltaTime` — không bị ảnh hưởng bởi tốc độ game) khi đang là lượt Player và Player không auto-play. Hết giờ → Enemy gây `max(Swordrain, Slash)` damage **xuyên Shield Stack** (vì đây là phạt, không phải đòn Slash/Sword tile thật) → lượt chuyển sang Enemy (không tính là 1 Turn vì Player không thật sự move).
- **Tốc độ game** (`GameSpeedController`, component độc lập, không cần BattleController biết tới): đổi `Time.timeScale` 1x→2x→4x→1x, DOTween và các `WaitForSeconds` (delay AI, v.v.) tự động nhanh theo. Timer đếm giờ Player **không** bị ảnh hưởng (cố ý, xem comment trong code).

## 3.2. Setup thêm trong Scene

### a. GameSpeedController
Thêm component **Game Speed Controller** vào bất kỳ GameObject nào tồn tại suốt trận (không cần gán gì).

### b. Chỉnh BattleController
Trên component `Battle Controller` đã có, giờ có thêm 2 field: `Automated Move Delay Seconds` (mặc định 0.5s — delay cho AI/auto-play, thuần thẩm mỹ) và `Player Move Time Limit Seconds` (mặc định 20s).

## 3.3. Dựng UI (Canvas)

Cần **TextMeshPro** (Window > TextMeshPro > Import TMP Essential Resources nếu Unity hỏi lúc kéo component đầu tiên).

Tạo 1 `Canvas` (Screen Space - Overlay là đơn giản nhất), bên trong dựng theo cấu trúc gợi ý — mỗi mục là 1 GameObject UI, gán script tương ứng:

| UI cần | GameObject | Script | Field cần gán |
|---|---|---|---|
| Thanh HP/VHP Player | 2x `Slider` (+ `TMP_Text` con) | `Stat Bar View` | Slider, Value Text |
| Toàn bộ stat Player | GameObject cha chứa 2 Stat Bar View ở trên + 4 `TMP_Text` (Mana/Swordrain/Slash/Shield) | `Character Hud View` | Battle Controller, Side=Player, 2 bar, 4 text |
| Toàn bộ stat Enemy | y hệt trên | `Character Hud View` | Side=Enemy |
| Turn/Cycle/Lượt ai/Timer | 4x `TMP_Text` | `Turn Cycle View` | Battle Controller, 4 text |
| Nút tua game | `Button` + `TMP_Text` con | `Speed Toggle View` | Game Speed Controller, Button, Label |
| Toggle Auto-play | `Toggle` (UI mặc định của Unity) | `Auto Play Toggle View` | Battle Controller, Toggle |

M��i `Character Hud View`/`Stat Bar View` là component **tái sử dụng** — tạo 1 prefab HUD dùng chung cho cả 2 bên, chỉ đổi `Side` (Player/Enemy) là xong, không cần viết thêm code.

Tất cả UI đều tự refresh qua event có sẵn (`CharacterStatsChanged`, `TurnTracker.SideChanged/CycleCompleted`, `MoveTimer.RemainingSecondsChanged`, `GameSpeedController.SpeedChanged`) — không cần Update() polling ở đâu cả.

## 3.4. Trạng thái trận đấu kết thúc

`battleController.BattleEnded += (winningSide) => { ... };` — hiện tại chưa có màn hình Win/Lose, bạn tự bắt event này để chuyển scene/hiện popup theo ý muốn.

---

# Phần 4 — Sửa lỗi, xem chỉ số trong Inspector, GameObject nhân vật + animation tấn công

## 4.1. Về lỗi `NullReferenceException` ở `OnEnable()`

Nguyên nhân: Unity **không đảm bảo** thứ tự `Awake()` giữa các script khác nhau — `TurnCycleView.OnEnable()` có thể chạy trước cả `BattleController.Awake()` (nơi tạo `TurnTracker`), nên lúc đó `TurnTracker` vẫn là `null`. Unity chỉ đảm bảo **toàn bộ `Awake()` của mọi object chạy xong trước khi bất kỳ `Start()` nào chạy** — đó là lý do đổi sang `Start()` hết lỗi.

Cách bạn sửa (đổi hẳn `OnEnable()` → `Start()`) đúng hướng nhưng có tác dụng phụ: nếu sau này bạn tắt/bật lại panel UI đó (`SetActive(false)` rồi `true`), `Start()` không chạy lại lần 2 → panel sẽ đứng hình vĩnh viễn vì không subscribe lại. Mình đã sửa cả 4 file UI (`TurnCycleView`, `CharacterHudView`, `SpeedToggleView`, `AutoPlayToggleView`) theo pattern: thử subscribe ở **cả `OnEnable()` lẫn `Start()`** (có cờ `_isSubscribed` chống subscribe trùng), `OnDisable()` luôn unsubscribe — vừa an toàn về thứ tự Awake, vừa hoạt động đúng khi bật/tắt lại UI. Đây cũng chính là lý do vì sao UI trước đó hiện "New Text" lúc mới Play — `Refresh()` chưa từng được gọi thành công do subscribe thất bại; giờ sẽ tự hiện đúng giá trị (HP=CurrentHP/MaxHP, VHP=0, Mana=0/MaxMana, Slash/Swordrain theo config, Shield=0, Turn=0, Cycle=0, lượt của Player) **ngay khi Play**, không cần chờ đánh.

## 4.2. Xem chỉ số Player/Enemy trong Inspector

`BattleController` giờ có 2 field **Debug (read-only, updates during Play)**: `Player Debug Snapshot` và `Enemy Debug Snapshot`. Chọn GameObject chứa `Battle Controller` lúc đang Play, mở rộng 2 field này trong Inspector — thấy đầy đủ CurrentHp/MaxHp/CurrentVhp/MaxVhp/Mana/MaxMana/ShieldCount/ShieldStack, tự cập nhật mỗi khi chỉ số đổi. Sửa tay các field này không có tác dụng gì (chỉ là gương phản chiếu, bị ghi đè liên tục) — nó không phải ô nhập liệu, chỉ để xem.

## 4.3. Character Config có thêm Sprite/Color/Max Mana

`CharacterConfig` giờ có thêm **Sprite**, **Color** (hình + màu hiển thị nhân vật) và **Max Mana** (Mana giờ bị giới hạn để hiện được dạng thanh, mặc định 100 — trước đây không giới hạn, đây là thay đổi so với bản trước, báo lại nếu bạn muốn Mana quay lại vô hạn).

## 4.4. Tạo GameObject Player / Enemy

1. Tạo 2 GameObject trống, đặt tên `PlayerCharacter` / `EnemyCharacter`, đặt Position 2 bên board (ví dụ trái/phải).
2. Mỗi cái add **Sprite Renderer** + component **Character View** (tự thêm Sprite Renderer nếu quên, do `[RequireComponent]`).
3. (Tuỳ chọn) Tạo child Transform tên `ProjectileOrigin` và `SwordSpawnPoint` nếu muốn đạn/sword bay ra từ vị trí khác vị trí nhân vật (ví dụ Sword rơi từ trên cao) — kéo vào 2 field tương ứng trên `Character View`. Để trống thì mặc định dùng chính vị trí nhân vật.

## 4.5. Tạo Projectile Prefab (đạn tấn công)

1. GameObject trống → add **Sprite Renderer** + component **Attack Projectile View**.
2. Kéo thành Prefab, xoá khỏi Scene (giống Tile Prefab, được instantiate qua pool lúc runtime).

## 4.6. Tạo GameObject Attack Visual Controller

1. GameObject trống, add component **Attack Visual Controller**.
2. Gán: `Player View` / `Enemy View` → 2 CharacterView vừa tạo. `Projectile Prefab` → prefab vừa tạo. `Projectile Container` để trống cũng được (tự tạo child `"Projectiles"`).

## 4.7. Gán vào Battle Controller

Trên component `Battle Controller` đã có, gán field mới **Attack Visuals** → GameObject Attack Visual Controller vừa tạo.

## 4.8. Cách hoạt động của animation tấn công

- **Slash — Cận chiến**: nhân vật lao tới ~70% quãng đường tới đối phương, **damage áp dụng đúng lúc chạm** (giữa animation, không phải lúc bắt đầu), rồi chạy về vị trí cũ.
- **Slash — Tầm xa**: bắn `Ranged Object Count` (config theo nhân vật) đối tượng từ vị trí nhân vật (hoặc `Projectile Origin` nếu có) bay tới đối phương, mỗi đối tượng **tự trừ máu phần của nó khi chạm đích** (tổng damage chia đều cho số object, đúng công thức "sát thương của 1 object" trong harder.txt).
- **Sword**: bắn N object (dùng chung `Ranged Object Count`) từ `Sword Spawn Point` (hoặc vị trí nhân vật) bay tới đối phương — mỗi object tự trừ máu khi chạm. Ngay sau khi cả loạt Sword bay xong, **tự động kích hoạt tiếp 1 đòn Slash** (theo đúng loại Cận chiến/Tầm xa của nhân vật, giảm 50% dmg) — hai đòn chạy **tuần tự**, không chồng animation lên nhau.
- Toàn bộ animation được xếp hàng đợi (queue) — nếu 1 lượt đánh ăn nhiều loại tile cùng lúc (VD Slash + Sword), các đòn chạy lần lượt chứ không đè lên nhau. **Lượt chỉ thật sự chuyển sang bên kia sau khi toàn bộ animation tấn công của lượt đó chạy xong** (không phải ngay khi board hết cascade).
- Object bắn ra dùng **Object Pool** (`AttackProjectilePool`, cùng cơ chế `UnityEngine.Pool.ObjectPool<T>` như tile) — không Instantiate/Destroy liên tục.

## 4.9. Giả định mới cần lưu ý

- Số lượng object của **Sword** dùng chung `Ranged Object Count` của nhân vật (không phải bằng số Sword tile ăn được) — vì trong harder.txt cụm "bắn n object" ở phần mô tả loại tấn công (Cận chiến/Tầm xa) đã nói rõ "số lượng object có thể setup", nên mình hiểu Sword cũng dùng chung khái niệm đó để nhất quán và dễ cân bằng độ dày hình ảnh, thay vì mật độ object thay đổi thất thường theo số tile ăn được mỗi lần. Nếu bạn muốn Sword dùng đúng số tile đã ăn làm số object, báo mình đổi lại (1 dòng).
