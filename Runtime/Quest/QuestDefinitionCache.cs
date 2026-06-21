using System.Collections.Generic;

namespace GGemCo2DQuest
{
    /// <summary>
    /// 파싱된 Quest 정의를 Active, Preload, Recent 상태로 분류하고 LRU 순서로 관리합니다.
    /// </summary>
    public sealed class QuestDefinitionCache
    {
        private sealed class CacheEntry
        {
            public Quest Definition;
            public bool IsActive;
            public int PreloadMapUid;
            public LinkedListNode<int> RecentNode;
        }

        private readonly int _maxCachedDefinitionCount;
        private readonly Dictionary<int, CacheEntry> _entries = new Dictionary<int, CacheEntry>();
        private readonly LinkedList<int> _recentQuestUids = new LinkedList<int>();

        /// <summary>
        /// Quest 정의 캐시를 생성합니다.
        /// </summary>
        /// <param name="maxCachedDefinitionCount">활성 퀘스트를 제외하고 유지할 최대 정의 수입니다.</param>
        public QuestDefinitionCache(int maxCachedDefinitionCount)
        {
            _maxCachedDefinitionCount = maxCachedDefinitionCount > 0 ? maxCachedDefinitionCount : 1;
        }

        /// <summary>
        /// 캐시된 Quest 정의를 조회하고 최근 사용 순서를 갱신합니다.
        /// </summary>
        /// <param name="questUid">조회할 퀘스트 UID입니다.</param>
        /// <param name="quest">캐시된 퀘스트 정의입니다.</param>
        /// <returns>캐시에 정의가 있으면 true를 반환합니다.</returns>
        public bool TryGet(int questUid, out Quest quest)
        {
            if (_entries.TryGetValue(questUid, out CacheEntry entry) && entry.Definition != null)
            {
                Touch(questUid, entry);
                quest = entry.Definition;
                return true;
            }

            quest = null;
            return false;
        }

        /// <summary>
        /// Quest 정의를 캐시에 저장하고 최근 사용 항목으로 등록합니다.
        /// </summary>
        /// <param name="questUid">저장할 퀘스트 UID입니다.</param>
        /// <param name="quest">저장할 퀘스트 정의입니다.</param>
        public void Store(int questUid, Quest quest)
        {
            if (questUid <= 0 || quest == null)
            {
                return;
            }

            if (!_entries.TryGetValue(questUid, out CacheEntry entry))
            {
                entry = new CacheEntry();
                _entries.Add(questUid, entry);
            }

            entry.Definition = quest;
            Touch(questUid, entry);
            Trim();
        }

        /// <summary>
        /// 지정한 퀘스트를 진행 중인 활성 정의로 고정합니다.
        /// </summary>
        /// <param name="questUid">활성 상태로 표시할 퀘스트 UID입니다.</param>
        public void MarkActive(int questUid)
        {
            if (!_entries.TryGetValue(questUid, out CacheEntry entry))
            {
                return;
            }

            entry.IsActive = true;
            entry.PreloadMapUid = 0;
            Touch(questUid, entry);
        }

        /// <summary>
        /// 지정한 퀘스트의 활성 상태를 해제하여 LRU 정리 대상에 포함합니다.
        /// </summary>
        /// <param name="questUid">비활성 상태로 표시할 퀘스트 UID입니다.</param>
        public void MarkInactive(int questUid)
        {
            if (!_entries.TryGetValue(questUid, out CacheEntry entry))
            {
                return;
            }

            entry.IsActive = false;
            Touch(questUid, entry);
            Trim();
        }

        /// <summary>
        /// 지정한 퀘스트를 현재 맵의 프리로드 정의로 표시합니다.
        /// </summary>
        /// <param name="questUid">프리로드한 퀘스트 UID입니다.</param>
        /// <param name="mapUid">프리로드 범위를 소유하는 맵 UID입니다.</param>
        public void MarkPreloaded(int questUid, int mapUid)
        {
            if (mapUid <= 0 || !_entries.TryGetValue(questUid, out CacheEntry entry) || entry.IsActive)
            {
                return;
            }

            entry.PreloadMapUid = mapUid;
            Touch(questUid, entry);
        }

        /// <summary>
        /// 현재 맵과 다른 프리로드 표시를 해제하고 캐시 한도를 초과한 정의를 제거합니다.
        /// </summary>
        /// <param name="currentMapUid">현재 유지할 맵 프리로드 범위입니다.</param>
        public void ReleaseUnused(int currentMapUid)
        {
            foreach (KeyValuePair<int, CacheEntry> pair in _entries)
            {
                CacheEntry entry = pair.Value;
                if (!entry.IsActive && entry.PreloadMapUid > 0 && entry.PreloadMapUid != currentMapUid)
                {
                    entry.PreloadMapUid = 0;
                }
            }

            Trim();
        }

        /// <summary>
        /// 캐시된 모든 Quest 정의와 LRU 상태를 초기화합니다.
        /// </summary>
        public void Clear()
        {
            _entries.Clear();
            _recentQuestUids.Clear();
        }

        /// <summary>
        /// 최근 사용 순서를 갱신합니다.
        /// </summary>
        private void Touch(int questUid, CacheEntry entry)
        {
            if (entry.RecentNode != null)
            {
                _recentQuestUids.Remove(entry.RecentNode);
            }

            entry.RecentNode = _recentQuestUids.AddLast(questUid);
        }

        /// <summary>
        /// Active와 현재 맵 Preload 정의를 제외한 최근 사용 항목을 LRU 한도까지 정리합니다.
        /// </summary>
        private void Trim()
        {
            int retainedNonActiveCount = 0;
            foreach (KeyValuePair<int, CacheEntry> pair in _entries)
            {
                if (!pair.Value.IsActive)
                {
                    retainedNonActiveCount++;
                }
            }

            LinkedListNode<int> node = _recentQuestUids.First;
            while (retainedNonActiveCount > _maxCachedDefinitionCount && node != null)
            {
                LinkedListNode<int> next = node.Next;
                int questUid = node.Value;
                if (_entries.TryGetValue(questUid, out CacheEntry entry) &&
                    !entry.IsActive &&
                    entry.PreloadMapUid <= 0)
                {
                    _recentQuestUids.Remove(node);
                    _entries.Remove(questUid);
                    retainedNonActiveCount--;
                }

                node = next;
            }
        }
    }
}
