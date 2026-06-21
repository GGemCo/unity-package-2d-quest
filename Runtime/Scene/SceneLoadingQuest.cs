using GGemCo2DCore;
using UnityEngine;

namespace GGemCo2DQuest
{
    /// <summary>
    /// Quest 패키지의 테이블, Localization, 설정 리소스 로딩 단계를
    /// Core 로딩 씬에 등록하는 씬 컴포넌트입니다.
    /// </summary>
    /// <remarks>
    /// - Addressables 설정이 준비되지 않은 경우 PreIntro 씬으로 되돌립니다.
    /// - 로딩 씬에서 GameLoaderManager의 훅 이벤트를 통해 로딩 스텝을 등록합니다.
    /// - 필요한 매니저/로더가 씬에 없으면 런타임에 생성하여 사용합니다.
    /// </remarks>
    public class SceneLoadingQuest : DefaultScene
    {
        /// <summary>
        /// Addressables 로더 설정이 존재하지 않으면 PreIntro 씬으로 강제 이동합니다.
        /// </summary>
        private void Awake()
        {
            if (!AddressableLoaderSettings.Instance)
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(ConfigDefine.SceneNamePreIntro);
                return;
            }
        }

        /// <summary>
        /// 오브젝트 활성화 시 로딩 시작 직전 이벤트 훅을 구독합니다.
        /// </summary>
        private void OnEnable()
        {
            // PreIntro 씬/Loading 씬에서 로딩 시작 직전 훅
            GameLoaderManager.BeforeLoadStartInLoadingScene += OnBeforeLoadStartInLoadingScene;
        }

        /// <summary>
        /// 오브젝트 비활성화 시 이벤트 구독을 해제합니다.
        /// </summary>
        private void OnDisable()
        {
            GameLoaderManager.BeforeLoadStartInLoadingScene -= OnBeforeLoadStartInLoadingScene;
        }

        /// <summary>
        /// 로딩 씬에서 실제 로딩이 시작되기 직전에 호출되며,
        /// Quest 관련 테이블, Localization, 설정 리소스 로딩 스텝을 등록합니다.
        /// </summary>
        /// <param name="sender">로딩 스텝을 등록할 <see cref="GameLoaderManager"/>입니다.</param>
        /// <param name="e">로딩 시작 직전 이벤트 인자입니다.</param>
        private void OnBeforeLoadStartInLoadingScene(
            GameLoaderManager sender,
            GameLoaderManager.EventArgsBeforeLoadStart e)
        {
            // 설정 스크립터블 오브젝트
            /*
            var addrSettings = CompatObjectFind.FindFirst<AddressableLoaderSettingsQuest>() ??
                               new GameObject("AddressableLoaderSettingsQuest")
                                   .AddComponent<AddressableLoaderSettingsQuest>();
            var step = new AddressableTaskStep(
                id: "quest.settings",
                order: 251,
                localizedKey: LocalizationConstants.Keys.Loading.TextTypeSettings(),
                startTask: () => addrSettings.LoadAllSettingsAsync(),
                getProgress: () => addrSettings.GetLoadProgress()
            );
            sender.Register(step);
            */

            // 테이블 로더 준비 및 테이블 로딩 스텝 등록
            var tableLoader = CompatObjectFind.FindFirst<TableLoaderManagerQuest>() ??
                              new GameObject("TableLoaderManagerQuest").AddComponent<TableLoaderManagerQuest>();

            var targetTables = ConfigAddressableTableQuest.All;
            var stepTable = new TablePackLoadStep(
                id: "core.table.quest",
                order: 246,
                localizedKey: LocalizationConstants.Keys.Loading.TextTypeTables(),
                tableLoader: tableLoader,
                tablePack: ConfigAddressableTablePack.Quest,
                fallbackTables: targetTables
            );
            sender.Register(stepTable);

            // 로컬라이징 매니저 준비 및 로컬라이징 로딩 스텝 등록
            /*
            var loc =
                CompatObjectFind.FindFirst<LocalizationManagerQuest>() ??
                new GameObject("LocalizationManagerQuest").AddComponent<LocalizationManagerQuest>();

            var stepLocalization = new LocalizationLoadStep(
                "core.localization.quest",
                order: 221,
                localizedKey: LocalizationConstants.Keys.Loading.TextTypeLocalization(),
                localizationManager: loc,
                localeCode: PlayerPrefsManager.LoadLocalizationLocaleCode()
            );
            sender.Register(stepLocalization);
            */
        }
    }
}
