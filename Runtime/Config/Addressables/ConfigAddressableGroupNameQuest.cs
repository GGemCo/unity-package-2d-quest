using GGemCo2DCore;

namespace GGemCo2DQuest
{
    /// <summary>
    /// Quest 도메인에서 사용하는 Addressables Group 이름 규칙을 정의한다.
    /// </summary>
    /// <remarks>
    /// - TCG(Addressables) 리소스의 빌드/패키징 단위인 Group 이름을 중앙에서 관리한다.
    /// - 문자열 하드코딩을 방지하고, SDK 공통 접두사(<see cref="ConfigDefine.NameSDK"/>)를 일관되게 적용한다.
    /// - Group 이름 변경 시 이 클래스만 수정하면 되도록 단일 진입점(Single Source of Truth)으로 사용한다.
    /// </remarks>
    public static class ConfigAddressableGroupNameQuest
    {
        /// <summary>
        /// Quest Addressables 그룹 이름입니다.
        /// </summary>
        public const string Quest = ConfigDefine.NameSDK + "_Quest";
    }
}