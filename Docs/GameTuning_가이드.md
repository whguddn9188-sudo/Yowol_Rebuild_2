# Yowol 게임 튜닝 가이드 (GameTuning.json)

모든 조정값은 **`Assets/Resources/Tuning/GameTuning.json`** 한 파일에 있습니다.
Unity 메뉴 **Yowol ▸ Game Tuning (JSON)** 창에서 편집하고 **Save**를 누르면 됩니다. (텍스트 편집기로 직접 고쳐도 됩니다.)
플레이 중에는 **Apply to running game** 버튼으로 저장 없이 바로 확인할 수 있습니다. HUD는 즉시 다시 그려지고, 새로 스폰되는 적부터 새 값이 적용됩니다.

## 1. 스킬 아이콘 (E / Q)  — 탭 "HUD & Skills"

| 키 | 기본값 | 의미 |
|---|---|---|
| `hud.skillSlotSize` | 124 (이전 96) | 슬롯 크기(px, 1920×1080 기준) |
| `hud.skillIconSize` | 104 | 슬롯 안 아이콘 크기 |
| `hud.skillSlotSpacing` | 140 | E–Q 슬롯 간격(중심 간) |
| `hud.skillOffset` | (-40, 48) | 화면 오른쪽 아래 기준 Q 슬롯 위치 |
| `hud.cooldownIconBrightness` | 0.45 | 쿨타임 중 아이콘 밝기 |

**무기 아이콘 (1/2/3 슬롯):** `weapons[].icon` → `Assets/Resources/UI/WeaponIcons/` (Sword_01 / Bow_02 / L_Sword_01). 창의 "Weapon" 칸에 끌어다 놓아도 됩니다.

**스킬 아이콘 교체:** 창의 무기별 E/Q 칸에 스프라이트를 끌어다 놓으면 끝입니다. Resources 폴더 밖의 이미지는 자동으로 `Assets/Resources/UI/SkillIcons/`에 복사됩니다.
JSON에서는 `weapons[].skill1Icon` (E), `skill2Icon` (Q)에 Resources 기준 경로(확장자 없이)를 적습니다. 예: `"UI/SkillIcons/Sword_E"`.
현재 들어 있는 6개 아이콘은 각 스킬 이펙트 프레임으로 만든 **임시 아이콘**입니다. 같은 이름의 PNG로 덮어써도 바로 바뀝니다.

담당 스크립트: `Assets/Scripts/Game/UI/GameUI.cs` → `BuildSkillSlots()`, `RefreshSkillIcons()`

## 2. 스킬 이펙트 크기 — `skillFx`

| 키 | 기본값 | 의미 |
|---|---|---|
| `skillFx.scale` | 2.2 | 플레이어 E/Q 스킬의 모든 이펙트·발광 크기 (2.2 = 220%) |
| `skillFx.hitboxScale` | 1.0 | E/Q 스킬 판정 범위. 이펙트만큼 판정도 넓히려면 1.2로 |

일반 공격 이펙트는 그대로입니다. 담당: `PlayerCombat.cs` → `SkillFx()`, `SkillFlash()`, `SkillProjectile()`, `SkillHitSize()`

## 3. 대검 기수 동작 — 탭 "Weapons"

| 키 | 대검 기본값 | 의미 |
|---|---|---|
| `attackWindup` | 0.35 | 평타 휘두르기 전 멈춤 시간(초). 0이면 없음 |
| `windupFrame` | 0 | 기수 동안 보여줄 공격 클립 프레임 |
| `windupMoveMultiplier` | 0 | 기수 중 이동 배율 (0 = 제자리) |
| `windupFirstComboOnly` | false | true면 콤보 첫 타에만 기수 |
| `windupTint` / `windupColor` | true / 하늘색 | 기수 중 캐릭터 점멸(예고 신호) |

검·활에도 같은 값을 넣으면 똑같이 적용됩니다. 기수 중 피격되면 공격이 취소됩니다(슈퍼아머 없음).
담당: `PlayerCombat.cs` → `AttackRoutine()`

## 4. 원거리 몬스터 (등불, Lantern) — 탭 "Lantern" / "Animations" / "Spawns"

- 스프라이트 시트: `Assets/Resources/Enemies/Lantern/` (Idle, Attack(준비4+공격8), Death, Projectile, FlameSweep)
- 동작: 플레이어를 발견하면 거리 유지(가까우면 후퇴, 멀면 접근) → 준비 동작(청록 점멸) → 공격 2번째 프레임에 발사 이펙트와 함께 화염 투사체 발사 → 쿨타임
- 처음 등불을 마주치면 안내 대사가 한 번 나옵니다 (`lantern.introLines`, 비우면 생략).
- 대검 패링(E)으로 투사체를 받아치면 반격이 등불에게 들어갑니다. 대검 같은 강공격은 준비 동작을 끊습니다.
- 주요 값: `maxHp 90`, `projectileDamage 10`, `projectileSpeed 7.5`, `attackCooldown 2.6`, `attackRange 9`, `preferredRange 6.5`, `minRange 3.5`
- 애니메이션 프레임 시간·크기(`ppu`)·피벗은 "Animations" 탭에서 조정 (Readme/Timing.txt 값 그대로 반영)
- 배치(`spawns`): Stage1 x=4, 31.5 / Stage2 x=81, 102.5, 123.5. 스테이지 시작 시 `StageController`가 생성하고 바닥에 맞춰 내려놓습니다.
  Stage 씬을 열고 창의 Spawns 탭을 보면 **씬 뷰에 위치와 사거리 원이 표시**되고, 점을 드래그해서 옮길 수 있습니다. `"enemy": "Reaper"`로 사신도 추가 가능합니다.

Stage1/2 씬 파일(.unity)은 수정하지 않았습니다 (Stage2 출구 대사만 Stage3 연결에 맞게 변경).

담당: `Enemies/LanternEnemy.cs`, `Stage/StageSpawns.cs`, `Animation/RuntimeAnims.cs`, 편집기 `Editor/GameTuningWindow.cs`

## 5. Stage 3 — 무너진 공장 지대 · 달빛 옥상

흐름: **Stage1 → Stage2 → Stage3 → BossStage** · 길이 364 (x -8 ~ 356)

- 씬: `Assets/Scenes/Stage3.unity` — 메뉴 **Yowol ▸ Build Stage3** 로 다시 생성 (씬에서 직접 고친 내용은 덮어써짐)
- 레이아웃 코드: `Editor/Stage3Builder.cs` → `BuildFactory()`, `BuildRooftops()` (x 좌표 순서), 배경은 `BuildBackdrops()`
- 아트: `Assets/Art/Sprites/Stage_3/` (공장 타일, Iron_Frame 철골), `Stage_3/Rooftops/` (Charcoal Night Rooftops 팩)

### A. 무너진 공장 지대 (x -8 ~ 160)
| 구간 (x) | 기믹 |
|---|---|
| -8 ~ 31 | 시작 지점, 오르막 경사로 |
| 31 ~ 42 | 낭떠러지 + 벽돌 발판 + **철골 트러스** |
| 50 ~ 62 | 내리막 경사로, 위에 철골 고가 발판(선택 경로) |
| 62 ~ 76 | 넓은 낭떠러지 + 좌우 이동 발판 |
| 76 ~ 100 | 승강 발판 → 높은 벽돌 단 |
| 100 ~ 112 | 무너지는 벽돌 다리 |
| 112 ~ 160 | 내리막 경사로 2개 → **철계단으로 공장 밖** |

### B. 달빛 옥상 (x 160 ~ 356)
| 구간 (x) | 기믹 |
|---|---|
| 160 ~ 184 | 옥상 R1 → 철골 다리 → R2 |
| 184 ~ 212 | 환풍기(밟을 수 있음) → 높은 옥상 R3 |
| 212 ~ 224 | 넓은 골목 + **좌우 이동 철골** |
| 224 ~ 262 | R4 → **철계단 오르막** → 난간 옥상 R5 |
| 262 ~ 276 | **녹슨 철골 4개** (밟으면 0.6초 뒤 추락) |
| 276 ~ 312 | R6(환풍기) → **철계단 내리막** → R7 |
| 312 ~ 319 | **승강 철골** (4.4 → 9.4) |
| 319 ~ 356 | 최고층 옥상 → 출구 |

- 철골 발판은 **아래에서 점프해 통과**하고 위에 착지하는 단방향 발판입니다.
- 건물 사이 골목, 공장 낭떠러지에 떨어지면 **즉사** (`KillZone.instantKill`)
- 적: `spawns`의 `"scene": "Stage3"` — 공장 8 + 옥상 8 = 16마리

### 배경 레이어 (`Stage/BackdropLayer.cs`)
| 레이어 | 이동 비율 (factor) | 구간 |
|---|---|---|
| Sky_VioletDawn (달·남산타워 원경) | 0.94 | 전체 (공장에서는 공장 배경 뒤에 가려짐) |
| Factory_BG / Factory_Structure (벽돌 기둥·철골 보·쇠사슬) | 0.9 / 0.55 | 공장 (카메라 x ≤ 150 이후 서서히 사라짐) |
| Factory_Foreground (반투명 기둥·철골) | -0.3 (화면 앞) | 공장 |
| Roof_FarCity / Roof_NearCity (건물·물탱크·안테나·전봇대·전선) | 0.7 / 0.4 | 옥상 (카메라 x ≥ 148부터 나타남) |
| Roof_Foreground (전봇대 실루엣·전선) | -0.25 (화면 앞) | 옥상 |

- 각 그룹의 `factor`, `anchor`, 표시 구간(`showFrom/showTo/fade`)은 씬에서 Inspector로 조정 가능
- 조명: `ZoneAmbience`가 공장(따뜻한 빛) → 옥상(차가운 달빛)으로 전역 조명을 바꿉니다.
- 관련 스크립트: `MovingPlatform`, `CrumblePlatform`, `KillZone`, `PlayerController`(경사로·계단 이동), `BackdropLayer`, `ZoneAmbience`
