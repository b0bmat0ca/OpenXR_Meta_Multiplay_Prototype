using Fusion;
using UnityEngine;

namespace b0bmat0ca.OpenXr.Multiplayer.Photon.Data
{
    /// <summary>
    /// Grab（掴む）/ UnGrab（離す）アクションの状態を定義するデータ構造体
    /// </summary>
    [System.Serializable]
    public struct GrabInfo : INetworkStruct
    {
        /// <summary>
        /// 掴んだオブジェクトの NetworkBehaviourId
        /// </summary>
        public NetworkBehaviourId grabbedObjectId;

        /// <summary>
        /// 掴んだ時のローカル位置オフセット
        /// </summary>
        public Vector3 localPositionOffset;

        /// <summary>
        /// 掴んだ時のローカル回転オフセット
        /// </summary>
        public Quaternion localRotationOffset;

        /// <summary>
        /// 離した時の正確なワールド位置
        /// </summary>
        public Vector3 ungrabPosition;

        /// <summary>
        /// 離した時の正確なワールド回転
        /// </summary>
        public Quaternion ungrabRotation;

        /// <summary>
        /// 離した時の速度
        /// </summary>
        public Vector3 ungrabVelocity;

        /// <summary>
        /// 離した時の角速度
        /// </summary>
        public Vector3 ungrabAngularVelocity;
    }
}
