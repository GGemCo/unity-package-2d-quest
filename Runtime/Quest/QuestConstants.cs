using UnityEngine;

namespace GGemCo2DQuest
{
    /// <summary>
    /// 퀘스트 상태, 시작 조건, 목표 타입과 JSON 경로 규칙을 정의합니다.
    /// </summary>
    public static class QuestConstants
    {
        public const string JsonFolderName = "Quests/";
        public const string JsonFolderPath = "/Resources/"+JsonFolderName;
        /// <summary>
        /// 퀘스트의 분류입니다.
        /// </summary>
        public enum Type
        {
            None = 0,
            Main = 1,
            Sub = 2,
        }
        /// <summary>
        /// 퀘스트의 진행 상태입니다.
        /// 기존 저장 데이터 호환을 위해 선언 순서와 정수값을 유지합니다.
        /// </summary>
        public enum Status
        {
            None = 0,
            Ready = 1,
            InProgress = 2,
            Complete = 3, // 보상 받기 전
            End = 4, // 보상 받은 후
        }

        /// <summary>
        /// 퀘스트가 시작되는 조건을 정의합니다.
        /// </summary>
        public enum TriggerType
        {
            None = 0,
            TalkToNpc = 1,
            EnterMap = 2,
        }

        /// <summary>
        /// 퀘스트 단계에서 수행할 목표 타입입니다.
        /// 기존 JSON 호환을 위해 모든 값을 명시적으로 지정합니다.
        /// </summary>
        public enum ObjectiveType
        {
            None = 0,
            TalkToNpc = 1,
            KillMonster = 2,
            CollectItem = 3,
            EnterMap = 4,
            ReachPosition = 5,
            PlayCutscene = 6,
            KillMonsterInMap = 7,
            PlayDialogue = 8,
        }
        /// <summary>
        /// 레거시 Resources 기반 Quest JSON 폴더의 절대 경로를 반환합니다.
        /// </summary>
        /// <returns>Quest JSON 폴더의 절대 경로입니다.</returns>
        public static string GetJsonFolderPath()
        {
            return Application.dataPath+ JsonFolderPath;
        }
    }
}
