# Quest 패키지 구조와 핵심 클래스 정리

## 1. 문서 목적

이 문서는 **Core 패키지에서 분리된 Quest 패키지**의 Runtime / Editor 핵심 클래스를 정리한 문서입니다.

정리 기준은 다음과 같습니다.

* Quest 패키지의 런타임 진입점인가
* 퀘스트 진행 상태, 목표 처리, 보상 지급에 직접 관여하는가
* Core와의 연결 지점인가
* Quest 데이터 제작/검증 흐름에서 먼저 확인해야 하는가

---

## 2. 패키지 개요

- Runtime 네임스페이스: `GGemCo2DQuest`
- Editor 네임스페이스: `GGemCo2DQuestEditor`
- 의존성 방향: `Quest → Core`

Quest 패키지는 퀘스트 진행과 제작 도구를 담당하는 독립 상위 패키지입니다.
Core는 Quest를 직접 참조하지 않고, Quest가 Core의 공통 포트와 레지스트리에 자신을 등록하는 구조를 사용합니다.

핵심 책임은 다음과 같습니다.

* Quest 테이블 전체 선로드와 Quest JSON 지연 로딩
* 진행 중 Quest 우선 복원, 현재 맵 후보 프리로드, LRU 정의 캐시
* 퀘스트 시작 조건 처리(`TalkToNpc`, `EnterMap`)
* 목표 단계 처리(`TalkToNpc`, `PlayDialogue`, `KillMonster`, `KillMonsterInMap`, `CollectItem`, `EnterMap`, `ReachPosition`, `PlayCutscene`)
* Quest 전용 저장 파일(`SaveDataQuest.json`) 저장/복원
* 기존 Core `quest.progress` 확장 섹션 하위 호환 복원
* NPC 퀘스트 아이콘과 상호작용 선택지 제공
* 진행 중 Quest HUD와 보상 UI 표시
* Quest JSON 제작, 단계 편집, 보상 편집, Addressables 설정 툴 제공

---

# 3. Runtime 핵심 클래스

## 3-1. 부트스트랩 / 패키지 수명주기

### `BootstrapQuestRuntime`

**위치**
`Bootstrapper/BootstrapQuestRuntime.cs`

**역할**
Core 캐릭터 수명주기와 Quest NPC 표시 컴포넌트를 연결하는 런타임 부트스트랩입니다.

**주요 책임**

* `CharacterManager.OnCharacterActivated` 구독
* NPC에 `NpcQuestController`가 없으면 자동 연결
* Quest 패키지 초기화 전에 활성화된 NPC를 대기 목록으로 관리
* `CharacterPresentationRefreshRegistry`를 통한 NPC Quest 표시 갱신
* 활성화/비활성화 시 Core 이벤트 구독과 대기 참조 정리

**왜 중요한가**
Quest 전용 캐릭터 표현 로직을 `QuestPackageManager`에서 분리하고, Core 캐릭터에 대한 연결 책임을 한곳에 모읍니다.

**책임 경계**

* Quest 테이블과 저장 파일을 직접 로드하지 않습니다.
* 씬을 감시하거나 `QuestPackageManager`를 자동 생성하지 않습니다.
* 데이터 로딩은 `SceneLoadingQuest`, Quest 서비스 수명주기는 `QuestPackageManager`가 담당합니다.

---

### `QuestPackageManager`

**위치**
`Core/QuestPackageManager.cs`

**역할**
Quest 런타임 데이터, 전용 저장 매니저, `QuestManager`, Core 확장 정책과 NPC 부트스트랩의 수명주기를 관리합니다.

**주요 책임**

* Game 씬 단위 싱글톤 등록과 `BootstrapQuestRuntime` 컴포넌트 보장
* `SceneGame`, Core 저장 매니저, Quest 테이블 로더 준비 대기
* `SaveDataManagerQuest` 생성과 초기화
* `SaveDataManagerQuest`에서 복원한 `QuestData` 연결
* `QuestManager` 생성과 `SceneGame` 연결
* `QuestInteractionChoiceContributor` 등록
* `QuestMonsterRespawnSuppressionPolicy` 등록
* `BootstrapQuestRuntime` 활성화
* Game 씬 종료 시 패키지 오브젝트와 모든 Quest 런타임 연결 정리

**왜 중요한가**
Quest 패키지와 Core 사이의 서비스 조립 지점입니다. Core는 Quest를 참조하지 않고, Quest가 Core 레지스트리에 구현체를 등록하고 제거합니다.

---

### `SceneLoadingQuest`

**위치**
`Scene/SceneLoadingQuest.cs`

**역할**
로딩 씬에서 Quest 테이블과 전용 저장 파일 로딩 스텝을 등록하는 씬 컴포넌트입니다.

**주요 책임**

* Addressables 설정 준비 여부 확인
* 로딩 시작 직전 이벤트 훅 구독
* `TableLoaderManagerQuest` 준비
* Quest 테이블 팩 또는 개별 테이블 로딩 단계 등록
* `SaveDataLoaderQuest` 준비
* Quest 전용 저장 파일 로딩 단계(`core.savedata.quest`) 등록
* 향후 Quest Localization/Settings 로딩 단계 확장 위치 제공

**왜 중요한가**
Quest 데이터 로딩의 단일 진입점입니다. `BootstrapQuestRuntime`이나 `QuestPackageManager`에서 테이블을 다시 로드하지 않도록 책임을 유지해야 합니다.

### Runtime 초기화 흐름

```text
[Loading Scene]
SceneLoadingQuest
  → TableLoaderManagerQuest 생성
  → quest 테이블 전체 로드
  → SaveDataLoaderQuest로 SaveDataQuest.json 로드

[Game Scene]
QuestPackageManager
  → SaveDataManagerQuest 생성/복원
  → QuestManager 생성
  → Core 확장 정책 등록
  → BootstrapQuestRuntime 활성화

[NPC Activated]
BootstrapQuestRuntime
  → NpcQuestController 연결/초기화
```

---

## 3-2. 테이블 / 정의 데이터

### `TableLoaderManagerQuest`

**위치**
`TableLoader/TableLoaderManagerQuest.cs`

**역할**
Quest 패키지 전용 테이블을 등록하고 조회하는 테이블 로더입니다.

**주요 책임**

* 싱글톤 인스턴스 유지
* `TableRegistry` 생성
* `TableQuest` 등록
* 씬 전환 후에도 유지되도록 `DontDestroyOnLoad` 처리

---

### `TableQuest`

**위치**
`TableLoader/TableQuest.cs`

**역할**
`quest` 테이블을 파싱하고 시작 조건별 인덱스를 구성합니다.

**주요 데이터**

* `Uid`
* `Name`
* `Type`
* `TriggerType`
* `FileName`
* `MapUid`
* `NpcUid`

**주요 조회**

* `GetQuestsByNpcUid(mapUid, npcUid)`
* `GetQuestsByEnterMap(mapUid)`
* `GetQuestsByMap(mapUid)`

**왜 중요한가**
퀘스트가 어떤 NPC 대화나 맵 입장으로 시작되는지 결정하는 데이터 진입점이며, 현재 맵 Quest JSON 프리로드 후보를 구성하는 카탈로그입니다.

---

### `Quest`

**위치**
`Quest/Quest.cs`

**역할**
Quest JSON 한 건의 단계와 보상 정의를 보관합니다.

**주요 구성**

* `Quest.uid`
* `Quest.title`
* `Quest.steps`
* `Quest.reward`
* `QuestStep.objectiveType`
* `QuestStep.mapUid`
* `QuestStep.targetUid`
* `QuestStep.cutsceneUid`
* `QuestStep.count`
* `QuestStep.dialogueUid`

**왜 중요한가**
테이블은 Quest JSON 파일을 찾기 위한 인덱스이고, 실제 목표 단계와 보상 정의는 이 데이터 구조가 기준입니다.

---

### `QuestConstants`

**위치**
`Quest/QuestConstants.cs`

**역할**
퀘스트 분류, 진행 상태, 시작 조건, 목표 타입을 정의합니다.

**주요 enum**

* `QuestConstants.Type`
* `QuestConstants.Status`
* `QuestConstants.TriggerType`
* `QuestConstants.ObjectiveType`

**주의점**
저장 데이터와 기존 JSON 호환을 위해 enum 정수값은 신중하게 변경해야 합니다.

**대화 목표 구분**

* `TalkToNpc = 1`: 대상 NPC와 상호작용했을 때 대화를 시작합니다.
* `PlayDialogue = 8`: 목표 단계가 활성화되는 즉시 대화를 시작합니다.

`PlayDialogue`는 기존 enum 정수값을 변경하지 않고 마지막 값 뒤에 추가되어 기존 Quest JSON과 저장 데이터의 단계 인덱스 구조를 유지합니다.

---

### `QuestReward`

**위치**
`Quest/QuestReward.cs`

**역할**
퀘스트 완료 보상 정보를 정의합니다.

**주요 보상 축**

* 경험치
* 재화
* 아이템 보상
* 맵 진행도 보상
* 라이선스 보상

---

## 3-3. Quest JSON Repository / 캐시

### `IQuestDefinitionRepository`

**위치**
`Quest/IQuestDefinitionRepository.cs`

**역할**
Quest JSON 정의 조회, 프리로드, 활성 상태와 캐시 수명주기의 계약입니다.

**주요 API**

* `GetAsync(questUid)`
* `PreloadAsync(questUids, mapUid)`
* `TryGet(questUid, out quest)`
* `MarkActive(questUid)`
* `MarkInactive(questUid)`
* `ReleaseUnused(mapUid)`

---

### `QuestDefinitionRepository`

**위치**
`Quest/QuestDefinitionRepository.cs`

**역할**
`TableQuest`를 JSON 카탈로그로 사용하여 Quest 정의를 지연 로드하고 캐시에 보관합니다.

**주요 책임**

* 동일 Quest UID의 동시 로드 요청 병합
* `QuestJsonAddressableLoader`를 통한 JSON 로드
* Quest UID, 목표 단계, 대상 UID, 맵 UID, 보상 수량 최소 검증
* Active / Preload / Recent 캐시 상태 변경
* 저장소 폐기 후 완료되는 비동기 요청의 캐시 반영 차단

---

### `QuestJsonAddressableLoader`

**위치**
`Quest/QuestJsonAddressableLoader.cs`

**역할**
Quest JSON `TextAsset`을 Addressables에서 로드하고 `Quest` 객체로 역직렬화합니다.

**중요한 수명주기**

```text
TextAsset 로드
  → JSON 문자열 읽기
  → Quest 역직렬화
  → Addressables handle 즉시 Release
  → 순수 C# Quest 정의만 캐시에 보관
```

---

### `QuestDefinitionCache`

**위치**
`Quest/QuestDefinitionCache.cs`

**역할**
파싱된 Quest 정의를 Active, Preload, Recent 상태로 분류하고 LRU 순서로 관리합니다.

**캐시 정책**

* Active: 진행 중 Quest이며 자동 제거하지 않음
* Preload: 현재 맵에서 시작될 가능성이 있는 Quest
* Recent: 최근 조회된 비활성 Quest
* 기본 비활성 캐시 한도: 50개
* 맵 변경 시 이전 맵 Preload 표시를 해제하고 LRU 한도 기준으로 정리

---

## 3-4. 진행 관리 / 저장

### `QuestManager`

**위치**
`Quest/QuestManager.cs`

**역할**
퀘스트 상태와 진행 흐름을 관리하는 Runtime 중심 클래스입니다.

**주요 책임**

* 저장된 진행 중 Quest JSON만 초기 로드
* 진행 중 Quest 정의를 Active 캐시로 고정
* 맵 입장 이벤트 기반 Quest 시작
* 현재 맵 Quest JSON 후보의 비동기 프리로드
* NPC 대화 기반 Quest 시작
* 선택된 `TalkToNpc` 처리기로 대화 시작 요청 직접 전달
* 목표 처리기 시작/해제
* 목표 완료 요청 큐 처리
* 다음 단계 진행 또는 퀘스트 완료 처리
* 보상 UI 및 HUD 갱신
* 보상 지급 후 상태 저장

**왜 중요한가**
Quest 패키지의 실질적인 실행 오케스트레이터입니다. 버그가 “퀘스트가 시작되지 않는다”, “완료되지 않는다”, “보상이 지급되지 않는다” 유형이라면 가장 먼저 확인해야 합니다.

**주의점**

* Addressables와 JSON 파싱을 직접 처리하지 않고 `IQuestDefinitionRepository`에 위임합니다.
* 완료된 Quest는 Active 캐시에서 해제하여 LRU 정리 대상이 됩니다.
* 맵 진입 처리는 프리로드 완료를 기다리지 않습니다.

---

### `SaveDataManagerQuest`

**위치**
`SaveData/SaveDataManagerQuest.cs`

**역할**
Quest 진행 데이터의 생성, 복원과 Quest 전용 파일 저장을 담당합니다.

**주요 책임**

* `QuestData` 생성 및 `SaveRegistry` 등록
* `SaveDataLoaderQuest`가 읽은 전용 저장 데이터 복원
* 기존 Core `quest.progress` 확장 섹션을 하위 호환 폴백으로 사용
* 진행 상태 변경 시 `SaveDataQuest.json` 저장
* Quest 패키지 종료 시 저장 기여자 등록 해제

---

### `SaveDataLoaderQuest`

**위치**
`SaveData/SaveDataLoaderQuest.cs`

**역할**
Loading 씬에서 선택된 슬롯의 Quest 전용 저장 파일을 로드합니다.

**주요 책임**

* Quest 전용 파일 경로 계산
* Quest 전용 암호화 AAD 사용
* Core 백업 파일과 충돌하지 않는 Quest 전용 복구 경로 사용
* 로드/복구 결과를 `SaveDataContainerQuest`로 역직렬화

---

### `SaveDataConstantsQuest`

**위치**
`SaveData/SaveDataConstantsQuest.cs`

**역할**
Quest 저장 파일명, 백업 파일명과 논리 저장 Scope를 정의합니다.

**주요 값**

* 파일명: `SaveDataQuest.json`
* 복구용 백업 경로명: `SaveDataQuest.backup.json`
* 저장 Scope: `quest`

---

### `QuestData`

**위치**
`SaveData/QuestData.cs`

**역할**
Quest 진행 상태를 저장하고 복원합니다.

**주요 책임**

* `ISaveContributor` 구현
* `SaveRegistry` 등록/해제
* Quest 전용 저장 컨테이너의 진행 상태 복원
* 기존 Core 저장 확장 섹션 `quest.progress`의 하위 호환 복원
* Quest 상태 변경 시 `SaveDataManagerQuest.StartSaveData()` 요청
* Quest 상태와 목표 진행 수량 저장
* HUD 진행 수량 갱신

**왜 중요한가**
실행 중 변하는 Quest 진행 상태의 단일 데이터 소스입니다. Quest 정의 JSON 전체는 저장하지 않고 UID, 단계, 수량, 상태만 저장합니다.

---

### `QuestSaveData`

**위치**
`SaveData/QuestData.cs`

**역할**
개별 Quest 한 건의 저장 상태입니다.

**주요 필드**

* `QuestUid`
* `QuestStepIndex`
* `Count`
* `Status`

---

### Quest 저장/복원 흐름

```text
[Load]
SceneLoadingQuest
  → SaveDataLoaderQuest
  → SaveDataQuest.json 역직렬화
  → SaveDataManagerQuest.InitializeData
  → QuestData 생성 및 quest.progress 폴백 복원
  → 전용 Quest 데이터가 있으면 우선 적용

[Save]
QuestData.SaveStatus / SaveCount
  → SaveDataManagerQuest.StartSaveData
  → SaveDataQuest.json 기록
```

---

## 3-5. 목표 처리기 계층

### `IObjectiveHandler`

**위치**
`Quest/IObjectiveHandler.cs`

**역할**
Quest 목표 처리기의 공통 계약입니다.

---

### `IObjectiveCompletionSink`

**위치**
`Quest/IObjectiveCompletionSink.cs`

**역할**
목표 처리기가 Quest 완료 요청을 전달할 대상의 계약입니다.

---

### `ObjectiveHandlerBase`

**위치**
`Quest/ObjectiveHandlerBase.cs`

**역할**
목표 처리기의 공통 기반 클래스입니다.

**주요 책임**

* 목표 시작 공통 API 제공
* 목표 완료 여부 확인 API 제공
* 이벤트 구독 해제용 `OnDispose` 제공
* `QuestManager`에 목표 완료 요청 전달

---

### `ObjectiveHandlerFactory`

**위치**
`Quest/ObjectiveHandlerFactory.cs`

**역할**
`QuestConstants.ObjectiveType`에 맞는 목표 처리기 인스턴스를 생성합니다.

**왜 중요한가**
새 목표 타입을 추가할 때 반드시 확장해야 하는 생성 지점입니다.

---

### 개별 목표 처리기

**위치**
`Quest/*/ObjectiveHandler*.cs`

**지원 목표**

* `ObjectiveHandlerTalkToNpc`
* `ObjectiveHandlerPlayDialogue`
* `ObjectiveHandlerKillMonster`
* `ObjectiveHandlerKillMonsterInMap`
* `ObjectiveHandlerCollectItem`
* `ObjectiveHandlerEnterMap`
* `ObjectiveHandlerReachPosition`
* `ObjectiveHandlerPlayCutscene`

**왜 중요한가**
각 목표 타입은 서로 다른 Core 이벤트나 상태를 구독합니다. `TalkToNpc`는 NPC 상호작용 시작을 기다리고, `PlayDialogue`는 활성화 즉시 `UIWindowDialogue.LoadDialogue`를 호출합니다. 두 목표 모두 대상 NPC가 일치하는 대화 종료 이벤트에서 완료됩니다. 특정 목표만 완료되지 않는 문제는 해당 처리기에서 이벤트 구독/해제, 카운트 갱신, 완료 요청 조건을 먼저 확인해야 합니다.

---

## 3-6. Core 연동 계층

### `NpcQuestController`

**위치**
`Integration/NpcQuestController.cs`

**역할**
NPC의 시작 가능/진행 중 Quest를 조회하고 상태 아이콘을 표시합니다.

**주요 책임**

* NPC 초기화 시 Quest 표시 컴포넌트 연결
* 느낌표/물음표 아이콘 표시
* 현재 NPC에서 받을 수 있거나 진행 가능한 Quest 조회
* 인터랙션 선택지에서 사용할 `NpcQuestData` 제공

---

### `QuestInteractionChoiceContributor`

**위치**
`Integration/QuestInteractionChoiceContributor.cs`

**역할**
NPC 대화창에 Quest 시작/진행 선택지를 제공합니다.

**왜 중요한가**
Core의 상호작용 시스템과 Quest 패키지를 결합하는 어댑터입니다. Core는 선택지 제공자 인터페이스만 알고, Quest가 선택지 구현을 등록합니다.

진행 중인 `TalkToNpc` 선택지는 전역 `DialogStartEvent`를 명령으로 발행하지 않고, Quest UID와 단계 인덱스를 포함해 `QuestManager.TryStartTalkToNpcDialogue`에 직접 전달합니다. 실제 대화 시작 알림과 실행 요청을 분리하여 중복 대화 로드를 방지합니다.

---

### `QuestMonsterRespawnSuppressionPolicy`

**위치**
`Integration/QuestMonsterRespawnSuppressionPolicy.cs`

**역할**
맵 전체 몬스터 처치 Quest가 진행 중일 때 일반 몬스터 리스폰을 억제합니다.

**왜 중요한가**
Quest 진행 조건과 Map/Monster 리스폰 정책을 느슨하게 연결하는 정책 구현체입니다.

---

## 3-7. UI 계층

### `UIWindowHudQuest`

**위치**
`UI/WindowHud/UIWindowHudQuest.cs`

**역할**
진행 중인 Quest 목표를 HUD 요소로 관리합니다.

**주요 책임**

* Quest HUD 요소 추가/삭제
* Quest UID 기준 HUD 요소 조회
* 목표 진행 수량 갱신

---

### `UIElementHudQuest`

**위치**
`UI/WindowHud/UIElementHudQuest.cs`

**역할**
개별 Quest 제목과 현재 목표를 HUD에 표시합니다.

**주요 책임**

* Quest 제목 표시
* 목표 타입별 안내 문구 표시
* 목표 진행 수량 표시
* EnterMap / PlayCutscene / PlayDialogue 같은 특수 목표 문구 구성

---

### `UIWindowQuestReward`

**위치**
`UI/WindowQuestReward/UIWindowQuestReward.cs`

**역할**
완료된 Quest의 보상 정보를 표시합니다.

**주요 책임**

* 경험치/재화/아이템 보상 표시
* 보상 아이템 아이콘 표시
* 아이템 정보창 연동
* 확인 버튼 처리

---

### `SlotIconBuildStrategyQuestReward`

**위치**
`UI/WindowQuestReward/SlotIconBuildStrategyQuestReward.cs`

**역할**
Quest 보상 아이템 슬롯 아이콘 생성 전략입니다.

---

### `QuestWindowConstants`

**위치**
`UI/QuestWindowConstants.cs`

**역할**
Quest HUD와 보상 창 UID 같은 Quest 전용 UI 상수를 정의합니다.

---

## 3-8. Addressables / Config / Localization

### `ConfigAddressable*Quest`

**위치**
`Config/Addressables/*.cs`

**역할**
Quest 테이블, 설정, 그룹, 라벨, 경로, 키 규칙을 정의합니다.

---

### `ConfigResourcesQuest`

**위치**
`Config/ConfigResourcesQuest.cs`

**역할**
Quest 리소스 경로 규칙을 정의합니다.

---

### `QuestLocalizationConstants`

**위치**
`Localization/QuestLocalizationConstants.cs`

**역할**
Quest 전용 로컬라이징 키 규칙을 정의합니다.

---

# 4. Editor 핵심 클래스

## 4-1. Quest 제작 툴

### `QuestEditorWindow`

**위치**
`Quest/QuestEditorWindow.cs`

**역할**
Quest JSON을 생성/편집하는 메인 EditorWindow입니다.

**주요 책임**

* Quest 테이블 선택 및 Quest JSON 로드
* Quest 제목/UID/단계/보상 편집
* NPC, 몬스터, 맵, 대화, 아이템, 라이선스, 컷신 테이블 참조 제공
* 단계 목록과 보상 목록 ReorderableList 구성
* Quest JSON 저장
* 선택 슬롯의 Core `quest.progress`와 `SaveDataQuest.json` 진행 상태 동시 초기화

**왜 중요한가**
Quest 패키지 Editor의 중심 도구입니다. 기획 데이터 제작 흐름은 이 창을 기준으로 추적하는 것이 가장 빠릅니다.

---

### `QuestStepListDrawer`

**위치**
`Quest/QuestStepListDrawer.cs`

**역할**
Quest 단계 목록을 ReorderableList로 표시하고 목표 타입별 Drawer를 호출합니다.

---

### `QuestStepDrawerFactory`

**위치**
`Quest/StepDrawer/QuestStepDrawerFactory.cs`

**역할**
목표 타입에 맞는 단계 편집 Drawer를 반환합니다.

**왜 중요한가**
새 목표 타입을 추가할 때 Editor 표시를 연결하는 핵심 지점입니다.

---

### 개별 Step Drawer

**위치**
`Quest/StepDrawer/StepDrawer*.cs`

**지원 Drawer**

* `StepDrawerTalkToNpc`
* `StepDrawerPlayDialogue`
* `StepDrawerKillMonster`
* `StepDrawerKillMonsterInMap`
* `StepDrawerCollectItem`
* `StepDrawerEnterMap`
* `StepDrawerReachPosition`
* `StepDrawerPlayCutscene`

**역할**
각 목표 타입에 필요한 필드만 표시하고, 관련 테이블 선택지를 제공합니다.

---

### `RewardItemListDrawer`

**위치**
`Quest/RewardItemListDrawer.cs`

**역할**
Quest 보상 목록과 맵 진행도/라이선스 보상을 편집합니다.

**주요 책임**

* 아이템 보상 목록 편집
* 클리어 맵 UID 목록 편집
* 월드맵 노드 표시/활성 목록 편집
* 라이선스 보상 목록 편집

---

## 4-2. Table Editor / 테이블 로더

### `QuestTableEditorModule`

**위치**
`TableEditor/QuestTableEditorModule.cs`

**역할**
공용 `TableEditorWindow`에 Quest 테이블 정의를 제공합니다.

**왜 중요한가**
Quest 테이블을 공용 테이블 편집 시스템에서 관리할 수 있게 하는 연결 모듈입니다.

---

### `TableLoaderManagerQuestEditor`

**위치**
`TableLoader/TableLoaderManagerQuestEditor.cs`

**역할**
Quest Editor 도구에서 Quest 테이블을 파일 경로 기준으로 로드합니다.

---

## 4-3. Addressables / 설정 툴

### `AddressableEditorQuest`

**위치**
`Addressables/AddressableEditorQuest.cs`

**역할**
Quest 패키지 Addressables 설정을 한 화면에서 실행하는 메인 에디터 창입니다.

---

### `SettingQuest`

**위치**
`Addressables/SettingQuest.cs`

**역할**
Quest Addressables 설정 항목을 묶어서 실행합니다.

---

### `SettingScriptableObjectQuest`

**위치**
`Addressables/SettingScriptableObjectQuest.cs`

**역할**
Quest 설정 ScriptableObject를 Addressables에 등록합니다.

---

### `SettingTableQuest`

**위치**
`Addressables/SettingTableQuest.cs`

**역할**
Quest 관련 데이터 테이블과 런타임 테이블 팩을 Addressables에 등록합니다.

---

## 4-4. Editor 기반 클래스 / 메뉴 설정

### `DefaultEditorWindowQuest`

**위치**
`DefaultEditorWindowQuest.cs`

**역할**
Quest EditorWindow의 공통 기반 클래스입니다.

---

### `ConfigEditorQuest`

**위치**
`Config/ConfigEditorQuest.cs`

**역할**
Quest 에디터 메뉴 경로와 정렬 순서를 정의합니다.

---

# 5. 유지보수 포인트

## 5-1. 새 목표 타입을 추가할 때

다음 위치를 함께 수정해야 합니다.

1. `QuestConstants.ObjectiveType`
2. `QuestStep` 데이터 필드 필요 여부
3. 새 `ObjectiveHandler*` 구현
4. `ObjectiveHandlerFactory`
5. 새 `StepDrawer*` 구현
6. `QuestStepDrawerFactory`
7. `UIElementHudQuest` 목표 문구 표시
8. 필요 시 `QuestManager` 완료 흐름과 Core 이벤트 연결

---

## 5-2. 새 보상 타입을 추가할 때

다음 위치를 함께 확인합니다.

1. `QuestReward`
2. `QuestManager` 보상 지급 흐름
3. `UIWindowQuestReward`
4. `RewardItemListDrawer`
5. 저장 데이터 호환성

---

## 5-3. Core 연동을 추가할 때

Quest가 Core 클래스를 직접 수정하거나 Core가 Quest를 참조하도록 만들지 않습니다.

권장 흐름은 다음과 같습니다.

1. Core에 인터페이스/이벤트/레지스트리 같은 포트를 둡니다.
2. Quest에 구현체를 둡니다.
3. 서비스/정책은 `QuestPackageManager`, 캐릭터 표시 연결은 `BootstrapQuestRuntime`에서 등록합니다.
4. 씬 종료 시 반드시 등록을 해제합니다.

---

## 5-4. Quest JSON 로딩 정책을 변경할 때

다음 위치를 함께 확인합니다.

1. `TableQuest`의 맵/NPC/트리거 인덱스
2. `IQuestDefinitionRepository`
3. `QuestDefinitionRepository`
4. `QuestDefinitionCache`
5. `QuestJsonAddressableLoader`
6. `QuestManager`의 초기 복원과 맵 프리로드 흐름
7. Addressables load/release 대칭

Quest JSON 전체 선로드를 다시 `QuestManager`에 추가하지 않습니다.

---

## 5-5. Quest 저장 구조를 변경할 때

다음 위치를 함께 확인합니다.

1. `SaveDataContainerQuest`
2. `SaveDataManagerQuest`
3. `SaveDataLoaderQuest`
4. `SaveDataConstantsQuest`
5. `QuestData`
6. `SceneLoadingQuest`의 저장 로딩 단계
7. `QuestEditorWindow`의 진행 상태 초기화 도구
8. 기존 Core `quest.progress` 마이그레이션 또는 하위 호환 정책

---

# 6. 우선적으로 읽으면 좋은 추천 순서

## Runtime 추천 순서

1. `QuestConstants`
2. `TableQuest`
3. `Quest`, `QuestStep`, `QuestReward`
4. `QuestJsonAddressableLoader`
5. `QuestDefinitionCache`, `QuestDefinitionRepository`
6. `SaveDataLoaderQuest`, `SaveDataManagerQuest`
7. `QuestData`, `QuestSaveData`
8. `QuestManager`
9. `ObjectiveHandlerBase`, `ObjectiveHandlerFactory`
10. 개별 `ObjectiveHandler*`
11. `SceneLoadingQuest`
12. `QuestPackageManager`
13. `BootstrapQuestRuntime`
14. `NpcQuestController`, `QuestInteractionChoiceContributor`
15. `UIWindowHudQuest`, `UIElementHudQuest`, `UIWindowQuestReward`

## Editor 추천 순서

1. `ConfigEditorQuest`
2. `QuestEditorWindow`
3. `QuestStepListDrawer`
4. `QuestStepDrawerFactory`와 개별 `StepDrawer*`
5. `RewardItemListDrawer`
6. `QuestTableEditorModule`
7. `AddressableEditorQuest`, `SettingTableQuest`

---

# 7. Quest 패키지 구조를 한 문장으로 요약하면

Quest 패키지는 **`SceneLoadingQuest`가 테이블과 저장 파일을 준비하고, `QuestPackageManager`가 런타임 서비스를 조립하며, `QuestManager`가 Repository 기반 지연 로드 Quest 정의와 목표 처리기, 전용 저장 데이터, NPC 상호작용, HUD/보상 UI를 연결하는 독립 패키지 구조**입니다.
