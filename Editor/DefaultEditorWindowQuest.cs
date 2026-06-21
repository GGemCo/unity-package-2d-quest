using GGemCo2DCore;
using GGemCo2DCoreEditor;

namespace GGemCo2DQuestEditor
{
    public class DefaultEditorWindowQuest : DefaultEditorWindow
    {
        protected override void OnEnable()
        {
            base.OnEnable();
            packageType = ConfigPackageInfo.PackageType.Quest;
        }
    }
}