using GGemCo2DCore;

namespace GGemCo2DQuest
{
    /// <summary>
    /// Quest 도메인에서 사용하는 Addressables Key 규칙을 정의한다.
    /// </summary>
    /// <remarks>
    /// - Addressables 로드 시 사용하는 Key 문자열을 중앙에서 관리한다.
    /// - 문자열 하드코딩을 방지하고, SDK 공통 접두사(<see cref="ConfigDefine.NameSDK"/>)를 일관되게 적용한다.
    /// - Group 이름(<see cref="ConfigAddressableGroupNameQuest"/>)과는 역할이 다르므로 혼용하지 않도록 한다.
    /// </remarks>
    public static class ConfigAddressableKeyQuest
    {
        /// <summary>
        /// Quest JSON 에셋 키 접두사입니다.
        /// </summary>
        public const string Quest = ConfigDefine.NameSDK + "_Quest";
    }
}