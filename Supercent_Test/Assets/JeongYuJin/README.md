
> Git : https://github.com/UjinJeong/Supercent_ClientTest.git

---

## 📁 스크립트 구조

```
Script/
├── Core/
│   ├── CameraFollow.cs          ← 아이소메트릭 카메라 추적
│   ├── StackUtils.cs            ← 프리팹 높이 기반 스택 간격 계산 유틸 (static)
│   └── WorldToScreenBillboard.cs← 월드 좌표 → 스크린 UI 동기화
├── Manager/
│   └── GameManager.cs           ← 싱글톤 / 돈 관리 / OnMoneyChanged 이벤트
├── NPC/
│   └── Prisoner.cs              ← 수감자 이동 / 상태 / UI / 색상
├── Player/
│   └── PlayerController.cs      ← 이동 / 채굴 / 돌·수갑 스택 관리
├── UI/
│   ├── Joystick.cs              ← 가상 조이스틱 (IPointer 이벤트)
│   └── UIManager.cs             ← HUD / 오디오 토글 전담
└── Zone/
    ├── Base/
    │   ├── BaseZone.cs          ← OnTrigger + 코루틴 생명주기 추상 기반
    │   └── BaseStackZone.cs     ← 아이템 스폰·소비·초기화 추상 기반
    ├── HandcuffZone.cs          ← 돌 → 수갑 변환
    ├── HandcuffMoneyZone.cs     ← 수갑 → 테이블 적재 (PrisonerZone이 소비)
    ├── PrisonerZone.cs          ← 수감자 자동 스폰 / 처리 / 돈 지급
    └── Rock.cs                  ← 채굴 바위 (데미지 / 파괴 / 리스폰)
```

---

## 🧬 상속 구조

```
BaseZone (abstract)
└── BaseStackZone (abstract)
    ├── HandcuffZone
    └── HandcuffMoneyZone
```

- **BaseZone** : `OnTriggerEnter/Exit` + `ZoneRoutine()` 코루틴 생명주기 공통 처리  
- **BaseStackZone** : `SpawnItem()` / `ConsumeLastItem()` / `ClearItems()` + `StackUtils`로 간격 자동 계산  
- 새 Zone 추가 시 `ItemPrefab` · `OutputPoint` · `StackSpacing` 프로퍼티 + `ZoneRoutine()` 구현만 하면 됨

---

## ⚙️ 주요 시스템

### 플레이어 → 돌 채굴 → 수갑 → 테이블
```
[Rock] TakeDamage → 파괴 → PlayerController.PickupMoney()  (돌 획득)
[HandcuffZone] 트리거 진입 → 돌 소비 → 수갑 스폰 → OverlapSphere 감지 → 수갑 픽업
[HandcuffMoneyZone] 트리거 진입 → 수갑 소비 → 테이블에 수갑 적재
```

### 수감자 처리 (PrisonerZone)
```
SpawnLoop  ──(spawnInterval마다)──▶  조건 확인
                                     ├─ handcuffTable.HandcuffCount > 0
                                     └─ activePrisoners < maxConcurrentPrisoners
                                            │
                                            ▼
                                    ProcessPrisoner (코루틴, 병렬 실행)
                                     1. 스폰 + 요구 수갑 수 결정
                                     2. 테이블 앞 이동 (테이블 점유 중이면 제자리 대기)
                                     3. 테이블 점유 → 수갑 1개씩 소비
                                        └─ 수갑 없으면 WaitUntil 보충 대기
                                     4. 처리 완료 → 색상 변환 + SFX
                                     5. 돈 스폰 + GameManager.AddMoney()
                                     6. 퇴장 → Destroy
```

- `isTableOccupied` 플래그로 테이블 동시 사용 방지  
- `Prisoner.WalkTo(target, () => isTableOccupied)` — 점유 중이면 이동 자동 정지  
- `PrisonerState` enum으로 Inspector에서 수감자 처리 단계 실시간 확인 가능

### 오디오 토글 (UIManager)
- Toggle `normalColor`(ON) / `selectedColor`(OFF) 캐시 후 상태에 따라 교체  
- `EventSystem.SetSelectedGameObject(null)` 로 Selected 색 고착 방지

---

## 📦 에셋 출처

| 분류 | 에셋 | 링크 |
|---|---|---|
| UI 조이스틱 | Virtual Buttons | https://assetstore.unity.com/packages/tools/input-management/virtual-buttons-200159 |
| 효과음 | Shapeforms Audio Free Sound Effects | https://assetstore.unity.com/packages/audio/sound-fx/shapeforms-audio-free-sound-effects-183649 |
