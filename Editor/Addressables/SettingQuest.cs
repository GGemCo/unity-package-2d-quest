using GGemCo2DQuest;
using GGemCo2DCoreEditor;
using System.Collections.Generic;
using System.IO;
using GGemCo2DCore;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace GGemCo2DQuestEditor
{
    /// <summary>
    /// Quest 테이블과 JSON 에셋을 Addressables 그룹에 등록합니다.
    /// </summary>
    public class SettingQuest : DefaultAddressable
    {
        private const string Title = "퀘스트 추가하기";
        /// <summary>
        /// Quest Addressables 설정 모듈을 생성합니다.
        /// </summary>
        public SettingQuest()
        {
            targetGroupName = ConfigAddressableQuest.GroupName;
        }

        /// <summary>
        /// Quest Addressables 설정 버튼을 그립니다.
        /// </summary>
        /// <param name="buttonWidth">버튼 폭입니다.</param>
        /// <param name="buttonHeight">버튼 높이입니다.</param>
        public void OnGUI(float buttonWidth, float buttonHeight)
        {
            if (!File.Exists(ConfigAddressableQuest.TableQuest.Path))
            {
                EditorGUILayout.HelpBox($"{ConfigAddressableQuest.TableName} 테이블이 없습니다.", MessageType.Info);
            }
            else
            {
                if (GUILayout.Button(Title, GUILayout.Width(buttonWidth), GUILayout.Height(buttonHeight)))
                {
                    try
                    {
                        Setup();
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogException(e);
                        EditorUtility.DisplayDialog(Title, "퀘스트 Addressable 설정 중 오류가 발생했습니다.\n자세한 내용은 콘솔 로그를 확인해주세요.", "OK");
                    }
                }
            }
        }
        /// <summary>
        /// Quest 테이블과 JSON 에셋의 Addressables 설정을 갱신합니다.
        /// </summary>
        /// <param name="ctx">프로젝트 자동 설정 컨텍스트이며, 수동 실행 시 null입니다.</param>
        public void Setup(EditorSetupContext ctx = null)
        {
            if (ctx == null)
            {
                bool result = EditorUtility.DisplayDialog(TextDisplayDialogTitle, TextDisplayDialogMessage, "네", "아니요");
                if (!result) return;
            }
            
            Dictionary<int, StruckTableQuest> dictionary =
                TableLoaderManagerQuestEditor.LoadQuestTable().GetDatas();
            
            // AddressableSettings 가져오기 (없으면 생성)
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (!settings)
            {
                HelperLog.Warn("Addressable 설정을 찾을 수 없습니다. 새로 생성합니다.", ctx);
                settings = CreateAddressableSettings();
            }

            // GGemCo_Tables 그룹 가져오기 또는 생성
            AddressableAssetGroup group = GetOrCreateGroup(settings, targetGroupName);
            if (!group)
            {
                HelperLog.Error($"'{targetGroupName}' 그룹을 설정할 수 없습니다.", ctx);
                return;
            }
            
            ClearGroupEntries(settings, group);

            // Quest 전용 테이블도 같은 그룹에 등록하여 Core 테이블 설정과 독립적으로 로드할 수 있게 합니다.
            Add(
                settings,
                group,
                ConfigAddressableQuest.TableQuest.Key,
                ConfigAddressableQuest.TableQuest.Path,
                ConfigAddressableLabel.Table);
            
            // foreach 문을 사용하여 딕셔너리 내용을 출력
            foreach (KeyValuePair<int, StruckTableQuest> outerPair in dictionary)
            {
                var info = outerPair.Value;
                if (info.Uid <= 0) continue;
            
                string key = ConfigAddressableQuest.GetQuestKey(info.Uid);
                string assetPath = $"{ConfigAddressableQuest.QuestJsonPath}/{info.FileName}.json";
                string label = ConfigAddressableQuest.Label;
            
                Add(settings, group, key, assetPath, label);
            }
            
            // 설정 저장
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true);
            if (ctx != null)
            {
                HelperLog.Info("[Addressable] 퀘스트 설정 완료", ctx);
            }
            else
            {
                AssetDatabase.SaveAssets();
                EditorUtility.DisplayDialog(Title, "[Addressable] 퀘스트 설정 완료", "OK");
            }
        }
    }
}
