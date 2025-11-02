using Fusion;

namespace b0bmat0ca.OpenXr.Multiplayer.Photon
{
    /// <summary>
    /// 共有アンカーに紐づくネットワークオブジェクト
    /// </summary>
    public class AnchoredNetworkObject : NetworkBehaviour
    {
        /// <summary>
        /// 共有オブジェクトの一意なID
        /// </summary>
        [Networked] public int SharedObjectId { get; set; }

        /// <summary>
        /// オブジェクトのタイプ（"Cube", "Sphere"など）
        /// </summary>
        [Networked] public NetworkString<_32> ObjectType { get; set; }
    }
}
