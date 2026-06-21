using UnityEditor;
using UnityEngine;

namespace GGemCo2DQuestEditor
{
    /// <summary>
    /// Quest JSON 에셋의 Addressables 등록을 실행하는 전용 EditorWindow입니다.
    /// </summary>
    public sealed class QuestAddressableEditorWindow : EditorWindow
    {
        private SettingQuest _settingQuest;

        /// <summary>
        /// Quest Addressables 설정 창을 엽니다.
        /// </summary>
        [MenuItem(ConfigEditorQuest.NameToolAddressables)]
        public static void ShowWindow()
        {
            GetWindow<QuestAddressableEditorWindow>("Quest Addressables");
        }

        /// <summary>
        /// 창이 활성화될 때 설정 모듈을 생성합니다.
        /// </summary>
        private void OnEnable()
        {
            _settingQuest = new SettingQuest();
        }

        /// <summary>
        /// Quest Addressables 설정 UI를 그립니다.
        /// </summary>
        private void OnGUI()
        {
            _settingQuest?.OnGUI(position.width - 12f, 40f);
        }
    }
}
