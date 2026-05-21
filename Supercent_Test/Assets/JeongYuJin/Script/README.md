# Prison Life - 스크립트 구현 현황 (2026-05-21 기준)

## 📁 실제 구현된 스크립트 목록

```
JeongYuJin/Script/
├── Manager/
│   └── GameManager.cs              ← 싱글톤 / 돈 관리 / 이벤트
├── Player/
│   └── PlayerController.cs         ← 이동 + 채굴 + 돌 스택 + 수갑 스택 + MAX 인디케이터
├── Zone/
│   ├── Rock.cs                     ← 채굴 바위 (데미지 / 파괴 / 리스폰)
│   ├── HandcuffZone.cs             ← 돌 → 수갑 변환 존
│   └── HandcuffMoneyZone.cs        ← 수갑 → 돈 변환 존
├── UI/
│   ├── UIManager.cs                ← HUD + 조이스틱 참조 + 오디오 토글
│   ├── Joystick.cs                 ← 가상 조이스틱 (IPointerDownHandler)
│   └── WorldToScreenBillboard.cs   ← 월드 → 스크린 UI 추적
└── Core/
    └── CameraFollow.cs             ← 아이소메트릭 카메라 추적
```

---

## 🗺️ 씬 계층 구조

```
[UI Canvas]  (Screen Space - Overlay)
├── Joystick            ← Joystick.cs 부착 (Image + Raycast Target 필수)
├── Audio Toggle        ← Toggle 컴포넌트 (UIManager가 코드로 관리)
│   ├── Background      ← targetGraphic (색상 변환 대상)
│   └── Text (TMP)      ← "Audio On" / "Audio Off" 텍스트

[Manager]  (빈 게임오브젝트)
├── UIManager           ← UIManager.cs 부착
└── Game Managers       ← GameManager.cs 부착

[Player]
└── PlayerController.cs + CharacterController 부착

[Zone]
├── HandcuffZone        ← HandcuffZone.cs + BoxCollider (IsTrigger)
└── HandcuffMoneyZone   ← HandcuffMoneyZone.cs + BoxCollider (IsTrigger)

[Rocks]
└── Rock_*              ← Rock.cs + Collider (Layer: Rock)
```

---

## 📝 스크립트별 핵심 요약

### GameManager.cs
- **싱글톤** (단일 씬 구조, DontDestroyOnLoad 없음)
- `money` 값 관리 → `AddMoney()` / `SpendMoney()`
- `OnMoneyChanged` (System.Action<int>) 이벤트로 UI 자동 갱신

---

### UIManager.cs
- **싱글톤** (Manager 오브젝트에 부착, Canvas 밖)
- `GameManager.OnMoneyChanged` 구독 → `moneyText` 자동 갱신
- 조이스틱 입력 프로퍼티 노출: `Horizontal` / `Vertical`
- **오디오 토글 관리**
  - Toggle ColorBlock에서 `normalColor`(ON) / `selectedColor`(OFF) 캐시
  - 토글 클릭 시 `normalColor` 교체 + `EventSystem 디셀렉트`
    → Unity Selected 색 고착 문제 완전 해결
  - `audioToggleText` : ON = "Audio On" / OFF = "Audio Off" 자동 변경
- **인스펙터 연결 항목**

| 슬롯 | 연결 대상 |
|------|-----------|
| moneyText | UI Canvas > MoneyText (TMP) |
| joystick | UI Canvas > Joystick |
| audioToggle | UI Canvas > Audio Toggle |
| audioToggleText | UI Canvas > Audio Toggle > Text (TMP) |

---

### Joystick.cs
- `IPointerDownHandler` / `IDragHandler` / `IPointerUpHandler` 구현
- `Horizontal` / `Vertical` 프로퍼티 → PlayerController가 읽음
- **반드시 UI Canvas 계층 안**에 부착 (Canvas 밖이면 포인터 이벤트 수신 불가)

---

### PlayerController.cs
- **이동**: UIManager.Instance 경유 조이스틱 입력 → 카메라 기준 방향 이동 (키보드 폴백)
- **채굴**: `Physics.OverlapSphere` → 가장 가까운 Rock에 `mineDamage` 주기적 적용
- **돌 스택**: `PickupMoney()` / `ConsumeRock()` / `DepositMoney()` + 시각적 스택 프리팹
- **수갑 스택**: `AddHandcuffToCarry()` / `ConsumeHandcuff()` + 시각적 스택 프리팹
- **MAX 인디케이터**: 스택 가득 찼을 때 팝업 코루틴 애니메이션
- `CalcSpacing()` : 프리팹 Renderer 실제 높이 기반 스택 간격 자동 계산

---

### Rock.cs
- `TakeDamage(float)` → 스케일 피드백 → `IsDestroyed` 플래그
- 파괴 시 가장 가까운 플레이어 탐색 → `PickupMoney(moneyDropAmount)` 호출
- 일정 시간 후 리스폰 코루틴

---

### HandcuffZone.cs
- **[진입]** `OnTriggerEnter` → `ExchangeRoutine` 코루틴 시작
  - 플레이어 돌 1개 소비 → `outputPoint`에 수갑 프리팹 스폰 (List 추적)
- **[픽업]** `Update`의 `Physics.OverlapSphere(outputPoint, pickupDistance)`
  - 플레이어 접근 감지 → `AddHandcuffToCarry(count)` → 스폰된 수갑 전량 Destroy
- **[퇴장]** `OnTriggerExit` → 코루틴 중지 + `player = null`
- 스폰 시 AudioClip 재생 지원

---

### HandcuffMoneyZone.cs
- **[진입]** `OnTriggerEnter` → `DepositRoutine` 코루틴 시작
  - 수갑 1개 소비 → `outputPoint`에 수갑 스폰 → `GameManager.AddMoney(moneyPerHandcuff)`
- **[퇴장]** `OnTriggerExit` → 코루틴 중지 + `player = null`
- 입금 시 AudioClip 재생 지원

---

### CameraFollow.cs
- `LateUpdate`에서 `target.position + offset`을 `Vector3.Lerp`로 부드럽게 추적
- `useBounds` 옵션으로 XZ 이동 범위 제한 가능

### WorldToScreenBillboard.cs
- 월드 좌표(`worldPosition`)를 `Camera.WorldToScreenPoint`로 UI 위치에 동기화
- `lifetime` 이후 자동 Destroy

---

## 🔧 주요 해결 이슈 기록

| 문제 | 원인 | 해결 |
|------|------|------|
| 조이스틱 미작동 | UIManager가 Canvas 밖 → IPointerDownHandler 수신 불가 | Joystick.cs를 Canvas 안 오브젝트에 분리 부착 |
| 수갑 변환 중단 후 재발 | PickupHandcuffs 후 코루틴만 중지, player 참조가 남아있어 재진입 시 즉시 재개 | player = null 을 OnTriggerExit + PickupHandcuffs 양쪽에 적용 |
| 픽업 거리 인식 실패 | OnTriggerExit 후 player = null → Update에서 null 참조 | 픽업을 OverlapSphere로 교체 (trigger 상태와 독립) |
| Toggle Selected 색 고착 | 클릭 후 EventSystem이 Toggle을 Selected 상태 유지 | normalColor 교체 후 SetSelectedGameObject(null)로 디셀렉트 |

---

## 📦 사용 에셋 출처 (Asset Store)

| 분류 | 에셋 이름 | 링크 |
|------|-----------|------|
| UI (조이스틱 / 버튼) | Virtual Buttons | https://assetstore.unity.com/packages/tools/input-management/virtual-buttons-200159 |
| 오디오 | Shapeforms Audio Free Sound Effects | https://assetstore.unity.com/packages/audio/sound-fx/shapeforms-audio-free-sound-effects-183649 |

---

## ⚠️ 주의사항

- `AudioToggle.cs` 는 더 이상 사용하지 않음 → UIManager가 오디오 토글 전담
- Toggle의 On Value Changed 이벤트에 별도 함수 연결 불필요 (AddListener로 자동 등록)
- HandcuffZone 픽업은 Trigger가 아닌 **OverlapSphere** 기반 → Collider 설정과 무관하게 동작
