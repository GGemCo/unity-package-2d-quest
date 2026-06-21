using GGemCo2DCore;

namespace GGemCo2DQuest
{
    /// <summary>
    /// Quest 패키지에서 사용하는 Addressables 키, 라벨, 경로를 정의합니다.
    /// </summary>
    public static class ConfigAddressableQuest
    {
        /// <summary>
        /// Quest Addressables 그룹 이름입니다.
        /// </summary>
        public const string GroupName = "GGemCo_Quest";

        /// <summary>
        /// Quest JSON 에셋 키 접두사입니다.
        /// </summary>
        public const string KeyPrefix = "GGemCo_Quest";

        /// <summary>
        /// Quest JSON 에셋 라벨입니다.
        /// </summary>
        public const string Label = "GGemCo_Quest";

        /// <summary>
        /// Quest 테이블 이름입니다.
        /// </summary>
        public const string TableName = "quest";

        /// <summary>
        /// Quest JSON 에셋 폴더 경로입니다.
        /// </summary>
        public static string QuestJsonPath => $"{ConfigAddressablePath.Root}/Quests";

        /// <summary>
        /// Quest 테이블 Addressables 정보입니다.
        /// </summary>
        public static readonly AddressableAssetInfo TableQuest =
            new AddressableAssetInfo(
                $"{ConfigAddressableKey.Table}_{TableName}",
                $"{ConfigAddressablePath.Tables}/{TableName}.txt",
                ConfigAddressableLabel.Table,
                TableName);

        /// <summary>
        /// Quest 패키지 테이블 팩 Addressables 정보입니다.
        /// </summary>
        public static readonly AddressableAssetInfo TablePack =
            ConfigAddressableTablePack.Make("quest");

        /// <summary>
        /// 지정한 퀘스트 UID의 JSON Addressables 키를 반환합니다.
        /// </summary>
        /// <param name="questUid">퀘스트 UID입니다.</param>
        /// <returns>Quest JSON Addressables 키입니다.</returns>
        public static string GetQuestKey(int questUid)
        {
            return $"{KeyPrefix}_{questUid}";
        }
    }
}
