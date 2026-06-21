using GGemCo2DCore;

namespace GGemCo2DQuest
{
    /// <summary>
    /// 맵 전체 몬스터 처치 퀘스트가 진행 중일 때 일반 몬스터 리스폰을 억제합니다.
    /// </summary>
    public sealed class QuestMonsterRespawnSuppressionPolicy : IMonsterRespawnSuppressionPolicy
    {
        private readonly QuestPackageManager _packageManager;

        /// <summary>
        /// Quest 몬스터 리스폰 억제 정책을 생성합니다.
        /// </summary>
        /// <param name="packageManager">Quest 런타임 소유자입니다.</param>
        public QuestMonsterRespawnSuppressionPolicy(QuestPackageManager packageManager)
        {
            _packageManager = packageManager;
        }

        /// <summary>
        /// 지정한 맵에서 맵 전체 처치 퀘스트가 진행 중인지 확인합니다.
        /// </summary>
        /// <param name="mapUid">검사할 맵 UID입니다.</param>
        /// <returns>리스폰을 억제해야 하면 <see langword="true"/>입니다.</returns>
        public bool ShouldSuppress(int mapUid)
        {
            return _packageManager?.QuestManager?.HasActiveObjective(
                mapUid,
                QuestConstants.ObjectiveType.KillMonsterInMap) == true;
        }
    }
}
