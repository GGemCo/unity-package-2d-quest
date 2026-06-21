using GGemCo2DCore;
using System.Collections.Generic;

namespace GGemCo2DQuest
{
    /// <summary>
    /// 저장 파일에 기록되는 개별 퀘스트 진행 상태입니다.
    /// </summary>
    public class QuestSaveData
    {
        public int QuestUid;
        public int QuestStepIndex;
        public int Count;
        public QuestConstants.Status Status;

        /// <summary>
        /// JSON 역직렬화를 위한 기본 생성자입니다.
        /// </summary>
        public QuestSaveData()
        {
        }

        /// <summary>
        /// 개별 퀘스트 진행 상태를 생성합니다.
        /// </summary>
        /// <param name="questUid">퀘스트 UID입니다.</param>
        /// <param name="questStepIndex">현재 단계 인덱스입니다.</param>
        /// <param name="status">현재 퀘스트 상태입니다.</param>
        /// <param name="count">현재 목표 진행 수량입니다.</param>
        public QuestSaveData(int questUid, int questStepIndex, QuestConstants.Status status, int count = 0)
        {
            QuestUid = questUid;
            QuestStepIndex = questStepIndex;
            Count = count;
            Status = status;
        }
    }
    /// <summary>
    /// 세이브 데이터 - 퀘스트 정보
    /// </summary>
    public class QuestData : DefaultData, ISaveData, ISaveContributor
    {
        /// <summary>
        /// Core 저장 파일의 확장 섹션에서 사용하는 고유 키입니다.
        /// </summary>
        public const string SaveSectionKey = "quest.progress";

        /// <summary>
        /// 퀘스트 UID별 진행 상태입니다.
        /// JSON 직렬화를 위해 public 필드로 유지합니다.
        /// </summary>
        public Dictionary<int, QuestSaveData> QuestDatas = new Dictionary<int, QuestSaveData>();

        private UIWindowHudQuest _uiWindowHudQuest;

        /// <summary>
        /// 저장 확장 섹션의 고유 키입니다.
        /// </summary>
        public string SectionKey => SaveSectionKey;

        /// <summary>
        /// Quest 저장 기여자를 Core 저장 레지스트리에 등록합니다.
        /// 레지스트리에 보류된 복원 데이터가 있으면 등록 즉시 복원됩니다.
        /// </summary>
        public void Register()
        {
            SaveRegistry.Register(this);
        }

        /// <summary>
        /// Quest 저장 기여자 등록을 해제합니다.
        /// </summary>
        public void Unregister()
        {
            SaveRegistry.Unregister(this);
        }

        /// <summary>
        /// 현재 Quest 진행 상태를 저장 봉투에 기록합니다.
        /// </summary>
        /// <param name="env">Core 저장 봉투입니다.</param>
        public void Capture(SaveEnvelope env)
        {
            env?.SetSection(SectionKey, new QuestProgressSnapshot
            {
                QuestDatas = new Dictionary<int, QuestSaveData>(QuestDatas),
            });
        }

        /// <summary>
        /// 저장 봉투에서 Quest 진행 상태를 복원합니다.
        /// 기존 Core의 QuestData JSON 구조도 같은 필드 이름을 사용하므로 함께 호환됩니다.
        /// </summary>
        /// <param name="env">Core 저장 봉투입니다.</param>
        public void Restore(SaveEnvelope env)
        {
            QuestDatas.Clear();
            if (env == null ||
                !env.TryGetSection(SectionKey, out QuestProgressSnapshot snapshot) ||
                snapshot?.QuestDatas == null)
            {
                return;
            }

            QuestDatas = new Dictionary<int, QuestSaveData>(snapshot.QuestDatas);
        }

        private QuestConstants.Status GetStatus(int questUid)
        {
            var data = QuestDatas.GetValueOrDefault(questUid);
            return data?.Status ?? QuestConstants.Status.None;
        }
        public bool IsStatusNone(int questUid)
        {
            return GetStatus(questUid) == QuestConstants.Status.None;
        }
        public bool IsStatusInProgress(int questUid)
        {
            return GetStatus(questUid) == QuestConstants.Status.InProgress;
        }
        public bool IsStatusComplete(int questUid)
        {
            return GetStatus(questUid) == QuestConstants.Status.Complete;
        }
        public bool IsStatusEnd(int questUid)
        {
            return GetStatus(questUid) == QuestConstants.Status.End;
        }

        public void SaveStatus(int questUid, int questStepIndex, QuestConstants.Status status)
        {
            if (QuestDatas.ContainsKey(questUid))
            {
                QuestDatas[questUid].QuestStepIndex = questStepIndex;
                QuestDatas[questUid].Status = status;
            }
            else
            {
                QuestDatas.TryAdd(questUid, new QuestSaveData(questUid, questStepIndex, status));
            }
            SaveDatas();
        }

        public void SaveCount(int questUid, int count)
        {
            if (!QuestDatas.TryGetValue(questUid, out var data)) return;
            data.Count = count;
            
            _uiWindowHudQuest ??=
                SceneGame.Instance.uIWindowManager?.GetUIWindowByUid<UIWindowHudQuest>(
                    QuestWindowConstants.HudQuest);
            _uiWindowHudQuest?.SetCount(questUid, count);
            SaveDatas();
        }

        protected override int GetMaxSlotCount()
        {
            return 0;
        }

        public Dictionary<int, QuestSaveData> GetQuestDatas()
        {
            return QuestDatas;
        }
        /// <summary>
        /// 진행중인 퀘스트 찾기
        /// </summary>
        /// <param name="mapUid">NPC가 배치된 맵 UID입니다.</param>
        /// <param name="npcUid">대상 NPC UID입니다.</param>
        /// <returns>조건에 맞는 진행 중 퀘스트 상태 목록입니다.</returns>
        public List<QuestSaveData> GetInProgressQuest(int mapUid, int npcUid)
        {
            List<QuestSaveData> questUids = new List<QuestSaveData>();
            foreach (var data in QuestDatas)
            {
                int uid = data.Value.QuestUid;
                int stepIndex = data.Value.QuestStepIndex;
                QuestConstants.Status status = data.Value.Status;
                if (data.Value.QuestUid <= 0) continue;
                QuestStep questStep = QuestPackageManager.Instance?.QuestManager?.GetQuestStep(uid, stepIndex);
                if (questStep == null || questStep.targetUid <= 0) continue;
                if (questStep.mapUid != mapUid) continue;
                if (questStep.targetUid != npcUid) continue;
                if (status != QuestConstants.Status.InProgress) continue;
                questUids.Add(new QuestSaveData(data.Value.QuestUid, data.Value.QuestStepIndex, data.Value.Status));
            }

            return questUids;
        }

        public int GetCount(int questUid)
        {
            var data = QuestDatas.GetValueOrDefault(questUid);
            return data?.Count ?? 0;
        }

        public QuestSaveData GetQuestData(int questUid)
        {
            return QuestDatas.GetValueOrDefault(questUid);
        }

        /// <summary>
        /// 저장 확장 섹션에 기록되는 Quest 진행 데이터 DTO입니다.
        /// </summary>
        public sealed class QuestProgressSnapshot
        {
            /// <summary>
            /// 퀘스트 UID별 진행 상태입니다.
            /// </summary>
            public Dictionary<int, QuestSaveData> QuestDatas;
        }
    }
}
