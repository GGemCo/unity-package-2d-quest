using System;
using System.Threading.Tasks;
using GGemCo2DCore;

namespace GGemCo2DQuest
{
    /// <summary>
    /// <see cref="QuestConstants.ObjectiveType.PlayDialogue"/> 목표를 처리합니다.
    /// 목표가 활성화되면 지정한 대화를 즉시 시작하고, 대화가 끝나면 다음 단계로 진행합니다.
    /// </summary>
    public sealed class ObjectiveHandlerPlayDialogue : ObjectiveHandlerBase
    {
        private int _currentQuestUid;
        private int _currentStepIndex;
        private int _currentDialogueUid;
        private int _currentNpcUid;
        private int _loadGeneration;
        private bool _isRegisteredDialogEnd;
        private bool _isOpeningDialogue;

        /// <summary>
        /// 기본 자동 대화 목표 처리기를 생성합니다.
        /// </summary>
        public ObjectiveHandlerPlayDialogue()
        {
        }

        /// <summary>
        /// 목표 완료 요청을 전달할 소유자를 지정하여 자동 대화 목표 처리기를 생성합니다.
        /// </summary>
        /// <param name="completionSink">목표 완료 요청을 받을 소유자입니다.</param>
        public ObjectiveHandlerPlayDialogue(IObjectiveCompletionSink completionSink) : base(completionSink)
        {
        }

        /// <summary>
        /// 자동 대화 목표를 시작하고 대화 종료 이벤트를 구독한 뒤 대화창을 엽니다.
        /// </summary>
        /// <param name="questUid">진행 중인 퀘스트 UID입니다.</param>
        /// <param name="step">현재 퀘스트 단계 데이터입니다.</param>
        /// <param name="stepIndex">현재 단계 인덱스입니다.</param>
        /// <param name="npcUid">대화 대상 NPC UID입니다.</param>
        protected override void StartObjectiveTyped(int questUid, QuestStep step, int stepIndex, int npcUid)
        {
            if (questUid <= 0 || step == null)
            {
                return;
            }

            int resolvedNpcUid = npcUid > 0 ? npcUid : step.targetUid;
            if (resolvedNpcUid <= 0 || step.dialogueUid <= 0)
            {
                GcLogger.LogError(
                    $"[Quest] PlayDialogue 목표 데이터가 유효하지 않습니다. questUid: {questUid}, stepIndex: {stepIndex}, npcUid: {resolvedNpcUid}, dialogueUid: {step.dialogueUid}");
                return;
            }

            _currentQuestUid = questUid;
            _currentStepIndex = stepIndex;
            _currentDialogueUid = step.dialogueUid;
            _currentNpcUid = resolvedNpcUid;

            RegisterDialogEndEvent();
            int generation = ++_loadGeneration;
            _ = OpenDialogueAsync(generation);
        }

        /// <summary>
        /// PlayDialogue 목표는 대화 종료 이벤트 기반으로 완료되므로 직접 상태 판정은 사용하지 않습니다.
        /// </summary>
        /// <param name="step">현재 단계 정보입니다.</param>
        /// <returns>항상 <see langword="false"/>를 반환합니다.</returns>
        protected override bool IsObjectiveCompleteTyped(QuestStep step)
        {
            return false;
        }

        /// <summary>
        /// 현재 게임 씬의 대화창을 찾아 자동 대화를 비동기로 시작합니다.
        /// </summary>
        /// <param name="generation">폐기된 비동기 완료 결과를 구분할 처리기 세대 값입니다.</param>
        /// <returns>대화 데이터 로드와 대화창 시작 처리가 끝나면 완료되는 작업입니다.</returns>
        private async Task OpenDialogueAsync(int generation)
        {
            if (_isOpeningDialogue)
            {
                return;
            }

            UIWindowDialogue dialogueWindow =
                SceneGame.Instance?.uIWindowManager?.GetUIWindowByUid<UIWindowDialogue>(
                    UIWindowConstants.WindowUid.Dialogue);
            if (dialogueWindow == null)
            {
                GcLogger.LogError(
                    $"[Quest] PlayDialogue 목표에서 대화창을 찾을 수 없습니다. questUid: {_currentQuestUid}, stepIndex: {_currentStepIndex}");
                UnregisterDialogEndEvent();
                return;
            }

            _isOpeningDialogue = true;
            try
            {
                bool loaded = await dialogueWindow.LoadDialogue(_currentDialogueUid, _currentNpcUid);
                if (generation != _loadGeneration)
                {
                    return;
                }

                if (!loaded)
                {
                    GcLogger.LogError(
                        $"[Quest] PlayDialogue 대화 로드에 실패했습니다. questUid: {_currentQuestUid}, stepIndex: {_currentStepIndex}, dialogueUid: {_currentDialogueUid}");
                    UnregisterDialogEndEvent();
                }
            }
            catch (Exception exception)
            {
                if (generation == _loadGeneration)
                {
                    GcLogger.LogException(exception);
                    UnregisterDialogEndEvent();
                }
            }
            finally
            {
                if (generation == _loadGeneration)
                {
                    _isOpeningDialogue = false;
                }
            }
        }

        /// <summary>
        /// 대화 종료 이벤트를 중복 없이 구독합니다.
        /// </summary>
        private void RegisterDialogEndEvent()
        {
            if (_isRegisteredDialogEnd)
            {
                return;
            }

            GameEventManager.DialogEndEvent += OnDialogEnd;
            _isRegisteredDialogEnd = true;
        }

        /// <summary>
        /// 대화 종료 이벤트를 받아 현재 자동 대화의 대상 NPC와 일치하면 목표를 완료합니다.
        /// </summary>
        /// <param name="eventData">종료된 대화의 NPC 정보입니다.</param>
        private void OnDialogEnd(DialogEventData eventData)
        {
            if (_currentQuestUid <= 0 || eventData.NpcUid != _currentNpcUid)
            {
                return;
            }

            int completedQuestUid = _currentQuestUid;
            OnDispose();
            CompleteObjectiveThroughOwner(completedQuestUid);
        }

        /// <summary>
        /// 대화 종료 이벤트 구독을 해제합니다.
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
        /// 진행 중인 비동기 결과를 무효화하고 대화 종료 이벤트 구독과 런타임 상태를 정리합니다.
        /// </summary>
        public override void OnDispose()
        {
            _loadGeneration++;
            UnregisterDialogEndEvent();
            _currentQuestUid = 0;
            _currentStepIndex = 0;
            _currentDialogueUid = 0;
            _currentNpcUid = 0;
            _isOpeningDialogue = false;
        }
    }
}
