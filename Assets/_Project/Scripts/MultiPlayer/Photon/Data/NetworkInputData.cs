using Fusion;
using UnityEngine;

namespace b0bmat0ca.OpenXr.Multiplayer.Photon.Data
{
    /// <summary>
    /// プレイヤーのトラッキングと操作をまとめたネットワーク入力データ構造体
    /// </summary>
    public struct NetworkInputData : INetworkInput
    {
        // プレイエリア（XR Origin）
        public Vector3 playAreaPosition;
        public Quaternion playAreaRotation;

        // ヘッドセット（Main Camera）
        public Vector3 headPosition;
        public Quaternion headRotation;

        // 左右の手首（Wrist）
        public Vector3 leftHandPosition;
        public Quaternion leftHandRotation;
        public Vector3 rightHandPosition;
        public Quaternion rightHandRotation;

        // 左右の指関節データ
        public HandJointData leftHandJointData;
        public HandJointData rightHandJointData;

        // Grab 情報
        public GrabInfo leftHandGrabInfo;
        public GrabInfo rightHandGrabInfo;
    }
}
