using GGemCo2DCore;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DQuest
{
    /// <summary>
    /// 진행 중인 퀘스트 목표를 HUD 요소로 관리합니다.
    /// </summary>
    public class UIWindowHudQuest : UIWindow
    {
        [Header(UIWindowConstants.TitleHeaderIndividual)]
        [Tooltip("퀘스트 UI Element 프리팹")]
        public GameObject prefabElement;
        [Tooltip("퀘스트 UI Element가 들어갈 오브젝트")]
        public GameObject containerElement;
        
        private readonly Dictionary<int, UIElementHudQuest> _elements =
            new Dictionary<int, UIElementHudQuest>();
        
        protected override void Awake()
        {
            // Core 윈도우 초기화 전에 Quest 패키지가 소유한 UID를 지정합니다.
            uid = QuestWindowConstants.HudQuest;
            base.Awake();
        }
        /// <summary>
        /// 퀘스트 Element 추가하기
        /// </summary>
        /// <param name="questUid">표시할 퀘스트 UID입니다.</param>
        /// <param name="questStepIndex">표시할 단계 인덱스입니다.</param>
        public void AddQuestElement(int questUid, int questStepIndex)
        {
            UIElementHudQuest elementHudQuest = _elements.GetValueOrDefault(questUid);
            if (elementHudQuest == null)
            {
                GameObject element = Instantiate(prefabElement, containerElement.transform);
                if (element == null) return;
                elementHudQuest = element.GetComponent<UIElementHudQuest>();
                if (elementHudQuest == null) return;
                _elements.TryAdd(questUid, elementHudQuest);
                elementHudQuest.InitializeInfo(questUid, questStepIndex);
            }
            else
            {
                elementHudQuest.InitializeInfo(questUid, questStepIndex);
                elementHudQuest.UpdateInfo();
            }
        }
        /// <summary>
        /// 지정한 퀘스트의 HUD 요소를 반환합니다.
        /// </summary>
        /// <param name="questUid">조회할 퀘스트 UID입니다.</param>
        /// <returns>등록된 HUD 요소이며, 없으면 null입니다.</returns>
        public UIElementHudQuest GetQuestElement(int questUid)
        {
            return _elements.GetValueOrDefault(questUid);
        }
        /// <summary>
        /// 퀘스트 Element 지우기
        /// </summary>
        /// <param name="questUid">제거할 퀘스트 UID입니다.</param>
        public void RemoveQuestElement(int questUid)
        {
            UIElementHudQuest element = _elements.GetValueOrDefault(questUid);
            if (element == null) return;
            _elements.Remove(questUid);
            Destroy(element.gameObject);
        }

        /// <summary>
        /// 지정한 퀘스트의 HUD 진행 수량을 갱신합니다.
        /// </summary>
        /// <param name="questUid">갱신할 퀘스트 UID입니다.</param>
        /// <param name="count">현재 진행 수량입니다.</param>
        public void SetCount(int questUid, int count)
        {
            UIElementHudQuest element = _elements.GetValueOrDefault(questUid);
            if (element == null) return;
            element.SetCount(count);
        }
    }
}
