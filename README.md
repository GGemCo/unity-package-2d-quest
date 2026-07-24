# GGemCo 2D Quest

`com.ggemco.2d.core` 위에서 동작하는 퀘스트 전용 Unity 패키지입니다.

## 주요 기능

- 퀘스트 테이블과 JSON 정의 로딩
- NPC 상호작용 대화, 단계 진입 즉시 자동 대화, 몬스터 처치, 아이템 수집, 맵 입장, 컷신 목표 처리
- 경험치, 재화, 아이템, 맵 진행도, 라이선스 보상 지급
- Core 저장 파일의 `quest.progress` 확장 섹션 저장
- NPC 퀘스트 표시, HUD, 보상 창
- Quest JSON 제작 도구와 Addressables 설정 도구

## 의존성

```text
Core
  ↑
Quest
```

Quest 패키지는 Core를 참조하지만 Core는 Quest 런타임 타입을 참조하지 않습니다. NPC 인터랙션 선택지와 몬스터 리스폰 정책은 Core의 확장 레지스트리를 통해 연결됩니다.

Quest UI를 사용할 때는 `Samples~/DataAddressable/WindowTableRows/window_quest_rows.txt`의 UID 20, 21 행을 프로젝트의 Core `window.txt`에 병합해야 합니다. 윈도우 제목 행은 기존 윈도우 제목 컬렉션에 병합하고, `GGemCo_QuestSystem.csv`는 Quest 전용 Localization 컬렉션으로 가져옵니다.

## 대화 목표 구분

- `TalkToNpc`: 플레이어가 대상 NPC와 상호작용하면 지정한 대화를 시작하고, 대화 종료 시 목표를 완료합니다.
- `PlayDialogue`: 목표 단계가 활성화되는 즉시 지정한 대화를 시작하고, 대화 종료 시 목표를 완료합니다.

맵 입장 직후 자동으로 대사를 재생해야 하는 흐름은 `EnterMap` 다음 단계에 `PlayDialogue`를 배치합니다. 기존 `TalkToNpc` JSON의 의미는 변경되지 않습니다.

Quest의 대화 실행 요청은 `QuestManager.TryStartTalkToNpcDialogue` 또는 `ObjectiveHandlerPlayDialogue`에서 `UIWindowDialogue.LoadDialogue`로 직접 전달합니다. `GameEventManager.DialogStartEvent`는 대화창이 실제로 열린 뒤의 알림이므로 Quest 실행 명령으로 사용하지 않습니다.

## 네임스페이스

- Runtime: `GGemCo2DQuest`
- Editor: `GGemCo2DQuestEditor`
