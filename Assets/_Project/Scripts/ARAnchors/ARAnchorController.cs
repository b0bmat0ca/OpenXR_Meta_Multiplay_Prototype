using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using Unity.XR.CoreUtils.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.OpenXR.NativeTypes;

namespace b0bmat0ca.OpenXr.ARAnchors
{
    using UnityEngine.XR.OpenXR.Features.Meta;
    
    /// <summary>
    /// UnityEngine.XR.ARFoundation.Tests.ARAnchorManagerSamplesクラスを元にして、利用可能な形で実装したものです
    /// </summary>
    public class ARAnchorController : MonoBehaviour
    {
        #region DescriptorChecks
        
        /// <summary>
        /// トラッキング可能オブジェクトへのアンカーアタッチメント機能をサポートしているかチェック
        /// </summary>
        public bool SupportsTrackableAttachments(ARAnchorManager manager)
        {
            return manager.descriptor.supportsTrackableAttachments;
        }

        /// <summary>
        /// アンカーの保存機能をサポートしているかチェック
        /// </summary>
        public bool SupportsSynchronousAdd(ARAnchorManager manager)
        {
            return manager.descriptor.supportsSynchronousAdd;
        }

        /// <summary>
        /// アンカーの保存機能をサポートしているかチェック
        /// </summary>
        public bool SupportsSaveAnchor(ARAnchorManager manager)
        {
            return manager.descriptor.supportsSaveAnchor;
        }

        /// <summary>
        /// アンカーの読み込み機能をサポートしているかチェック
        /// </summary>
        public bool SupportsLoadAnchor(ARAnchorManager manager)
        {
            return manager.descriptor.supportsLoadAnchor;
        }

        /// <summary>
        /// アンカーの削除機能をサポートしているかチェック
        /// </summary>
        public bool SupportsEraseAnchor(ARAnchorManager manager)
        {
            return manager.descriptor.supportsEraseAnchor;
        }

        /// <summary>
        /// 保存されたアンカーID取得機能をサポートしているかチェック
        /// </summary>
        public bool SupportsGetSavedAnchorIds(ARAnchorManager manager)
        {
            return manager.descriptor.supportsGetSavedAnchorIds;
        }

        /// <summary>
        /// 非同期処理のキャンセレーション機能をサポートしているかチェック
        /// </summary>
        public bool SupportsAsyncCancellation(ARAnchorManager manager)
        {
            return manager.descriptor.supportsAsyncCancellation;
        }

        /// <summary>
        /// 全ての機能サポート状況を確認
        /// </summary>
        public void CheckForOptionalFeatureSupport(ARAnchorManager manager)
        {
            // Use manager.descriptor to determine which optional features
            // are supported on the device.
            
            Debug.Log($"TrackableAttachments: {SupportsTrackableAttachments(manager)}");
            Debug.Log($"SynchronousAdd: {SupportsSynchronousAdd(manager)}");
            Debug.Log($"SaveAnchor: {SupportsSaveAnchor(manager)}");
            Debug.Log($"LoadAnchor: {SupportsLoadAnchor(manager)}");
            Debug.Log($"EraseAnchor: {SupportsEraseAnchor(manager)}");
            Debug.Log($"GetSavedAnchorIds: {SupportsGetSavedAnchorIds(manager)}");
            Debug.Log($"AsyncCancellation: {SupportsAsyncCancellation(manager)}");
        }
        
        #endregion

        #region TryAddAnchorAsync
        
        /// <summary>
        /// 指定した位置と回転でアンカーを非同期で作成します
        /// </summary>
        public async UniTask<ARAnchor> CreateAnchorAsync(ARAnchorManager manager, Pose pose)
        {
            Result<ARAnchor> result = await manager.TryAddAnchorAsync(pose);
            if (result.status.IsSuccess())
            {
                return result.value;
            }
            return null;
        }
        #endregion

        #region AttachAnchor
        
        /// <summary>
        /// ARPlaneにアンカーをアタッチします
        /// </summary>
        public ARAnchor AttachAnchor(ARAnchorManager manager, ARPlane plane, Pose pose)
        {
            if (SupportsTrackableAttachments(manager))
            {
                ARAnchor anchor = manager.AttachAnchor(plane, pose);
                return anchor;
            }
            return null;
        }
        #endregion

        #region TrySaveAnchorAsync
        
        /// <summary>
        /// アンカーを永続化して保存します
        /// </summary>
        public async UniTask<SerializableGuid?> SaveAnchorAsync(ARAnchorManager manager, ARAnchor anchor)
        {
            if (!SupportsSaveAnchor(manager))
            {
                Debug.LogWarning("SaveAnchor is not supported on this device");
                return null;
            }
            
            Result<SerializableGuid> result = await manager.TrySaveAnchorAsync(anchor);
            if (result.status.IsError())
            {
                // handle error
                return null;
            }

            // Save this value, then use it as an input parameter
            // to TryLoadAnchorAsync or TryEraseAnchorAsync
            return result.value;
        }
        #endregion

        #region TrySaveAnchorsAsync
        
        /// <summary>
        /// 複数のアンカーを一括で永続化して保存します（Meta OpenXRでは全体成功か全体失敗）
        /// </summary>
        public async UniTask<List<ARSaveOrLoadAnchorResult>> SaveAnchorsAsync(ARAnchorManager manager, IEnumerable<ARAnchor> anchors)
        {
            if (!SupportsSaveAnchor(manager))
            {
                Debug.LogWarning("SaveAnchor is not supported on this device");
                return new List<ARSaveOrLoadAnchorResult>();
            }
            
            List<ARSaveOrLoadAnchorResult> results = new List<ARSaveOrLoadAnchorResult>();
            await manager.TrySaveAnchorsAsync(anchors, results);

            // Check results - each result indicates success or failure for each anchor
            return results;
        }
        #endregion

        #region TryLoadAnchorAsync
        
        /// <summary>
        /// 保存されたアンカーをGUIDから読み込みます
        /// </summary>
        public async UniTask<ARAnchor> LoadAnchorAsync(ARAnchorManager manager, SerializableGuid guid)
        {
            if (!SupportsLoadAnchor(manager))
            {
                Debug.LogWarning("LoadAnchor is not supported on this device");
                return null;
            }
            
            Result<ARAnchor> result = await manager.TryLoadAnchorAsync(guid);
            if (result.status.IsError())
            {
                // handle error
                return null;
            }

            // You can use this anchor as soon as it's returned to you.
            return result.value;
        }
        #endregion

        #region TryLoadAnchorsAsync
        
        /// <summary>
        /// 複数の保存されたアンカーを一括でGUIDから読み込みます
        /// </summary>
        public async UniTask<List<ARSaveOrLoadAnchorResult>> LoadAnchorsAsync(ARAnchorManager manager, IEnumerable<SerializableGuid> savedAnchorGuids, Action<ReadOnlyListSpan<ARSaveOrLoadAnchorResult>> onIncrementalResults = null)
        {
            if (!SupportsLoadAnchor(manager))
            {
                Debug.LogWarning("LoadAnchor is not supported on this device");
                return new List<ARSaveOrLoadAnchorResult>();
            }
            
            List<ARSaveOrLoadAnchorResult> results = new List<ARSaveOrLoadAnchorResult>();
            await manager.TryLoadAnchorsAsync(
                savedAnchorGuids,
                results,
                onIncrementalResults ?? OnIncrementalResultsAvailable);

            // Check results - each result indicates success or failure for each anchor
            return results;
        }

        /// <summary>
        /// アンカーの段階的読み込み結果を処理します
        /// </summary>
        private void OnIncrementalResultsAvailable(ReadOnlyListSpan<ARSaveOrLoadAnchorResult> loadAnchorResults)
        {
            foreach (ARSaveOrLoadAnchorResult loadAnchorResult in loadAnchorResults)
            {
                // You can use these anchors immediately without waiting for the
                // entire batch to finish loading.
                // loadAnchorResult.resultStatus.IsSuccess() will always be true
                // for anchors passed to the incremental results callback.
                ARAnchor loadedAnchor = loadAnchorResult.anchor;
            }
        }
        #endregion

        #region TryEraseAnchorAsync
        
        /// <summary>
        /// 保存されたアンカーをGUIDで指定して削除します
        /// </summary>
        public async UniTask<bool> EraseAnchorAsync(ARAnchorManager manager, SerializableGuid guid)
        {
            if (!SupportsEraseAnchor(manager))
            {
                Debug.LogWarning("EraseAnchor is not supported on this device");
                return false;
            }
            
            XRResultStatus status = await manager.TryEraseAnchorAsync(guid);
            if (status.IsError())
            {
                // handle error
                return false;
            }

            // The anchor was successfully erased.
            return status.IsSuccess();
        }
        #endregion

        #region TryEraseAnchorsAsync
        
        /// <summary>
        /// 複数の保存されたアンカーを一括でGUIDから削除します（Meta OpenXRでは全体成功か全体失敗）
        /// </summary>
        public async UniTask<List<XREraseAnchorResult>> EraseAnchorsAsync(ARAnchorManager manager, IEnumerable<SerializableGuid> savedAnchorGuids)
        {
            if (!SupportsEraseAnchor(manager))
            {
                Debug.LogWarning("EraseAnchor is not supported on this device");
                return new List<XREraseAnchorResult>();
            }
            
            List<XREraseAnchorResult> eraseAnchorResults = new List<XREraseAnchorResult>();
            await manager.TryEraseAnchorsAsync(savedAnchorGuids, eraseAnchorResults);
            
            // Check results - each result indicates success or failure for each anchor
            return eraseAnchorResults;
        }
        #endregion

        #region TryGetSavedAnchorIdsAsync
        
        /// <summary>
        /// デバイスに保存されている全てのアンカーのGUID一覧を取得します
        /// </summary>
        public async UniTask<NativeArray<SerializableGuid>?> GetSavedAnchorIdsAsync(ARAnchorManager manager, Allocator allocator = Allocator.Temp)
        {
            if (!SupportsGetSavedAnchorIds(manager))
            {
                Debug.LogWarning("GetSavedAnchorIds is not supported on this device");
                return null;
            }
            
            // If you need to keep the saved anchor IDs longer than a frame, use
            // Allocator.Persistent instead, then remember to Dispose the array.
            Result<NativeArray<SerializableGuid>> result = await manager.TryGetSavedAnchorIdsAsync(allocator);

            if (result.status.IsError())
            {
                // handle error
                return null;
            }

            // Do something with the saved anchor IDs
            return result.value;
        }
        #endregion

        #region AsyncCancellation
        
        /// <summary>
        /// キャンセレーショントークン付きで保存されたアンカーGUID一覧を取得します
        /// </summary>
        public async UniTask<NativeArray<SerializableGuid>?> GetSavedAnchorIdsWithCancellationAsync(ARAnchorManager manager, CancellationToken cancellationToken, Allocator allocator = Allocator.Temp)
        {
            if (!SupportsGetSavedAnchorIds(manager) || !SupportsAsyncCancellation(manager))
            {
                Debug.LogWarning("GetSavedAnchorIds or AsyncCancellation is not supported on this device");
                return null;
            }
            
            // Create a CancellationTokenSource to serve our CancellationToken
            // Use one of the other methods in the persistent anchor API
            Result<NativeArray<SerializableGuid>> result = await manager.TryGetSavedAnchorIdsAsync(allocator, cancellationToken);

            if (result.status.IsError())
            {
                return null;
            }

            // Cancel the async operation before it completes if needed
            return result.value;
        }
        #endregion

        #region Meta OpenXR固有のヘルパーメソッド
        /// <summary>
        /// Meta OpenXRの詳細エラー情報を取得します
        /// </summary>
        public string GetDetailedErrorInfo(XRResultStatus status)
        {
            if (status.IsSuccess())
                return "";
            
            try
            {
                // Meta OpenXRのnativeStatusCodeをXrResultとして取得を試行
                XrResult xrResult = (XrResult)status.nativeStatusCode;
                return $"エラー: {status} (XrResult: {xrResult})";
            }
            catch (InvalidCastException)
            {
                // XrResult型が利用できない場合のフォールバック
                return $"エラー: {status} (nativeCode: {status.nativeStatusCode})";
            }
        }

        /// <summary>
        /// アンカーのtrackableIdとGUIDの関係を確認します（Meta OpenXRでは同じ値）
        /// </summary>
        public bool ValidateAnchorGuid(ARAnchor anchor, SerializableGuid savedGuid)
        {
            // Meta OpenXRではアンカーのGUIDはtrackableIdと同じ
            return anchor.trackableId.Equals(savedGuid);
        }

        /// <summary>
        /// バッチ保存が成功したかを確認します（Meta OpenXRでは全体成功か全体失敗）
        /// </summary>
        public bool IsBatchSaveSuccessful(List<ARSaveOrLoadAnchorResult> results)
        {
            // Meta OpenXRでは1つでも失敗したら全体が失敗する
            if (results == null || results.Count == 0) return false;
            
            foreach (ARSaveOrLoadAnchorResult result in results)
            {
                if (result.resultStatus.IsError())
                    return false;
            }
            return true;
        }

        /// <summary>
        /// バッチ削除が成功したかを確認します（Meta OpenXRでは全体成功か全体失敗）
        /// </summary>
        public bool IsBatchEraseSuccessful(List<XREraseAnchorResult> results)
        {
            // Meta OpenXRでは1つでも失敗したら全体が失敗する
            if (results == null || results.Count == 0) return false;
            
            foreach (XREraseAnchorResult result in results)
            {
                if (result.resultStatus.IsError())
                    return false;
            }
            return true;
        }
        #endregion
        
        #region SharedAnchors - Support Check
        /// <summary>
        /// 共有アンカー機能がサポートされているかチェック
        /// </summary>
        public bool SupportsSharedAnchors(ARAnchorManager manager)
        {
            try
            {
                MetaOpenXRAnchorSubsystem metaAnchorSubsystem = (MetaOpenXRAnchorSubsystem)manager.subsystem;
                
                if (metaAnchorSubsystem != null)
                {
                    return metaAnchorSubsystem.isSharedAnchorsSupported == Supported.Supported;
                }
                
                return false;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"共有アンカーサポート確認中にエラーが発生しました: {e.Message}");
                return false;
            }
        }
        
        #endregion
        
        #region SharedAnchors - Group ID Management
        /// <summary>
        /// 共有アンカー用のグループIDを設定します
        /// </summary>
        public bool SetSharedAnchorsGroupId(ARAnchorManager manager, SerializableGuid groupId)
        {
            if (!SupportsSharedAnchors(manager))
            {
                Debug.LogWarning("共有アンカー機能がサポートされていないため、グループIDを設定できません");
                return false;
            }

            try
            {
                MetaOpenXRAnchorSubsystem metaAnchorSubsystem = (MetaOpenXRAnchorSubsystem)manager.subsystem;
                
                if (metaAnchorSubsystem != null)
                {
                    metaAnchorSubsystem.sharedAnchorsGroupId = groupId;
                    Debug.Log($"共有アンカーのグループIDを設定しました: {groupId}");
                    return true;
                }
                
                Debug.LogWarning("MetaOpenXRAnchorSubsystemの取得に失敗しました");
                return false;
            }
            catch (Exception e)
            {
                Debug.LogError($"グループID設定中にエラーが発生しました: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 新しいグループIDを生成して設定します
        /// </summary>
        public SerializableGuid GenerateAndSetNewGroupId(ARAnchorManager manager)
        {
            SerializableGuid newGroupId = new SerializableGuid(Guid.NewGuid());
            
            if (SetSharedAnchorsGroupId(manager, newGroupId))
            {
                Debug.Log($"新しいグループIDを生成・設定しました: {newGroupId}");
                return newGroupId;
            }
            
            Debug.LogError("新しいグループIDの生成・設定に失敗しました");
            return new SerializableGuid();
        }

        /// <summary>
        /// 現在設定されているグループIDを取得します
        /// </summary>
        public SerializableGuid? GetCurrentGroupId(ARAnchorManager manager)
        {
            if (!SupportsSharedAnchors(manager))
            {
                Debug.LogWarning("共有アンカー機能がサポートされていません");
                return null;
            }

            try
            {
                MetaOpenXRAnchorSubsystem metaAnchorSubsystem = (MetaOpenXRAnchorSubsystem)manager.subsystem;
                
                if (metaAnchorSubsystem != null)
                {
                    return metaAnchorSubsystem.sharedAnchorsGroupId;
                }
                
                return null;
            }
            catch (Exception e)
            {
                Debug.LogError($"グループID取得中にエラーが発生しました: {e.Message}");
                return null;
            }
        }
        #endregion

        #region SharedAnchors - Share

        /// <summary>
        /// アンカーを共有します
        /// </summary>
        public async UniTask<bool> ShareAnchorAsync(ARAnchorManager manager, ARAnchor anchor)
        {
            if (!SupportsSharedAnchors(manager))
            {
                Debug.LogWarning("共有アンカー機能がサポートされていません");
                return false;
            }

            if (anchor == null)
            {
                Debug.LogWarning("共有するアンカーがnullです");
                return false;
            }

            try
            {
                XRResultStatus resultStatus = await manager.TryShareAnchorAsync(anchor);
                
                if (resultStatus.IsSuccess())
                {
                    Debug.Log($"アンカーの共有に成功しました: {anchor.trackableId}");
                    return true;
                }
                else
                {
                    string errorInfo = GetDetailedErrorInfo(resultStatus);
                    Debug.LogError($"アンカーの共有に失敗しました: {errorInfo}");
                    return false;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"アンカー共有中にエラーが発生しました: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 複数のアンカーを一括で共有します（Meta OpenXRでは全体成功か全体失敗）
        /// </summary>
        public async UniTask<List<XRShareAnchorResult>> ShareAnchorsAsync(ARAnchorManager manager, IEnumerable<ARAnchor> anchors)
        {
            List<XRShareAnchorResult> results = new List<XRShareAnchorResult>();
            
            if (!SupportsSharedAnchors(manager))
            {
                Debug.LogWarning("共有アンカー機能がサポートされていません");
                return results;
            }

            if (anchors == null)
            {
                Debug.LogWarning("共有するアンカーリストがnullです");
                return results;
            }

            try
            {
                await manager.TryShareAnchorsAsync(anchors, results);
                
                // 結果を確認
                int successCount = 0;
                foreach (XRShareAnchorResult result in results)
                {
                    if (result.resultStatus.IsSuccess())
                    {
                        successCount++;
                        Debug.Log($"アンカーの共有に成功: {result.anchorId}");
                    }
                    else
                    {
                        string errorInfo = GetDetailedErrorInfo(result.resultStatus);
                        Debug.LogWarning($"アンカーの共有に失敗: {result.anchorId} - {errorInfo}");
                    }
                }
                
                Debug.Log($"アンカー共有完了: {successCount}/{results.Count}個成功");
                return results;
            }
            catch (Exception e)
            {
                Debug.LogError($"アンカー一括共有中にエラーが発生しました: {e.Message}");
                return results;
            }
        }

        /// <summary>
        /// バッチ共有が成功したかを確認します
        /// </summary>
        public bool IsBatchShareSuccessful(List<XRShareAnchorResult> results)
        {
            if (results == null || results.Count == 0) return false;
            
            foreach (XRShareAnchorResult result in results)
            {
                if (result.resultStatus.IsError())
                    return false;
            }
            return true;
        }

        #endregion

        #region SharedAnchors - Load

        /// <summary>
        /// すべての共有アンカーをXRAnchorとして読み込みます
        /// </summary>
        private async UniTask<List<XRAnchor>> LoadAllSharedAnchorsAsync(ARAnchorManager manager, Action<ReadOnlyListSpan<XRAnchor>> onIncrementalResults = null)
        {
            List<XRAnchor> loadedAnchors = new List<XRAnchor>();
            
            if (!SupportsSharedAnchors(manager))
            {
                Debug.LogWarning("共有アンカー機能がサポートされていません");
                return loadedAnchors;
            }

            try
            {
                bool success = await manager.TryLoadAllSharedAnchorsAsync(loadedAnchors, onIncrementalResults ?? OnIncrementalXRAnchorResults);

                if (success)
                {
                    Debug.Log($"共有XRAnchor読み込み完了: {loadedAnchors.Count}個のXRAnchorを読み込みました");
                }
                else
                {
                    Debug.LogWarning("共有XRAnchorの読み込みに失敗しました");
                }
                
                return loadedAnchors;
            }
            catch (Exception e)
            {
                Debug.LogError($"共有XRAnchor読み込み中にエラーが発生しました: {e.Message}");
                return loadedAnchors;
            }
        }

        /// <summary>
        /// XRAnchorの段階的読み込み結果を処理します
        /// </summary>
        private void OnIncrementalXRAnchorResults(ReadOnlyListSpan<XRAnchor> sharedAnchors)
        {
            foreach (XRAnchor sharedAnchor in sharedAnchors)
            {
                Debug.Log($"共有XRAnchorを段階的に読み込みました: {sharedAnchor.trackableId}");
            }
        }

        /// <summary>
        /// 共有アンカーを読み込んでARAnchorとして取得します
        /// </summary>
        public async UniTask<List<ARAnchor>> LoadAndCreateSharedAnchorsAsync(ARAnchorManager manager, Action<ARAnchor> onAnchorCreated = null, float timeoutSeconds = 10.0f)
        {
            List<XRAnchor> loadedXRAnchors = await LoadAllSharedAnchorsAsync(manager);
            
            if (loadedXRAnchors.Count == 0)
            {
                Debug.Log("読み込み可能な共有アンカーがありません");
                return new List<ARAnchor>();
            }

            List<ARAnchor> arAnchors = await WaitForSharedAnchorsAsARAnchorAsync(manager, loadedXRAnchors, onAnchorCreated, timeoutSeconds);
            
            return arAnchors;
        }
        
        /// <summary>
        /// 読み込み済みのXRAnchorリストからtrackablesChangedイベントでARAnchorを取得します
        /// </summary>
        private async UniTask<List<ARAnchor>> WaitForSharedAnchorsAsARAnchorAsync(ARAnchorManager manager, List<XRAnchor> loadedXRAnchors, Action<ARAnchor> onAnchorCreated = null, float timeoutSeconds = 10.0f)
        {
            List<ARAnchor> foundARAnchors = new List<ARAnchor>();
            HashSet<TrackableId> expectedTrackableIds = new HashSet<TrackableId>();

            if (loadedXRAnchors == null || loadedXRAnchors.Count == 0)
            {
                Debug.Log("待機対象のXRAnchorがありません");
                return foundARAnchors;
            }

            // 期待するTrackableIdを記録
            foreach (XRAnchor xrAnchor in loadedXRAnchors)
            {
                expectedTrackableIds.Add(xrAnchor.trackableId);
            }

            UniTaskCompletionSource<bool> completionSource = new UniTaskCompletionSource<bool>();
            bool isCompleted = false;

            // trackablesChangedイベントハンドラを設定
            void OnTrackablesChanged(ARTrackablesChangedEventArgs<ARAnchor> eventArgs)
            {
                foreach (ARAnchor addedAnchor in eventArgs.added)
                {
                    if (expectedTrackableIds.Contains(addedAnchor.trackableId))
                    {
                        foundARAnchors.Add(addedAnchor);
                        Debug.Log($"共有アンカーのARAnchorを検出: {addedAnchor.trackableId}");
                        
                        // 外部コールバック通知
                        onAnchorCreated?.Invoke(addedAnchor);
                        
                        // すべてのアンカーが見つかったかチェック
                        if (foundARAnchors.Count >= expectedTrackableIds.Count)
                        {
                            if (!isCompleted)
                            {
                                isCompleted = true;
                                completionSource.TrySetResult(true);
                            }
                        }
                    }
                }
            }

            try
            {
                // trackablesChangedイベントを監視開始
                manager.trackablesChanged.AddListener(OnTrackablesChanged);

                Debug.Log($"共有アンカーのARAnchor変換を開始: {expectedTrackableIds.Count}個のアンカーを待機中");

                // タイムアウト付きでARAnchorの作成を待機
                CancellationTokenSource timeoutCancellation = new CancellationTokenSource();
                timeoutCancellation.CancelAfter((int)(timeoutSeconds * 1000));

                try
                {
                    await completionSource.Task.AttachExternalCancellation(timeoutCancellation.Token);
                    Debug.Log($"共有アンカー変換完了: {foundARAnchors.Count}/{expectedTrackableIds.Count}個のARAnchorを発見");
                }
                catch (OperationCanceledException)
                {
                    Debug.LogWarning($"共有アンカー変換タイムアウト: {foundARAnchors.Count}/{expectedTrackableIds.Count}個のARAnchorを発見 (タイムアウト: {timeoutSeconds}秒)");
                }

                return foundARAnchors;
            }
            catch (Exception e)
            {
                Debug.LogError($"共有アンカー変換中にエラーが発生しました: {e.Message}");
                return foundARAnchors;
            }
            finally
            {
                // イベントリスナーを削除
                manager.trackablesChanged.RemoveListener(OnTrackablesChanged);
            }
        }

        #endregion
    }
}
