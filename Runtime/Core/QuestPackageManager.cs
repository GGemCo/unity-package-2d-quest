using System.Collections;
using GGemCo2DCore;
using UnityEngine;

namespace GGemCo2DQuest
{
    /// <summary>
    /// Quest 런타임 데이터, 실행 매니저, Core 확장 정책과 NPC 부트스트랩의 수명주기를 관리합니다.
    /// </summary>
    public sealed class QuestPackageManager : MonoBehaviour
    {
        /// <summary>
        /// 현재 게임 씬에서 사용하는 Quest 패키지 매니저입니다.
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
        private BootstrapQuestRuntime _bootstrapRuntime;
        private QuestInteractionChoiceContributor _interactionContributor;
        private QuestMonsterRespawnSuppressionPolicy _respawnPolicy;
        private Coroutine _initializeCoroutine;
        private bool _isInitialized;

        /// <summary>
        /// Quest 패키지 싱글톤을 등록하고 NPC 연결 부트스트랩 컴포넌트를 보장합니다.
        /// Core 테이블 로더가 없는 직접 실행 씬에서는 SceneGame의 표준 복귀 흐름에 맡기고 초기화하지 않습니다.
        /// </summary>
        private void Awake()
        {
            if (TableLoaderManager.Instance == null)
            {
                return;
            }

            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            _bootstrapRuntime = GetComponent<BootstrapQuestRuntime>();
            if (_bootstrapRuntime == null)
            {
                _bootstrapRuntime = gameObject.AddComponent<BootstrapQuestRuntime>();
            }
        }

        /// <summary>
        /// 모든 Core 게임 씬 의존성이 준비된 뒤 Quest 런타임 초기화를 시작합니다.
        /// </summary>
        private void Start()
        {
            if (Instance != this || _initializeCoroutine != null)
            {
                return;
            }

            _initializeCoroutine = StartCoroutine(InitializeWhenReady());
        }

        /// <summary>
        /// SceneGame과 저장 매니저 준비를 기다린 뒤 Quest 패키지를 현재 게임 씬에 연결합니다.
        /// Quest 테이블 자체는 Loading 씬의 <see cref="SceneLoadingQuest"/>에서 미리 로드되어야 합니다.
        /// </summary>
        private IEnumerator InitializeWhenReady()
        {
            while (SceneGame.Instance == null || SceneGame.Instance.saveDataManager == null)
            {
                yield return null;
            }

            if (TableLoaderManagerQuest.Instance == null)
            {
                GcLogger.LogError(
                    "Quest 테이블 로더가 준비되지 않았습니다. Loading 씬의 SceneLoadingQuest 설정을 확인해주세요.");
                _initializeCoroutine = null;
                Destroy(gameObject);
                yield break;
            }

            InitializeForScene(SceneGame.Instance);
            _initializeCoroutine = null;
        }

        /// <summary>
        /// 지정한 Core 게임 씬에 Quest 저장, 실행 매니저와 확장 정책을 연결합니다.
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

            _bootstrapRuntime?.Activate(this);
            sceneGame.OnSceneGameDestroyed += HandleSceneGameDestroyed;
            _isInitialized = true;
        }

        /// <summary>
        /// Core 게임 씬 종료 이벤트를 받으면 Quest 패키지 오브젝트를 파괴합니다.
        /// 실제 이벤트와 저장 데이터 정리는 <see cref="OnDestroy"/>에서 일괄 수행합니다.
        /// </summary>
        private void HandleSceneGameDestroyed()
        {
            Destroy(gameObject);
        }

        /// <summary>
        /// 현재 게임 씬에 등록한 부트스트랩, 확장 정책, 실행 매니저와 저장 기여자를 정리합니다.
        /// </summary>
        private void DeinitializeScene()
        {
            if (_sceneGame != null)
            {
                _sceneGame.OnSceneGameDestroyed -= HandleSceneGameDestroyed;
            }

            _bootstrapRuntime?.Deactivate();
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
        /// 패키지 매니저가 제거될 때 초기화 코루틴과 모든 Quest 런타임 연결을 정리합니다.
        /// </summary>
        private void OnDestroy()
        {
            if (_initializeCoroutine != null)
            {
                StopCoroutine(_initializeCoroutine);
                _initializeCoroutine = null;
            }

            DeinitializeScene();
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
