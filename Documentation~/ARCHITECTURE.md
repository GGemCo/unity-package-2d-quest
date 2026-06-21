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
- `QuestData`: `ISaveContributor`를 통해 `quest.progress` 확장 섹션을 저장하고 복원합니다.
- `QuestInteractionChoiceContributor`: NPC 인터랙션 대화창에 Quest 선택지를 제공합니다.
- `QuestMonsterRespawnSuppressionPolicy`: 맵 전체 처치 목표 중 몬스터 리스폰을 억제합니다.

## 하위 호환성

Core 저장 파일의 최상위 `QuestData` 필드는 Core에서 `JToken`으로 읽은 후 `quest.progress` 확장 섹션으로 전달합니다. Quest 패키지가 등록되면 동일한 `QuestDatas` 구조로 복원하며, 다음 저장부터 확장 섹션에 기록합니다.
