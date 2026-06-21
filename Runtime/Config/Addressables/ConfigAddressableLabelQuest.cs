using GGemCo2DCore;

namespace GGemCo2DQuest
{
    /// <summary>
    /// Quest 도메인에서 사용하는 Addressables Label 네이밍 규칙을 정의한다.
    /// </summary>
    /// <remarks>
    /// - Label은 여러 에셋을 논리적으로 묶어 조회하거나 일괄 로딩하기 위한 식별자이다.
    /// - 문자열 하드코딩을 방지하고, SDK 공통 접두사(<see cref="ConfigDefine.NameSDK"/>)를 일관되게 적용한다.
    /// - Group(<see cref="ConfigAddressableGroupNameQuest"/>) 및 Key(<see cref="ConfigAddressableKeyQuest"/>)와
    ///   역할이 다르므로 혼용하지 않도록 한다.
    /// </remarks>
    public static class ConfigAddressableLabelQuest
    {
        /// <summary>
        /// Quest JSON 에셋 라벨입니다.
        /// </summary>
        public const string Quest = ConfigDefine.NameSDK + "_Quest";
    }
}