using GGemCo2DCore;
using GGemCo2DCoreEditor;
using GGemCo2DQuest;
using UnityEngine;

namespace GGemCo2DQuestEditor
{
    /// <summary>
    /// <see cref="QuestConstants.ObjectiveType.PlayDialogue"/> 단계의 대상 NPC와 대화 데이터를 편집합니다.
    /// </summary>
    public sealed class StepDrawerPlayDialogue : IQuestStepDrawer
    {
        private int _selectedIndexNpc;
        private int _selectedIndexDialogue;

        /// <summary>
        /// 자동 대화에 사용할 NPC와 대화 UID 선택 팝업을 그립니다.
        /// </summary>
        /// <param name="y">현재 그리기 Y 좌표입니다.</param>
        /// <param name="rect">리스트 요소의 표시 영역입니다.</param>
        /// <param name="step">수정할 퀘스트 단계 데이터입니다.</param>
        /// <param name="metadataQuestStepListDrawer">NPC와 대화 테이블 메타데이터입니다.</param>
        public void Draw(
            ref float y,
            Rect rect,
            QuestStep step,
            MetadataQuestStepListDrawer metadataQuestStepListDrawer)
        {
            if (step == null || metadataQuestStepListDrawer == null)
            {
                return;
            }

            EditorPopupUtils.DrawUidPopup(
                "NPC",
                ref _selectedIndexNpc,
                metadataQuestStepListDrawer.NameNpc,
                metadataQuestStepListDrawer.StruckTableNpcs,
                ref step.targetUid,
                rect,
                ref y);

            EditorPopupUtils.DrawUidPopup(
                "대화",
                ref _selectedIndexDialogue,
                metadataQuestStepListDrawer.NameDialogue,
                metadataQuestStepListDrawer.StruckTableDialogues,
                ref step.dialogueUid,
                rect,
                ref y);
        }

        /// <summary>
        /// 드로어가 사용하는 고정 높이를 반환합니다.
        /// </summary>
        /// <returns>두 줄 높이(40)를 반환합니다.</returns>
        public float GetHeight() => 2 * 20;
    }
}
