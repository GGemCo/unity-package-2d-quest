using GGemCo2DQuest;
using GGemCo2DCoreEditor;
using GGemCo2DCore;
using UnityEngine;

namespace GGemCo2DQuestEditor
{
    public interface IQuestStepDrawer
    {
        void Draw(ref float y, Rect rect, QuestStep step, MetadataQuestStepListDrawer metadataQuestStepListDrawer);
        float GetHeight();
    }
}