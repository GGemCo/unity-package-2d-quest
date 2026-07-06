using System.Collections.Generic;
using GGemCo2DCore;

namespace GGemCo2DQuest
{
    /// <summary>
    /// Quest 도메인에서 사용하는 Addressables 테이블 리소스 정의를 중앙에서 관리한다.
    /// </summary>
    /// <remarks>
    /// - 테이블 이름 문자열과 AddressableAssetInfo 생성을 단일 소스(Single Source of Truth)로 유지한다.
    /// - 로딩/초기화 단계에서 필요한 테이블 집합을 <see cref="All"/>로 일괄 참조할 수 있다.
    /// </remarks>
    public static class ConfigAddressableTableQuest
    {
        /// <summary>
        /// Quest 런타임 테이블 pack 식별자입니다.
        /// </summary>
        public const string PackageId = "quest";

        /// <summary>
        /// Quest 기본 정의 테이블의 논리적 이름.
        /// </summary>
        public const string Quest = "quest";

        /// <summary>
        /// Quest 기본 정의 테이블에 대한 Addressables 자산 정보.
        /// </summary>
        public static readonly AddressableAssetInfo TableQuest =
            ConfigAddressableTable.Make(Quest);

        /// <summary>
        /// Quest 패키지 런타임 테이블 pack Addressables 자산 정보입니다.
        /// </summary>
        public static readonly AddressableAssetInfo TablePackQuest =
            ConfigAddressableTablePack.Make(PackageId);

        /// <summary>
        /// Quest 도메인에서 사용하는 모든 테이블 Addressables 자산 목록.
        /// </summary>
        /// <remarks>
        /// - 초기 로딩 단계에서 일괄 로드/검증 용도로 사용한다.
        /// - Kind별 상세 테이블은 quest_modifier 공통 메타와 함께 필수 데이터로 관리한다.
        /// - 테이블이 추가되면 반드시 이 목록에 함께 등록한다.
        /// </remarks>
        public static readonly List<AddressableAssetInfo> All = new()
        {
            TableQuest,
        };
        
        /// <summary>
        /// 지정한 퀘스트 UID의 JSON Addressables 키를 반환합니다.
        /// </summary>
        /// <param name="questUid">퀘스트 UID입니다.</param>
        /// <returns>Quest JSON Addressables 키입니다.</returns>
        public static string GetQuestKey(int questUid)
        {
            return $"{ConfigAddressableKeyQuest.Quest}_{questUid}";
        }
    }
}
