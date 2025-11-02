using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.UI;

namespace b0bmat0ca.OpenXr.Multiplayer.Photon
{
    /// <summary>
    /// 共有アンカーベースのオブジェクト管理クラス
    /// PlayerPrefab以外の共有オブジェクトを管理
    /// NetworkObjectとAnchoredNetworkObjectで状態を管理（ObjectInfos削除済み）
    /// </summary>
    public class SharedObjectManager : NetworkBehaviour
    {
        public event Action<int, NetworkObject> OnObjectSpawned;
        public event Action<int> OnObjectDestroyed;

        [Header("Spawn Object Settings")]
        [SerializeField] private NetworkPrefabRef _sharedCubePrefab;
        [SerializeField] private NetworkPrefabRef _sharedSpherePrefab;
        [SerializeField] private Vector3 _spawnOffset = new Vector3(0f, 0.5f, 1f);

        [Header("UI")]
        [SerializeField] private GameObject _objectSpawnPanel;
        [SerializeField] private Button _spawnCubeButton;
        [SerializeField] private Button _spawnSphereButton;
        
        private Camera _mainCamera;
        private float _objectSpawnPanelDistance;
        
        private Dictionary<int, NetworkObject> _spawnedObjects = new Dictionary<int, NetworkObject>();
        private Transform _sharedAnchorTransform;
        private int _nextObjectId = 0;

        #region Unity Lifecycle
        
        private void Start()
        {
            _mainCamera = Camera.main;
            _objectSpawnPanelDistance = _objectSpawnPanel.transform.position.z;
            _objectSpawnPanel.SetActive(false);

            _spawnCubeButton.onClick.AddListener(OnSpawnCubeButtonClicked);
            _spawnSphereButton.onClick.AddListener(OnSpawnSphereButtonClicked);
        }

        private void OnDestroy()
        {
            _spawnCubeButton.onClick.RemoveListener(OnSpawnCubeButtonClicked);
            _spawnSphereButton.onClick.RemoveListener(OnSpawnSphereButtonClicked);
            
            DestroyAllSharedObjects();
        }

        #endregion
        
        #region UI Callbacks
        
        /// <summary>
        /// Cube ボタンクリック時
        /// </summary>
        private void OnSpawnCubeButtonClicked()
        {
            SpawnSharedObject("Cube", _sharedCubePrefab, _spawnOffset);
            
            Debug.Log($"{this.GetType().Name} : Cubeスポーンボタンがクリックされました");
        }

        /// <summary>
        /// Sphere ボタンクリック時
        /// </summary>
        private void OnSpawnSphereButtonClicked()
        {
            SpawnSharedObject("Sphere", _sharedSpherePrefab, _spawnOffset);
            
            Debug.Log($"{this.GetType().Name} : Sphereスポーンボタンがクリックされました");
        }
        
        #endregion
        
        #region Spawn / Despawn Shared Objects (Host Only)

        /// <summary>
        /// 共有アンカーを起点として、共有オブジェクトを配置
        /// </summary>
        private void SpawnSharedObject(string objectType, NetworkPrefabRef prefab, Vector3 relativePosition)
        {
            if (!Object.HasStateAuthority || _sharedAnchorTransform == null) return;

            // オブジェクトID生成
            int objectId = _nextObjectId++;

            Debug.Log($"{this.GetType().Name} : オブジェクト生成開始 - ID: {objectId}, Type: {objectType}");

            CreateObjectAtAnchor(objectId, objectType, prefab, relativePosition);
        }

        /// <summary>
        /// 共有アンカーの座標系でオブジェクトを生成
        /// </summary>
        private void CreateObjectAtAnchor(int objectId, string objectType, NetworkPrefabRef prefab, Vector3 relativePosition)
        {
            // ワールド位置計算
            Vector3 worldPosition = _sharedAnchorTransform.TransformPoint(relativePosition);
            Quaternion worldRotation = _sharedAnchorTransform.rotation;

            if (prefab.IsValid)
            {
                NetworkObject networkObject = Runner.Spawn(prefab, worldPosition, worldRotation);
                if (networkObject != null)
                {
                    // AnchoredNetworkObjectに情報を設定
                    if (networkObject.TryGetComponent(out AnchoredNetworkObject anchoredObject))
                    {
                        anchoredObject.SharedObjectId = objectId;
                        anchoredObject.ObjectType = objectType;
                    }

                    _spawnedObjects[objectId] = networkObject;
                    OnObjectSpawned?.Invoke(objectId, networkObject);

                    Debug.Log($"{this.GetType().Name} : CreateObjectAtAnchor - ネットワークオブジェクト生成完了 - ID: {objectId}, Type: {objectType}");
                }
            }
        }
        
        /// <summary>
        /// 指定されたオブジェクトを削除
        /// </summary>
        private void DestroySharedObject(int objectId)
        {
            if (!Object.HasStateAuthority) return;

            // NetworkObjectを削除（Fusionが自動的に全クライアントで削除）
            if (_spawnedObjects.TryGetValue(objectId, out NetworkObject networkObject))
            {
                if (networkObject != null)
                {
                    Runner.Despawn(networkObject);
                }
                _spawnedObjects.Remove(objectId);
                OnObjectDestroyed?.Invoke(objectId);

                Debug.Log($"{this.GetType().Name} : オブジェクト削除完了 - ID: {objectId}");
            }
        }

        /// <summary>
        /// 全ての共有オブジェクトを削除
        /// </summary>
        public void DestroyAllSharedObjects()
        {
            if (!Object.HasStateAuthority) return;

            List<int> objectIds = new List<int>(_spawnedObjects.Keys);
            foreach (int objectId in objectIds)
            {
                DestroySharedObject(objectId);
            }

            Debug.Log($"{this.GetType().Name} : 全オブジェクト削除リクエスト - {objectIds.Count}個");
        }
        
        #endregion

        #region NetworkBehaviour Overrides

        public override void Spawned()
        {
            base.Spawned();
            Debug.Log($"{this.GetType().Name} : Spawned - StateAuthority: {Object.HasStateAuthority}");

            // ホストの処理
            if (Object.HasStateAuthority)
            {
                // 共有アンカーを取得
                _sharedAnchorTransform = ConnectionManager.instance.SharedAnchorTransform;

                // オブジェクト生成パネルを表示
                _objectSpawnPanel.transform.position = _mainCamera.transform.position + _mainCamera.transform.forward * _objectSpawnPanelDistance;
                
                // カメラの方向を向くように回転（Y軸のみ）
                Quaternion rotation = Quaternion.LookRotation(_objectSpawnPanel.transform.position - _mainCamera.transform.position);
                _objectSpawnPanel.transform.rotation = Quaternion.Euler(0f, rotation.eulerAngles.y, 0f);
                    
                _objectSpawnPanel.SetActive(true);
                
                Debug.Log($"{this.GetType().Name} : オブジェクト生成UIパネルを表示");
            }
        }

        #endregion
    }
}
