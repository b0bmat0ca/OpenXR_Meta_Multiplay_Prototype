using Fusion;
using UnityEngine;

namespace b0bmat0ca.OpenXr.Multiplayer.Photon
{
    using Shared;
    
    /// <summary>
    /// 指関節の回転データとトラッキング状態をネットワーク同期するクラス
    /// </summary>
    public class NetworkHandSync : NetworkBehaviour
    {
        #region Network Properties
        
        [Networked] private bool IsHandTracked { get; set; }
        
        // 26関節の回転データ
        [Networked, Capacity(26)] public NetworkArray<Quaternion> JointRotations { get; }
        
        #endregion
        
        [SerializeField] private RemoteHandSkeletonDriver _handSkeletonDriver;

        // キャッシュ用配列（26関節）
        private Quaternion[] _jointRotationsCache = new Quaternion[26];
        
        private ChangeDetector _changeDetector;

        #region NetworkBehaviour Overrides
        
        public override void Spawned()
        {
            base.Spawned();
            
            // ネットワークオブジェクトの状態変更を検出するChangeDetectorを取得
            _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
        }
        
        /// <summary>
        /// ネットワーク状態の変更を検出し、必要に応じて手のスケルトンに回転データを適用
        /// </summary>
        public override void Render()
        {
            if (!IsHandTracked) return;
            
            foreach (string propertyName in _changeDetector.DetectChanges(this, out NetworkBehaviourBuffer previous, out NetworkBehaviourBuffer current))
            {
                if (propertyName == nameof(JointRotations))
                {
                    ApplyJointRotationsToSkeleton();
                }
            }
        }
        
        #endregion

        #region Public Methods
        
        /// <summary>
        /// ローカルの手のトラッキング状態と指関節回転データを受け取り、ネットワーク状態を更新
        /// </summary>
        public void UpdateFromInput(bool isTracked, Quaternion[] jointRotations)
        {
            if (!Object.HasStateAuthority) return;

            IsHandTracked = isTracked;

            if (isTracked && jointRotations != null && jointRotations.Length == 26)
            {
                // NetworkArrayに指関節データをコピー
                for (int i = 0; i < jointRotations.Length; i++)
                {
                    JointRotations.Set(i, jointRotations[i]);
                }
            }
        }
        
        #endregion

        #region Private Methods
        
        /// <summary>
        /// 手のスケルトンに指関節の回転データを適用
        /// </summary>
        private void ApplyJointRotationsToSkeleton()
        {
            for (int i = 0; i < _jointRotationsCache.Length; i++)
            {
                _jointRotationsCache[i] = JointRotations.Get(i);
            }

            _handSkeletonDriver.ApplyJointRotations(_jointRotationsCache);
        }
        
        #endregion
    }
}
