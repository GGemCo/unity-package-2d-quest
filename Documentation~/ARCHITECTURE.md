# Quest 패키지 아키텍처

## 의존성 방향

```text
GGemCo2DCore
      ↑
GGemCo2DQuest
```

Quest Runtime은 Core의 캐릭터, 이벤트, 테이블 기반 클래스, 저장 확장 포트와 UI 기반 타입을 사용합니다. Core Runtime은 Quest 어셈블리나 Quest 네임스페이스를 참조하지 않습니다.

## 런타임 구성

- `QuestRuntimeBootstrap`: Quest 테이블 로딩 단계와 게임 씬 초기화를 연결합니다.
- `QuestPackageManager`: Quest 진행 데이터와 매니저, Core 확장 포트 등록 수명주기를 관리합니다.
- `QuestManager`: Quest JSON, 목표 단계 전환, 보상 지급을 담당합니다.
- `ObjectiveHandlerTalkToNpc`: 대상 NPC 상호작용으로 시작된 대화를 감시하고 종료 시 목표를 완료합니다.
- `ObjectiveHandlerPlayDialogue`: 목표 활성화 즉시 대화를 시작하고 종료 시 목표를 완료합니다.
- `QuestData`: `ISaveContributor`를 통해 `quest.progress` 확장 섹션을 저장하고 복원합니다.
- `QuestInteractionChoiceContributor`: NPC 인터랙션 대화창에 Quest 선택지를 제공합니다.
- `QuestMonsterRespawnSuppressionPolicy`: 맵 전체 처치 목표 중 몬스터 리스폰을 억제합니다.

## 대화 목표 경계

`TalkToNpc`는 플레이어 상호작용이 필요한 목표이고, `PlayDialogue`는 단계 전환 직후 자동으로 대사를 재생하는 목표입니다. 자동 대화를 위해 `DialogStartEvent`를 명령처럼 재발행하지 않으며, `PlayDialogue` 처리기가 Core의 `UIWindowDialogue.LoadDialogue` API를 직접 호출합니다. `DialogStartEvent`는 실제 대화 시작 알림으로 취급하고, 목표 완료는 대상 NPC가 일치하는 `DialogEndEvent`에서 처리합니다.

기존 Quest JSON 호환성을 위해 `PlayDialogue`는 `ObjectiveType`의 새 정수값 `8`로 추가하며, 기존 enum 값은 변경하지 않습니다.

## 저장 경계

Quest 진행 데이터는 Core 저장 컨테이너의 `Extensions` 아래 `quest.progress` 섹션으로만 저장하고 복원합니다. Core는 Quest 저장 구조나 Quest 전용 섹션 키를 알지 않으며, Quest 패키지가 `ISaveContributor`를 통해 수명주기를 관리합니다.
