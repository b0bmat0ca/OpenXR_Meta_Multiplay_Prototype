using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace b0bmat0ca.OpenXr.Player
{
    /// <summary>
    /// XR Origin のハードウェア情報へのアクセスを提供するヘルパークラス
    /// </summary>
    public class PlayerSyncHelper : MonoBehaviour
    {
        public bool IsInitialized => _isInitialized;
        public bool SyncFingerJoint => _syncFingerJoint;
        
        [Header("Target Transforms")]
        public Transform headTargetTransform;
        public Transform leftHandTargetTransform;
        public Transform rightHandTargetTransform;
        
        [Header("Hand Skeleton Drivers")]
        [SerializeField] private XRHandSkeletonDriver _leftHandSkeletonDriver;
        [SerializeField] private XRHandSkeletonDriver _rightHandSkeletonDriver;

        [Header("Interactors")]
        public NearFarInteractor leftHandInteractor;
        public NearFarInteractor rightHandInteractor;
        
        // 指関節データを同期するかどうか
        [SerializeField] private bool _syncFingerJoint = true;
        
        // XR Hand Subsystem
        private XRHandSubsystem _handSubsystem;
        private static readonly List<XRHandSubsystem> _subsystemsReuse = new List<XRHandSubsystem>();
        
        // キャッシュ用配列（26関節）
        private Quaternion[] _leftHandJointRotationsCache = new Quaternion[26];
        private Quaternion[] _rightHandJointRotationsCache = new Quaternion[26];

        private bool _isInitialized = false;

        #region Unity Lifecycle
        
        private async void Start()
        {
            await InitializeHandSubsystemAsync();
        }
        
        #endregion
        
        #region Initialization
        
        /// <summary>
        /// XRHandSubsystemを初期化
        /// </summary>
        private async UniTask InitializeHandSubsystemAsync()
        {
            await UniTask.WaitUntil(() =>
            {
                _subsystemsReuse.Clear();
                SubsystemManager.GetSubsystems(_subsystemsReuse);
                return _subsystemsReuse.Count > 0;
            });
            
            _handSubsystem = _subsystemsReuse[0];
            _isInitialized = true;
            
            Debug.Log($"{this.GetType().Name}: InitializeHandSubsystem - Found XRHandSubsystem");
        }

        #endregion
        
        #region Finger Joint Data Getters
        
        /// <summary>
        /// 左手指関節データを取得
        /// </summary>
        public bool TryGetLeftHandJoints(out Quaternion[] jointRotations, out bool isTracked)
        {
            isTracked = _isInitialized && _handSubsystem.leftHand.isTracked;
            jointRotations = _leftHandJointRotationsCache;

            if (!isTracked)
            {
                return false;
            }

            return TryGetHandJointsFromDriver(_leftHandSkeletonDriver, _leftHandJointRotationsCache);
        }

        /// <summary>
        /// 右手指関節データを取得
        /// </summary>
        public bool TryGetRightHandJoints(out Quaternion[] jointRotations, out bool isTracked)
        {
            isTracked = _isInitialized && _handSubsystem.rightHand.isTracked;
            jointRotations = _rightHandJointRotationsCache;

            if (!isTracked)
            {
                return false;
            }

            return TryGetHandJointsFromDriver(_rightHandSkeletonDriver, _rightHandJointRotationsCache);
        }

        /// <summary>
        /// XRHandSkeletonDriverからローカル座標系の関節回転データを取得
        /// </summary>
        private bool TryGetHandJointsFromDriver(XRHandSkeletonDriver handSkeletonDriver, Quaternion[] cache)
        {
            if (handSkeletonDriver == null) return false;

            List<JointToTransformReference> jointTransformReferences = handSkeletonDriver.jointTransformReferences;
            if (jointTransformReferences == null || jointTransformReferences.Count == 0) return false;

            // キャッシュをリセット
            for (int i = 0; i < cache.Length; i++)
            {
                cache[i] = Quaternion.identity;
            }

            // XRHandSkeletonDriverのjointTransformReferencesからローカル回転を取得
            foreach (JointToTransformReference jointRef in jointTransformReferences)
            {
                int jointIndex = jointRef.xrHandJointID.ToIndex();

                if (jointIndex >= 0 && jointIndex < cache.Length)
                {
                    Transform jointTransform = jointRef.jointTransform;
                    if (jointTransform != null)
                    {
                        cache[jointIndex] = jointTransform.localRotation;
                    }
                }
            }

            return true;
        }
        
        #endregion
    }
}
