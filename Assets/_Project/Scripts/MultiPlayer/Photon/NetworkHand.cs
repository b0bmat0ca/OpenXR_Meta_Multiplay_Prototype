using Fusion;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace b0bmat0ca.OpenXr.Multiplayer.Photon
{
    using OpenXr.Player;
    
    /// <summary>
    /// ネットワーク同期される手の基点
    /// </summary>
    public class NetworkHand : NetworkBehaviour
    {
        /// <summary>
        /// ローカルプレーヤーの判定
        /// </summary>
        public bool IsLocalPlayer
        {
            get
            {
                NetworkObject playerNetworkObject = GetComponentInParent<NetworkObject>();
                return playerNetworkObject.HasInputAuthority;
            }
        }

        /// <summary>
        /// NearFarInteractor参照
        /// Grab 操作に使用
        /// </summary>
        public NearFarInteractor Interactor {get; private set;}

        #region NetworkBehaviour Overrides
        
        public override void Spawned()
        {
            base.Spawned();
            
            if (IsLocalPlayer)
            {
                // NearFarInteractorを取得
                PlayerSyncHelper playerSyncHelper = FindFirstObjectByType<PlayerSyncHelper>();
                
                if (this.name == "LeftHand")
                {
                    Interactor = playerSyncHelper.leftHandInteractor;
                }
                else if (this.name == "RightHand")
                {
                    Interactor = playerSyncHelper.rightHandInteractor;
                }
                else
                {
                    Debug.LogError($"{this.GetType().Name} : Spawned : 配置された NetworkHand オブジェクトの名前が不正です。'LeftHand' または 'RightHand' にしてください。");
                }
            }
        }
        
        #endregion
    }
}
