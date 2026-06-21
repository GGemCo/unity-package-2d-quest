using System.Collections.Generic;
using System.Threading.Tasks;
using GGemCo2DCore;

namespace GGemCo2DQuest
{
    /// <summary>
    /// NPC 인터랙션 대화창에 시작 가능하거나 진행 중인 퀘스트 선택지를 제공합니다.
    /// </summary>
    public sealed class QuestInteractionChoiceContributor : IInteractionChoiceContributor
    {
        private readonly QuestPackageManager _packageManager;

        /// <summary>
        /// Quest 인터랙션 선택지 제공자를 생성합니다.
        /// </summary>
        /// <param name="packageManager">Quest 런타임 소유자입니다.</param>
        public QuestInteractionChoiceContributor(QuestPackageManager packageManager)
        {
            _packageManager = packageManager;
        }

        /// <summary>
        /// 현재 NPC에서 사용할 수 있는 퀘스트 선택지를 수집합니다.
        /// </summary>
        /// <param name="npc">인터랙션 대상 캐릭터입니다.</param>
        /// <param name="npcData">대상 NPC의 테이블 데이터입니다.</param>
        /// <param name="interactionData">기본 인터랙션 데이터입니다.</param>
        /// <param name="results">선택지를 추가할 결과 목록입니다.</param>
        public void CollectChoices(
            CharacterBase npc,
            StruckTableNpc npcData,
            StruckTableInteraction interactionData,
            List<InteractionChoiceContribution> results)
        {
            if (npc is not Npc questNpc || results == null)
            {
                return;
            }

            NpcQuestController controller = questNpc.GetComponent<NpcQuestController>();
            if (controller == null)
            {
                controller = questNpc.gameObject.AddComponent<NpcQuestController>();
                controller.Initialize(questNpc);
            }

            List<NpcQuestData> questInfos = controller.GetQuestInfos();
            TableQuest tableQuest = TableLoaderManagerQuest.Instance?.TableQuest;
            if (questInfos == null || tableQuest == null)
            {
                return;
            }

            for (int i = 0; i < questInfos.Count; i++)
            {
                NpcQuestData questInfo = questInfos[i];
                StruckTableQuest row = tableQuest.GetDataByUid(questInfo.QuestUid);
                if (row == null)
                {
                    continue;
                }

                int npcUid = questNpc.uid;
                results.Add(new InteractionChoiceContribution(
                    row.Name,
                    () => ExecuteQuestChoiceAsync(questInfo, npcUid)));
            }
        }

        /// <summary>
        /// 선택한 퀘스트의 시작 또는 진행 대화를 실행합니다.
        /// </summary>
        /// <param name="questInfo">선택한 NPC 퀘스트 정보입니다.</param>
        /// <param name="npcUid">인터랙션 대상 NPC UID입니다.</param>
        private async Task ExecuteQuestChoiceAsync(NpcQuestData questInfo, int npcUid)
        {
            SceneGame sceneGame = SceneGame.Instance;
            sceneGame?.InteractionManager?.RemoveCurrentNpc();
            sceneGame?.uIWindowManager
                ?.GetUIWindowByUid<UIWindowInteractionDialogue>(UIWindowConstants.WindowUid.InteractionDialogue)
                ?.Show(false);

            QuestManager questManager = _packageManager?.QuestManager;
            if (questManager == null || questInfo == null)
            {
                return;
            }

            if (questInfo.Status == QuestConstants.Status.Ready)
            {
                await questManager.StartQuest(questInfo.QuestUid, npcUid);
                return;
            }

            if (questInfo.Status == QuestConstants.Status.InProgress)
            {
                GameEventManager.DialogStart(new DialogEventData(npcUid));
            }
        }
    }
}
