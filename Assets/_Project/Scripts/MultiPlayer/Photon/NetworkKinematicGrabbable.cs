using UnityEngine;

namespace b0bmat0ca.OpenXr.Multiplayer.Photon
{
    using Data;

    /// <summary>
    /// Kinematic の掴むことが可能なオブジェクト用ネットワーク同期クラス
    /// </summary>
    public class NetworkKinematicGrabbable : NetworkGrabbable
    {
        private readonly float UNGRAB_RESYNC_DURATION = 1.0f;

        // Grab 情報のキャッシュ
        private Vector3 _localPositionOffset;
        private Quaternion _localRotationOffset;
        private Vector3 _ungrabPosition;
        private Quaternion _ungrabRotation;
        private float _ungrabTime = -1f;

        #region NetworkGrabbable Overrides

        public override void Grab(NetworkGrabber newGrabber, GrabInfo grabInfo)
        {
            // GrabInfoからオフセットを毎回更新
            _localPositionOffset = grabInfo.localPositionOffset;
            _localRotationOffset = grabInfo.localRotationOffset;

            CurrentGrabber = newGrabber;

            Debug.Log($"{this.GetType().Name} : Grab: Object: {name}, Grabber: {CurrentGrabber.name}, Offset: {_localPositionOffset}, HasStateAuthority: {Object.HasStateAuthority}");

            DidGrab();
        }

        public override void Ungrab(NetworkGrabber grabber, GrabInfo grabInfo)
        {
            if (CurrentGrabber != grabber) return;

            NetworkGrabber lastGrabber = CurrentGrabber;
            CurrentGrabber = null;

            // 現在の位置を保存
            _ungrabPosition = transform.position;
            _ungrabRotation = transform.rotation;
            _ungrabTime = Time.time;

            DidUngrab(lastGrabber);
        }

        #endregion

        #region NetworkBehaviour Overrides

        public override void FixedUpdateNetwork()
        {
            base.FixedUpdateNetwork();

            // ホストのみが位置更新を行う
            if (!Object.HasStateAuthority) return;

            if (_sharedAnchorTransform == null) return;

            if (IsGrabbed && CurrentGrabber != null)
            {
                // NetworkGrabber（手）の位置に追従
                FollowGrabber(CurrentGrabber.transform);
            }

            // 現在のワールド座標を共有アンカー基準のローカル座標に変換して同期
            LocalPosition = _sharedAnchorTransform.InverseTransformPoint(transform.position);
            LocalRotation = Quaternion.Inverse(_sharedAnchorTransform.rotation) * transform.rotation;
        }

        public override void Render()
        {
            if (_sharedAnchorTransform == null) return;

            if (IsGrabbed && CurrentGrabber != null)
            {
                // 掴んでいる場合は、手に追従
                FollowGrabber(CurrentGrabber.transform);
            }
            else if (_ungrabTime != -1f)
            {
                // 離した直後の再同期期間
                if ((Time.time - _ungrabTime) < UNGRAB_RESYNC_DURATION)
                {
                    // ネットワーク補間が追いつくまで位置を固定
                    transform.position = _ungrabPosition;
                    transform.rotation = _ungrabRotation;
                }
                else
                {
                    // 通常の同期に戻す
                    _ungrabTime = -1f;
                }
            }
            else
            {
                // 共有アンカー基準のローカル座標をワールド座標に変換
                transform.position = _sharedAnchorTransform.TransformPoint(LocalPosition);
                transform.rotation = _sharedAnchorTransform.rotation * LocalRotation;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 手の位置に追従
        /// </summary>
        private void FollowGrabber(Transform grabberTransform)
        {
            if (grabberTransform == null) return;

            // ローカルオフセットを適用してワールド位置・回転を計算
            Vector3 targetPosition = grabberTransform.TransformPoint(_localPositionOffset);
            Quaternion targetRotation = grabberTransform.rotation * _localRotationOffset;

            transform.position = targetPosition;
            transform.rotation = targetRotation;
        }

        #endregion
    }
}
