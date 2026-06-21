using System.Collections.Generic;
using System.Threading.Tasks;

namespace GGemCo2DQuest
{
    /// <summary>
    /// Quest JSON 정의의 지연 로드, 프리로드, 활성 상태와 캐시 수명주기를 관리하는 저장소 계약입니다.
    /// </summary>
    public interface IQuestDefinitionRepository : System.IDisposable
    {
        /// <summary>
        /// 지정한 퀘스트 정의를 캐시 또는 Addressables에서 비동기로 조회합니다.
        /// </summary>
        /// <param name="questUid">조회할 퀘스트 UID입니다.</param>
        /// <returns>로드된 퀘스트 정의입니다. 유효하지 않거나 로드에 실패하면 null을 반환합니다.</returns>
        Task<Quest> GetAsync(int questUid);

        /// <summary>
        /// 지정한 퀘스트 정의 목록을 백그라운드 사용 목적으로 미리 로드합니다.
        /// </summary>
        /// <param name="questUids">프리로드할 퀘스트 UID 목록입니다.</param>
        /// <param name="mapUid">프리로드 범위를 소유하는 맵 UID입니다.</param>
        Task PreloadAsync(IReadOnlyList<int> questUids, int mapUid);

        /// <summary>
        /// 캐시에 이미 적재된 퀘스트 정의를 조회하고 최근 사용 순서를 갱신합니다.
        /// </summary>
        /// <param name="questUid">조회할 퀘스트 UID입니다.</param>
        /// <param name="quest">캐시된 퀘스트 정의입니다.</param>
        /// <returns>캐시에 정의가 있으면 true를 반환합니다.</returns>
        bool TryGet(int questUid, out Quest quest);

        /// <summary>
        /// 진행 중인 퀘스트를 활성 캐시로 고정합니다.
        /// </summary>
        /// <param name="questUid">활성 상태로 표시할 퀘스트 UID입니다.</param>
        void MarkActive(int questUid);

        /// <summary>
        /// 종료된 퀘스트를 활성 캐시에서 해제하고 최근 사용 캐시로 전환합니다.
        /// </summary>
        /// <param name="questUid">비활성 상태로 표시할 퀘스트 UID입니다.</param>
        void MarkInactive(int questUid);

        /// <summary>
        /// 현재 맵과 무관한 프리로드 표시를 해제하고 LRU 한도를 초과한 정의를 정리합니다.
        /// </summary>
        /// <param name="mapUid">현재 유지할 맵 프리로드 범위입니다.</param>
        void ReleaseUnused(int mapUid);

    }
}
