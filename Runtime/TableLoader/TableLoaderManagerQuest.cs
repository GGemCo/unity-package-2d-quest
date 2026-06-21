using GGemCo2DCore;
using UnityEngine;

namespace GGemCo2DQuest
{
    /// <summary>
    /// Quest 패키지 전용 테이블을 등록하고 조회합니다.
    /// </summary>
    public sealed class TableLoaderManagerQuest : TableLoaderBase
    {
        /// <summary>
        /// 현재 Quest 테이블 로더 인스턴스입니다.
        /// </summary>
        public static TableLoaderManagerQuest Instance { get; private set; }

        /// <summary>
        /// 퀘스트 기본 테이블입니다.
        /// </summary>
        public TableQuest TableQuest { get; } = new TableQuest();

        /// <summary>
        /// Quest 테이블 레지스트리를 초기화합니다.
        /// </summary>
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            registry = new TableRegistry();
            registry.Register(TableQuest);
        }

        /// <summary>
        /// 인스턴스가 제거될 때 정적 참조를 해제합니다.
        /// </summary>
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
