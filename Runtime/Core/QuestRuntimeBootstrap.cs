using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using GGemCo2DCore;
using UnityEngine;
using UnityEngine.SceneManagement;
using SceneManager = UnityEngine.SceneManagement.SceneManager;

namespace GGemCo2DQuest
{
    /// <summary>
    /// 로딩 단계와 게임 씬 생명주기에 Quest 패키지를 연결합니다.
    /// </summary>
    public sealed class QuestRuntimeBootstrap : MonoBehaviour
    {
        private Coroutine _initializeCoroutine;

        /// <summary>
        /// 씬 로드 전에 Quest 부트스트랩 오브젝트를 생성합니다.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if (CompatObjectFind.FindFirst<QuestRuntimeBootstrap>() != null)
            {
                return;
            }

            GameObject bootstrapObject = new GameObject(nameof(QuestRuntimeBootstrap));
            DontDestroyOnLoad(bootstrapObject);
            bootstrapObject.AddComponent<QuestRuntimeBootstrap>();
        }

        /// <summary>
        /// 로딩 시작 및 씬 로드 이벤트를 구독합니다.
        /// </summary>
        private void OnEnable()
        {
            GameLoaderManager.BeforeLoadStartInLoadingScene += HandleBeforeLoadStart;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        /// <summary>
        /// 등록한 이벤트를 해제합니다.
        /// </summary>
        private void OnDisable()
        {
            GameLoaderManager.BeforeLoadStartInLoadingScene -= HandleBeforeLoadStart;
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        /// <summary>
        /// Quest 테이블 로딩 단계를 Core 로더에 등록합니다.
        /// </summary>
        /// <param name="sender">게임 로더 매니저입니다.</param>
        /// <param name="eventArgs">로딩 시작 이벤트 정보입니다.</param>
        private void HandleBeforeLoadStart(
            GameLoaderManager sender,
            GameLoaderManager.EventArgsBeforeLoadStart eventArgs)
        {
            TableLoaderManagerQuest tableLoader =
                CompatObjectFind.FindFirst<TableLoaderManagerQuest>();
            if (tableLoader == null)
            {
                tableLoader = new GameObject(nameof(TableLoaderManagerQuest))
                    .AddComponent<TableLoaderManagerQuest>();
            }

            var fallbackTables = new List<AddressableAssetInfo>
            {
                ConfigAddressableTableQuest.TableQuest,
            };

            sender.Register(new TablePackLoadStep(
                id: "quest.table",
                order: 248,
                localizedKey: LocalizationConstants.Keys.Loading.TextTypeTables(),
                tableLoader: tableLoader,
                tablePack: null,
                fallbackTables: fallbackTables));
        }

        /// <summary>
        /// 게임 씬이 로드되면 Core 준비 완료 후 Quest 런타임을 초기화합니다.
        /// </summary>
        /// <param name="scene">로드된 씬입니다.</param>
        /// <param name="mode">씬 로드 모드입니다.</param>
        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (_initializeCoroutine != null)
            {
                StopCoroutine(_initializeCoroutine);
            }

            _initializeCoroutine = StartCoroutine(InitializeWhenReady());
        }

        /// <summary>
        /// Core 게임 씬과 Quest 테이블 로더가 준비될 때까지 기다린 후 패키지 매니저를 초기화합니다.
        /// </summary>
        private IEnumerator InitializeWhenReady()
        {
            while (SceneGame.Instance == null ||
                   SceneGame.Instance.saveDataManager == null)
            {
                yield return null;
            }

            TableLoaderManagerQuest tableLoader = TableLoaderManagerQuest.Instance;
            if (tableLoader == null)
            {
                tableLoader = new GameObject(nameof(TableLoaderManagerQuest))
                    .AddComponent<TableLoaderManagerQuest>();

                // Game 씬을 직접 실행한 Editor 시나리오에서도 Quest 테이블을 준비합니다.
                Task loadTask = tableLoader.LoadDataFile(ConfigAddressableTableQuest.TableQuest);
                while (!loadTask.IsCompleted)
                {
                    yield return null;
                }

                if (loadTask.IsFaulted)
                {
                    GcLogger.LogException(loadTask.Exception);
                }
            }

            QuestPackageManager packageManager =
                GetComponent<QuestPackageManager>();
            if (packageManager == null)
            {
                packageManager = gameObject.AddComponent<QuestPackageManager>();
            }

            packageManager.InitializeForScene(SceneGame.Instance);
            _initializeCoroutine = null;
        }
    }
}
