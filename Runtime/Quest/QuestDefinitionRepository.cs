using System.Collections.Generic;
using System.Threading.Tasks;
using GGemCo2DCore;

namespace GGemCo2DQuest
{
    /// <summary>
    /// Quest 테이블을 카탈로그로 사용하여 JSON 정의를 지연 로드하고 캐시하는 저장소입니다.
    /// </summary>
    public sealed class QuestDefinitionRepository : IQuestDefinitionRepository
    {
        private const int DefaultMaxCachedDefinitionCount = 50;

        private readonly TableQuest _tableQuest;
        private readonly QuestJsonAddressableLoader _loader;
        private readonly QuestDefinitionCache _cache;
        private readonly Dictionary<int, Task<Quest>> _loadingTasks = new Dictionary<int, Task<Quest>>();
        private int _generation;
        private bool _isDisposed;

        /// <summary>
        /// Quest 정의 저장소를 생성합니다.
        /// </summary>
        /// <param name="tableQuest">Quest JSON 키를 조회할 테이블입니다.</param>
        /// <param name="maxCachedDefinitionCount">활성 퀘스트 외에 유지할 최대 캐시 수입니다.</param>
        public QuestDefinitionRepository(
            TableQuest tableQuest,
            int maxCachedDefinitionCount = DefaultMaxCachedDefinitionCount)
        {
            _tableQuest = tableQuest;
            _loader = new QuestJsonAddressableLoader();
            _cache = new QuestDefinitionCache(maxCachedDefinitionCount);
        }

        /// <inheritdoc />
        public Task<Quest> GetAsync(int questUid)
        {
            if (_isDisposed || questUid <= 0 || _tableQuest == null)
            {
                return Task.FromResult<Quest>(null);
            }

            if (_cache.TryGet(questUid, out Quest cached))
            {
                return Task.FromResult(cached);
            }

            if (_loadingTasks.TryGetValue(questUid, out Task<Quest> loadingTask))
            {
                return loadingTask;
            }

            var completion = new TaskCompletionSource<Quest>();
            _loadingTasks.Add(questUid, completion.Task);
            _ = CompleteLoadAsync(questUid, _generation, completion);
            return completion.Task;
        }

        /// <inheritdoc />
        public async Task PreloadAsync(IReadOnlyList<int> questUids, int mapUid)
        {
            if (_isDisposed || questUids == null || questUids.Count <= 0 || mapUid <= 0)
            {
                return;
            }

            for (int i = 0; i < questUids.Count; i++)
            {
                if (_isDisposed)
                {
                    return;
                }

                int questUid = questUids[i];
                Quest quest = await GetAsync(questUid);
                if (quest != null)
                {
                    _cache.MarkPreloaded(questUid, mapUid);
                }
            }
        }

        /// <inheritdoc />
        public bool TryGet(int questUid, out Quest quest)
        {
            if (_isDisposed)
            {
                quest = null;
                return false;
            }

            return _cache.TryGet(questUid, out quest);
        }

        /// <inheritdoc />
        public void MarkActive(int questUid)
        {
            if (_isDisposed) return;
            _cache.MarkActive(questUid);
        }

        /// <inheritdoc />
        public void MarkInactive(int questUid)
        {
            if (_isDisposed) return;
            _cache.MarkInactive(questUid);
        }

        /// <inheritdoc />
        public void ReleaseUnused(int mapUid)
        {
            if (_isDisposed) return;
            _cache.ReleaseUnused(mapUid);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _generation++;
            _loadingTasks.Clear();
            _cache.Clear();
        }

        /// <summary>
        /// Quest JSON 로드를 완료하고 대기 중인 모든 동일 UID 요청에 결과를 전달합니다.
        /// </summary>
        /// <param name="questUid">로드할 퀘스트 UID입니다.</param>
        /// <param name="generation">로드 요청 시점의 저장소 세대 값입니다.</param>
        /// <param name="completion">동일 UID 요청에 결과를 전달할 완료 소스입니다.</param>
        private async Task CompleteLoadAsync(
            int questUid,
            int generation,
            TaskCompletionSource<Quest> completion)
        {
            try
            {
                Quest quest = await LoadDefinitionAsync(questUid);
                if (generation == _generation && quest != null)
                {
                    _cache.Store(questUid, quest);
                }

                completion.TrySetResult(generation == _generation ? quest : null);
            }
            catch (System.Exception exception)
            {
                GcLogger.LogException(exception);
                completion.TrySetResult(null);
            }
            finally
            {
                if (generation == _generation)
                {
                    _loadingTasks.Remove(questUid);
                }
            }
        }

        /// <summary>
        /// Quest JSON을 로드하고 런타임 최소 검증을 수행합니다.
        /// </summary>
        /// <param name="questUid">로드할 퀘스트 UID입니다.</param>
        /// <returns>검증을 통과한 퀘스트 정의입니다. 실패하면 null을 반환합니다.</returns>
        private async Task<Quest> LoadDefinitionAsync(int questUid)
        {
            StruckTableQuest tableRow = _tableQuest.GetDataByUid(questUid);
            if (tableRow == null)
            {
                GcLogger.LogError($"Quest 테이블에 정의가 없습니다. uid: {questUid}");
                return null;
            }

            string key = ConfigAddressableTableQuest.GetQuestKey(tableRow.Uid);
            Quest quest = await _loader.LoadAsync(questUid, key);
            return Validate(questUid, quest) ? quest : null;
        }

        /// <summary>
        /// 런타임 진행을 중단시킬 수 있는 Quest JSON의 치명적인 데이터 오류를 검사합니다.
        /// </summary>
        /// <param name="expectedQuestUid">Quest 테이블에서 요청한 UID입니다.</param>
        /// <param name="quest">검증할 Quest JSON 정의입니다.</param>
        /// <returns>런타임에서 사용할 수 있는 정의이면 true를 반환합니다.</returns>
        private static bool Validate(int expectedQuestUid, Quest quest)
        {
            if (quest == null)
            {
                return false;
            }

            if (quest.uid != expectedQuestUid)
            {
                GcLogger.LogError(
                    $"Quest JSON UID가 테이블 UID와 다릅니다. tableUid: {expectedQuestUid}, jsonUid: {quest.uid}");
                return false;
            }

            if (quest.steps == null || quest.steps.Count <= 0)
            {
                GcLogger.LogError($"Quest JSON에 목표 단계가 없습니다. uid: {expectedQuestUid}");
                return false;
            }

            for (int i = 0; i < quest.steps.Count; i++)
            {
                QuestStep step = quest.steps[i];
                if (step == null || step.objectiveType == QuestConstants.ObjectiveType.None || step.count < 0)
                {
                    GcLogger.LogError(
                        $"Quest JSON 목표 단계가 유효하지 않습니다. uid: {expectedQuestUid}, stepIndex: {i}");
                    return false;
                }

                if (RequiresTargetUid(step.objectiveType) && step.targetUid <= 0)
                {
                    GcLogger.LogError(
                        $"Quest JSON 목표 대상 UID가 없습니다. uid: {expectedQuestUid}, stepIndex: {i}, objective: {step.objectiveType}");
                    return false;
                }

                if (RequiresMapUid(step.objectiveType) && step.mapUid <= 0)
                {
                    GcLogger.LogError(
                        $"Quest JSON 목표 맵 UID가 없습니다. uid: {expectedQuestUid}, stepIndex: {i}, objective: {step.objectiveType}");
                    return false;
                }
            }

            if (quest.reward?.items == null)
            {
                return true;
            }

            for (int i = 0; i < quest.reward.items.Count; i++)
            {
                RewardItem item = quest.reward.items[i];
                if (item != null && item.itemUid > 0 && item.amount <= 0)
                {
                    GcLogger.LogError(
                        $"Quest JSON 아이템 보상 수량이 유효하지 않습니다. uid: {expectedQuestUid}, rewardIndex: {i}");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 목표 처리에 대상 UID가 필수인지 확인합니다.
        /// </summary>
        private static bool RequiresTargetUid(QuestConstants.ObjectiveType objectiveType)
        {
            return objectiveType == QuestConstants.ObjectiveType.TalkToNpc ||
                   objectiveType == QuestConstants.ObjectiveType.KillMonster ||
                   objectiveType == QuestConstants.ObjectiveType.CollectItem;
        }

        /// <summary>
        /// 목표 처리에 맵 UID가 필수인지 확인합니다.
        /// </summary>
        private static bool RequiresMapUid(QuestConstants.ObjectiveType objectiveType)
        {
            return objectiveType == QuestConstants.ObjectiveType.EnterMap ||
                   objectiveType == QuestConstants.ObjectiveType.KillMonsterInMap ||
                   objectiveType == QuestConstants.ObjectiveType.ReachPosition;
        }
    }
}
