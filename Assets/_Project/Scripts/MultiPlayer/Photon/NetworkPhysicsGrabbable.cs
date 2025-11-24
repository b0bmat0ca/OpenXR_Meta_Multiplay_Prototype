using System.Collections.Generic;
using Fusion.Addons.Physics;
using UnityEngine;

namespace b0bmat0ca.OpenXr.Multiplayer.Photon
{
    using Data;

    /// <summary>
    /// 物理挙動対応のネットワークグラブ可能オブジェクト
    /// Photon Fusion 2 のホストモードにおける技術サンプルを参考に実装
    /// https://doc.photonengine.com/ja-jp/fusion/current/technical-samples/fusion-vr-host
    /// </summary>
    [RequireComponent(typeof(PhysicsGrabbable))]
    [RequireComponent(typeof(NetworkRigidbody3D))]
    public class NetworkPhysicsGrabbable : NetworkGrabbable
    {
        /// <summary>
        /// 位置履歴データ構造体
        /// </summary>
        private struct Localization
        {
            public float time;
            public Vector3 position;
            public Quaternion rotation;
        }

        private PhysicsGrabbable _physicsGrabbable;
        private NetworkRigidbody3D _networkRigidbody;

        // グラブ情報のキャッシュ
        private Vector3 _localPositionOffset;
        private Quaternion _localRotationOffset;

        // RemoteRenderTime補間用の位置履歴
        private List<Localization> _lastLocalizations = new List<Localization>();
        private const int MAX_LOCALIZATION_HISTORY = 20;

        #region Unity Lifecycle

        private void Awake()
        {
            _physicsGrabbable = GetComponent<PhysicsGrabbable>();
            _networkRigidbody = GetComponent<NetworkRigidbody3D>();
        }

        #endregion

        #region NetworkGrabbable

        public override void Grab(NetworkGrabber newGrabber, GrabInfo grabInfo)
        {
            // GrabInfoからオフセットを毎回更新
            _localPositionOffset = grabInfo.localPositionOffset;
            _localRotationOffset = grabInfo.localRotationOffset;

            CurrentGrabber = newGrabber;

            Debug.Log($"{this.GetType().Name} : Grab: Object: {name}, Grabber: {newGrabber.name}, Offset: {_localPositionOffset}, FollowMode: {_physicsGrabbable.followMode}");

            DidGrab();
        }

        public override void Ungrab(NetworkGrabber grabber, GrabInfo grabInfo)
        {
            if (CurrentGrabber != grabber) return;

            NetworkGrabber lastGrabber = CurrentGrabber;
            CurrentGrabber = null;

            // 物理を通常状態に戻す
            _physicsGrabbable.ReleasePhysics();

            // リリース速度を適用（物理的な投げ動作）
            if (Object.HasInputAuthority)
            {
                _physicsGrabbable.rigidBody.linearVelocity = grabInfo.ungrabVelocity;
                _physicsGrabbable.rigidBody.angularVelocity = grabInfo.ungrabAngularVelocity;

                Debug.Log($"{this.GetType().Name} : Ungrab: 速度適用 - Velocity: {grabInfo.ungrabVelocity}");
            }

            DidUngrab(lastGrabber);
        }

        #endregion

        #region NetworkBehaviour Overrides

        public override void Spawned()
        {
            base.Spawned();

            // プロキシでも物理シミュレーションを有効化
            // InputAuthorityを持つクライアントがローカルで物理演算できるようにする
            Runner.SetIsSimulated(Object, true);
        }

        public override void FixedUpdateNetwork()
        {
            base.FixedUpdateNetwork();

            // ホストのみが位置更新を行う
            if (!Object.HasStateAuthority) return;

            if (_sharedAnchorTransform == null) return;

            // RemoteRenderTime補間用の位置履歴を保存
            if (Runner.IsFirstTick && Runner.IsForward == false)
            {
                _lastLocalizations.Add(new Localization
                {
                    time = Runner.SimulationTime,
                    position = transform.position,
                    rotation = transform.rotation
                });

                // 最大20フレーム分を保持
                while (_lastLocalizations.Count > MAX_LOCALIZATION_HISTORY)
                {
                    _lastLocalizations.RemoveAt(0);
                }
            }

            if (IsGrabbed && CurrentGrabber != null)
            {
                // 手の位置に追従
                _physicsGrabbable.Follow(CurrentGrabber.transform, _localPositionOffset, _localRotationOffset, Runner.DeltaTime);
            }

            // 現在のワールド座標を共有アンカー基準のローカル座標に変換して同期
            LocalPosition = _sharedAnchorTransform.InverseTransformPoint(transform.position);
            LocalRotation = Quaternion.Inverse(_sharedAnchorTransform.rotation) * transform.rotation;
        }

        public override void Render()
        {
            if (_sharedAnchorTransform == null) return;

            // リモートユーザーが掴んでいる場合、RemoteRenderTimeで補間
            if (CurrentGrabber != null && CurrentGrabber.HasInputAuthority == false)
            {
                // RemoteRenderTime補間
                if (_lastLocalizations.Count >= 2)
                {
                    Localization from = default;
                    Localization to = default;
                    bool fromFound = false;
                    bool toFound = false;
                    float targetTime = Runner.RemoteRenderTime;

                    // 履歴から補間用の2点を探す
                    foreach (Localization loc in _lastLocalizations)
                    {
                        if (loc.time < targetTime)
                        {
                            fromFound = true;
                            from = loc;
                        }
                        else
                        {
                            to = loc;
                            toFound = true;
                            break;
                        }
                    }

                    // 2点間を補間してInterpolationTargetに設定
                    if (fromFound && toFound && _networkRigidbody.InterpolationTarget != null)
                    {
                        float alpha = Mathf.Clamp01((targetTime - from.time) / (to.time - from.time));
                        _networkRigidbody.InterpolationTarget.transform.position = Vector3.Lerp(from.position, to.position, alpha);
                        _networkRigidbody.InterpolationTarget.transform.rotation = Quaternion.Slerp(from.rotation, to.rotation, alpha);
                    }
                }
            }
            // ローカルプレイヤーが掴んでいる場合、視覚的な追従
            else if (IsGrabbed && CurrentGrabber != null && Object.HasInputAuthority)
            {
                _physicsGrabbable.Follow(CurrentGrabber.transform, _localPositionOffset, _localRotationOffset, Time.deltaTime);
            }
            // 掴んでいない通常状態：共有アンカー基準のローカル座標をワールド座標に変換
            else
            {
                transform.position = _sharedAnchorTransform.TransformPoint(LocalPosition);
                transform.rotation = _sharedAnchorTransform.rotation * LocalRotation;
            }
        }

        #endregion

        #region Protected Methods
        
        protected override void OnCurrentGrabberChanged()
        {
            base.OnCurrentGrabberChanged();

            // InputAuthorityを管理
            HandleGrabberChange();
        }

        #endregion
        
        #region Private Methods

        /// <summary>
        /// CurrentGrabber 変更時の処理
        /// InputAuthority を掴んだプレイヤーに転送
        /// </summary>
        private void HandleGrabberChange()
        {
            if (!Object.HasStateAuthority) return;

            if (CurrentGrabber != null)
            {
                // 新しいグラバーにInputAuthorityを転送（既に持っている場合はスキップ）
                if (Object.InputAuthority != CurrentGrabber.Object.InputAuthority)
                {
                    Object.AssignInputAuthority(CurrentGrabber.Object.InputAuthority);
                    Debug.Log($"{this.GetType().Name} : HandleGrabberChange: InputAuthority転送 - Object: {name}, NewAuthority: {CurrentGrabber.Object.InputAuthority}");
                }
            }
        }

        #endregion
    }
}
