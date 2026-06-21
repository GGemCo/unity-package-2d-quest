using GGemCo2DCore;
using UnityEngine;

namespace GGemCo2DQuest
{
    /// <summary>
    /// Quest 런타임 데이터, 매니저, Core 연동 객체의 수명주기를 관리합니다.
    /// </summary>
    public sealed class QuestPackageManager : MonoBehaviour
    {
        /// <summary>
        /// 현재 Quest 패키지 매니저입니다.
        /// </summary>
        public static QuestPackageManager Instance { get; private set; }

        /// <summary>
        /// 현재 퀘스트 진행 저장 데이터입니다.
        /// </summary>
        public QuestData QuestData { get; private set; }

        /// <summary>
        /// 현재 퀘스트 실행 매니저입니다.
        /// </summary>
        public QuestManager QuestManager { get; private set; }

        private SceneGame _sceneGame;
        private QuestInteractionChoiceContributor _interactionContributor;
        private QuestMonsterRespawnSuppressionPolicy _respawnPolicy;
        private bool _isInitialized;

        /// <summary>
        /// Quest 패키지 매니저의 싱글톤 참조를 설정합니다.
        /// </summary>
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        /// <summary>
        /// 지정한 Core 게임 씬에 Quest 런타임을 연결합니다.
        /// </summary>
        /// <param name="sceneGame">연결할 Core 게임 씬입니다.</param>
        public void InitializeForScene(SceneGame sceneGame)
        {
            if (sceneGame == null || (_isInitialized && _sceneGame == sceneGame))
            {
                return;
            }

            DeinitializeScene();
            _sceneGame = sceneGame;

            QuestData = new QuestData();
            QuestData.Register();

            QuestManager = new QuestManager();
            QuestManager.Initialize(sceneGame, QuestData);
            QuestManager.OnStartBySceneGame();

            _interactionContributor = new QuestInteractionChoiceContributor(this);
            InteractionChoiceContributorRegistry.Register(_interactionContributor);

            _respawnPolicy = new QuestMonsterRespawnSuppressionPolicy(this);
            MonsterRespawnSuppressionPolicyRegistry.Register(_respawnPolicy);

            CharacterManager.OnCharacterActivated += HandleCharacterActivated;
            CharacterPresentationRefreshRegistry.Register(RefreshCharacterPresentation);
            sceneGame.OnSceneGameDestroyed += HandleSceneGameDestroyed;
            _isInitialized = true;
        }

        /// <summary>
        /// 캐릭터가 활성화되면 NPC에 퀘스트 표시 컴포넌트를 연결합니다.
        /// </summary>
        /// <param name="character">활성화된 캐릭터입니다.</param>
        private static void HandleCharacterActivated(CharacterBase character)
        {
            if (character is not Npc npc)
            {
                return;
            }

            NpcQuestController controller = npc.GetComponent<NpcQuestController>();
            if (controller == null)
            {
                controller = npc.gameObject.AddComponent<NpcQuestController>();
            }

            controller.Initialize(npc);
        }

        /// <summary>
        /// Core의 표시 갱신 요청을 NPC 퀘스트 표시 컴포넌트에 전달합니다.
        /// </summary>
        /// <param name="character">표시를 갱신할 캐릭터입니다.</param>
        private static void RefreshCharacterPresentation(CharacterBase character)
        {
            if (character is not Npc npc)
            {
                return;
            }

            npc.GetComponent<NpcQuestController>()?.LoadQuest();
        }

        /// <summary>
        /// Core 게임 씬이 종료되면 현재 Quest 씬 연결을 해제합니다.
        /// </summary>
        private void HandleSceneGameDestroyed()
        {
            DeinitializeScene();
        }

        /// <summary>
        /// 현재 게임 씬에 등록한 이벤트, 정책, 저장 기여자를 정리합니다.
        /// </summary>
        private void DeinitializeScene()
        {
            if (_sceneGame != null)
            {
                _sceneGame.OnSceneGameDestroyed -= HandleSceneGameDestroyed;
            }

            CharacterManager.OnCharacterActivated -= HandleCharacterActivated;
            CharacterPresentationRefreshRegistry.Unregister(RefreshCharacterPresentation);
            InteractionChoiceContributorRegistry.Unregister(_interactionContributor);
            MonsterRespawnSuppressionPolicyRegistry.Unregister(_respawnPolicy);

            QuestManager?.OnDestroy();
            QuestData?.Unregister();

            QuestManager = null;
            QuestData = null;
            _interactionContributor = null;
            _respawnPolicy = null;
            _sceneGame = null;
            _isInitialized = false;
        }

        /// <summary>
        /// 패키지 매니저가 제거될 때 모든 런타임 연결을 정리합니다.
        /// </summary>
        private void OnDestroy()
        {
            DeinitializeScene();
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
