using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Addons.Physics;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

namespace b0bmat0ca.OpenXr.Multiplayer.Photon
{
    using ARAnchors;
    using OpenXr.Player;
    using Data;
    
    /// <summary>
    /// Photon Fusionのネットワークセッション管理、プレイヤーの生成、およびローカルプレイヤーの入力収集を行うクラス
    /// ホストモード版
    /// </summary>
    public class ConnectionManager : MonoBehaviour, INetworkRunnerCallbacks
    {
        public static ConnectionManager instance;
        public Transform SharedAnchorTransform => _sharedAnchorTransform;
        
        [Header("Player")]
        [SerializeField] private PlayerSyncHelper _playerSyncHelper;
        [SerializeField] private NetworkPrefabRef _playerPrefab;
        [SerializeField] private float _playerSpawnOffset = 2.0f;

        [Header("UI")]
        [SerializeField] private GameObject _startPanelObject;
        [SerializeField] private Button _hostButton;
        [SerializeField] private Button _clientButton;

        private float _startPanelDistance;
        
        private List<ARAnchor> _sharedAnchors;
        private Transform _sharedAnchorTransform;
        
        private NetworkRunner _runner;
        private Dictionary<PlayerRef, NetworkObject> _spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();

        private bool _isLocalPlayerSpawned = false;
        private NetworkGrabber _localLeftGrabber;
        private NetworkGrabber _localRightGrabber;

        #region Unity Lifecycle
        
        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else
            {
                Destroy(this.gameObject);
            }
        }

        private void Start()
        {
            _startPanelDistance = _startPanelObject.transform.position.z;
            _startPanelObject.SetActive(false);
            
            _hostButton.onClick.AddListener(StartGameAsHost);
            _clientButton.onClick.AddListener(StartGameAsClient);
            AnchorPlacer.OnLoadSharedAnchorsCompleted += OnSharedAnchorsLoaded;
        }
        
        private void OnDestroy()
        {
            _hostButton.onClick.RemoveListener(StartGameAsHost);
            _clientButton.onClick.RemoveListener(StartGameAsClient);
            AnchorPlacer.OnLoadSharedAnchorsCompleted -= OnSharedAnchorsLoaded;
        }
        
        #endregion
        
        #region Network Session Management

        private async void StartGame(GameMode mode)
        {
            _runner = gameObject.AddComponent<NetworkRunner>();
            _runner.ProvideInput = true;

            // NetworkRigidbody3D使用時に必要
            RunnerSimulatePhysics3D runnerPhysics = gameObject.AddComponent<RunnerSimulatePhysics3D>();
            runnerPhysics.ClientPhysicsSimulation = ClientPhysicsSimulation.SimulateForward;

            // Photon Fusion のシーンオブジェクトを利用するために必要（SharedObjectManager）
            SceneRef scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);
            NetworkSceneInfo sceneInfo = new NetworkSceneInfo();
            if (scene.IsValid)
            {
                sceneInfo.AddSceneRef(scene, LoadSceneMode.Additive);
            }

            await _runner.StartGame(new StartGameArgs()
                {
                    GameMode = mode,
                    SessionName = "OpenXrRoom",
                    Scene = scene,
                    SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
                }
            );
        }

        #endregion
        
        #region public Methods
        
        /// <summary>
        /// ローカルプレイヤーの NetworkGrabber を登録
        /// </summary>
        public void RegisterLocalGrabber(NetworkGrabber leftGrabber, NetworkGrabber rightGrabber)
        {
            _localLeftGrabber = leftGrabber;
            _localRightGrabber = rightGrabber;
            
            _isLocalPlayerSpawned = true;
        }

        #endregion
        
        #region UI Callbacks
        
        private void StartGameAsHost()
        {
            StartGame(GameMode.Host);
            
            _startPanelObject.SetActive(false);
        }
        
        private void StartGameAsClient()
        {
            StartGame(GameMode.Client);
            
            _startPanelObject.SetActive(false);
        }
        
        #endregion

        # region Event Handlers
        
        private void OnSharedAnchorsLoaded(List<ARAnchor> anchors)
        {
            _sharedAnchors = anchors;
            
            // 最後のアンカーを共有アンカーとして使用
            _sharedAnchorTransform = anchors[^1].transform;
            
            // 利用しないアンカーを無効化
            for (int i = 0; i < anchors.Count - 1; i++)
            {
                anchors[i].gameObject.SetActive(false);
            }

            Camera mainCamera = Camera.main;
            if (mainCamera == null) return;
            
            _startPanelObject.transform.position = mainCamera.transform.position + mainCamera.transform.forward * _startPanelDistance;
            
            // UIがカメラの方向を向くように回転（Y軸のみ）
            Quaternion rotation = Quaternion.LookRotation(_startPanelObject.transform.position - mainCamera.transform.position);
            _startPanelObject.transform.rotation = Quaternion.Euler(0f, rotation.eulerAngles.y, 0f);
                
            _startPanelObject.SetActive(true);
        }
        
        #endregion

        #region INetworkRunnerCallbacks Implementation
        
        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            if (runner.IsServer)
            {
                // 共有アンカーが利用可能な場合のみスポーン
                if (_sharedAnchorTransform != null)
                {
                    // スポーン時は、アンカーを基準にし、プレイヤー間オフセットを計算して配置
                    // 実際の位置は、ローカルプレイヤーの入力で更新される
                    float playerOffset = (player.RawEncoded % runner.Config.Simulation.PlayerCount) * _playerSpawnOffset;
                    Vector3 spawnPositionOffset = new Vector3(playerOffset, 0f, 0f);
                    Vector3 spawnPosition = _sharedAnchorTransform.TransformPoint(spawnPositionOffset);
                    
                    NetworkObject networkPlayerObject = runner.Spawn(_playerPrefab, spawnPosition, Quaternion.identity, player);
                    _spawnedCharacters.Add(player, networkPlayerObject);
                    
                    Debug.Log($"{this.GetType().Name} : OnPlayerJoined - Player {player.PlayerId} spawned at {spawnPosition} (anchor + {playerOffset}m offset)");
                }
            }
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            // ホストのみがプレイヤーオブジェクトをDespawn
            if (runner.IsServer && _spawnedCharacters.TryGetValue(player, out NetworkObject networkObject))
            {
                runner.Despawn(networkObject);
                _spawnedCharacters.Remove(player);
            }

            // ローカルプレイヤーが退出した場合のクリーンアップ
            if (player == runner.LocalPlayer)
            {
                _isLocalPlayerSpawned = false;
                _localLeftGrabber = null;
                _localRightGrabber = null;
            }
        }

        public void OnInput(NetworkRunner runner, NetworkInput input)
        {
            if (!_isLocalPlayerSpawned || !_playerSyncHelper.IsInitialized || _sharedAnchorTransform == null) return;

            NetworkInputData data = new NetworkInputData
            {
                // 共有アンカー基準のローカルプレイヤーの位置と回転を収集
                playAreaPosition = _sharedAnchorTransform.InverseTransformPoint(_playerSyncHelper.transform.position),
                playAreaRotation = Quaternion.Inverse(_sharedAnchorTransform.rotation) * _playerSyncHelper.transform.rotation,
                headPosition = _sharedAnchorTransform.InverseTransformPoint(_playerSyncHelper.headTargetTransform.position),
                headRotation = Quaternion.Inverse(_sharedAnchorTransform.rotation) * _playerSyncHelper.headTargetTransform.rotation,
                leftHandPosition = _sharedAnchorTransform.InverseTransformPoint(_playerSyncHelper.leftHandTargetTransform.position),
                leftHandRotation = Quaternion.Inverse(_sharedAnchorTransform.rotation) * _playerSyncHelper.leftHandTargetTransform.rotation,
                rightHandPosition = _sharedAnchorTransform.InverseTransformPoint(_playerSyncHelper.rightHandTargetTransform.position),
                rightHandRotation = Quaternion.Inverse(_sharedAnchorTransform.rotation) * _playerSyncHelper.rightHandTargetTransform.rotation,

                // グラブ情報を収集
                leftHandGrabInfo = _localLeftGrabber.LocalGrabInfo,
                rightHandGrabInfo = _localRightGrabber.LocalGrabInfo
            };

            // 指関節データを収集
            if (_playerSyncHelper.SyncFingerJoint && _playerSyncHelper.TryGetLeftHandJoints(out Quaternion[] leftJoints, out bool leftTracked))
            {
                data.leftHandJointData = new HandJointData { isTracked = leftTracked };
                data.leftHandJointData.SetJoints(leftJoints);
            }
            else
            {
                data.leftHandJointData = HandJointData.Empty;
            }

            if (_playerSyncHelper.SyncFingerJoint && _playerSyncHelper.TryGetRightHandJoints(out Quaternion[] rightJoints, out bool rightTracked))
            {
                data.rightHandJointData = new HandJointData { isTracked = rightTracked };
                data.rightHandJointData.SetJoints(rightJoints);
            }
            else
            {
                data.rightHandJointData = HandJointData.Empty;
            }

            input.Set(data);
        }
        
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
        public void OnConnectedToServer(NetworkRunner runner) { }
        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        public void OnSceneLoadDone(NetworkRunner runner) { }
        public void OnSceneLoadStart(NetworkRunner runner) { }
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
        
        #endregion
    }
}
