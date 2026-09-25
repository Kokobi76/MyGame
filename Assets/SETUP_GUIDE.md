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

Mỗi `Character Hud View`/`Stat Bar View` là component **tái sử dụng** — tạo 1 prefab HUD dùng chung cho cả 2 bên, chỉ đổi `Side` (Player/Enemy) là xong, không cần viết thêm code.

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

## 4.9. Floating Combat Text (số bay lên khi trừ/hồi máu, mana, shield)

### a. Mọi giá trị tính toán giờ là số nguyên, tối thiểu 1

Thêm `ResolveMath.RoundToMeaningfulAmount()` — áp cho **tất cả** giá trị ra từ công thức Tile Resolve (heal HP/VHP, damage Slash/Sword, kể cả phần chia theo từng object khi bắn loạt): làm tròn số nguyên, nếu ra 0 thì ép lên 1. Đây nhiều khả năng chính là lý do bạn "không thấy trừ máu" trước đó — công thức % (0.3%, 0.6%, 5%) nhân với số nhỏ rất dễ ra kết quả như 0.3 hay 0.5, gần như không nhìn thấy được trên thanh máu 100 điểm. Giờ mọi hiệu ứng luôn là số nguyên có ý nghĩa.

### b. Tạo Floating Text Prefab

1. GameObject trống → add component **TextMeshPro - Text** (3D, không phải UI) + component **Floating Text View**.
2. Chỉnh font size/outline tuỳ ý cho dễ đọc trên nền game.
3. Kéo thành Prefab, xoá khỏi Scene.

> Lưu ý: dùng **TextMeshPro (3D)** chứ không phải TextMeshPro UGUI, vì text này cần hiển thị tại toạ độ world (ngay trên đầu nhân vật), không nằm trong Canvas.

### c. Tạo GameObject Floating Combat Text Controller

1. GameObject trống, add component **Floating Combat Text Controller**.
2. Gán `Text Prefab` → prefab vừa tạo. `Text Container` để trống cũng được. Màu cho từng loại (damage/heal HP/heal VHP/mana/shield) đã có default hợp lý, chỉnh lại tuỳ ý.

### d. Gán vào Battle Controller

Field mới **Floating Text** → GameObject Floating Combat Text Controller vừa tạo.

### e. Khi nào text nào xuất hiện

| Tình huống | Text | Vị trí |
|---|---|---|
| Ăn HP tile | `+X` (xanh lá) | Trên đầu người vừa đánh |
| Ăn VHP tile | `+X VHP` (xanh dương nhạt) | Trên đầu người vừa đánh |
| Ăn Mana tile | `+X MP` (tím) | Trên đầu người vừa đánh |
| Ăn Shield tile | `+X Shield` (vàng) | Trên đầu người vừa đánh |
| Object Slash/Sword chạm đối phương, **không** bị chặn | `-X` (đỏ) | Trên đầu người bị đánh, đúng lúc object chạm |
| Object chạm nhưng **bị Shield Stack chặn** | `-1 Shield` (xám) | Trên đầu người bị đánh — không hiện số damage vì damage = 0, chỉ báo mất 1 shield stack |
| Hết giờ move, bị Enemy phạt | `-X` (đỏ) | Trên đầu Player |

Text tự bay lên + fade rồi trả về pool (dùng `DOVirtual.Float` để chỉnh alpha thủ công thay vì `TMP_Text.DOFade`, vì `DOFade` cần module DOTween Pro/TMP riêng mà bản Free bạn cài trước đó chưa chắc có — cách này chắc chắn chạy được với DOTween Free).

## 4.10. Cập nhật sau phản hồi (Sword object count, bug object không biến mất)

- **Số object của Sword**: đã sửa lại đúng bằng **số Sword tile ăn được** (n), không dùng `Ranged Object Count` nữa — khớp với công thức, và mỗi object giờ luôn gây đúng `Swordrain Damage * sword%` cố định (vì tổng `SwordrainDamage * n * sword%` chia đều cho đúng n object).
- **Object không biến mất sau khi trúng đích**: lỗi thật — `flight.OnComplete(...)` (áp damage + trả object về pool) bị chính `AsCoroutine()` gọi sau đó **ghi đè mất** (DOTween: gọi `OnComplete` lần 2 trên cùng 1 tween sẽ thay thế lần 1, không cộng dồn), nên riêng object cuối cùng trong mỗi loạt bắn không bao giờ được release, đứng khựng ở vị trí đối phương. Đã tách hẳn việc "chờ bay xong" và "áp damage + trả pool" ra 1 coroutine riêng cho từng object (`WaitForProjectileImpact`), không còn tween nào bị set `OnComplete` 2 lần.
- **"Không thấy trừ máu"**: sau khi rà lại toàn bộ pipeline damage, không tìm thấy chỗ nào damage bị bỏ qua/không thực thi — khả năng cao nhất là do công thức % (0.3%, 0.6%, 5%) cho ra kết quả rất nhỏ (VD 0.3, 0.5 điểm) trên thanh máu 100 điểm, gần như không thấy được. Xem mục 4.9.a — giờ mọi giá trị đã ép về số nguyên tối thiểu 1, cộng thêm floating text ở mục 4.9 để thấy rõ từng lần trừ/hồi. Nếu áp dụng 2 thứ này mà vẫn không thấy máu đổi, khả năng là do Slider trong Scene của bạn thiếu gán Fill Rect (lỗi setup UI, không phải lỗi code) — báo mình kiểm tra tiếp.

---

# Phần 5 — Fix bug match 4+ nhiều lần, Stats System (Primal → Main → Battle)

## 5.1. Fix: ăn nhiều match 4+ trong 1 lượt

Trước đây hệ thống chỉ check "lượt này có ít nhất 1 match 4+ hay không" (đúng/sai) — nên ăn 2, 3 hay 5 match-4+ trong cùng 1 lượt vẫn chỉ +1 lượt đi thêm. Đã sửa thành **đếm số lượng** match ≥ `Extra Turn Match Length` trong toàn bộ lượt đánh (kể cả nhiều bước cascade), rồi "để dành" (bank) đúng số đó — mỗi lượt tiêu 1, còn dư thì tiếp tục, ăn thêm match-4+ nữa thì cộng dồn tiếp. Không cần setup gì thêm, tự hoạt động.

## 5.2. Stats System — Primal → Main → Battle Stats

Theo tài liệu `KobiOne_text.txt` bạn gửi, mình đã tách lại:
- **Primal Stats** (nhập tay trong `Character Config`): Endurance, Strength, Intelligence, Dexterity.
- **Main Stats** (tự tính, không nhập tay nữa): HP ← Endurance, Attack ← Strength, Magic Attack ← Intelligence, Slash Damage ← Attack, Swordrain Damage ← Magic Attack, Max VHP ← HP.
- **Battle Stats** (runtime, thay đổi trong trận): Current HP, Current VHP, Mana, Shield Count, Shield Stack — không đổi so với trước.

Công thức derive (mục "công thức tính toán mình tự quyết định" bạn giao) nằm hết trong asset mới **Stat Derivation Config** — 1 asset dùng chung cho mọi nhân vật (không phải setting riêng theo Config để mọi nhân vật scale cùng 1 kiểu, dễ so sánh/cân bằng):

```
Max HP = Base HP + Endurance * HP per Endurance      (mặc định 50 + Endurance*10)
Attack = Base Attack + Strength * Attack per Strength  (mặc định 5 + Strength*2)
Magic Attack = Base Magic Attack + Intelligence * ...  (mặc định 5 + Intelligence*2)
Slash Damage = Attack * Slash% (mặc định 100% = bằng Attack)
Swordrain Damage = Magic Attack * Swordrain% (mặc định 100% = bằng Magic Attack)
Max VHP = Max HP * VHP% (giới hạn Inspector tối đa 50%, đúng rule "Max VHP <= Max HP/2")
```

Toàn bộ hệ số trên đều chỉnh được trong Inspector, không cần sửa code để cân bằng lại.

**Lưu ý — Dexterity hiện chưa có Main Stat nào**: tài liệu liệt kê Dexterity là Primal Stat nhưng không map sang Main Stat nào cả (khác 3 stat còn lại), nên mình để nguyên là field cấu hình chờ dùng sau, không tự bịa ra công thức.

### Setup

1. **Create > Match3 > Battle > Stat Derivation Config** — tạo 1 asset duy nhất, chỉnh hệ số nếu muốn.
2. Trên 2 asset `Character Config` (Player/Enemy) đã có: field `Max HP`, `Swordrain Damage`, `Slash Damage`, `Max VHP` **không còn nữa** — thay bằng `Endurance`, `Strength`, `Intelligence`, `Dexterity` (mặc định 10 mỗi loại — chỉnh để 2 bên có độ mạnh yếu khác nhau).
3. Trên `Battle Controller`, gán field mới **Stat Derivation** → asset vừa tạo.

Mỗi nơi hiển thị/tính toán (UI, damage, debug snapshot) đều tự động đọc theo Main Stat đã derive, không cần sửa gì thêm.

---

# Phần 6 — Buff System

Buff/Debuff/Effect theo `KobiOne_text.txt`. Skill System (nơi thực sự tạo ra các buff object trong lúc chơi) sẽ làm ở lượt sau — phần này là nền tảng, bạn đã có thể gọi buff bằng code/debug ngay để test.

## 6.1. Kiến trúc

- **BuffDefinition** (ScriptableObject) — "công thức" 1 buff: Category (Effect/Buff), Target (Self/Opponent), Duration (Turn/Cycle/Instant/Permanence/Condition), Stacking (Stack/Override/Waiting), và tuỳ Category mà có thêm Control Type (Stun/Recovery Block/Silences/Invincible) hoặc list Stat Modifiers (Slash/Swordrain Damage, +,-,*,/ với số hoặc %).
- **BuffManager** — mỗi `CharacterState` có 1 cái, quản lý danh sách buff đang active, xử lý stacking khi bị gọi lại, tick duration.
- `CharacterState.SlashDamage`/`SwordrainDamage` giờ **tự động** chạy qua mọi Buff/Debuff đang active — không cần sửa gì ở `BattleResolveProcessor`, mọi công thức damage đã tự nhận giá trị đã buff.

## 6.2. Duration hoạt động thế nào

- **Turn**: tick sau **mỗi lượt hoàn thành** (kể cả lượt bị Enemy/Player khác đi, không riêng người giữ buff) — đúng tinh thần "Turn: mỗi move loop hoàn thành" trong tài liệu là khái niệm chung cho toàn trận.
- **Cycle**: tick khi `TurnCycleTracker` báo hết 1 Cycle.
- **Instant**: tương đương Turn = 1.
- **Permanence**: không bao giờ tự hết, tồn tại tới hết trận.
- **Condition**: không tự hết theo Turn/Cycle — code gọi buff phải tự gọi `buffInstance.MarkConditionEnded()` khi điều kiện của nó xảy ra (tài liệu không cho ví dụ cụ thể nên phần này chỉ có sẵn hạ tầng, chưa có điều kiện cụ thể nào cắm vào).

## 6.3. Riêng Stun — 1 quyết định cần lưu ý

Stun **không** tick theo cơ chế Turn chung ở trên — nếu tick chung, Stun 2 lượt có thể hết ngay trong lúc đối phương đang đi (lượt không phải của người bị stun), mất hết ý nghĩa "trừ lượt đối thủ". Mình cho Stun tick **riêng**, chỉ trừ đúng lúc tới phiên người bị stun và bị bỏ qua — `BattleController` tự phát hiện việc này (`ResolveStunSkips`) ngay khi chuẩn bị vào lượt của ai đó: nếu người đó đang bị Stun thì bỏ qua thẳng, không cho input/AI chạy, rồi mới xét tiếp tới lượt kế. Nhờ vậy "Stun 2 lượt" nghĩa đúng là "đối thủ mất đúng 2 lượt đi của họ", bất kể bên kia đi bao nhiêu lượt xen giữa.

## 6.4. Invincible & Recovery Block

- **Invincible**: khi áp dụng, xoá sạch mọi debuff/negative buff hiện có trên người đó, và trong lúc còn hiệu lực thì mọi debuff mới bị chặn thẳng, mọi damage nhận vào (kể cả damage phạt do hết giờ) đều = 0.
- **Recovery Block**: trong lúc còn hiệu lực, `HealHp`/`HealVhp` gọi vào coi như không có gì xảy ra (0 điểm hồi).
- **Silences**: đã có cờ `HasControlBuff(ControlBuffType.Silences)` để tra, nhưng **chưa có nơi nào đọc nó** — chờ Skill System (khoá kỹ năng) mới dùng tới.

## 6.5. Tạo buff asset & test thử

**Create > Match3 > Battle > Buff Definition**. Ví dụ tạo buff "AttackBoost": Category = Buff, Target = Self, Duration = Turn (3), Stacking = Stack, thêm 1 Stat Modifier: Stat = Slash Damage, Operation = Add, Value = 20, Is Percentage ✓.

Test nhanh bằng code (chưa có UI/skill gọi buff):
```csharp
battleController.ApplyBuff(BattleSide.Player, attackBoostBuffDefinition, BattleSide.Player);
```

`ApplyBuff(target, definition, source)` là entry point công khai duy nhất để áp buff — đây cũng chính là hàm Skill System sẽ gọi sau này, nên không cần đổi gì khi nối Skill System vào.

## 6.6. Passive Buff — xác nhận ngoài phạm vi

Theo câu trả lời của bạn, Passive Buff gắn với skill tree (chưa có), nên mình **không** implement Passive Buff/skill tree trong phần này. `BuffCategory` hiện chỉ có `Effect` và `Buff` — thêm `PassiveBuff` sau này khi build skill tree không ảnh hưởng gì tới 2 loại đã có.

---

# Phần 7 — Skill System (theo `KobiOne_text.txt` mục SKILL SYSTEM) + UI test Buff/Skill

Phần này build trên nền Buff System ở Phần 6. Đã cập nhật theo phản hồi của bạn — xem mục 7.9 để biết chính xác chỗ nào đổi so với bản đầu.

## 7.1. Nguyên tắc xuyên suốt: không đụng code cũ

Toàn bộ Phần 7 chỉ **thêm file mới** hoặc **thêm dòng/hàm mới vào file cũ** — không có dòng code cũ nào (Core, Buff System, Phase 1-3, animation tấn công...) bị sửa hay xoá. Cụ thể, đúng 4 file cũ có bổ sung, mọi thứ khác trong đó **giữ nguyên y hệt**:

- `AttackAction.cs` — thêm 1 property mới `BypassesShield` (settable, mặc định `false`). Constructor cũ **không đổi 1 ký tự nào** — property được set riêng sau khi tạo object, không qua constructor, nên mọi chỗ gọi `new AttackAction(...)` kiểu cũ (trong `BattleResolveProcessor`) không cần sửa gì và chạy y hệt trước giờ.
- `CharacterState.cs` — thêm 1 method mới `TrySpendMana(amount)`. Không đụng field/method nào khác.
- `BoardController.cs` (core, không phải Battle) — thêm 1 property `IsIdle` và 1 method `DestroyPositionsRoutine(...)` (dùng riêng cho Tiles Skill phá bàn cờ). Method này **tự chứa toàn bộ logic riêng** (không gọi vào `ResolveCascadeRoutine` cũ), nên `ResolveCascadeRoutine` và mọi hàm khác trong file giữ nguyên y hệt bản gốc.
- `BattleController.cs` — thêm các hàm/field mới cho skill casting (`TryCastSkill`, `CanCastSkill`, `CastSkillRoutine`...). `OnEnable`/`OnDisable`/`BoardController_CascadeCompleted` **giữ nguyên y hệt bản gốc** — bản đầu tiên mình từng thêm 1 dòng subscribe thừa vào đây, đã bỏ lại theo phản hồi của bạn (xem 7.9).

`CharacterConfig.cs` — **không đổi gì cả**. Loadout skill (3 slot) nằm ở asset riêng (`SkillLoadout`, mục 7.5) chứ không nhét vào Character Config, để không phải sửa file này.

## 7.2. Kiến trúc (file mới, namespace `Match3.Battle.Skills`)

- **SkillDefinition** (ScriptableObject, giống `BuffDefinition`) — "công thức" 1 skill: Category (Buff Skill / Opponent Skill / Tiles Skill), Requires Mana + Mana Cost, list Buff để áp (tái dùng thẳng asset `BuffDefinition` đã có — Target Self/Opponent đã nằm sẵn trong asset đó), list Damage Modifier (tái dùng thẳng `StatModifier` của Buff System — thêm 1 entry nhắm `Slash Damage` = nguồn sát thương "Slash", thêm entry nhắm `Swordrain Damage` = nguồn "Sword", thêm cả 2 = "Sword + Slash"), Attack Range (Melee/Ranged, chỉ dùng cho Opponent Skill), và riêng Tiles Skill: Objects Attack Opponent Min/Max, Objects Attack Board Min/Max, Destroy Area shape.
- **SkillLoadout** (ScriptableObject mới, riêng) — bar 3 skill chủ động của 1 nhân vật (mục 7.5).
- **DestroyAreaResolver** — quy đổi 1 Destroy Area (1x1/2x2/3x3/row/col/special/all) neo tại 1 ô thành tập hợp ô bị phá; roll số "object đánh bàn cờ" rồi neo **ngẫu nhiên, không trùng ô** cho từng object, sau đó **gộp (union)** toàn bộ ô bị ảnh hưởng — 2 vùng gần/chồng nhau tự nhập lại thành 1 vùng phá lớn hơn (mục 7.6).
- **SkillDamageResolver** — quy đổi list Damage Modifier thành các `AttackAction`, bắn qua đúng pipeline visual Slash/Sword đã có (melee lunge / ranged volley / sword rain) — không cần code visual mới.
- **SkillRandom** — roll ngẫu nhiên 1 field Min/Max, tách riêng khỏi `SkillDefinition` (giống cách `TileTypeRandomizer` tách khỏi `BoardConfig`) để asset config giữ thuần dữ liệu.
- **BattleController.TryCastSkill(side, skillDefinition)** — entry point công khai duy nhất, y hệt tinh thần `ApplyBuff`. Trả `false` nếu cast không hợp lệ lúc này — không throw.

## 7.3. 3 loại Skill map vào field nào

| Category | Costs Move | Buffs | Damage Modifiers | Attack Range | Tiles Skill Objects |
|---|---|---|---|---|---|
| **Buff Skill** | **Không bao giờ** — cố định theo Category, không phải field chỉnh trong Inspector | Có (mục đích chính) | Để trống | Bỏ qua | Bỏ qua |
| **Opponent Skill** | **Luôn luôn** | Tuỳ chọn | Slash/Sword/cả 2 | Melee hoặc Ranged (+ số object) | Bỏ qua |
| **Tiles Skill** | **Luôn luôn** | Chỉ Debuff nhắm Opponent (code tự lọc, xem 7.4) | Slash/Sword/cả 2 — dùng cho phần "object đánh thẳng đối phương" | Bỏ qua (Tiles Skill không có Melee) | Có — xem 7.6 |

`Costs Move` (Turn Count Yes/No trong tài liệu gốc) giờ là **thuộc tính cố định theo Category** (`SkillDefinition.CostsMove` tự tính từ `Category`, không phải ô Inspector nữa) — đúng đúng bảng gốc trong `harder.txt` (Buff Skill = No, Opponent/Tiles Skill = Yes), không thể set sai qua Inspector.

## 7.4. Tiles Skill chỉ tạo Debuff — code tự lọc

Theo yêu cầu của bạn, lúc cast Tiles Skill, mỗi entry trong `Buffs To Apply` chỉ thật sự được áp nếu **vừa Negative vừa nhắm Opponent** — bất kỳ entry nào không thoả (VD lỡ tay gắn 1 buff tốt cho bản thân) sẽ **tự động bị bỏ qua** lúc cast, không báo lỗi, không crash. Opponent Skill và Buff Skill không bị lọc gì cả — buff của chúng luôn áp đúng theo Target đã gán trong asset.

## 7.5. Skill Loadout — tối đa 3 slot chủ động

Mỗi nhân vật có tối đa **3 slot skill chủ động**, nằm ở asset riêng `SkillLoadout` (không nhét vào `CharacterConfig` — xem lý do ở 7.1), tự cắt bớt nếu bạn lỡ kéo quá 3 skill vào list trong Inspector.

### Setup

1. **Create > Match3 > Battle > Skill Loadout** — tạo 2 asset, 1 cho Player 1 cho Enemy (hoặc dùng chung nếu 2 bên học chung skill).
2. Kéo tối đa 3 asset `SkillDefinition` vào list `Skills` theo đúng thứ tự slot (0, 1, 2).
3. Trên `Battle Controller`, gán 2 field mới **Player Skill Loadout** / **Enemy Skill Loadout** → 2 asset vừa tạo. Bỏ trống cũng được nếu trận đấu chưa dùng skill.

## 7.6. Tiles Skill — Destroy Area hoạt động thế nào

- Lúc cast, roll ngẫu nhiên 1 số trong [Objects Attack Board Min, Max] → đó là số lần áp dụng Destroy Area shape. **Mỗi lần neo tại 1 ô đang có tile, chọn ngẫu nhiên và không bao giờ trùng ô đã chọn trong cùng lượt cast đó** (đúng yêu cầu "phải random và không trùng nhau"). Toàn bộ ô bị ảnh hưởng qua tất cả các lần áp dụng được **gộp lại** trước khi phá — 2 vùng gần/chồng nhau (VD 2 khối 2x2 sát nhau) tự nhập thành 1 vùng lớn hơn, không phá 2 lần trên cùng 1 ô ("gần nhau thì gộp nổ" theo đúng ý bạn).
- Ô bị phá theo cách này chạy qua **đúng công thức Tile Resolve đã có** (`BattleResolveProcessor`) y như bị match thường — HP tile bị phá vẫn hồi máu, Sword/Slash tile bị phá vẫn gây damage, cascade tiếp theo do refill tạo ra cũng xử lý tiếp bình thường. Đây là lý do "Phá hủy bàn cờ" trong tài liệu vừa là hiệu ứng phá vừa có thể gây sát thương/hồi máu — tái dùng 100% pipeline Tile Resolve sẵn có, không cần code riêng.
- Song song đó, roll thêm 1 số trong [Objects Attack Opponent Min, Max] object bắn thẳng vào đối phương, gây damage theo Damage Modifiers — giống Opponent Skill, nhưng luôn kiểu Ranged vì Tiles Skill không có Melee.
- `Destroy Area = All`: sau khi phá xong, xoá sạch move cộng dồn (extra move) của lượt đó và **luôn luôn** chuyển lượt ngay (bỏ qua Costs Move) — đúng tài liệu.
- **Shield không chặn damage từ phần "object bắn thẳng đối phương"** của Opponent Skill/Tiles Skill (đúng câu "Shield không có tác dụng chặn skill"). Phần "phá bàn cờ" thì bản chất vẫn là Tile Resolve của tile thường, nên vẫn theo đúng rule Shield cũ của Tile Resolve (không đổi).

## 7.7. Mana & giới hạn số lần cast

- `CharacterState.TrySpendMana(amount)` — trừ Mana nếu đủ, trả `false` nếu không đủ (không trừ âm).
- **Buff Skill**: dùng **vô hạn lần** trong 1 lượt, miễn còn đủ Mana mỗi lần — không tốn move, không có giới hạn "1 lần/lượt" nào khác ngoài Mana.
- **Opponent Skill / Tiles Skill**: tối đa **1 lần mỗi move** — tự nhiên có sẵn vì 2 loại này luôn tốn move (Costs Move = Yes cố định), nên vừa cast xong là move đó đã kết thúc/chuyển tiếp (qua `CompleteMove`, y hệt cơ chế move thường bao gồm cả move cộng dồn từ match 4+), không cần code đếm số lần riêng.

## 7.8. UI test Buff & Skill

Component mới `Skill Slot View` (`Match3.Battle.UI`) — 1 nút bấm ứng với 1 slot (0/1/2) của 1 bên, đọc skill từ `SkillLoadout` qua `BattleController.GetSkillLoadout(side)`, tự bật/tắt `interactable` theo `BattleController.CanCastSkill(...)` (đúng lượt, đủ mana, không đang bận...), bấm để gọi `TryCastSkill`.

**Không có UI test buff riêng** — muốn test 1 `BuffDefinition` bất kỳ, bọc nó vào 1 `SkillDefinition` Category = Buff Skill rồi gán vào 1 slot: vì Buff Skill miễn phí move và cast được nhiều lần (mục 7.7), việc này vừa test buff vừa test luôn skill UI cùng lúc, không cần dựng thêm UI riêng.

### Setup

1. Tạo 6 GameObject UI (Button + TMP_Text con) trong Canvas — 3 cho Player, 3 cho Enemy (hoặc chỉ 3 cho Player nếu bạn chỉ cần test 1 bên).
2. Mỗi cái add component **Skill Slot View**, gán `Battle Controller`, `Side` (Player/Enemy), `Slot Index` (0/1/2 — đúng thứ tự trong `SkillLoadout`), `Button`, `Label`.
3. Cùng 1 prefab dùng lại cho cả 6 nút — chỉ đổi Side + Slot Index, giống hệt cách `Character Hud View` tái sử dụng ở Phần 3.

## 7.9. Test nhanh bằng code

```csharp
bool started = battleController.TryCastSkill(BattleSide.Player, someSkillDefinition);
```

Theo dõi qua các event đã có — `CharacterStatsChanged` (Mana/HP/Shield đổi), `SkillCast` (event mới, bắn ngay khi 1 cast hợp lệ bắt đầu), `BoardController.MatchesResolved`/`CascadeCompleted` (vẫn bắn bình thường khi Tiles Skill phá bàn cờ, y như match thật).

## 7.10. Tạo asset

1. **Create > Match3 > Battle > Skill Definition** — điền Category trước, sau đó chỉ điền đúng nhóm field theo bảng ở 7.3.
2. **Create > Match3 > Battle > Skill Loadout** — xem 7.5.

Damage Modifiers và Buffs to Apply tái dùng picker asset `StatModifier`/`BuffDefinition` y hệt cách bạn đã làm ở Buff System (Phần 6.5).

## 7.11. Đã sửa theo phản hồi của bạn (so với bản Phần 7 đầu tiên)

- **Vị trí neo Destroy Area**: xác nhận là random — giữ nguyên, nhưng sửa thêm để **không trùng ô** giữa các object trong cùng 1 lần cast (trước đó có thể trùng do random độc lập từng object).
- **Costs Move** (Turn Count): đổi từ 1 field Inspector tự set thành **cố định theo Category** — đúng bảng gốc trong tài liệu (Buff Skill = No, Opponent/Tiles Skill = Yes), Buff Skill giờ **dùng vô hạn miễn đủ Mana**, không giới hạn "1 skill/lượt" nữa; Opponent/Tiles Skill tự nhiên giới hạn 1 lần/move nhờ luôn tốn move.
- **Tiles Skill buffs**: từ "quy ước, không ép runtime" → giờ **code tự lọc**, chỉ áp phần Debuff nhắm Opponent.
- **Bỏ hẳn cơ chế "đã dùng skill trong lượt"** (field + subscribe `SideChanged` ở bản đầu) — không cần nữa nhờ đổi Costs Move như trên, đồng thời giúp `BattleController.OnEnable`/`OnDisable` quay lại y hệt bản gốc (đúng nguyên tắc ở 7.1: không đụng code cũ nếu không thật sự cần).
- **`BoardController.DestroyPositionsRoutine`**: đổi từ "tách 1 hàm dùng chung với `ResolveCascadeRoutine`" sang **hoàn toàn độc lập, tự chứa toàn bộ logic riêng** — `ResolveCascadeRoutine` gốc giờ không bị đụng tới 1 dòng nào.
- **`AttackAction.BypassesShield`**: đổi từ tham số constructor sang **property set sau khi tạo** — để constructor gốc không đổi chữ nào.
- Thêm UI test (`Skill Slot View`) và Skill Loadout (tối đa 3 slot) theo yêu cầu — xem 7.5, 7.8.
