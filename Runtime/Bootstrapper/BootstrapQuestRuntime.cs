using System.Collections.Generic;
using GGemCo2DCore;
using UnityEngine;

namespace GGemCo2DQuest
{
    /// <summary>
    /// Core 캐릭터 수명주기와 Quest NPC 표시 컴포넌트를 연결하는 런타임 부트스트랩입니다.
    /// Quest 테이블과 Addressables 데이터 로딩은 <see cref="SceneLoadingQuest"/>가 담당합니다.
    /// </summary>
    public sealed class BootstrapQuestRuntime : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("NPC에 NpcQuestController가 없을 때 자동으로 추가할지 여부입니다.")]
        private bool addIfMissing = true;

        private readonly HashSet<Npc> _pendingNpcs = new HashSet<Npc>();
        private QuestPackageManager _packageManager;
        private bool _isSubscribed;

        /// <summary>
        /// Quest 패키지 매니저와 연결하고 보류 중인 NPC 초기화를 완료합니다.
        /// </summary>
        /// <param name="packageManager">Quest 데이터와 실행 매니저를 소유한 패키지 매니저입니다.</param>
        public void Activate(QuestPackageManager packageManager)
        {
            if (packageManager == null)
            {
                return;
            }

            _packageManager = packageManager;
            Subscribe();
            FlushPendingNpcs();
        }

        /// <summary>
        /// Quest 패키지 연결을 해제하고 초기화 대기 중인 NPC 참조를 정리합니다.
        /// </summary>
        public void Deactivate()
        {
            Unsubscribe();
            _pendingNpcs.Clear();
            _packageManager = null;
        }

        /// <summary>
        /// 컴포넌트가 활성화되면 캐릭터 활성화와 표시 갱신 이벤트를 구독합니다.
        /// 패키지 매니저 초기화 전 수신한 NPC는 대기 목록에 보관합니다.
        /// </summary>
        private void OnEnable()
        {
            Subscribe();
        }

        /// <summary>
        /// 컴포넌트가 비활성화되면 모든 Core 이벤트 구독을 해제합니다.
        /// </summary>
        private void OnDisable()
        {
            Unsubscribe();
        }

        /// <summary>
        /// Core 캐릭터 이벤트와 표시 갱신 레지스트리를 중복 없이 구독합니다.
        /// </summary>
        private void Subscribe()
        {
            if (_isSubscribed)
            {
                return;
            }

            CharacterManager.OnCharacterActivated += HandleCharacterActivated;
            CharacterPresentationRefreshRegistry.Register(RefreshCharacterPresentation);
            _isSubscribed = true;
        }

        /// <summary>
        /// Core 캐릭터 이벤트와 표시 갱신 레지스트리 구독을 해제합니다.
        /// </summary>
        private void Unsubscribe()
        {
            if (!_isSubscribed)
            {
                return;
            }

            CharacterManager.OnCharacterActivated -= HandleCharacterActivated;
            CharacterPresentationRefreshRegistry.Unregister(RefreshCharacterPresentation);
            _isSubscribed = false;
        }

        /// <summary>
        /// 활성화가 완료된 NPC에 Quest 표시 컨트롤러를 연결합니다.
        /// Quest 패키지 초기화 전이면 NPC를 보류 목록에 저장합니다.
        /// </summary>
        /// <param name="character">Core 초기화가 완료된 캐릭터입니다.</param>
        private void HandleCharacterActivated(CharacterBase character)
        {
            if (character is not Npc npc)
            {
                return;
            }

            if (!IsPackageReady())
            {
                _pendingNpcs.Add(npc);
                return;
            }

            InitializeNpcController(npc);
        }

        /// <summary>
        /// Core의 캐릭터 표시 갱신 요청을 NPC Quest 표시 컨트롤러에 전달합니다.
        /// </summary>
        /// <param name="character">표시 상태를 갱신할 캐릭터입니다.</param>
        private void RefreshCharacterPresentation(CharacterBase character)
        {
            if (character is not Npc npc)
            {
                return;
            }

            NpcQuestController controller = npc.GetComponent<NpcQuestController>();
            if (controller != null)
            {
                controller.LoadQuest();
                return;
            }

            if (!IsPackageReady())
            {
                _pendingNpcs.Add(npc);
                return;
            }

            InitializeNpcController(npc);
        }

        /// <summary>
        /// Quest 패키지 초기화 전에 활성화된 NPC를 순회하여 표시 컨트롤러를 연결합니다.
        /// 파괴된 Unity Object는 초기화 대상에서 제외합니다.
        /// </summary>
        private void FlushPendingNpcs()
        {
            if (!IsPackageReady() || _pendingNpcs.Count <= 0)
            {
                return;
            }

            foreach (Npc npc in _pendingNpcs)
            {
                if (npc == null)
                {
                    continue;
                }

                InitializeNpcController(npc);
            }

            _pendingNpcs.Clear();
        }

        /// <summary>
        /// NPC에 <see cref="NpcQuestController"/>를 보장하고 현재 Quest 데이터로 초기화합니다.
        /// </summary>
        /// <param name="npc">Quest 표시 기능을 연결할 NPC입니다.</param>
        private void InitializeNpcController(Npc npc)
        {
            if (npc == null)
            {
                return;
            }

            NpcQuestController controller = npc.GetComponent<NpcQuestController>();
            if (controller == null)
            {
                if (!addIfMissing)
                {
                    return;
                }

                controller = npc.gameObject.AddComponent<NpcQuestController>();
            }

            controller.Initialize(npc);
        }

        /// <summary>
        /// NPC Quest 표시를 초기화할 수 있도록 패키지 데이터와 실행 매니저가 준비되었는지 확인합니다.
        /// </summary>
        /// <returns>Quest 런타임 사용 준비가 끝났으면 true를 반환합니다.</returns>
        private bool IsPackageReady()
        {
            return _packageManager != null &&
                   _packageManager.QuestData != null &&
                   _packageManager.QuestManager != null;
        }
    }
}
