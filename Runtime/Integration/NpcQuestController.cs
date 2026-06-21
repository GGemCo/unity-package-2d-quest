using GGemCo2DCore;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DQuest
{
    /// <summary>
    /// NPC 인터랙션 선택지에 표시할 퀘스트 진행 정보입니다.
    /// </summary>
    public class NpcQuestData
    {
        public int QuestUid;
        public int QuestStepIndex;
        public int Count;
        public QuestConstants.Status Status;

        /// <summary>
        /// NPC 퀘스트 진행 정보를 생성합니다.
        /// </summary>
        /// <param name="questUid">퀘스트 UID입니다.</param>
        /// <param name="questStepIndex">현재 단계 인덱스입니다.</param>
        /// <param name="status">현재 퀘스트 상태입니다.</param>
        /// <param name="count">현재 목표 진행 수량입니다.</param>
        public NpcQuestData(int questUid, int questStepIndex, QuestConstants.Status status, int count = 0)
        {
            QuestUid = questUid;
            QuestStepIndex = questStepIndex;
            Count = count;
            Status = status;
        }
    }
    /// <summary>
    /// NPC의 시작 가능 및 진행 중 퀘스트를 조회하고 상태 아이콘을 표시합니다.
    /// </summary>
    public class NpcQuestController : MonoBehaviour
    {
        private Npc _npc;
        private int _readyQuestCount;
        private QuestData _questData;
        private GameObject _iconQuestReady;
        private GameObject _iconQuestInProgress;
        private readonly Vector3 _diffPosition = new Vector3(0, 40f, 0);
        private GameObject _containerNpcName;
        private readonly List<int> _questUidsReady = new List<int>();
        private readonly List<QuestSaveData> _questUidsInProgress = new List<QuestSaveData>();

        /// <summary>
        /// NPC 퀘스트 표시 컴포넌트를 초기화합니다.
        /// </summary>
        /// <param name="npc">연결할 NPC입니다.</param>
        public void Initialize(Npc npc)
        {
            _npc = npc != null ? npc : GetComponent<Npc>();
            _questData = QuestPackageManager.Instance?.QuestData;
            CreateIconMark();
            LoadQuest();
        }

        private void Awake()
        {
            _npc = GetComponent<Npc>();
        }
        private void OnDestroy()
        {
            Destroy(_iconQuestReady);
            Destroy(_iconQuestInProgress);
        }

        private void Start()
        {
            if (_questData == null)
            {
                Initialize(_npc);
            }
        }
        /// <summary>
        /// 느낌표, 물음표 아이콘 생성
        /// </summary>
        private void CreateIconMark()
        {
            if (_iconQuestReady != null || _iconQuestInProgress != null)
            {
                return;
            }

            if (_containerNpcName == null)
            {
                _containerNpcName = SceneGame.Instance?.containerDropItemName;
            }
            if (_containerNpcName == null || _npc == null)
            {
                return;
            }

            GameObject prefabIconQuestReady = ConfigResourcesQuest.IconQuestReady.Load();
            GameObject prefabIconQuestInProgress = ConfigResourcesQuest.IconQuestInProgress.Load();
            if (prefabIconQuestReady != null)
            {
                _iconQuestReady = Instantiate(prefabIconQuestReady, _containerNpcName.transform);
                _iconQuestReady.SetActive(false);
                Vector3 npcNameWorldPosition = _npc.transform.position +
                                               new Vector3(0, _npc.GetHeightByScale(), 0) + _diffPosition;
                _iconQuestReady.transform.position = npcNameWorldPosition;
            }
            if (prefabIconQuestInProgress != null)
            {
                _iconQuestInProgress = Instantiate(prefabIconQuestInProgress, _containerNpcName.transform);
                _iconQuestInProgress.SetActive(false);
                Vector3 npcNameWorldPosition = _npc.transform.position +
                                               new Vector3(0, _npc.GetHeightByScale(), 0) + _diffPosition;
                _iconQuestInProgress.transform.position = npcNameWorldPosition;
            }
        }
        /// <summary>
        /// 컬링으로 인해 활성, 비활성 되기때문에 OnEnable 에서 처리한다
        /// </summary>
        private void OnEnable()
        {
            LoadQuest();
        }
        /// <summary>
        /// 받을 수 있는 퀘스트, 진행중인 퀘스트 찾기
        /// </summary>
        public void LoadQuest()
        {
            if (_npc?.CharacterRegenData == null || _questData == null) return;
            TableQuest tableQuest = TableLoaderManagerQuest.Instance?.TableQuest;
            if (tableQuest == null) return;

            // 현재 맵에서 Npc 가 시작할 수 있는 퀘스트 찾기
            _questUidsReady.Clear();
            IReadOnlyList<int> readyQuestUids =
                tableQuest.GetQuestsByNpcUid(_npc.CharacterRegenData.MapUid, _npc.uid);
            for (int i = 0; i < readyQuestUids.Count; i++)
            {
                _questUidsReady.Add(readyQuestUids[i]);
            }
            // GcLogger.Log("npc.uid: "+npc.uid+" / questuid: "+string.Join(", ", questUids));
            
            // quest 테이블 내용으로는 시작 퀘스트를 찾는다.
            _readyQuestCount = 0;
            foreach (int questUid in _questUidsReady)
            {
                if (_questData.IsStatusNone(questUid))
                {
                    _readyQuestCount++;
                }
            }
            
            // QuestData 에 저장된 정보로 진행중인 퀘스트를 처리한다.
            _questUidsInProgress.Clear();
            _questUidsInProgress.AddRange(
                _questData.GetInProgressQuest(_npc.CharacterRegenData.MapUid, _npc.uid));
            
            // 느낌표
            if (_readyQuestCount > 0)
            {
                _iconQuestReady?.SetActive(true);
                _iconQuestInProgress?.SetActive(false);
            }
            // 물음표
            else if (_questUidsInProgress.Count > 0)
            {
                _iconQuestReady?.SetActive(false);
                _iconQuestInProgress?.SetActive(true);
            }
            else
            {
                _iconQuestReady?.SetActive(false);
                _iconQuestInProgress?.SetActive(false);
            }
        }
        /// <summary>
        /// 현재 Npc 가 진행할 수 있는 퀘스트 가져오기
        /// 받을 수 있거나 Talk To Npc 이거나
        /// </summary>
        /// <returns>현재 NPC에서 선택할 수 있는 퀘스트 목록입니다.</returns>
        public List<NpcQuestData> GetQuestInfos()
        {
            List<NpcQuestData> questInfos = new List<NpcQuestData>();
            // 받을 수 있는 퀘스트
            foreach (var uid in _questUidsReady)
            {
                if (_questData.IsStatusNone(uid) != true) continue;
                questInfos.Add(new NpcQuestData(uid, 0, QuestConstants.Status.Ready));
            }
            // 진행중인 퀘스트 중 target Uid 가 현재 캐릭터와 같으면
            foreach (QuestSaveData questSaveData in _questUidsInProgress)
            {
                QuestStep questStep = QuestPackageManager.Instance?.QuestManager?.GetQuestStep(
                    questSaveData.QuestUid,
                    questSaveData.QuestStepIndex);
                if (questStep == null || questStep.objectiveType != QuestConstants.ObjectiveType.TalkToNpc ||
                    questStep.targetUid != _npc.uid) continue;
                questInfos.Add(new NpcQuestData(questSaveData.QuestUid,questSaveData.QuestStepIndex, questSaveData.Status, questSaveData.Count));
            }

            return questInfos;
        }
    }
}
