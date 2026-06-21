# Quest 패키지 핵심 클래스 정리

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

* Quest 테이블 로딩과 Quest JSON 정의 로딩
* 퀘스트 시작 조건 처리(`TalkToNpc`, `EnterMap`)
* 목표 단계 처리(`TalkToNpc`, `KillMonster`, `KillMonsterInMap`, `CollectItem`, `EnterMap`, `ReachPosition`, `PlayCutscene`)
* 퀘스트 진행 상태 저장/복원
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
Quest 패키지를 Core 로딩 단계와 Game 씬 생명주기에 연결하는 자동 부트스트랩입니다.

**주요 책임**

* `RuntimeInitializeOnLoadMethod`로 부트스트랩 오브젝트 생성
* `GameLoaderManager.BeforeLoadStartInLoadingScene` 구독
* Quest 테이블 로딩 스텝(`quest.table`) 등록
* Game 씬 로드 후 `SceneGame`과 `TableLoaderManagerQuest`가 준비될 때까지 대기
* `QuestPackageManager`를 생성/초기화

**왜 중요한가**
Quest가 Core 내부에 들어가지 않고도 로딩 파이프라인과 게임 씬에 붙을 수 있게 하는 핵심 진입점입니다.

---

### `QuestPackageManager`

**위치**
`Core/QuestPackageManager.cs`

**역할**
Quest 런타임 데이터, `QuestManager`, Core 연동 객체의 수명주기를 관리합니다.

**주요 책임**

* `QuestData` 생성 및 `SaveRegistry` 등록
* `QuestManager` 생성과 `SceneGame` 연결
* `QuestInteractionChoiceContributor` 등록
* `QuestMonsterRespawnSuppressionPolicy` 등록
* NPC 캐릭터 활성화 시 `NpcQuestController` 자동 연결
* Core 캐릭터 표시 갱신 요청을 NPC Quest 표시로 전달
* Game 씬 종료 시 Quest 런타임 연결 해제

**왜 중요한가**
Quest 패키지와 Core 사이의 실제 연결 지점입니다. Core는 Quest를 참조하지 않고, Quest가 Core 레지스트리에 구현체를 등록합니다.

---

### `SceneLoadingQuest`

**위치**
`Scene/SceneLoadingQuest.cs`

**역할**
로딩 씬에서 Quest 관련 로딩 스텝을 등록하는 씬 컴포넌트입니다.

**주요 책임**

* Addressables 설정 준비 여부 확인
* 로딩 시작 직전 이벤트 훅 구독
* Quest 테이블/리소스 로딩 흐름을 GameLoaderManager에 연결

**왜 중요한가**
자동 부트스트랩 외에 씬 구성 기반으로 Quest 로딩 흐름을 명시하고 싶을 때 기준이 됩니다.

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

**왜 중요한가**
퀘스트가 어떤 NPC 대화나 맵 입장으로 시작되는지 결정하는 데이터 진입점입니다.

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

## 3-3. 진행 관리 / 저장

### `QuestManager`

**위치**
`Quest/QuestManager.cs`

**역할**
퀘스트 상태와 진행 흐름을 관리하는 Runtime 중심 클래스입니다.

**주요 책임**

* Quest JSON 전체 로딩
* 저장된 진행 중 Quest 복원
* 맵 입장 이벤트 기반 Quest 시작
* NPC 대화 기반 Quest 시작
* 목표 처리기 시작/해제
* 목표 완료 요청 큐 처리
* 다음 단계 진행 또는 퀘스트 완료 처리
* 보상 UI 및 HUD 갱신
* 보상 지급 후 상태 저장

**왜 중요한가**
Quest 패키지의 실질적인 실행 오케스트레이터입니다. 버그가 “퀘스트가 시작되지 않는다”, “완료되지 않는다”, “보상이 지급되지 않는다” 유형이라면 가장 먼저 확인해야 합니다.

---

### `QuestData`

**위치**
`SaveData/QuestData.cs`

**역할**
Quest 진행 상태를 저장하고 복원합니다.

**주요 책임**

* `ISaveContributor` 구현
* `SaveRegistry` 등록/해제
* Core 저장 확장 섹션 `quest.progress`에 진행 상태 기록
* 기존 QuestData JSON 구조와 호환되는 형태로 복원
* Quest 상태와 목표 진행 수량 저장
* HUD 진행 수량 갱신

**왜 중요한가**
Quest가 Core 저장 시스템 안에 독립 섹션으로 들어가도록 만드는 핵심 데이터 클래스입니다.

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

## 3-4. 목표 처리기 계층

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
* `ObjectiveHandlerKillMonster`
* `ObjectiveHandlerKillMonsterInMap`
* `ObjectiveHandlerCollectItem`
* `ObjectiveHandlerEnterMap`
* `ObjectiveHandlerReachPosition`
* `ObjectiveHandlerPlayCutscene`

**왜 중요한가**
각 목표 타입은 서로 다른 Core 이벤트나 상태를 구독합니다. 특정 목표만 완료되지 않는 문제는 해당 처리기에서 이벤트 구독/해제, 카운트 갱신, 완료 요청 조건을 먼저 확인해야 합니다.

---

## 3-5. Core 연동 계층

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

---

### `QuestMonsterRespawnSuppressionPolicy`

**위치**
`Integration/QuestMonsterRespawnSuppressionPolicy.cs`

**역할**
맵 전체 몬스터 처치 Quest가 진행 중일 때 일반 몬스터 리스폰을 억제합니다.

**왜 중요한가**
Quest 진행 조건과 Map/Monster 리스폰 정책을 느슨하게 연결하는 정책 구현체입니다.

---

## 3-6. UI 계층

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
* EnterMap / PlayCutscene 같은 특수 목표 문구 구성

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

## 3-7. Addressables / Config / Localization

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
3. `QuestPackageManager` 또는 부트스트랩 단계에서 Core 레지스트리에 등록합니다.
4. 씬 종료 시 반드시 등록을 해제합니다.

---

# 6. 우선적으로 읽으면 좋은 추천 순서

## Runtime 추천 순서

1. `QuestConstants`
2. `TableQuest`
3. `Quest`, `QuestStep`, `QuestReward`
4. `QuestData`, `QuestSaveData`
5. `QuestManager`
6. `ObjectiveHandlerBase`, `ObjectiveHandlerFactory`
7. 개별 `ObjectiveHandler*`
8. `QuestPackageManager`
9. `BootstrapQuestRuntime`
10. `NpcQuestController`, `QuestInteractionChoiceContributor`
11. `UIWindowHudQuest`, `UIElementHudQuest`, `UIWindowQuestReward`

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

Quest 패키지는 **`QuestManager`를 중심으로 Quest 테이블/JSON, 목표 처리기, 저장 데이터, NPC 상호작용, HUD/보상 UI, 제작용 EditorWindow를 연결하여 퀘스트 진행 전체를 독립 패키지로 실행하는 구조**입니다.
