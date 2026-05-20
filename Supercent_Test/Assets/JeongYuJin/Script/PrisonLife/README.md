# Prison Life - Unity 2022.3.62f2 씬 구성 가이드

## 📁 스크립트 파일 목록

```
Scripts/
├── Manager/
│   └── GameManager.cs          ← 전체 게임 상태 (돈, 죄수, 수갑)
├── Player/
│   ├── PlayerController.cs     ← 이동 + 채굴 + 돈 스택
│   └── Joystick.cs             ← 모바일 가상 조이스틱
├── NPC/
│   ├── Prisoner.cs             ← 죄수 AI (Free / Arrested / Working)
│   └── PrisonerSpawner.cs      ← 죄수 스폰 관리
├── Zone/
│   ├── Rock.cs                 ← 채굴 가능한 바위
│   └── Zones.cs                ← MoneyDepositZone / HandcuffPickupZone
│                                   / ArrestZone / UpgradeZone
├── UI/
│   ├── UIManager.cs            ← HUD (돈, 죄수 카운터, 수갑)
│   └── MoneyPopup.cs           ← "+숫자" 팝업 애니메이션
└── Core/
    └── CameraFollow.cs         ← 아이소메트릭 카메라 추적
```

---

## 🗺️ 씬 구성 순서

### 1. Managers (빈 게임오브젝트)
- `GameManager` 컴포넌트 추가
- `UIManager` 컴포넌트 추가
- `PrisonerSpawner` 컴포넌트 추가

### 2. Camera
- Main Camera에 `CameraFollow` 추가
- Rotation: X=45, Y=45, Z=0 (아이소메트릭)
- Projection: Orthographic (Size: 8~10)
- target → Player 연결

### 3. Player
```
Player (GameObject)
├── CharacterController (R=0.3, H=1.8)
├── PlayerController 스크립트
├── Animator (humanoid rig)
└── MoneyStackPoint (빈 Transform, 플레이어 앞쪽)
```

### 4. 채석장 구역 (Quarry Zone)
```
QuarryZone (빈 게임오브젝트)
├── Rock_01 ~ Rock_30
│   ├── MeshFilter + MeshRenderer (검은 바위 모양)
│   ├── Collider (Layer: Rock)
│   └── Rock 스크립트
└── WallFence (철조망 울타리)
```
- Layer 설정: 바위 오브젝트 → Layer 이름 "Rock" 생성 필요

### 5. 경찰서 구역 (Police Station)
```
PoliceStation (빈 게임오브젝트)
├── Desk (책상 모델)
├── MoneyDepositZone (BoxCollider + IsTrigger + MoneyDepositZone 스크립트)
├── HandcuffStation
│   └── HandcuffPickupZone (BoxCollider + IsTrigger + HandcuffPickupZone 스크립트)
└── GuardPost (경비원 NPC 위치)
```

### 6. 감방 구역 (Cell Block)
```
CellBlock
├── Cell_01 ~ Cell_20 (감방 칸)
│   └── CellPoint (빈 Transform - 죄수 이동 목표 위치)
└── ArrestZone (BoxCollider + IsTrigger + ArrestZone 스크립트)
    └── cellPoints → 각 CellPoint 배열로 연결
```

### 7. 업그레이드 존
```
UpgradePanel_01 (BoxCollider + IsTrigger + UpgradeZone 스크립트)
├── cost: 50
├── upgradeName: "Mining Drill"
└── WorldSpace UI Canvas로 비용 표시
```

---

## 🎮 UI Canvas 구성 (Screen Space - Overlay)

```
Canvas (Screen Space - Overlay, 720x1280)
├── TopBar
│   ├── MoneyIcon (Image)
│   └── MoneyText (TextMeshPro) ← UIManager.moneyText 연결
├── PrisonerCounter (좌하단)
│   └── PrisonerCountText (TextMeshPro) ← UIManager.prisonerCountText 연결
├── HandcuffUI
│   └── HandcuffText (TextMeshPro) ← UIManager.handcuffText 연결
└── JoystickArea (우하단 or 좌하단)
    ├── JoystickBackground (Image, Sprite: 원형)
    │   └── Joystick 스크립트
    └── JoystickHandle (Image, Sprite: 작은 원)
```

---

## ⚙️ NavMesh 설정
1. Window → AI → Navigation
2. 바닥 오브젝트: Static → Navigation Static 체크
3. Bake 탭 → Bake 클릭
4. 바위, 울타리는 Not Walkable 영역으로 설정

---

## 📦 추천 무료 에셋 (Asset Store)

| 에셋 | 용도 |
|------|------|
| Starter Assets - Mobile | 조이스틱 UI |
| Low Poly Prison Pack (무료) | 배경 모델 |
| Simple People Cartoon (무료) | 캐릭터 |
| AllSky Free | 스카이박스 |

---

## 🐛 자주 나오는 오류 해결

| 오류 | 해결 |
|------|------|
| `NavMeshAgent` 오류 | Window → AI → Navigation → Bake |
| `TMPro` 없음 | Package Manager → TextMeshPro 설치 |
| Joystick 반응 없음 | Canvas에 EventSystem 있는지 확인 |
| 캐릭터 땅에 묻힘 | CharacterController Center Y 값 조정 |
