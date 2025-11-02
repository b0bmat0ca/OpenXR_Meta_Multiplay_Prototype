using System;
using Fusion;
using UnityEngine;

namespace b0bmat0ca.OpenXr.Multiplayer.Photon
{
    using Data;
    
    /// <summary>
    /// 掴むことが可能オブジェクトの基底クラス
    /// NetworkGrabber によって、掴んだり離されたりする
    /// </summary>
    public abstract class NetworkGrabbable : NetworkBehaviour
    {
        #region Networked Properties
        
        /// <summary>
        /// 共有アンカー基準のローカル位置
        /// </summary>
        [Networked] public Vector3 LocalPosition { get; set; }

        /// <summary>
        /// 共有アンカー基準のローカル回転
        /// </summary>
        [Networked] public Quaternion LocalRotation { get; set; }
        
        /// <summary>
        /// 現在掴んでいる NetworkGrabber
        /// </summary>
        [Networked] public virtual NetworkGrabber CurrentGrabber { get; set; }
        
        #endregion

        /// <summary>
        /// 掴んでいるか
        /// </summary>
        public bool IsGrabbed => CurrentGrabber != null;

        /// <summary>
        /// 掴んだイベント
        /// </summary>
        public event Action<NetworkGrabber> OnDidGrab;
        
        /// <summary>
        /// 離したイベント
        /// </summary>
        public event Action OnDidUngrab;

        protected ChangeDetector _changeDetector;
        protected Transform _sharedAnchorTransform;

        #region Public Methods

        /// <summary>
        /// 掴む処理（NetworkGrabber から呼ばれる）
        /// </summary>
        public abstract void Grab(NetworkGrabber newGrabber, GrabInfo grabInfo);

        /// <summary>
        /// 離す処理（NetworkGrabber から呼ばれる）
        /// </summary>
        public abstract void Ungrab(NetworkGrabber grabber, GrabInfo grabInfo);
        
        #endregion
        
        #region Protected Methods
        
        /// <summary>
        /// 掴んだ処理
        /// </summary>
        protected virtual void DidGrab()
        {
            OnDidGrab?.Invoke(CurrentGrabber);
        }

        /// <summary>
        /// 離した処理
        /// </summary>
        protected virtual void DidUngrab(NetworkGrabber lastGrabber)
        {
            OnDidUngrab?.Invoke();
        }

        /// <summary>
        /// CurrentGrabber変更時の処理
        /// </summary>
        protected virtual void OnCurrentGrabberChanged()
        {
            
        }
        
        #endregion

        #region NetworkBehaviour Overrides
        
        public override void Spawned()
        {
            base.Spawned();
            _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

            // 共有アンカーを取得
            if (_sharedAnchorTransform == null && ConnectionManager.instance != null)
            {
                _sharedAnchorTransform = ConnectionManager.instance.SharedAnchorTransform;
            }
            
            // 初期位置をローカル座標に変換
            if (Object.HasStateAuthority && _sharedAnchorTransform != null)
            {
                LocalPosition = _sharedAnchorTransform.InverseTransformPoint(transform.position);
                LocalRotation = Quaternion.Inverse(_sharedAnchorTransform.rotation) * transform.rotation;
            }
        }

        public override void FixedUpdateNetwork()
        {
            base.FixedUpdateNetwork();

            // CurrentGrabber変更を検出
            foreach (string changedPropertyName in _changeDetector.DetectChanges(this))
            {
                if (changedPropertyName == nameof(CurrentGrabber))
                {
                    OnCurrentGrabberChanged();
                }
            }
        }
        
        #endregion
    }
}
