using Fusion;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace b0bmat0ca.OpenXr.Multiplayer.Photon
{
    using Data;
    
    /// <summary>
    /// NearFarInteractor からのイベントを GrabInfo に変換
    /// </summary>
    [RequireComponent(typeof(NetworkHand))]
    public class NetworkGrabber : NetworkBehaviour
    {
        /// <summary>
        /// ローカルプレイヤーの GrabInfo（OnInputで読み取られる）
        /// </summary>
        public GrabInfo LocalGrabInfo => _localGrabInfo;

        [Networked] public GrabInfo GrabInfo { get; set; }
        
        private GrabInfo _localGrabInfo;
        private NetworkHand _hand;
        private NetworkGrabbable _grabbedObject;
        private ChangeDetector _changeDetector;

        // 速度トラッキング用
        private const int VelocityBufferSize = 5;
        private Vector3[] _velocityBuffer = new Vector3[VelocityBufferSize];
        private Vector3[] _angularVelocityBuffer = new Vector3[VelocityBufferSize];
        private int _velocityIndex = 0;
        private Vector3 _lastPosition;
        private Quaternion _lastRotation;

        #region Unity Lifecycle
        
        private void Update()
        {
            if (_hand != null && _hand.IsLocalPlayer)
            {
                UpdateVelocityTracking();
            }
        }
        
        #endregion
        
        #region Velocity Tracking

        /// <summary>
        /// 手の速度と角速度をトラッキング
        /// </summary>
        private void UpdateVelocityTracking()
        {
            float deltaTime = Time.deltaTime;
            if (deltaTime <= 0) return;

            // 速度を計算
            Vector3 velocity = (transform.position - _lastPosition) / deltaTime;
            _velocityBuffer[_velocityIndex] = velocity;

            // 角速度を計算
            Quaternion rotationDelta = transform.rotation * Quaternion.Inverse(_lastRotation);
            rotationDelta.ToAngleAxis(out float angle, out Vector3 axis);
            if (angle > 180f) angle -= 360f;

            Vector3 angularVelocity = axis * (angle * Mathf.Deg2Rad / deltaTime);
            _angularVelocityBuffer[_velocityIndex] = angularVelocity;

            _velocityIndex = (_velocityIndex + 1) % VelocityBufferSize;

            // 現在値を保存
            _lastPosition = transform.position;
            _lastRotation = transform.rotation;
        }

        /// <summary>
        /// 平均速度を取得（投げる動作用）
        /// </summary>
        private Vector3 GetAverageVelocity()
        {
            Vector3 sum = Vector3.zero;
            for (int i = 0; i < VelocityBufferSize; i++)
            {
                sum += _velocityBuffer[i];
            }
            return sum / VelocityBufferSize;
        }

        /// <summary>
        /// 平均角速度を取得（投げる動作用）
        /// </summary>
        private Vector3 GetAverageAngularVelocity()
        {
            Vector3 sum = Vector3.zero;
            for (int i = 0; i < VelocityBufferSize; i++)
            {
                sum += _angularVelocityBuffer[i];
            }
            return sum / VelocityBufferSize;
        }

        #endregion

        #region NetworkBehaviour Overrides
        
        public override void Spawned()
        {
            base.Spawned();

            _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

            _hand = GetComponent<NetworkHand>();

            // 速度トラッキング初期化
            _lastPosition = transform.position;
            _lastRotation = transform.rotation;

            if (!_hand.IsLocalPlayer) return;

            _hand.Interactor.selectEntered.AddListener(OnSelectEntered);
            _hand.Interactor.selectExited.AddListener(OnSelectExited);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (_hand.IsLocalPlayer)
            {
                _hand.Interactor.selectEntered.RemoveListener(OnSelectEntered);
                _hand.Interactor.selectExited.RemoveListener(OnSelectExited);
            }

            base.Despawned(runner, hasState);
        }
        
        public override void FixedUpdateNetwork()
        {
            base.FixedUpdateNetwork();

            // Forwardティックのみで処理（リワインド時の重複実行を防ぐ）
            if (Runner.IsForward)
            {
                foreach (string changedPropertyName in _changeDetector.DetectChanges(this))
                {
                    if (changedPropertyName == nameof(GrabInfo))
                    {
                        HandleGrabInfoChange(GrabInfo);
                    }
                }
            }
        }

        #endregion
        
        #region XR Interaction Toolkit Events

        /// <summary>
        /// オブジェクトを掴んだ時（ローカルプレイヤーのみ）
        /// </summary>
        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            // NetworkGrabbableコンポーネントを取得
            NetworkGrabbable grabbable = args.interactableObject.transform.GetComponent<NetworkGrabbable>();
            if (grabbable == null)
            {
                Debug.LogWarning($"{this.GetType().Name} : SelectEntered: NetworkGrabbableコンポーネントがありません - Object: {args.interactableObject.transform.name}");
                return;
            }

            // ローカルオフセットを計算
            Vector3 localPositionOffset = transform.InverseTransformPoint(grabbable.transform.position);
            Quaternion localRotationOffset = Quaternion.Inverse(transform.rotation) * grabbable.transform.rotation;

            // Grab 情報を設定
            _localGrabInfo = new GrabInfo
            {
                grabbedObjectId = grabbable.Id,
                localPositionOffset = localPositionOffset,
                localRotationOffset = localRotationOffset,
                ungrabPosition = Vector3.zero,
                ungrabRotation = Quaternion.identity
            };

            Debug.Log($"NetworkGrabber.OnSelectEntered: LocalGrabInfo更新 - ObjectId: {grabbable.Id}, IsLocalNetworkRig: {_hand.IsLocalPlayer}");
        }

        /// <summary>
        /// オブジェクトを離した時（ローカルプレイヤーのみ）
        /// </summary>
        private void OnSelectExited(SelectExitEventArgs args)
        {
            NetworkGrabbable grabbable = args.interactableObject.transform.GetComponent<NetworkGrabbable>();
            if (grabbable == null) return;

            // ローカルアングラブ情報を設定
            _localGrabInfo = new GrabInfo
            {
                grabbedObjectId = NetworkBehaviourId.None,
                ungrabPosition = grabbable.transform.position,
                ungrabRotation = grabbable.transform.rotation,
                ungrabVelocity = GetAverageVelocity(),
                ungrabAngularVelocity = GetAverageAngularVelocity()
            };

            Debug.Log($"{this.GetType().Name} : OnSelectExited: LocalGrabInfo更新 - Position: {_localGrabInfo.ungrabPosition}, Velocity: {_localGrabInfo.ungrabVelocity}");
        }

        #endregion
        
        #region Private Methods

        /// <summary>
        /// GrabInfo変更時の処理
        /// </summary>
        private void HandleGrabInfoChange(GrabInfo newGrabInfo)
        {
            // 既存のオブジェクトを離す
            if (_grabbedObject != null)
            {
                _grabbedObject.Ungrab(this, newGrabInfo);
                _grabbedObject = null;
            }

            // 新しいオブジェクトを掴む
            if (newGrabInfo.grabbedObjectId != NetworkBehaviourId.None && Runner.TryFindBehaviour(newGrabInfo.grabbedObjectId, out NetworkGrabbable newGrabbedObject))
            {
                _grabbedObject = newGrabbedObject;

                if (_grabbedObject != null)
                {
                    _grabbedObject.Grab(this, newGrabInfo);
                }
            }
        }

        #endregion
    }
}
