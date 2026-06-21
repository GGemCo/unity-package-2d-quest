using GGemCo2DCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GGemCo2DQuest
{
    /// <summary>
    /// 퀘스트 매니저
    /// </summary>
    public class QuestManager : IObjectiveCompletionSink
    {
        private SceneGame _sceneGame;
        private TableQuest _tableQuest;
        private UIWindowHudQuest _uiWindowHudQuest;
        private UIWindowQuestReward _uiWindowQuestReward;
        private UIWindowInventory _uiWindowInventory;
        private QuestData _questData;
        private PlayerData _playerData;
        private InventoryData _inventoryData;
        private IQuestDefinitionRepository _definitionRepository;
        private bool _isInitialDefinitionLoadCompleted;
        private bool _isRegisteredMapEntered;
        private int _pendingMapEnteredUid;
        private int _objectiveStartDepth;
        private int _lifecycleVersion;
        private bool _isFlushingObjectiveCompletions;

        private readonly ObjectiveHandlerFactory _handlerFactory = new ObjectiveHandlerFactory();
        private readonly Queue<int> _pendingObjectiveCompletionQuestUids = new Queue<int>();
        private readonly HashSet<int> _queuedObjectiveCompletionQuestUids = new HashSet<int>();

        // QuestUid → StepIndex → Handler
        private readonly Dictionary<int, Dictionary<int, IObjectiveHandler>> _activeHandlers =
            new Dictionary<int, Dictionary<int, IObjectiveHandler>>();

        /// <summary>
        /// 퀘스트 매니저를 현재 게임 씬과 저장 데이터에 연결합니다.
        /// </summary>
        /// <param name="scene">현재 Core 게임 씬입니다.</param>
        /// <param name="questData">Quest 패키지 진행 저장 데이터입니다.</param>
        public void Initialize(SceneGame scene, QuestData questData)
        {
            _activeHandlers.Clear();
            _lifecycleVersion++;
            _definitionRepository?.Dispose();
            _definitionRepository = null;
            _isInitialDefinitionLoadCompleted = false;
            _pendingMapEnteredUid = 0;
            _objectiveStartDepth = 0;
            _isFlushingObjectiveCompletions = false;
            _pendingObjectiveCompletionQuestUids.Clear();
            _queuedObjectiveCompletionQuestUids.Clear();
            _sceneGame = scene;
            _tableQuest = TableLoaderManagerQuest.Instance?.TableQuest;
            _questData = questData;
            _definitionRepository = new QuestDefinitionRepository(_tableQuest);
        }

        /// <summary>
        /// 게임 씬 시작 후 저장 데이터와 UI 참조를 연결하고 퀘스트 이벤트를 구독합니다.
        /// </summary>
        public void OnStartBySceneGame()
        {
            _playerData = _sceneGame.saveDataManager.Player;
            _inventoryData = _sceneGame.saveDataManager.Inventory;
            _uiWindowHudQuest =
                _sceneGame.uIWindowManager?.GetUIWindowByUid<UIWindowHudQuest>(QuestWindowConstants.HudQuest);
            _uiWindowQuestReward =
                _sceneGame.uIWindowManager?.GetUIWindowByUid<UIWindowQuestReward>(QuestWindowConstants.QuestReward);
            _uiWindowInventory =
                _sceneGame.uIWindowManager?.GetUIWindowByUid<UIWindowInventory>(UIWindowConstants.WindowUid.Inventory);
            RegisterMapEnteredEvent();
            _ = LoadInitialQuestDefinitions(_lifecycleVersion);
        }

        /// <summary>
        /// 맵 입장 이벤트를 중복 없이 구독합니다.
        /// </summary>
        private void RegisterMapEnteredEvent()
        {
            if (_isRegisteredMapEntered) return;
            GameEventManager.MapEnteredEvent += OnMapEntered;
            _isRegisteredMapEntered = true;
        }

        /// <summary>
        /// 저장 데이터에서 진행 중인 퀘스트 정의만 우선 로드하고 목표 처리기를 복원합니다.
        /// </summary>
        /// <param name="lifecycleVersion">초기화를 시작한 QuestManager 수명주기 버전입니다.</param>
        private async Task LoadInitialQuestDefinitions(int lifecycleVersion)
        {
            IQuestDefinitionRepository repository = _definitionRepository;
            if (repository == null || _questData == null)
            {
                return;
            }

            try
            {
                var datas = _questData.GetQuestDatas();
                if (datas != null)
                {
                    foreach (KeyValuePair<int, QuestSaveData> data in datas)
                    {
                        QuestSaveData questSaveData = data.Value;
                        if (questSaveData == null ||
                            questSaveData.Status != QuestConstants.Status.InProgress)
                        {
                            continue;
                        }

                        Quest quest = await repository.GetAsync(questSaveData.QuestUid);
                        if (lifecycleVersion != _lifecycleVersion)
                        {
                            return;
                        }

                        if (quest == null)
                        {
                            GcLogger.LogError(
                                $"진행 중인 Quest JSON을 복원하지 못했습니다. uid: {questSaveData.QuestUid}");
                            continue;
                        }

                        repository.MarkActive(questSaveData.QuestUid);
                        StartObjective(questSaveData.QuestUid, questSaveData.QuestStepIndex);
                    }
                }
            }
            catch (System.Exception exception)
            {
                GcLogger.LogException(exception);
            }

            if (lifecycleVersion != _lifecycleVersion)
            {
                return;
            }

            _isInitialDefinitionLoadCompleted = true;
            await TryStartPendingEnterMapQuests();
        }

        /// <summary>
        /// 맵 입장 이벤트를 받아 EnterMap 트리거 퀘스트를 시작합니다.
        /// 진행 중인 퀘스트 정의 복원이 끝나기 전이면 실제 맵 입장 이벤트로 들어온 맵만 보류합니다.
        /// </summary>
        /// <param name="eventData">입장 완료된 맵 정보입니다.</param>
        private async void OnMapEntered(MapEnteredEventData eventData)
        {
            if (eventData.MapUid <= 0) return;
            if (!_isInitialDefinitionLoadCompleted)
            {
                _pendingMapEnteredUid = eventData.MapUid;
                return;
            }

            await TryStartQuestsByEnterMap(eventData.MapUid);
            _ = PreloadQuestDefinitionsForMap(eventData.MapUid);
        }

        /// <summary>
        /// 초기 Quest 정의 복원 전에 수신한 맵 입장 이벤트가 있으면 해당 맵의 EnterMap 퀘스트를 시작합니다.
        /// 임의의 현재 맵을 추정하지 않고 OnMapLoadComplete에서 발행된 이벤트만 처리합니다.
        /// </summary>
        private async Task TryStartPendingEnterMapQuests()
        {
            if (_pendingMapEnteredUid <= 0) return;

            int mapUid = _pendingMapEnteredUid;
            _pendingMapEnteredUid = 0;
            await TryStartQuestsByEnterMap(mapUid);
            _ = PreloadQuestDefinitionsForMap(mapUid);
        }

        /// <summary>
        /// 지정한 맵에 입장했을 때 자동 시작 가능한 퀘스트를 찾아 시작합니다.
        /// </summary>
        /// <param name="mapUid">입장한 맵 UID입니다.</param>
        private async Task TryStartQuestsByEnterMap(int mapUid)
        {
            if (mapUid <= 0 || _tableQuest == null || _questData == null) return;

            IReadOnlyList<int> questUids = _tableQuest.GetQuestsByEnterMap(mapUid);
            for (int i = 0; i < questUids.Count; i++)
            {
                int questUid = questUids[i];
                if (!_questData.IsStatusNone(questUid)) continue;
                await StartQuest(questUid, 0, false);
            }
        }

        /// <summary>
        /// 현재 맵에서 시작될 가능성이 있는 Quest JSON을 낮은 우선순위로 미리 로드합니다.
        /// 맵 입장 처리 자체는 프리로드 완료를 기다리지 않습니다.
        /// </summary>
        /// <param name="mapUid">프리로드 후보를 조회할 현재 맵 UID입니다.</param>
        private async Task PreloadQuestDefinitionsForMap(int mapUid)
        {
            if (mapUid <= 0 || _tableQuest == null || _definitionRepository == null)
            {
                return;
            }

            _definitionRepository.ReleaseUnused(mapUid);
            IReadOnlyList<int> questUids = _tableQuest.GetQuestsByMap(mapUid);
            await _definitionRepository.PreloadAsync(questUids, mapUid);
        }

        /// <summary>
        /// 지정한 퀘스트를 시작하고 첫 번째 목표를 활성화합니다.
        /// </summary>
        /// <param name="questUid">시작할 퀘스트 UID입니다.</param>
        /// <param name="npcUid">퀘스트를 시작한 NPC UID입니다. 맵 입장 시작 퀘스트는 0을 사용합니다.</param>
        /// <param name="showAlreadyStartedWarning">이미 진행 중일 때 시스템 경고 메시지를 표시할지 여부입니다.</param>
        /// <returns>퀘스트를 새로 시작했으면 <see langword="true"/>, 시작할 수 없으면 <see langword="false"/>입니다.</returns>
        public async Task<bool> StartQuest(int questUid, int npcUid, bool showAlreadyStartedWarning = true)
        {
            if (questUid <= 0) return false;
            var info = _tableQuest.GetDataByUid(questUid);
            if (info == null) return false;

            if (_questData.IsStatusNone(questUid) != true)
            {
                if (showAlreadyStartedWarning)
                {
                    string message = LocalizationManager.Instance?.GetSmartString(
                        QuestLocalizationConstants.SystemMessageTable,
                        QuestLocalizationConstants.AlreadyInProgressKey);
                    if (string.IsNullOrEmpty(message))
                    {
                        // Quest Localization 샘플을 아직 병합하지 않은 프로젝트에서도 의미가 전달되도록 기본 문구를 사용합니다.
                        message = "이미 진행 중인 퀘스트입니다.";
                    }

                    _sceneGame.systemMessageManager.ShowMessageWarning(message);
                }

                return false;
            }

            IQuestDefinitionRepository repository = _definitionRepository;
            if (repository == null)
            {
                return false;
            }

            Quest quest = await repository.GetAsync(questUid);
            if (repository != _definitionRepository)
            {
                return false;
            }

            if (quest == null)
            {
                GcLogger.LogError("퀘스트 json 파일을 불러오지 못 했습니다. uid: " + questUid);
                return false;
            }

            repository.MarkActive(questUid);
            // 첫 단계 시작
            int stepIndex = 0;
            StartObjective(quest.uid, stepIndex, npcUid);
            QuestStep questStep = GetQuestStep(quest.uid, stepIndex);
            // 첫 단계가 talk to npc 이면 바로 시작
            if (questStep != null && questStep.objectiveType == QuestConstants.ObjectiveType.TalkToNpc)
            {
                int dialogNpcUid = npcUid > 0 ? npcUid : questStep.targetUid;
                var data = new DialogEventData(
                    npcUid: dialogNpcUid
                );
                GameEventManager.DialogStart(data);
            }

            return true;
        }

        /// <summary>
        /// quest 상태 변경
        /// </summary>
        /// <param name="questUid"></param>
        /// <param name="stepIndex"></param>
        /// <param name="status"></param>
        private void ChangeStatus(int questUid, int stepIndex, QuestConstants.Status status)
        {
            _questData.SaveStatus(questUid, stepIndex, status);
        }

        /// <summary>
        /// UIWindowHudQuest 에 element 추가하기 
        /// </summary>
        /// <param name="questUid"></param>
        /// <param name="questStepIndex"></param>
        private void AddHudQuestElement(int questUid, int questStepIndex)
        {
            if (questUid <= 0) return;
            _uiWindowHudQuest?.AddQuestElement(questUid, questStepIndex);
        }

        /// <summary>
        /// 목표 처리기에서 전달한 완료 요청을 처리합니다.
        /// 목표 시작 중 즉시 완료된 요청은 핸들러 등록이 끝난 뒤 순차 처리합니다.
        /// </summary>
        /// <param name="questUid">완료할 퀘스트 UID입니다.</param>
        public void CompleteObjective(int questUid)
        {
            if (questUid <= 0) return;

            if (_objectiveStartDepth > 0 || _isFlushingObjectiveCompletions)
            {
                EnqueueObjectiveCompletion(questUid);
                return;
            }

            NextStep(questUid);
        }

        /// <summary>
        /// 목표 시작 중 발생한 완료 요청을 중복 없이 대기열에 등록합니다.
        /// </summary>
        /// <param name="questUid">완료 대기열에 추가할 퀘스트 UID입니다.</param>
        private void EnqueueObjectiveCompletion(int questUid)
        {
            if (questUid <= 0) return;
            if (!_queuedObjectiveCompletionQuestUids.Add(questUid)) return;

            _pendingObjectiveCompletionQuestUids.Enqueue(questUid);
        }

        /// <summary>
        /// 목표 시작이 끝난 뒤 보류된 완료 요청을 순서대로 처리합니다.
        /// </summary>
        private void FlushPendingObjectiveCompletions()
        {
            if (_isFlushingObjectiveCompletions) return;
            if (_objectiveStartDepth > 0) return;

            _isFlushingObjectiveCompletions = true;
            try
            {
                while (_pendingObjectiveCompletionQuestUids.Count > 0)
                {
                    int questUid = _pendingObjectiveCompletionQuestUids.Dequeue();
                    _queuedObjectiveCompletionQuestUids.Remove(questUid);
                    NextStep(questUid);
                }
            }
            finally
            {
                _isFlushingObjectiveCompletions = false;
            }
        }

        /// <summary>
        /// 다음 목표 시작
        /// 다음 목표가 없으면 end 처리 
        /// </summary>
        /// <param name="questUid"></param>
        public void NextStep(int questUid)
        {
            if (_definitionRepository == null || !_definitionRepository.TryGet(questUid, out _))
            {
                GcLogger.LogError("캐시에 없는 진행 중 퀘스트입니다. quest uid:" + questUid);
                return;
            }

            if (_questData == null) return;

            // 현재 step 가져오기
            QuestSaveData questSaveData = _questData.GetQuestData(questUid);
            if (questSaveData == null || questSaveData.Status != QuestConstants.Status.InProgress)
            {
                DisposeQuestHandlers(questUid);
                return;
            }

            var stepDict = _activeHandlers.GetValueOrDefault(questUid);
            if (stepDict == null || !stepDict.ContainsKey(questSaveData.QuestStepIndex))
            {
                GcLogger.LogError("진행중인 퀘스트가 아닙니다. quest uid:" + questUid);
                return;
            }

            // 현제 handler 지우기
            DisposeQuestStepHandlers(questUid, questSaveData.QuestStepIndex);

            int nextStepIndex = questSaveData.QuestStepIndex + 1;
            QuestStep questStep = GetQuestStep(questUid, nextStepIndex);
            // 다음 단계가 없으면 종료 처리
            if (questStep == null)
            {
                EndQuest(questUid);
            }
            else
            {
                // count 초기화 먼저 해주기
                _questData.SaveCount(questUid, 0);
                StartObjective(questUid, nextStepIndex, questStep.targetUid);
            }
        }

        /// <summary>
        /// 퀘스트 완료 처리
        /// </summary>
        /// <param name="questUid"></param>
        private void EndQuest(int questUid)
        {
            if (questUid <= 0) return;
            QuestSaveData questSaveData = _questData.GetQuestData(questUid);
            if (questSaveData == null) return;
            // 보상 주기
            GiveReward(questUid);
            // 인벤토리 공간 부족할때
            _uiWindowQuestReward?.SetRewardInfoByQuestUid(questUid);

            // 저장하기
            _questData.SaveStatus(questUid, questSaveData.QuestStepIndex, QuestConstants.Status.End);

            // UIWindowHudQuest 에 element 빼기
            _uiWindowHudQuest?.RemoveQuestElement(questUid);
            DisposeQuestHandlers(questUid);
            _definitionRepository?.MarkInactive(questUid);
        }

        /// <summary>
        /// 퀘스트 완료 보상을 플레이어 데이터와 맵 진행 데이터에 적용합니다.
        /// </summary>
        /// <param name="questUid">보상을 지급할 퀘스트 UID입니다.</param>
        private void GiveReward(int questUid)
        {
            if (questUid <= 0) return;
            if (_definitionRepository == null || !_definitionRepository.TryGet(questUid, out Quest quest))
            {
                GcLogger.LogError("quest json 정보가 없습니다. uid: " + questUid);
                return;
            }

            if (quest.reward == null)
            {
                GcLogger.LogError("quest 보상 정보가 없습니다. uid: " + questUid);
                return;
            }

            _playerData?.AddExp(quest.reward.experience);
            _playerData?.AddCurrency(CurrencyConstants.Type.Gold, quest.reward.gold);
            _playerData?.AddCurrency(CurrencyConstants.Type.Silver, quest.reward.silver);
            GiveItemReward(quest.reward.items);
            GiveMapProgressReward(quest.reward.mapProgress);
            GiveLicenseReward(quest.reward.licenses);
        }

        /// <summary>
        /// 퀘스트 아이템 보상을 인벤토리에 추가하고 인벤토리 UI를 갱신합니다.
        /// </summary>
        /// <param name="items">지급할 아이템 보상 목록입니다.</param>
        private void GiveItemReward(List<RewardItem> items)
        {
            if (items == null || items.Count <= 0) return;

            foreach (var rewardItem in items)
            {
                if (rewardItem == null) continue;
                ResultCommon result = _inventoryData?.AddItem(rewardItem.itemUid, rewardItem.amount);
                _uiWindowInventory?.SetIcons(result);
            }
        }

        /// <summary>
        /// 퀘스트 완료 보상으로 맵 클리어, 월드맵 노드 표시, 월드맵 노드 활성화를 적용합니다.
        /// </summary>
        /// <param name="mapProgress">맵 진행 보상 정보입니다.</param>
        private void GiveMapProgressReward(QuestRewardMapProgress mapProgress)
        {
            if (mapProgress == null) return;

            bool hasClearMapReward = HasValidClearMapReward(mapProgress.clearMapUids);
            bool hasVisibleWorldMapNodes = mapProgress.visibleWorldMapNodeIds != null &&
                                           mapProgress.visibleWorldMapNodeIds.Count > 0;
            bool hasWorldMapNodes = mapProgress.activateWorldMapNodeIds != null &&
                                    mapProgress.activateWorldMapNodeIds.Count > 0;
            if (!hasClearMapReward && !hasVisibleWorldMapNodes && !hasWorldMapNodes) return;

            _sceneGame.saveDataManager.MapProgressController.ClearMaps(
                mapProgress.clearMapUids,
                mapProgress.activateWorldMapNodeIds,
                mapProgress.visibleWorldMapNodeIds);
        }

        /// <summary>
        /// 퀘스트 보상에 실제로 적용 가능한 클리어 맵 UID가 포함되어 있는지 확인합니다.
        /// </summary>
        /// <param name="clearMapUids">퀘스트 보상에 설정된 클리어 맵 UID 목록입니다.</param>
        /// <returns>0보다 큰 맵 UID가 하나 이상 있으면 true를 반환합니다.</returns>
        private static bool HasValidClearMapReward(List<int> clearMapUids)
        {
            if (clearMapUids == null || clearMapUids.Count <= 0) return false;

            foreach (int clearMapUid in clearMapUids)
            {
                if (clearMapUid > 0)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 퀘스트 완료 보상으로 라이센스 값을 저장합니다.
        /// </summary>
        /// <param name="licenses">저장할 라이센스 보상 목록입니다.</param>
        private void GiveLicenseReward(List<QuestRewardLicense> licenses)
        {
            if (licenses == null || licenses.Count <= 0) return;

            LicenseManager licenseManager = _sceneGame?.saveDataManager?.LicenseManager;
            if (licenseManager == null) return;

            foreach (QuestRewardLicense license in licenses)
            {
                if (license == null || license.licenseUid <= 0) continue;

                string value = string.IsNullOrWhiteSpace(license.value)
                    ? LicenseConstants.TrueValue
                    : license.value;
                licenseManager.SetByUid(license.licenseUid, value);
            }
        }

        /// <summary>
        /// 목표 시작
        /// </summary>
        /// <param name="questUid"></param>
        /// <param name="stepIndex"></param>
        /// <param name="npcUid"></param>
        private void StartObjective(int questUid, int stepIndex, int npcUid = 0)
        {
            QuestStep questStep = GetQuestStep(questUid, stepIndex);
            if (questStep == null)
            {
                GcLogger.LogError("퀘스트 json에 단계 정보가 없습니다. uid: " + questUid + ", stepIndex: " + stepIndex);
                return;
            }

            var handler = _handlerFactory.CreateHandler(questStep.objectiveType, this);
            if (handler == null)
            {
                GcLogger.LogError("퀘스트 목표 정보가 없습니다. uid: " + questUid + ", stepIndex: " + stepIndex + ", objecitve: " +
                                  questStep.objectiveType);
                return;
            }

            // 퀘스트 진행중으로, stepIndex 업데이트
            // 저장 먼저.
            ChangeStatus(questUid, stepIndex, QuestConstants.Status.InProgress);

            if (!_activeHandlers.ContainsKey(questUid))
                _activeHandlers[questUid] = new Dictionary<int, IObjectiveHandler>();

            _activeHandlers[questUid][stepIndex] = handler;

            // UIWindowHudQuest 에 element 추가
            AddHudQuestElement(questUid, stepIndex);

            // 목표 시작
            if (npcUid <= 0 && questStep.targetUid > 0)
            {
                npcUid = questStep.targetUid;
            }

            _objectiveStartDepth++;
            try
            {
                handler.StartObjective(questUid, questStep, stepIndex, npcUid);
            }
            finally
            {
                _objectiveStartDepth--;
                if (_objectiveStartDepth <= 0)
                {
                    _objectiveStartDepth = 0;
                    FlushPendingObjectiveCompletions();
                }
            }
        }

        public void CheckStepComplete(int questUid, int stepIndex, QuestStep step)
        {
            if (!_activeHandlers.TryGetValue(questUid, out var stepDict)) return;
            if (!stepDict.TryGetValue(stepIndex, out var handler)) return;
            if (handler.IsObjectiveComplete(step))
            {
                GcLogger.Log($"[QuestManager] 퀘스트 {questUid}, 스텝 {stepIndex} 완료!");
                // 다음 단계 or 완료 처리
            }
        }

        /// <summary>
        /// 지정한 맵에서 특정 목표 타입의 퀘스트가 진행 중인지 확인합니다.
        /// </summary>
        /// <param name="mapUid">확인할 맵 UID입니다.</param>
        /// <param name="objectiveType">확인할 목표 타입입니다.</param>
        /// <returns>해당 맵에서 목표 타입이 진행 중이면 true입니다.</returns>
        public bool HasActiveObjective(int mapUid, QuestConstants.ObjectiveType objectiveType)
        {
            if (mapUid <= 0) return false;

            foreach (var questHandlers in _activeHandlers)
            {
                int questUid = questHandlers.Key;
                foreach (int stepIndex in questHandlers.Value.Keys)
                {
                    QuestStep questStep = GetQuestStep(questUid, stepIndex);
                    if (questStep == null) continue;
                    if (questStep.objectiveType != objectiveType) continue;
                    if (questStep.mapUid != mapUid) continue;
                    return true;
                }
            }

            return false;
        }

        public void DisposeQuestHandlers(int questUid)
        {
            if (_activeHandlers.TryGetValue(questUid, out var stepDict))
            {
                foreach (var handler in stepDict.Values)
                    handler.OnDispose();
            }

            _activeHandlers.Remove(questUid);
        }

        private void DisposeQuestStepHandlers(int questUid, int stepIndex)
        {
            if (!_activeHandlers.TryGetValue(questUid, out var stepDict)) return;
            if (!stepDict.TryGetValue(stepIndex, out var handler)) return;
            handler.OnDispose();
            _activeHandlers[questUid].Remove(stepIndex);
        }

        /// <summary>
        /// 퀘스트 매니저가 구독한 이벤트와 진행 중인 목표 처리기를 모두 정리합니다.
        /// </summary>
        public void OnDestroy()
        {
            _lifecycleVersion++;
            if (_isRegisteredMapEntered)
            {
                GameEventManager.MapEnteredEvent -= OnMapEntered;
                _isRegisteredMapEntered = false;
            }

            _pendingMapEnteredUid = 0;
            _objectiveStartDepth = 0;
            _isFlushingObjectiveCompletions = false;
            _pendingObjectiveCompletionQuestUids.Clear();
            _queuedObjectiveCompletionQuestUids.Clear();

            DisposeAllHandlers();
            _definitionRepository?.Dispose();
            _definitionRepository = null;
        }

        private void DisposeAllHandlers()
        {
            foreach (var kvp in _activeHandlers)
            {
                foreach (var handler in kvp.Value.Values)
                    handler.OnDispose();
            }

            _activeHandlers.Clear();
        }

        /// <summary>
        /// 캐시에 적재된 퀘스트 정의에서 지정한 목표 단계를 조회합니다.
        /// </summary>
        /// <param name="questUid">조회할 퀘스트 UID입니다.</param>
        /// <param name="stepIndex">조회할 목표 단계 인덱스입니다.</param>
        /// <returns>유효한 목표 단계입니다. 정의가 없거나 인덱스 범위를 벗어나면 null을 반환합니다.</returns>
        public QuestStep GetQuestStep(int questUid, int stepIndex)
        {
            if (_definitionRepository == null ||
                !_definitionRepository.TryGet(questUid, out Quest quest) ||
                quest.steps == null ||
                stepIndex < 0 ||
                stepIndex >= quest.steps.Count)
            {
                return null;
            }

            return quest.steps[stepIndex];
        }

        /// <summary>
        /// 캐시에 적재된 퀘스트 정의를 반환하고 최근 사용 순서를 갱신합니다.
        /// </summary>
        /// <param name="questUid">조회할 퀘스트 UID입니다.</param>
        /// <returns>캐시된 퀘스트 정의입니다. 아직 로드되지 않았으면 null을 반환합니다.</returns>
        public Quest GetQuestInfo(int questUid)
        {
            return _definitionRepository != null &&
                   _definitionRepository.TryGet(questUid, out Quest quest)
                ? quest
                : null;
        }
    }
}
