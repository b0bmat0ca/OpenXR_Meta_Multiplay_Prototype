using UnityEngine;

namespace b0bmat0ca.OpenXr.Multiplayer.Photon
{
    /// <summary>
    /// 掴むことが可能な物理ベースのオブジェクトの基底クラス
    /// Photon Fusion 2 のホストモードにおける技術サンプルを参考に実装
    /// https://doc.photonengine.com/ja-jp/fusion/current/technical-samples/fusion-vr-host
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PhysicsGrabbable : MonoBehaviour
    {
        public Rigidbody rigidBody;
        
        /// <summary>
        /// オブジェクトが手に追従する方法
        /// </summary>
        public enum FollowMode
        {
            Velocity,   // 速度ベース
            Kinematic   // Kinematic変換（グラブ中はKinematic化）
        }

        [Header("Follow Configuration")]
        public FollowMode followMode = FollowMode.Velocity;

        [Header("Velocity Mode Settings"), Range(0f, 1f)]
        public float velocityAttenuation = 0.5f;

        public float maxVelocity = 10f;

        // Kinematicモード時の速度シミュレーション用
        private const int VelocityBufferSize = 5;
        private Vector3[] _lastMoves = new Vector3[VelocityBufferSize];
        private Vector3[] _lastAngularVelocities = new Vector3[VelocityBufferSize];
        private float[] _lastDeltaTime = new float[VelocityBufferSize];
        private int _lastMoveIndex = 0;
        private Vector3 _lastPosition;
        private Quaternion _previousRotation;

        #region Unity Lifecycle

        protected virtual void Awake()
        {
            rigidBody = GetComponent<Rigidbody>();
            rigidBody.isKinematic = false;
            
            _lastPosition = transform.position;
            _previousRotation = transform.rotation;
        }

        protected virtual void Update()
        {
            // Kinematic モード時の速度トラッキング
            if (followMode == FollowMode.Kinematic)
            {
                UpdateSimulatedVelocities();
            }
        }

        #endregion

        #region Follow Logic

        /// <summary>
        /// 指定されたTransformに追従する
        /// </summary>
        public virtual void Follow(Transform targetTransform, Vector3 localPositionOffset, Quaternion localRotationOffset, float deltaTime)
        {
            if (followMode == FollowMode.Kinematic)
            {
                KinematicFollow(targetTransform, localPositionOffset, localRotationOffset);
            }
            else if (followMode == FollowMode.Velocity)
            {
                VelocityFollow(targetTransform, localPositionOffset, localRotationOffset, deltaTime);
            }
        }

        /// <summary>
        /// Kinematicモード: 直接Transform更新
        /// </summary>
        protected virtual void KinematicFollow(Transform targetTransform, Vector3 localPositionOffset, Quaternion localRotationOffset)
        {
            // Kinematicに切り替え
            if (!rigidBody.isKinematic)
            {
                rigidBody.isKinematic = true;
            }

            // 直接位置・回転を設定
            transform.position = targetTransform.TransformPoint(localPositionOffset);
            transform.rotation = targetTransform.rotation * localRotationOffset;
        }

        /// <summary>
        /// Velocityモード: 速度ベースで追従
        /// </summary>
        protected virtual void VelocityFollow(Transform targetTransform, Vector3 localPositionOffset, Quaternion localRotationOffset, float deltaTime)
        {
            // Kinematicを解除
            if (rigidBody.isKinematic)
            {
                rigidBody.isKinematic = false;
            }

            // 目標位置・回転を計算
            Vector3 targetPosition = targetTransform.TransformPoint(localPositionOffset);
            Quaternion targetRotation = targetTransform.rotation * localRotationOffset;

            // 位置: 速度で追従
            if (deltaTime > 0)
            {
                Vector3 requiredVelocity = (targetPosition - rigidBody.position) / deltaTime;
                requiredVelocity *= velocityAttenuation;
                requiredVelocity = Vector3.ClampMagnitude(requiredVelocity, maxVelocity);
                rigidBody.linearVelocity = requiredVelocity;
            }

            // 回転: 角速度で追従
            Quaternion rotationDelta = targetRotation * Quaternion.Inverse(rigidBody.rotation);
            rotationDelta.ToAngleAxis(out float angle, out Vector3 axis);

            if (angle > 180f)
            {
                angle -= 360f;
            }

            if (deltaTime > 0 && angle != 0)
            {
                Vector3 angularVelocity = axis * (angle * Mathf.Deg2Rad / deltaTime);
                rigidBody.angularVelocity = angularVelocity;
            }
        }

        /// <summary>
        /// グラブ解除時にRigidbodyを通常物理に戻す
        /// </summary>
        public virtual void ReleasePhysics()
        {
            if (rigidBody.isKinematic && followMode == FollowMode.Kinematic)
            {
                rigidBody.isKinematic = false;
            }
        }

        #endregion

        #region Velocity Simulation (for Kinematic mode)

        private void UpdateSimulatedVelocities()
        {
            // 位置変化を記録
            _lastMoves[_lastMoveIndex] = transform.position - _lastPosition;

            // 角速度を記算
            float deltaTime = Time.deltaTime;
            if (deltaTime > 0)
            {
                Quaternion rotationDelta = transform.rotation * Quaternion.Inverse(_previousRotation);
                rotationDelta.ToAngleAxis(out float angle, out Vector3 axis);

                if (angle > 180f)
                {
                    angle -= 360f;
                }

                _lastAngularVelocities[_lastMoveIndex] = axis * (angle * Mathf.Deg2Rad / deltaTime);
            }
            else
            {
                _lastAngularVelocities[_lastMoveIndex] = Vector3.zero;
            }

            _lastDeltaTime[_lastMoveIndex] = deltaTime;
            _lastMoveIndex = (_lastMoveIndex + 1) % VelocityBufferSize;

            // 現在値を保存
            _lastPosition = transform.position;
            _previousRotation = transform.rotation;
        }

        #endregion
    }
}
