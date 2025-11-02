using Fusion;
using UnityEngine;

namespace b0bmat0ca.OpenXr.Multiplayer.Photon.Data
{
    /// <summary>
    /// ハンドトラッキングの26関節データを定義する構造体
    /// XRHandSubsystem 互換（XRHandJointID 順）
    /// </summary>
    [System.Serializable]
    public struct HandJointData : INetworkStruct
    {
        /// <summary>
        /// ハンドトラッキングが有効かどうか
        /// </summary>
        public bool isTracked;

        // 26関節の回転データ
        public Quaternion joint0;   // Palm
        public Quaternion joint1;   // Wrist
        public Quaternion joint2;   // ThumbMetacarpal
        public Quaternion joint3;   // ThumbProximal
        public Quaternion joint4;   // ThumbDistal
        public Quaternion joint5;   // ThumbTip
        public Quaternion joint6;   // IndexMetacarpal
        public Quaternion joint7;   // IndexProximal
        public Quaternion joint8;   // IndexIntermediate
        public Quaternion joint9;   // IndexDistal
        public Quaternion joint10;  // IndexTip
        public Quaternion joint11;  // MiddleMetacarpal
        public Quaternion joint12;  // MiddleProximal
        public Quaternion joint13;  // MiddleIntermediate
        public Quaternion joint14;  // MiddleDistal
        public Quaternion joint15;  // MiddleTip
        public Quaternion joint16;  // RingMetacarpal
        public Quaternion joint17;  // RingProximal
        public Quaternion joint18;  // RingIntermediate
        public Quaternion joint19;  // RingDistal
        public Quaternion joint20;  // RingTip
        public Quaternion joint21;  // LittleMetacarpal
        public Quaternion joint22;  // LittleProximal
        public Quaternion joint23;  // LittleIntermediate
        public Quaternion joint24;  // LittleDistal
        public Quaternion joint25;  // LittleTip

        /// <summary>
        /// 26関節配列を取得
        /// </summary>
        public readonly Quaternion[] GetJoints()
        {
            return new Quaternion[]
            {
                joint0, joint1, joint2, joint3, joint4, joint5, joint6, joint7, joint8, joint9,
                joint10, joint11, joint12, joint13, joint14, joint15, joint16, joint17, joint18, joint19,
                joint20, joint21, joint22, joint23, joint24, joint25
            };
        }

        /// <summary>
        /// 26関節配列から設定
        /// </summary>
        public void SetJoints(Quaternion[] joints)
        {
            if (joints.Length != 26) return;

            joint0 = joints[0]; joint1 = joints[1]; joint2 = joints[2]; joint3 = joints[3]; joint4 = joints[4];
            joint5 = joints[5]; joint6 = joints[6]; joint7 = joints[7]; joint8 = joints[8]; joint9 = joints[9];
            joint10 = joints[10]; joint11 = joints[11]; joint12 = joints[12]; joint13 = joints[13]; joint14 = joints[14];
            joint15 = joints[15]; joint16 = joints[16]; joint17 = joints[17]; joint18 = joints[18]; joint19 = joints[19];
            joint20 = joints[20]; joint21 = joints[21]; joint22 = joints[22]; joint23 = joints[23]; joint24 = joints[24];
            joint25 = joints[25];
        }

        /// <summary>
        /// 空のHandJointData（トラッキングなし）
        /// </summary>
        public static HandJointData Empty => new HandJointData { isTracked = false };
    }
}
