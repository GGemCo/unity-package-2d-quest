using GGemCo2DCore;

namespace GGemCo2DQuest
{
    /// <summary>
    /// Quest 패키지가 소유하는 윈도우 UID를 정의합니다.
    /// 기존 window 테이블과 직렬화 데이터의 숫자 값은 유지합니다.
    /// </summary>
    public static class QuestWindowConstants
    {
        /// <summary>
        /// 진행 중인 퀘스트를 표시하는 HUD 윈도우 UID입니다.
        /// </summary>
        public const UIWindowConstants.WindowUid HudQuest = (UIWindowConstants.WindowUid)20;

        /// <summary>
        /// 완료한 퀘스트의 보상을 표시하는 윈도우 UID입니다.
        /// </summary>
        public const UIWindowConstants.WindowUid QuestReward = (UIWindowConstants.WindowUid)21;
    }
}
