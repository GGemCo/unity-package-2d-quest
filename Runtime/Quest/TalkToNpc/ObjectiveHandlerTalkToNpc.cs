using System;
using System.Threading.Tasks;
using GGemCo2DCore;

namespace GGemCo2DQuest
{
    /// <summary>
    /// 지정한 NPC와의 대화 완료 목표를 처리합니다.
    /// </summary>
    public class ObjectiveHandlerTalkToNpc : ObjectiveHandlerBase
    {
        private QuestStep _currentStep;
        private int _currentQuestUid;
        private int _currentStepIndex;
        private bool _isRegisteredDialogEnd;
        private bool _isLoadingDialogue;
        private int _dialogueLoadGeneration;

        /// <summary>
        /// 기본 NPC 대화 목표 처리기를 생성합니다.
        /// </summary>
        public ObjectiveHandlerTalkToNpc()
        {
        }

        /// <summary>
        /// 목표 완료 요청을 전달할 소유자를 지정하여 NPC 대화 목표 처리기를 생성합니다.
        /// </summary>
        /// <param name="completionSink">목표 완료 요청을 받을 소유자입니다.</param>
        public ObjectiveHandlerTalkToNpc(IObjectiveCompletionSink completionSink) : base(completionSink)
        {
        }

        /// <summary>
        /// NPC 대화 목표를 시작하고 대상 NPC의 퀘스트 표시 정보를 갱신합니다.
        /// </summary>
        /// <param name="questUid">진행 중인 퀘스트 UID입니다.</param>
        /// <param name="step">현재 퀘스트 단계 정보입니다.</param>
        /// <param name="stepIndex">현재 퀘스트 단계 인덱스입니다.</param>
        /// <param name="npcUid">대화 대상 NPC UID입니다.</param>
        protected override void StartObjectiveTyped(int questUid, QuestStep step, int stepIndex, int npcUid)
        {
            if (step == null || step.targetUid != npcUid) return;
            if (step.dialogueUid <= 0) return;

            _currentQuestUid = questUid;
            _currentStep = step;
            _currentStepIndex = stepIndex;

            UpdateNpcQuestInfo(npcUid);
        }

        /// <summary>
        /// NPC 대화 목표는 이벤트 기반 완료이므로 직접 상태 판정은 사용하지 않습니다.
        /// </summary>
        /// <param name="step">현재 단계 정보입니다.</param>
        /// <returns>항상 <see langword="false"/>를 반환합니다.</returns>
        protected override bool IsObjectiveCompleteTyped(QuestStep step)
        {
            return false;
        }

        /// <summary>
        /// 현재 활성화된 TalkToNpc 목표의 대화를 명시적으로 시작합니다.
        /// </summary>
        /// <param name="npcUid">상호작용한 NPC UID입니다.</param>
        /// <returns>현재 목표와 NPC가 일치하여 대화 시작 요청을 수락했으면 <see langword="true"/>를 반환합니다.</returns>
        public bool TryStartDialogue(int npcUid)
        {
            if (_currentStep == null || _currentStep.targetUid != npcUid || _isLoadingDialogue)
            {
                return false;
            }

            UIWindowDialogue uiWindowDialogue =
                SceneGame.Instance?.uIWindowManager?.GetUIWindowByUid<UIWindowDialogue>(
                    UIWindowConstants.WindowUid.Dialogue);
            if (uiWindowDialogue == null)
            {
                GcLogger.LogError(
                    $"[Quest] TalkToNpc 목표에서 대화창을 찾을 수 없습니다. questUid: {_currentQuestUid}, stepIndex: {_currentStepIndex}");
                return false;
            }

            RegisterDialogEndEvent();
            int generation = ++_dialogueLoadGeneration;
            _isLoadingDialogue = true;
            _ = LoadDialogueAsync(uiWindowDialogue, npcUid, generation);
            return true;
        }

        /// <summary>
        /// 대화 종료 이벤트를 중복 없이 구독합니다.
        /// </summary>
        private void RegisterDialogEndEvent()
        {
            if (_isRegisteredDialogEnd) return;

            GameEventManager.DialogEndEvent += OnDialogEnd;
            _isRegisteredDialogEnd = true;
        }

        /// <summary>
        /// 대상 NPC의 대화 데이터를 비동기로 로드하고 실패 시 종료 이벤트 구독을 복구합니다.
        /// </summary>
        /// <param name="uiWindowDialogue">대화를 표시할 Core 대화창입니다.</param>
        /// <param name="npcUid">대화 대상 NPC UID입니다.</param>
        /// <param name="generation">폐기된 비동기 완료 결과를 구분할 처리기 세대 값입니다.</param>
        /// <returns>대화 데이터 로드와 대화창 시작 처리가 끝나면 완료되는 작업입니다.</returns>
        private async Task LoadDialogueAsync(
            UIWindowDialogue uiWindowDialogue,
            int npcUid,
            int generation)
        {
            try
            {
                bool loaded = await uiWindowDialogue.LoadDialogue(_currentStep.dialogueUid, npcUid);
                if (generation != _dialogueLoadGeneration)
                {
                    return;
                }

                if (!loaded)
                {
                    GcLogger.LogError(
                        $"[Quest] TalkToNpc 대화 로드에 실패했습니다. questUid: {_currentQuestUid}, stepIndex: {_currentStepIndex}, dialogueUid: {_currentStep.dialogueUid}");
                    UnregisterDialogEndEvent();
                }
            }
            catch (Exception exception)
            {
                if (generation == _dialogueLoadGeneration)
                {
                    GcLogger.LogException(exception);
                    UnregisterDialogEndEvent();
                }
            }
            finally
            {
                if (generation == _dialogueLoadGeneration)
                {
                    _isLoadingDialogue = false;
                }
            }
        }

        /// <summary>
        /// 대화 종료 이벤트를 받아 대상 NPC와의 대화가 끝났으면 목표를 완료합니다.
        /// </summary>
        /// <param name="eventData">대화를 종료한 NPC 정보입니다.</param>
        private void OnDialogEnd(DialogEventData eventData)
        {
            if (_currentStep == null) return;

            int npcUid = eventData.NpcUid;
            if (_currentStep.targetUid != npcUid) return;

            int completedQuestUid = _currentQuestUid;
            OnDispose();
            CompleteObjectiveThroughOwner(completedQuestUid);
            UpdateNpcQuestInfo(npcUid);
        }

        /// <summary>
        /// NPC의 퀘스트 표시 정보를 갱신합니다.
        /// </summary>
        /// <param name="npcUid">갱신할 NPC UID입니다.</param>
        private void UpdateNpcQuestInfo(int npcUid)
        {
            Npc npc = SceneGame.Instance?.mapManager?.GetNpcByUid(npcUid) as Npc;
            npc?.GetComponent<NpcQuestController>()?.LoadQuest();
        }

        /// <summary>
        /// 대화 종료 이벤트 구독을 해제합니다.
        /// 대화 로드 실패 시 목표 상태는 유지하여 플레이어가 다시 상호작용할 수 있게 합니다.
        /// </summary>
        private void UnregisterDialogEndEvent()
        {
            if (!_isRegisteredDialogEnd)
            {
                return;
            }

            GameEventManager.DialogEndEvent -= OnDialogEnd;
            _isRegisteredDialogEnd = false;
        }

        /// <summary>
        /// 진행 중인 비동기 대화 로드 결과를 무효화하고 구독한 대화 이벤트를 해제합니다.
        /// </summary>
        public override void OnDispose()
        {
            _dialogueLoadGeneration++;
            _isLoadingDialogue = false;
            UnregisterDialogEndEvent();
            _currentStep = null;
            _currentQuestUid = 0;
            _currentStepIndex = 0;
        }
    }
}
