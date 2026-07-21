using System.Collections.Generic;
using GGemCo2DCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GGemCo2DQuest
{
    /// <summary>
    /// Quest 전용 저장 파일에 기록되는 데이터 컨테이너입니다.
    /// </summary>
    public sealed class SaveDataContainerQuest
    {
        /// <summary>
        /// 퀘스트 UID별 진행 상태를 보관하는 Quest 데이터입니다.
        /// </summary>
        public QuestData QuestData;

        /// <summary>
        /// 향후 Quest 하위 기능이 추가할 수 있는 확장 저장 섹션입니다.
        /// </summary>
        public Dictionary<string, JToken> Extensions;
    }

    /// <summary>
    /// Quest 진행 데이터의 초기화, 복원과 Quest 전용 파일 저장을 담당합니다.
    /// </summary>
    public sealed class SaveDataManagerQuest : SaveDataManagerBase
    {
        /// <summary>
        /// 현재 게임에서 사용하는 Quest 진행 데이터입니다.
        /// </summary>
        public QuestData Quest { get; private set; }

        /// <summary>
        /// 로딩 씬에서 읽은 Quest 전용 저장 파일과 Core 확장 섹션을 이용해 진행 데이터를 복원합니다.
        /// Quest 전용 파일이 있으면 해당 데이터를 우선 적용하고, 없으면 기존 Core의 quest.progress 섹션을 사용합니다.
        /// </summary>
        protected override void InitializeData()
        {
            SaveDataContainerQuest saveDataContainer =
                SaveDataLoaderQuest.Instance?.GetSaveDataContainer();

            Quest = new QuestData();

            // 먼저 등록하여 기존 Core 저장 파일의 quest.progress 섹션을 하위 호환 폴백으로 복원합니다.
            Quest.Register();
            Quest.Initialize(saveDataContainer);

            if (saveDataContainer?.Extensions == null)
            {
                return;
            }

            var envelope = new SaveEnvelope();
            foreach (KeyValuePair<string, JToken> extension in saveDataContainer.Extensions)
            {
                envelope.Sections[extension.Key] = extension.Value;
            }

            SaveRegistry.ApplyRestore(envelope);
        }

        /// <summary>
        /// 현재 Quest 진행 데이터를 선택된 슬롯의 Quest 전용 저장 파일에 기록합니다.
        /// </summary>
        /// <returns>Quest 저장 파일 기록에 성공하면 true를 반환합니다.</returns>
        public override bool SaveData()
        {
            if (!base.SaveData())
            {
                return false;
            }

            string filePath = saveFileController.GetSaveFilePath(
                currentSaveSlot,
                SaveDataConstantsQuest.SaveDataFileName);

            var saveData = new SaveDataContainerQuest
            {
                QuestData = Quest,
            };

            string json = JsonConvert.SerializeObject(saveData);
            SaveDataFileService.WriteAllText(
                filePath,
                json,
                SaveDataConstantsQuest.CreateIdentity(currentSaveSlot));
            return true;
        }

        /// <summary>
        /// 매니저가 제거될 때 Quest 저장 기여자 등록을 해제합니다.
        /// </summary>
        protected override void OnDestroy()
        {
            base.OnDestroy();
            Quest?.Unregister();
            Quest = null;
        }
    }
}
