using Fusion;
using UnityEngine;

namespace b0bmat0ca.OpenXr.Multiplayer.Photon
{
    using OpenXr.Player;
    using Data;
    
    /// <summary>
    /// ネットワーク上でのプレイヤーアバターを管理するクラス
    /// </summary>
    public class Player : NetworkBehaviour
    {
        #region Network Properties
        
        [Networked] public Vector3 PlayAreaPosition { get; private set; }
        [Networked] public Quaternion PlayAreaRotation { get; private set; }
        [Networked] public Vector3 HeadPosition { get; private set; }
        [Networked] public Quaternion HeadRotation { get; private set; }
        [Networked] public Vector3 LeftHandPosition { get; private set; }
        [Networked] public Quaternion LeftHandRotation { get; private set; }
        [Networked] public Vector3 RightHandPosition { get; private set; }
        [Networked] public Quaternion RightHandRotation { get; private set; }
        
        #endregion
        
        [Header("Tracking Transforms")]
        [SerializeField] private Transform _headTrackingTransform;
        [SerializeField] private Transform _leftHandTrackingTransform;
        [SerializeField] private Transform _rightHandTrackingTransform;
        
        [Header("Visual Transforms")]
        [SerializeField] private Transform _headVisualTransform;
        [SerializeField] private Transform _leftHandVisualTransform;
        [SerializeField] private Transform _rightHandVisualTransform;

        [Header("Hand Sync Components")]
        [SerializeField] private NetworkHandSync _leftHandSync;
        [SerializeField] private NetworkHandSync _rightHandSync;

        [Header("Grabber Components")]
        [SerializeField] private NetworkGrabber _leftGrabber;
        [SerializeField] private NetworkGrabber _rightGrabber;

        private PlayerSyncHelper  _playerSyncHelper;
        private Transform _sharedAnchorTransform;
        private ChangeDetector _changeDetector;

        #region NetworkBehaviour Overrides
        
        public override void Spawned()
        {
            base.Spawned();

            Debug.Log($"{this.name} : Spawned - Player {Object.InputAuthority.PlayerId} HasInputAuthority: {Object.HasInputAuthority}");

            // ローカルプレイヤーの処理
            if (Object.HasInputAuthority)
            {
                // XR Originで設定されたビジュアルを利用するため、Prefabのビジュアルは非表示にする
                _headVisualTransform.gameObject.SetActive(false);
                _leftHandVisualTransform.gameObject.SetActive(false);
                _rightHandVisualTransform.gameObject.SetActive(false);

                // NetworkGrabberをConnectionManagerに登録
                ConnectionManager.instance.RegisterLocalGrabber(_leftGrabber, _rightGrabber);
            }

            // ネットワークオブジェクトの状態変更を検出するChangeDetectorを取得
            _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

            // SharedAnchor を取得
            if (_sharedAnchorTransform == null && ConnectionManager.instance != null)
            {
                _sharedAnchorTransform = ConnectionManager.instance.SharedAnchorTransform;
            }
            
        }

        public override void FixedUpdateNetwork()
        {
            if (GetInput(out NetworkInputData data))
            {
                PlayAreaPosition = data.playAreaPosition;
                PlayAreaRotation = data.playAreaRotation;
                HeadPosition = data.headPosition;
                HeadRotation = data.headRotation;
                LeftHandPosition = data.leftHandPosition;
                LeftHandRotation = data.leftHandRotation;
                RightHandPosition = data.rightHandPosition;
                RightHandRotation = data.rightHandRotation;

                // 指関節データをNetworkHandSyncに送信
                if (_leftHandSync != null)
                {
                    _leftHandSync.UpdateFromInput(data.leftHandJointData.isTracked, data.leftHandJointData.GetJoints());
                }

                if (_rightHandSync != null)
                {
                    _rightHandSync.UpdateFromInput(data.rightHandJointData.isTracked, data.rightHandJointData.GetJoints());
                }

                // グラブ情報をNetworkGrabberに設定
                if (_leftGrabber != null)
                {
                    _leftGrabber.GrabInfo = data.leftHandGrabInfo;
                }
                if (_rightGrabber != null)
                {
                    _rightGrabber.GrabInfo = data.rightHandGrabInfo;
                }
            }
        }
        
        /// <summary>
        /// ネットワーク状態の変更を検出し、必要に応じてTransformを更新
        /// </summary>
        public override void Render()
        {
            if (_sharedAnchorTransform == null) return;

            foreach (string propertyName in _changeDetector.DetectChanges(this, out NetworkBehaviourBuffer previous, out NetworkBehaviourBuffer current))
            {
                if (propertyName == nameof(PlayAreaPosition) || propertyName == nameof(PlayAreaRotation) ||
                    propertyName == nameof(HeadPosition) || propertyName == nameof(HeadRotation) ||
                    propertyName == nameof(LeftHandPosition) || propertyName == nameof(LeftHandRotation) ||
                    propertyName == nameof(RightHandPosition) || propertyName == nameof(RightHandRotation))
                {
                    UpdateTransforms();
                    
                    // 複数のプロパティが同時に変更されることがあるため、最初の変更検出で更新を行いループを抜ける
                    break;
                }
            }
        }
        
        #endregion

        #region Private Methods
        
        private void UpdateTransforms()
        {
            transform.position = _sharedAnchorTransform.TransformPoint(PlayAreaPosition);
            transform.rotation = _sharedAnchorTransform.rotation * PlayAreaRotation;
            _headTrackingTransform.position = _sharedAnchorTransform.TransformPoint(HeadPosition);
            _headTrackingTransform.rotation = _sharedAnchorTransform.rotation * HeadRotation;
            _leftHandTrackingTransform.position = _sharedAnchorTransform.TransformPoint(LeftHandPosition);
            _leftHandTrackingTransform.rotation = _sharedAnchorTransform.rotation * LeftHandRotation;
            _rightHandTrackingTransform.position = _sharedAnchorTransform.TransformPoint(RightHandPosition);
            _rightHandTrackingTransform.rotation = _sharedAnchorTransform.rotation * RightHandRotation;
        }
        
        #endregion
    }
}
