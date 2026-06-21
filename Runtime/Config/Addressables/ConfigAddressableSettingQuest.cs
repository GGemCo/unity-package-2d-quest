using System.Collections.Generic;
using GGemCo2DCore;

namespace GGemCo2DQuest
{
    public static class ConfigAddressableSettingQuest
    {
        public static readonly AddressableAssetInfo QuestSettings = ConfigAddressableSetting.Make(nameof(QuestSettings));

        public static readonly List<AddressableAssetInfo> NeedLoadInLoadingScene = new()
        {
            QuestSettings,
        };
    }
}
