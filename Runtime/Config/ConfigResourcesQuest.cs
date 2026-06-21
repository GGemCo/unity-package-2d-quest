using GGemCo2DCore;

namespace GGemCo2DQuest
{
    /// <summary>
    /// Quest 패키지가 Resources에서 사용하는 에셋 경로를 정의합니다.
    /// </summary>
    public static class ConfigResourcesQuest
    {
        /// <summary>
        /// 시작 가능한 퀘스트 표시 아이콘입니다.
        /// </summary>
        public static readonly ResourcesAssetInfo IconQuestReady =
            new ResourcesAssetInfo("GGemCo/UI/Icon/IconQuestReady");

        /// <summary>
        /// 진행 중인 퀘스트 표시 아이콘입니다.
        /// </summary>
        public static readonly ResourcesAssetInfo IconQuestInProgress =
            new ResourcesAssetInfo("GGemCo/UI/Icon/IconQuestInProgress");
    }
}
