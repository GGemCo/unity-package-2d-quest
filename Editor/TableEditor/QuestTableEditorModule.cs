using System.Collections.Generic;
using GGemCo2DCoreEditor;
using GGemCo2DQuest;

namespace GGemCo2DQuestEditor
{
    /// <summary>
    /// Core 테이블 에디터에 Quest 테이블 정의를 제공합니다.
    /// </summary>
    internal sealed class QuestTableEditorModule : ITableEditorModule
    {
        /// <summary>
        /// 테이블 에디터 모듈 이름입니다.
        /// </summary>
        public string ModuleName => "Quest";

        /// <summary>
        /// 테이블 에디터 패키지 분류 이름입니다.
        /// </summary>
        public string PackageName => "Quest";

        /// <summary>
        /// Quest 패키지에서 편집할 테이블 정의를 생성합니다.
        /// </summary>
        /// <returns>Quest 테이블 정의 목록입니다.</returns>
        public IEnumerable<TableEditorTableDefinition> BuildDefinitions()
        {
            yield return TableEditorDefinitionFactory.Create(
                ModuleName,
                PackageName,
                ConfigAddressableTableQuest.Quest,
                ConfigAddressableTableQuest.TableQuest.Path,
                ConfigAddressableTableQuest.Quest,
                typeof(TableQuest),
                typeof(StruckTableQuest),
                TableEditorDefinitionFactory.CreateDefaultReloadAction(
                    ConfigAddressableTableQuest.TableQuest.Path),
                TableEditorRegistry.FindReferenceTable);
        }
    }
}
