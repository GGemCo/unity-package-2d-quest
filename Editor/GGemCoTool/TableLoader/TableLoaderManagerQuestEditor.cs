using GGemCo2DCoreEditor;
using GGemCo2DQuest;

namespace GGemCo2DQuestEditor
{
    /// <summary>
    /// Quest 패키지 Editor 도구에서 사용하는 테이블 로더입니다.
    /// </summary>
    public static class TableLoaderManagerQuestEditor
    {
        /// <summary>
        /// Quest 테이블을 Editor 파일 경로에서 로드합니다.
        /// </summary>
        /// <param name="forceReload">기존 캐시를 무시하고 다시 로드할지 여부입니다.</param>
        /// <returns>로드된 Quest 테이블입니다.</returns>
        public static TableQuest LoadQuestTable(bool forceReload = true)
        {
            return TableLoaderManagerBase.LoadTable<TableQuest>(
                ConfigAddressableTableQuest.TableQuest.Path,
                forceReload);
        }
    }
}
