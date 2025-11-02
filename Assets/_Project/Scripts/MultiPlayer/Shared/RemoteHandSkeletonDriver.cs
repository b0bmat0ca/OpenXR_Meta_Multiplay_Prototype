using UnityEngine;
using UnityEngine.XR.Hands;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace b0bmat0ca.OpenXr.Multiplayer.Shared
{
    /// <summary>
    /// ネットワークで受信した回転データから手のスケルトンを再現するクラス
    /// </summary>
    public class RemoteHandSkeletonDriver : XRHandSkeletonDriver
    {
        [Header("Parent Anchor Linkage")]
        [SerializeField] private bool _forceWristPoseToIdentity = true;

        [Header("Default Bone Poses")]
        [SerializeField] private Pose[] _defaultBonePoses;

        /// <summary>
        /// 指定した関節IDの配列で回転データを適用
        /// </summary>
        public void ApplyJointRotations(Quaternion[] rotations)
        {
            if (!m_JointLocalPoses.IsCreated) return;
            if (_defaultBonePoses == null || _defaultBonePoses.Length != m_JointLocalPoses.Length)
            {
                Debug.LogError($"{this.name}: Default bone poses are not properly set. Please capture default poses first.");
                return;
            }

            // m_JointTransformReferencesの順番とrotationsの順番が一致している前提
            for (int i = 0; i < m_JointLocalPoses.Length; i++)
            {
                m_JointLocalPoses[i] = new Pose
                {
                    position = _defaultBonePoses[i].position, 
                    rotation = rotations[i]
                };
            }

            // 手首をidentityに設定
            if (_forceWristPoseToIdentity)
            {
                int wristIndex = XRHandJointID.Wrist.ToIndex();
                if (wristIndex >= 0 && wristIndex < m_JointLocalPoses.Length)
                {
                    m_JointLocalPoses[wristIndex] = Pose.identity;
                }
            }

            // Unity標準の適用メソッドを呼び出し
            ApplyUpdatedTransformPoses();
        }

        /// <summary>
        /// 手のルートポーズが更新されたときに呼び出される
        /// NetworkInputDataを利用して更新しているので、呼び出されることはない
        /// </summary>
        protected override void OnRootPoseUpdated(Pose rootPose)
        {
            Debug.Log($"{this.name}: OnRootPoseUpdated({rootPose})");
        }

#if UNITY_EDITOR
        [Space]
        [Button("Capture Default Poses")]
        [SerializeField] private bool _captureButton;
        
        /// <summary>
        /// Joint Transform Referencesからデフォルトの関節ポーズをキャプチャして保存
        /// </summary>
        public void CaptureDefaultPoses()
        {
            if (m_JointTransformReferences != null && m_JointTransformReferences.Count > 0)
            {
                _defaultBonePoses = new Pose[XRHandJointID.EndMarker.ToIndex()];

                foreach (JointToTransformReference jointRef in m_JointTransformReferences)
                {
                    int jointIndex = jointRef.xrHandJointID.ToIndex();
                    if (jointIndex >= 0 && jointIndex < _defaultBonePoses.Length)
                    {
                        _defaultBonePoses[jointIndex] = new Pose
                        {
                            position = jointRef.jointTransform.localPosition,
                            rotation = jointRef.jointTransform.localRotation
                        };
                    }
                }
                
                Debug.Log($"Captured {_defaultBonePoses.Length} default bone poses from Joint Transform References");
                EditorUtility.SetDirty(this);
            }
            else
            {
                Debug.LogWarning("Joint Transform References is not available. Make sure to assign joint transforms first.");
            }
        }
#endif
    }

#if UNITY_EDITOR
    public class ButtonAttribute : PropertyAttribute
    {
        public string buttonText;
        public ButtonAttribute(string text) { buttonText = text; }
    }

    [CustomPropertyDrawer(typeof(ButtonAttribute))]
    public class ButtonPropertyDrawer : UnityEditor.PropertyDrawer
    {
        public override void OnGUI(Rect position, UnityEditor.SerializedProperty property, GUIContent label)
        {
            ButtonAttribute buttonAttribute = (ButtonAttribute)attribute;
            if (GUI.Button(position, buttonAttribute.buttonText))
            {
                RemoteHandSkeletonDriver target = (RemoteHandSkeletonDriver)property.serializedObject.targetObject;
                target.CaptureDefaultPoses();
            }
        }
    }
#endif
}
