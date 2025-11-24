using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.Assertions;

namespace b0bmat0ca.OpenXr.ARAnchors
{
    /// <summary>
    /// コントローラーのボタン入力でARAnchorを配置するクラス
    /// </summary>
    public class AnchorPlacer : MonoBehaviour
    {
        public static event Action<List<ARAnchor>> OnLoadSharedAnchorsCompleted;
        
        [Header("Dependencies")]
        [SerializeField] private ARAnchorManager _anchorManager;
        [SerializeField] private ARAnchorController _anchorController;
        
        [Header("Controller Input")]
        [Header("Right Controller")]
        [SerializeField] private InputActionReference _rightControllerPositionAction;
        [SerializeField] private InputActionReference _rightTriggerButtonAction;
        [SerializeField] private InputActionReference _rightPrimaryButtonAction;
        [SerializeField] private InputActionReference _rightSecondaryButtonAction;
        
        [Header("Left Controller (共有アンカー)")]
        [SerializeField] private InputActionReference _leftControllerPositionAction;
        [SerializeField] private InputActionReference _leftTriggerButtonAction;
        [SerializeField] private InputActionReference _leftPrimaryButtonAction;
        
        [Header("Anchor Settings")]
        [SerializeField] private Vector3 _anchorOffset = Vector3.zero;
        
        [Header("Shared Anchor Settings")]
        [SerializeField] private string _sharedAnchorGroupId = "OpenXrMeta-SharedAnchors-Group-b0bmat0ca-001";
        
        private readonly string ANCHOR_IDS_KEY = "SavedAnchorIds";
        private readonly string ANCHOR_COUNT_KEY = "SavedAnchorCount";
        
        #region Unity Lifecycle
        private void Awake()
        {
            Assert.IsNotNull(_anchorManager, "ARAnchorManager is not assigned in AnchorPlacer");
            Assert.IsNotNull(_anchorController, "ARAnchorController is not assigned in AnchorPlacer");
            
            // Right Controller
            Assert.IsNotNull(_rightControllerPositionAction, "Right Controller Position Action is not assigned in AnchorPlacer");
            Assert.IsNotNull(_rightTriggerButtonAction, "Right Trigger Button Action is not assigned in AnchorPlacer");
            Assert.IsNotNull(_rightPrimaryButtonAction, "Right Primary Button Action is not assigned in AnchorPlacer");
            Assert.IsNotNull(_rightSecondaryButtonAction, "Right Secondary Button Action is not assigned in AnchorPlacer");
            
            // Left Controller
            Assert.IsNotNull(_leftControllerPositionAction, "Left Controller Position Action is not assigned in AnchorPlacer");
            Assert.IsNotNull(_leftTriggerButtonAction, "Left Trigger Button Action is not assigned in AnchorPlacer");
            Assert.IsNotNull(_leftPrimaryButtonAction, "Left Primary Button Action is not assigned in AnchorPlacer");
        }

        private void OnEnable()
        {
            EnableInputActions();
        }
        
        private void OnDisable()
        {
            DisableInputActions();
        }
        
        private void Start()
        {
            // 共有アンカーのグループIDを設定
            SetupSharedAnchorGroup();
            
            // コントローラーボタンのイベント設定
            SetupInputActions();
        }
        
        #endregion
        
        #region private methods
        
        private void EnableInputActions()
        {
            // Right Controller
            _rightControllerPositionAction?.action.Enable();
            _rightTriggerButtonAction?.action.Enable();
            _rightPrimaryButtonAction?.action.Enable();
            _rightSecondaryButtonAction?.action.Enable();
            
            // Left Controller
            _leftControllerPositionAction?.action.Enable();
            _leftTriggerButtonAction?.action.Enable();
            _leftPrimaryButtonAction?.action.Enable();
        }
        
        private void DisableInputActions()
        {
            // Right Controller
            _rightControllerPositionAction?.action.Disable();
            _rightTriggerButtonAction?.action.Disable();
            _rightPrimaryButtonAction?.action.Disable();
            _rightSecondaryButtonAction?.action.Disable();
            
            // Left Controller
            _leftControllerPositionAction?.action.Disable();
            _leftTriggerButtonAction?.action.Disable();
            _leftPrimaryButtonAction?.action.Disable();
        }
        
        private void SetupInputActions()
        {
            // Right Controller (個別アンカー)
            if (_rightTriggerButtonAction != null)
            {
                _rightTriggerButtonAction.action.performed += OnRightTriggerPressed;
            }
            
            if (_rightPrimaryButtonAction != null)
            {
                _rightPrimaryButtonAction.action.performed += OnRightPrimaryButtonPressed;
            }
            
            if (_rightSecondaryButtonAction != null)
            {
                _rightSecondaryButtonAction.action.performed += OnRightSecondaryButtonPressed;
            }
            
            // Left Controller (共有アンカー)
            if (_leftTriggerButtonAction != null)
            {
                _leftTriggerButtonAction.action.performed += OnLeftTriggerPressed;
            }
            
            if (_leftPrimaryButtonAction != null)
            {
                _leftPrimaryButtonAction.action.performed += OnLeftPrimaryButtonPressed;
            }
        }
        
        #endregion
        
        #region Right Controller Event Handlers
        
        private void OnRightTriggerPressed(InputAction.CallbackContext context)
        {
            PlaceAnchorAtControllerPosition().Forget();
        }
        
        private void OnRightPrimaryButtonPressed(InputAction.CallbackContext context)
        {
            LoadAnchors().Forget();
        }
        
        private void OnRightSecondaryButtonPressed(InputAction.CallbackContext context)
        {
            RemoveAllAnchors().Forget();
        }
        
        #endregion
        
        #region Left Controller Event Handlers (共有アンカー)
        
        private void OnLeftTriggerPressed(InputAction.CallbackContext context)
        {
            PlaceSharedAnchorAtControllerPosition().Forget();
        }
        
        private void OnLeftPrimaryButtonPressed(InputAction.CallbackContext context)
        {
            LoadSharedAnchors().Forget();
        }
        
        #endregion
        
        #region ARAnchor Methods
        
        /// <summary>
        /// コントローラーの現在位置にアンカーを配置
        /// </summary>
        private async UniTaskVoid PlaceAnchorAtControllerPosition()
        {
            // コントローラーの位置を取得（回転はWorld座標系で設定）
            Vector3 controllerPosition = GetRightControllerPosition();
            Vector3 anchorPosition = controllerPosition + _anchorOffset;
            Quaternion anchorRotation = Quaternion.identity; // World座標系の回転
            
            Pose anchorPose = new Pose(anchorPosition, anchorRotation);
            
            await PlaceAnchorAsync(anchorPose);
        }
        
        /// <summary>
        /// 右コントローラーの位置を取得
        /// </summary>
        private Vector3 GetRightControllerPosition()
        {
            if (_rightControllerPositionAction != null && _rightControllerPositionAction.action.enabled)
            {
                return _rightControllerPositionAction.action.ReadValue<Vector3>();
            }
            
            Debug.LogWarning("右コントローラー位置が取得できません。");
            return Vector3.zero;
        }
        
        /// <summary>
        /// 保存されているすべてのアンカーを読み込み
        /// </summary>
        private async UniTaskVoid LoadAnchors()
        {
            try
            {
                Debug.Log("PlayerPrefsから保存されたアンカーIDを取得中...");
                
                // PlayerPrefsから保存されたアンカーIDを取得
                List<SerializableGuid> savedAnchorIds = GetAnchorIdsFromPlayerPrefs();
                
                if (savedAnchorIds.Count == 0)
                {
                    Debug.Log("保存されたアンカーが見つかりませんでした");
                    return;
                }
                
                Debug.Log($"{savedAnchorIds.Count}個の保存されたアンカーが見つかりました");
                
                // アンカーをロード
                List<ARSaveOrLoadAnchorResult> results = await _anchorController.LoadAnchorsAsync(_anchorManager, savedAnchorIds);
                
                int successCount = 0;
                foreach (ARSaveOrLoadAnchorResult result in results)
                {
                    if (result.resultStatus.IsSuccess())
                    {
                        successCount++;
                        Debug.Log($"アンカーをロードしました: {result.anchor.trackableId}");
                    }
                    else
                    {
                        Debug.LogWarning($"アンカーのロードに失敗しました: {_anchorController.GetDetailedErrorInfo(result.resultStatus)}");
                    }
                }
                
                Debug.Log($"アンカーロード完了: {successCount}/{results.Count}個成功");
            }
            catch (Exception e)
            {
                Debug.LogError($"アンカーロード中にエラーが発生しました: {e.Message}");
            }
        }
        
        /// <summary>
        /// ARアンカーを配置
        /// </summary>
        private async UniTask PlaceAnchorAsync(Pose pose)
        {
            try
            {
                Debug.Log($"アンカー配置開始: {pose.position}");
                
                // ARAnchorControllerを使用してアンカーを作成
                ARAnchor anchor = await _anchorController.CreateAnchorAsync(_anchorManager, pose);
                
                if (anchor != null)
                {
                    Debug.Log($"アンカーが正常に配置されました: {anchor.trackableId} at {pose.position}");
                    
                    // アンカーを保存
                    SerializableGuid? savedGuid = await _anchorController.SaveAnchorAsync(_anchorManager, anchor);
                    if (savedGuid.HasValue)
                    {
                        SaveAnchorIdToPlayerPrefs(savedGuid.Value);
                        Debug.Log($"アンカーIDを保存しました: {savedGuid.Value}");
                    }
                    else
                    {
                        Debug.LogWarning("アンカーの保存に失敗しました");
                    }
                }
                else
                {
                    Debug.LogWarning("アンカーの作成に失敗しました");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"アンカー配置中にエラーが発生しました: {e.Message}");
                Debug.LogError($"スタックトレース: {e.StackTrace}");
            }
        }
        
        /// <summary>
        /// 現在配置されているすべてのアンカーを削除（PlayerPrefsからも削除）
        /// </summary>
        private async UniTaskVoid RemoveAllAnchors()
        {
            try
            {
                Debug.Log("すべてのアンカーを削除中...");
                
                // PlayerPrefsから保存されたアンカーIDを取得
                List<SerializableGuid> savedAnchorIds = GetAnchorIdsFromPlayerPrefs();
                
                if (savedAnchorIds.Count > 0)
                {
                    // 永続化されたアンカーを削除
                    List<XREraseAnchorResult> eraseResults = await _anchorController.EraseAnchorsAsync(_anchorManager, savedAnchorIds);
                    
                    int successCount = 0;
                    foreach (XREraseAnchorResult result in eraseResults)
                    {
                        if (result.resultStatus.IsSuccess())
                        {
                            successCount++;
                        }
                        else
                        {
                            Debug.LogWarning($"アンカーの削除に失敗: {_anchorController.GetDetailedErrorInfo(result.resultStatus)}");
                        }
                    }
                    
                    Debug.Log($"永続化アンカー削除完了: {successCount}/{eraseResults.Count}個成功");
                }
                
                // 現在のセッションのアンカーも削除
                if (_anchorManager != null)
                {
                    List<ARAnchor> anchorsToRemove = new List<ARAnchor>();
                    foreach (ARAnchor anchor in _anchorManager.trackables)
                    {
                        anchorsToRemove.Add(anchor);
                    }
                    
                    foreach (ARAnchor anchor in anchorsToRemove)
                    {
                        _anchorManager.TryRemoveAnchor(anchor);
                    }
                    
                    Debug.Log($"セッションアンカー削除完了: {anchorsToRemove.Count}個削除");
                }
                
                // PlayerPrefsからアンカーIDをすべて削除
                ClearSavedAnchorIds();
                
                Debug.Log("すべてのアンカーとIDの削除が完了しました");
            }
            catch (Exception e)
            {
                Debug.LogError($"アンカー削除中にエラーが発生しました: {e.Message}");
            }
        }
        
        #endregion
        
        #region Shared Anchor Methods
        
        /// <summary>
        /// 共有アンカーのグループIDを設定
        /// </summary>
        private void SetupSharedAnchorGroup()
        {
            try
            {
                // 固定グループIDを設定
                SerializableGuid groupId = new SerializableGuid(Guid.Parse(GenerateGuidFromString(_sharedAnchorGroupId)));
                bool success = _anchorController.SetSharedAnchorsGroupId(_anchorManager, groupId);
                
                if (success)
                {
                    Debug.Log($"共有アンカーのグループIDを設定しました: {_sharedAnchorGroupId}");
                }
                else
                {
                    Debug.LogWarning("共有アンカーのグループID設定に失敗しました");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"共有アンカーグループID設定エラー: {e.Message}");
            }
        }
        
        /// <summary>
        /// 指定された文字列からGUIDを生成
        /// </summary>
        private string GenerateGuidFromString(string input)
        {
            using MD5 md5 = MD5.Create();
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            byte[] hashBytes = md5.ComputeHash(inputBytes);
            Guid guid = new Guid(hashBytes);

            return guid.ToString();
        }
        
        /// <summary>
        /// コントローラーの現在位置に共有アンカーを配置
        /// </summary>
        private async UniTaskVoid PlaceSharedAnchorAtControllerPosition()
        {
            // コントローラーの位置を取得（回転はWorld座標系で設定）
            Vector3 controllerPosition = GetLeftControllerPosition();
            Vector3 anchorPosition = controllerPosition + _anchorOffset;
            Quaternion anchorRotation = Quaternion.identity; // World座標系の回転
            
            Pose anchorPose = new Pose(anchorPosition, anchorRotation);
            
            await PlaceSharedAnchorAsync(anchorPose);
        }
        
        /// <summary>
        /// 左コントローラーの位置を取得
        /// </summary>
        private Vector3 GetLeftControllerPosition()
        {
            if (_leftControllerPositionAction != null && _leftControllerPositionAction.action.enabled)
            {
                return _leftControllerPositionAction.action.ReadValue<Vector3>();
            }
            
            Debug.LogWarning("左コントローラー位置が取得できません。");
            return Vector3.zero;
        }
        
        /// <summary>
        /// 共有アンカーを読み込み
        /// </summary>
        private async UniTaskVoid LoadSharedAnchors()
        {
            try
            {
                Debug.Log("共有アンカーの読み込みを開始...");
                
                // 共有アンカーを読み込んでARAnchorとして取得
                List<ARAnchor> sharedAnchors = await _anchorController.LoadAndCreateSharedAnchorsAsync(_anchorManager, OnSharedAnchorCreated);
                
                Debug.Log($"共有アンカーロード完了: {sharedAnchors.Count}個のアンカーをロードしました");
                
                if (sharedAnchors.Count == 0)
                {
                    Debug.Log("読み込み可能な共有アンカーがありませんでした");
                }
                else
                {
                    OnLoadSharedAnchorsCompleted?.Invoke(sharedAnchors);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"共有アンカーロード中にエラーが発生しました: {e.Message}");
            }
        }
        
        /// <summary>
        /// 共有アンカーが作成された際のコールバック
        /// </summary>
        private void OnSharedAnchorCreated(ARAnchor anchor)
        {
            Debug.Log($"共有アンカーが作成されました: {anchor.trackableId} at {anchor.transform.position}");
            // 必要に応じてアンカー位置にオブジェクトを配置するなどの処理を追加
        }
        
        /// <summary>
        /// 共有アンカーを配置
        /// </summary>
        private async UniTask PlaceSharedAnchorAsync(Pose pose)
        {
            try
            {
                Debug.Log($"共有アンカー配置開始: {pose.position}");
                
                // ARAnchorControllerを使用してアンカーを作成
                ARAnchor anchor = await _anchorController.CreateAnchorAsync(_anchorManager, pose);
                
                if (anchor != null)
                {
                    Debug.Log($"アンカーが正常に配置されました: {anchor.trackableId} at {pose.position}");
                    
                    // アンカーを共有
                    bool shareSuccess = await _anchorController.ShareAnchorAsync(_anchorManager, anchor);
                    if (shareSuccess)
                    {
                        Debug.Log($"アンカーを共有しました: {anchor.trackableId}");
                    }
                    else
                    {
                        Debug.LogWarning("アンカーの共有に失敗しました");
                    }
                }
                else
                {
                    Debug.LogWarning("共有アンカーの作成に失敗しました");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"共有アンカー配置中にエラーが発生しました: {e.Message}");
                Debug.LogError($"スタックトレース: {e.StackTrace}");
            }
        }
        
        #endregion
        
        #region PlayerPrefs管理
        
        /// <summary>
        /// アンカーIDをPlayerPrefsに保存
        /// </summary>
        private void SaveAnchorIdToPlayerPrefs(SerializableGuid anchorId)
        {
            int currentCount = PlayerPrefs.GetInt(ANCHOR_COUNT_KEY, 0);
            string key = $"{ANCHOR_IDS_KEY}_{currentCount}";
            // SerializableGuidを標準的なGUID形式の文字列に変換
            string value = anchorId.guid.ToString();
            
            PlayerPrefs.SetString(key, value);
            PlayerPrefs.SetInt(ANCHOR_COUNT_KEY, currentCount + 1);
            PlayerPrefs.Save();
            
            Debug.Log($"PlayerPrefsに保存: Key={key}, Value={value}, Count={currentCount + 1}");
        }
        
        /// <summary>
        /// PlayerPrefsから保存されたアンカーIDを取得
        /// </summary>
        private List<SerializableGuid> GetAnchorIdsFromPlayerPrefs()
        {
            List<SerializableGuid> anchorIds = new List<SerializableGuid>();
            int count = PlayerPrefs.GetInt(ANCHOR_COUNT_KEY, 0);
            
            Debug.Log($"PlayerPrefsから読み込み: Count={count}");
            
            for (int i = 0; i < count; i++)
            {
                string key = $"{ANCHOR_IDS_KEY}_{i}";
                string guidString = PlayerPrefs.GetString(key, "");
                
                Debug.Log($"PlayerPrefs読み込み: Key={key}, Value={guidString}");
                
                if (!string.IsNullOrEmpty(guidString))
                {
                    if (Guid.TryParse(guidString, out System.Guid guid))
                    {
                        SerializableGuid serializableGuid = new SerializableGuid(guid);
                        anchorIds.Add(serializableGuid);
                        Debug.Log($"有効なアンカーID追加: {serializableGuid}");
                    }
                    else
                    {
                        Debug.LogWarning($"無効なGUID形式: {guidString}");
                    }
                }
                else
                {
                    Debug.LogWarning($"空のGUID文字列: Key={key}");
                }
            }
            
            Debug.Log($"PlayerPrefsから{anchorIds.Count}個のアンカーIDを取得しました");
            return anchorIds;
        }
        
        /// <summary>
        /// PlayerPrefsから保存されたアンカーIDをすべて削除
        /// </summary>
        private void ClearSavedAnchorIds()
        {
            int count = PlayerPrefs.GetInt(ANCHOR_COUNT_KEY, 0);
            
            for (int i = 0; i < count; i++)
            {
                PlayerPrefs.DeleteKey($"{ANCHOR_IDS_KEY}_{i}");
            }
            
            PlayerPrefs.DeleteKey(ANCHOR_COUNT_KEY);
            PlayerPrefs.Save();
            
            Debug.Log("保存されたアンカーIDをすべて削除しました");
        }
        #endregion
    }
}
