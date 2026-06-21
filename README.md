# GGemCo 2D Quest

`com.ggemco.2d.core` 위에서 동작하는 퀘스트 전용 Unity 패키지입니다.

## 주요 기능

- 퀘스트 테이블과 JSON 정의 로딩
- NPC 대화, 몬스터 처치, 아이템 수집, 맵 입장, 컷신 목표 처리
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

## 네임스페이스

- Runtime: `GGemCo2DQuest`
- Editor: `GGemCo2DQuestEditor`
