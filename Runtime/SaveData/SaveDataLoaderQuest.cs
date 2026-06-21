using GGemCo2DCore;
using Newtonsoft.Json;
using UnityEngine;

namespace GGemCo2DQuest
{
    /// <summary>
    /// 선택된 저장 슬롯의 Quest 전용 저장 파일을 로드하고 역직렬화합니다.
    /// </summary>
    public sealed class SaveDataLoaderQuest : SaveDataLoaderBase
    {
        /// <summary>
        /// 현재 Quest 저장 데이터 로더입니다.
        /// </summary>
        public static SaveDataLoaderQuest Instance { get; private set; }

        private SaveDataContainerQuest _saveDataContainer;

        /// <summary>
        /// Quest 저장 데이터 로더 싱글톤을 등록합니다.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                return;
            }

            if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Quest 전용 저장 파일 경로를 반환합니다.
        /// </summary>
        /// <param name="slotIndex">로드할 저장 슬롯 번호입니다.</param>
        /// <returns>Quest 저장 파일 경로입니다.</returns>
        protected override string GetSaveFilePath(int slotIndex)
        {
            return saveFileController.GetSaveFilePath(
                slotIndex,
                SaveDataConstantsQuest.SaveDataFileName);
        }

        /// <summary>
        /// Core 백업 파일과 충돌하지 않는 Quest 전용 백업 파일 경로를 반환합니다.
        /// </summary>
        /// <param name="slotIndex">로드할 저장 슬롯 번호입니다.</param>
        /// <returns>Quest 백업 저장 파일 경로입니다.</returns>
        protected override string GetBackupFilePath(int slotIndex)
        {
            return saveFileController.GetSaveFilePath(
                slotIndex,
                SaveDataConstantsQuest.BackupFileNameWithoutExtension);
        }

        /// <summary>
        /// Quest 저장 파일의 논리 저장 식별자를 반환합니다.
        /// </summary>
        /// <param name="slotIndex">로드할 저장 슬롯 번호입니다.</param>
        /// <returns>Quest 저장 파일 복호화에 사용할 논리 식별자입니다.</returns>
        protected override SaveDataIdentity GetSaveDataIdentity(int slotIndex)
        {
            return SaveDataConstantsQuest.CreateIdentity(slotIndex);
        }

        /// <summary>
        /// 로드한 JSON을 Quest 저장 데이터 컨테이너로 역직렬화합니다.
        /// </summary>
        /// <param name="json">복호화된 Quest 저장 JSON입니다.</param>
        protected override void OnLoaded(string json)
        {
            _saveDataContainer = JsonConvert.DeserializeObject<SaveDataContainerQuest>(json);
        }

        /// <summary>
        /// 저장 파일이 없거나 복구에 실패하면 신규 Quest 데이터로 시작하도록 컨테이너를 초기화합니다.
        /// </summary>
        /// <param name="result">Quest 저장 파일 로드 결과입니다.</param>
        protected override void OnLoadFailed(SaveDataLoadResult result)
        {
            _saveDataContainer = null;
        }

        /// <summary>
        /// 로딩 씬에서 준비한 Quest 저장 데이터 컨테이너를 반환합니다.
        /// </summary>
        /// <returns>로드된 Quest 저장 데이터입니다. 신규 게임이면 null을 반환합니다.</returns>
        public SaveDataContainerQuest GetSaveDataContainer()
        {
            return _saveDataContainer;
        }

        /// <summary>
        /// 로더가 제거될 때 보관 중인 컨테이너와 싱글톤 참조를 정리합니다.
        /// </summary>
        private void OnDestroy()
        {
            _saveDataContainer = null;
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
