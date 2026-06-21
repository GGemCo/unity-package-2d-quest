using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace GGemCo2DQuest
{
    /// <summary>
    /// Quest JSON TextAsset을 Addressables에서 로드하고 순수 C# 퀘스트 정의로 변환합니다.
    /// </summary>
    public sealed class QuestJsonAddressableLoader
    {
        /// <summary>
        /// 지정한 Addressables 키의 Quest JSON을 로드하고 역직렬화합니다.
        /// TextAsset 핸들은 파싱 성공 여부와 관계없이 메서드 종료 전에 해제합니다.
        /// </summary>
        /// <param name="questUid">오류 로그와 검증에 사용할 퀘스트 UID입니다.</param>
        /// <param name="key">Quest JSON Addressables 키입니다.</param>
        /// <returns>역직렬화된 퀘스트 정의입니다. 로드 또는 파싱에 실패하면 null을 반환합니다.</returns>
        public async Task<Quest> LoadAsync(int questUid, string key)
        {
            if (questUid <= 0 || string.IsNullOrWhiteSpace(key))
            {
                return null;
            }

            AsyncOperationHandle<TextAsset> handle = default;
            try
            {
                handle = Addressables.LoadAssetAsync<TextAsset>(key);
                await handle.Task;

                if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
                {
                    GGemCo2DCore.GcLogger.LogError(
                        $"Quest JSON Addressables 로드에 실패했습니다. uid: {questUid}, key: {key}");
                    return null;
                }

                string json = handle.Result.text;
                if (string.IsNullOrWhiteSpace(json))
                {
                    GGemCo2DCore.GcLogger.LogError(
                        $"Quest JSON 내용이 비어 있습니다. uid: {questUid}, key: {key}");
                    return null;
                }

                return JsonConvert.DeserializeObject<Quest>(json);
            }
            catch (Exception exception)
            {
                GGemCo2DCore.GcLogger.LogError(
                    $"Quest JSON 파싱 중 오류가 발생했습니다. uid: {questUid}, key: {key}, error: {exception.Message}");
                return null;
            }
            finally
            {
                // 파싱 이후에는 순수 C# 정의만 사용하므로 TextAsset 참조 카운트를 즉시 반환합니다.
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }
        }
    }
}
