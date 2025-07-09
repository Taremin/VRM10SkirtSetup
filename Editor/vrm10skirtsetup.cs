#if UNIVRM_EXIST
using UniVRM10;
#endif
#if VRC_SDK_EXIST
using VRC.SDK3;
using VRC.Core;
using VRC.Dynamics;
using VRC.SDKBase;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Dynamics.PhysBone.Components;
using VRC.SDK3.Dynamics.Constraint.Components;
#endif
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VRM10SkirtSetup
{
    public class VRM10SkirtSetupWindow : EditorWindow
    {
        enum TargetSystem
        {
            VRM10,
            VRChat,
        }

        static TargetSystem targetSystem = TargetSystem.VRM10;
        static string version = "v0.1.0";
        Vector2 scrollPosition; // Add this line

        static GameObject rootObject;
        static GameObject skirtRoot;
        static int skirtOffset = 1;
        static float legRadius = 0.05f;
        static string prefix = "VRM10SkirtSetup";
        static string prefixDelimiter = "_";

        static bool useConstraint = true;

        // VRM1.0 Settings
#if UNIVRM_EXIST
        static bool isJointSettingsOpen = true;
        static float jointStiffnessForce = 1.0f;
        static AnimationCurve jointStiffnessForceCurve = AnimationCurve.Constant(0.0f, 1.0f, 1.0f);
        static float jointGravityPower = 0;
        static AnimationCurve jointGravityPowerCurve = AnimationCurve.Constant(0.0f, 1.0f, 1.0f);
        static Vector3 jointGravityDir = new Vector3(0, -1.0f, 0);
        [Range(0, 1.0f)]
        static float jointDragForce = 0.4f;
        static AnimationCurve jointDragForceCurve = AnimationCurve.Constant(0.0f, 1.0f, 1.0f);
        static float jointRadius = 0.02f;
        static AnimationCurve jointRadiusCurve = AnimationCurve.Constant(0.0f, 1.0f, 1.0f);
        static bool jointDrawCollider = false;
#endif

        static bool isColliderSettingsOpen = true;
        static ColliderType colliderType = ColliderType.Plane;
        static float colliderRadius = 0.3f;
        static float colliderYOffset = 0.25f;
        static float colliderTailYOffset = 0.25f;

        // VRChat Settings
#if VRC_SDK_EXIST
        static bool isPhysBoneSettingsOpen = true;
        static VRCPhysBone tempPhysBone;
        static Editor physBoneEditor;
        static string physBoneSettingsJson; // PhysBoneの全設定をJSONで保持

        static bool isPhysBoneColliderSettingsOpen = true;
        static PhysBoneColliderType physBoneColliderType = PhysBoneColliderType.Capsule;
        static float physBoneColliderRadius = 0.0f;
        static float physBoneColliderHeight = 0.0f;
        static Vector3 physBoneColliderRotation = Vector3.zero;
#endif

        [System.Serializable]
        public class WrappedAnimationCurve
        {
            public AnimationCurve Curve;
            public WrappedAnimationCurve(AnimationCurve curve)
            {
                Curve = curve;
            }
        }

        enum ColliderType
        {
            Plane,
            Cupsule,
            CupsuleInside,
            Sphere,
            SphereInside,
        }

#if VRC_SDK_EXIST
        enum PhysBoneColliderType
        {
            Sphere,
            Capsule,
            Plane,
        }
#endif

        enum ColliderLeg
        {
            Left,
            Right,
            Both,
        }

        void OnEnable()
        {
            skirtOffset = int.Parse(EditorUserSettings.GetConfigValue(prefix + "/skirtOffset") ?? skirtOffset.ToString());
            legRadius = float.Parse(EditorUserSettings.GetConfigValue(prefix + "/legRadius") ?? legRadius.ToString());
            useConstraint = bool.Parse(EditorUserSettings.GetConfigValue(prefix + "/useConstraint") ?? useConstraint.ToString());

#if UNIVRM_EXIST
            isJointSettingsOpen = bool.Parse(EditorUserSettings.GetConfigValue(prefix + "/isJointSettingsOpen") ?? isJointSettingsOpen.ToString());
            jointStiffnessForce = float.Parse(EditorUserSettings.GetConfigValue(prefix + "/jointStiffnessForce") ?? jointStiffnessForce.ToString());
            jointStiffnessForceCurve = JsonUtility.FromJson<WrappedAnimationCurve>(EditorUserSettings.GetConfigValue(prefix + "/jointStiffnessForceCurve") ?? JsonUtility.ToJson(new WrappedAnimationCurve(jointStiffnessForceCurve))).Curve;
            jointGravityPower = float.Parse(EditorUserSettings.GetConfigValue(prefix + "/jointGravityPower") ?? jointGravityPower.ToString());
            jointGravityPowerCurve = JsonUtility.FromJson<WrappedAnimationCurve>(EditorUserSettings.GetConfigValue(prefix + "/jointGravityPowerCurve") ?? JsonUtility.ToJson(new WrappedAnimationCurve(jointGravityPowerCurve))).Curve;
            jointGravityDir = JsonUtility.FromJson<Vector3>(EditorUserSettings.GetConfigValue(prefix + "/jointGravityDir") ?? JsonUtility.ToJson(jointGravityDir));
            jointDragForce = float.Parse(EditorUserSettings.GetConfigValue(prefix + "/jointDragForce") ?? jointDragForce.ToString());
            jointDragForceCurve = JsonUtility.FromJson<WrappedAnimationCurve>(EditorUserSettings.GetConfigValue(prefix + "/jointDragForceCurve") ?? JsonUtility.ToJson(new WrappedAnimationCurve(jointDragForceCurve))).Curve;
            jointRadius = float.Parse(EditorUserSettings.GetConfigValue(prefix + "/jointRadius") ?? jointRadius.ToString());
            jointRadiusCurve = JsonUtility.FromJson<WrappedAnimationCurve>(EditorUserSettings.GetConfigValue(prefix + "/jointRadiusCurve") ?? JsonUtility.ToJson(new WrappedAnimationCurve(jointRadiusCurve))).Curve;

            isColliderSettingsOpen = bool.Parse(EditorUserSettings.GetConfigValue(prefix + "/isColliderSettingsOpen") ?? isColliderSettingsOpen.ToString());
            colliderType = (ColliderType)int.Parse(EditorUserSettings.GetConfigValue(prefix + "/colliderType") ?? ((int)colliderType).ToString());
            colliderRadius = float.Parse(EditorUserSettings.GetConfigValue(prefix + "/colliderRadius") ?? colliderRadius.ToString());
            colliderYOffset = float.Parse(EditorUserSettings.GetConfigValue(prefix + "/colliderYOffset") ?? colliderYOffset.ToString());
            colliderTailYOffset = float.Parse(EditorUserSettings.GetConfigValue(prefix + "/colliderTailYOffset") ?? colliderTailYOffset.ToString());
#endif
#if VRC_SDK_EXIST
            // 一時的なVRCPhysBoneインスタンスとEditorの初期化
            if (tempPhysBone == null)
            {
                GameObject tempGO = new GameObject("TempPhysBoneEditor");
                //tempGO.hideFlags = HideFlags.HideAndDontSave; // シーンに保存せず、ヒエラルキーにも表示しない
                tempGO.hideFlags = HideFlags.HideInHierarchy
                    | HideFlags.DontSaveInEditor
                    | HideFlags.DontSaveInBuild;
                tempPhysBone = tempGO.AddComponent<VRCPhysBone>();
            }

            if (physBoneEditor == null)
            {
                physBoneEditor = Editor.CreateEditor(tempPhysBone);
            }

            // EditorUserSettingsからPhysBoneのJSON設定を読み込み、tempPhysBoneに適用
            isPhysBoneSettingsOpen = bool.Parse(EditorUserSettings.GetConfigValue(prefix + "/isPhysBoneSettingsOpen") ?? "true");
            physBoneSettingsJson = EditorUserSettings.GetConfigValue(prefix + "/physBoneSettingsJson");
            if (!string.IsNullOrEmpty(physBoneSettingsJson))
            {
                EditorJsonUtility.FromJsonOverwrite(physBoneSettingsJson, tempPhysBone);
                EditorUtility.SetDirty(tempPhysBone); // オブジェクトが変更されたことをエディタに通知
            }

            isPhysBoneColliderSettingsOpen = bool.Parse(EditorUserSettings.GetConfigValue(prefix + "/isPhysBoneColliderSettingsOpen") ?? isPhysBoneColliderSettingsOpen.ToString());
            physBoneColliderType = (PhysBoneColliderType)int.Parse(EditorUserSettings.GetConfigValue(prefix + "/physBoneColliderType") ?? ((int)physBoneColliderType).ToString());
            physBoneColliderRadius = float.Parse(EditorUserSettings.GetConfigValue(prefix + "/physBoneColliderRadius") ?? physBoneColliderRadius.ToString());
            physBoneColliderHeight = float.Parse(EditorUserSettings.GetConfigValue(prefix + "/physBoneColliderHeight") ?? physBoneColliderHeight.ToString());
            physBoneColliderRotation = JsonUtility.FromJson<Vector3>(EditorUserSettings.GetConfigValue(prefix + "/physBoneColliderRotation") ?? JsonUtility.ToJson(physBoneColliderRotation));
#endif
        }

        void OnSelectionChange()
        {
            var editorEvent = EditorGUIUtility.CommandEvent("ChangeActiveObject");
            editorEvent.type = EventType.Used;
            SendEvent(editorEvent);
        }

        [MenuItem("GameObject/VRM10SkirtSetup", false, 20)]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow(typeof(VRM10SkirtSetupWindow));
        }

        private class UpperLegBone
        {
            public string name = "";
            public Transform start = null;
            public Transform end = null;
            public Transform colliderContainer = null;

            public UpperLegBone(string name, Transform start, Transform end, Transform colliderContainer)
            {
                this.name = name;
                this.start = start;
                this.end = end;
                this.colliderContainer = colliderContainer;
            }
        }

#if UNIVRM_EXIST
        private void SetupVRM(Animator animator, Vrm10Instance vrm10instance, GameObject skirtRoot, int skirtOffset, bool removeOnly = false)
        {
            Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            Transform upperLegLeft = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
            Transform upperLegRight = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
            Transform lowerLegLeft = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
            Transform lowerLegRight = animator.GetBoneTransform(HumanBodyBones.RightLowerLeg);

            if (!upperLegLeft || !upperLegRight || !lowerLegLeft || !lowerLegRight)
            {
                Debug.Log("Leg Bone not found");
                return;
            }

            if (!lowerLegLeft.IsChildOf(upperLegLeft))
            {
                Debug.Log("lowerLegLeftがupperLegLeftと親子関係にありません");
                return;
            }
            if (!lowerLegRight.IsChildOf(upperLegRight))
            {
                Debug.Log("lowerLegRightがupperLegRightと親子関係にありません");
                return;
            }

            var upperLegLeftEnd = lowerLegLeft;
            var upperLegRightEnd = lowerLegRight;

            while (upperLegLeft != upperLegLeftEnd.parent)
            {
                upperLegLeftEnd = upperLegLeftEnd.parent;
            }
            while (upperLegRight != upperLegRightEnd.parent)
            {
                upperLegRightEnd = upperLegRightEnd.parent;
            }

            // remove all spring bone in VRM instance
            foreach (VRM10SpringBoneColliderGroup colliderGroup in vrm10instance.SpringBone.ColliderGroups.ToList())
            {
                if (colliderGroup.Name.StartsWith(prefix + prefixDelimiter))
                {
                    vrm10instance.SpringBone.ColliderGroups.Remove(colliderGroup);
                }
            }

            // remove all spring bone in VRM instance
            foreach (Vrm10InstanceSpringBone.Spring spring in vrm10instance.SpringBone.Springs.ToList())
            {
                if (spring.Name.StartsWith(prefix + prefixDelimiter))
                {
                    vrm10instance.SpringBone.Springs.Remove(spring);
                }
            }

            // remove previous objects
            var allChildren = rootObject.transform.GetComponentsInChildren<Transform>(true);
            var prefixedObjects = allChildren.Where((_ => _.name.StartsWith(prefix + prefixDelimiter) &&
                (_.GetComponent<Vrm10AimConstraint>() != null || _.GetComponent<VRM10SpringBoneColliderGroup>() != null)
            )).ToList();
            foreach (Transform prefixedObject in prefixedObjects)
            {
                if (prefixedObject != null)
                {
                    DestroyImmediate(prefixedObject.gameObject);
                }
            }

            // remove prev joint
            RemoveSkirtJoint(skirtRoot.transform);

            if (removeOnly)
            {
                return;
            }

            var skirtTargetRootObjects = new List<GameObject>();
            GetSkirtTargetRootObjects(skirtRoot, ref skirtTargetRootObjects, skirtOffset);

            skirtTargetRootObjects.Sort(delegate (GameObject a, GameObject b)
            {
                var hipsPositionA = hips.InverseTransformPoint(a.transform.position);
                var hipsPositionB = hips.InverseTransformPoint(b.transform.position);

                var radA = System.Math.Atan2(hipsPositionA.z, hipsPositionA.x);
                var radB = System.Math.Atan2(hipsPositionB.z, hipsPositionB.x);

                if (radA > radB)
                {
                    return -1;
                }
                else if (radA < radB)
                {
                    return 1;
                }
                return 0;
            });

            // determine skirt root position
            Vector3[] targetPositions = new Vector3[skirtTargetRootObjects.Count];
            for (int i = 0; i < skirtTargetRootObjects.Count; ++i)
            {
                GameObject skirtRootObject = skirtTargetRootObjects[i];

                var leafs = GetLeafBones(skirtRootObject.transform);
                var sum = new Vector3();
                foreach (var leaf in leafs)
                {
                    sum += leaf.position;
                }
                targetPositions[i] = sum / leafs.Count;
            }

            // constraint
            var colliderLegLeft = upperLegLeft;
            var colliderLegRight = upperLegRight;
            if (useConstraint)
            {
                var name = prefix + prefixDelimiter;
                colliderLegLeft = AddEmpty(name + upperLegLeft.name, upperLegLeft.parent).transform;
                colliderLegLeft.position = upperLegLeft.position;
                colliderLegLeft.rotation = upperLegLeft.rotation;
                colliderLegLeft.localScale = upperLegLeft.localScale;

                var aimConstraintLeft = colliderLegLeft.gameObject.AddComponent<Vrm10AimConstraint>();
                aimConstraintLeft.Source = upperLegLeftEnd;
                aimConstraintLeft.AimAxis = UniGLTF.Extensions.VRMC_node_constraint.AimAxis.PositiveY;

                colliderLegRight = AddEmpty(name + upperLegRight.name, upperLegRight.parent).transform;
                colliderLegRight.position = upperLegRight.position;
                colliderLegRight.rotation = upperLegRight.rotation;
                colliderLegRight.localScale = upperLegRight.localScale;

                var aimConstraintRight = colliderLegRight.gameObject.AddComponent<Vrm10AimConstraint>();
                aimConstraintRight.Source = upperLegRightEnd;
                aimConstraintRight.AimAxis = UniGLTF.Extensions.VRMC_node_constraint.AimAxis.PositiveY;
            }

            var colliderGroupCache = new VRM10SpringBoneColliderGroup[skirtTargetRootObjects.Count, skirtTargetRootObjects.Count, 2];

            // setup start
            for (int i = 0; i < skirtTargetRootObjects.Count; ++i)
            {
                int nextIndex = (i + 1) % skirtTargetRootObjects.Count;
                int prevIndex = (i + skirtTargetRootObjects.Count - 1) % skirtTargetRootObjects.Count;
                GameObject skirtRootObject = skirtTargetRootObjects[i];
                GameObject nextSkirtRootObject = skirtTargetRootObjects[nextIndex];
                GameObject prevSkirtRootObject = skirtTargetRootObjects[prevIndex];
                Vector3 skirtRootPosition = targetPositions[i];
                Vector3 nextSkirtRootPosition = targetPositions[nextIndex];
                Vector3 prevSkirtRootPosition = targetPositions[prevIndex];

                Transform[] skirtRootChildren = GetChildren(skirtRootObject);
                var spring = new Vrm10InstanceSpringBone.Spring(prefix + prefixDelimiter + skirtRootObject.name + prefixDelimiter + i);
                vrm10instance.SpringBone.Springs.Add(spring);

                var averagePointNext = (skirtRootPosition + nextSkirtRootPosition) * 0.5f;
                var averagePointPrev = (skirtRootPosition + prevSkirtRootPosition) * 0.5f;

                // add Joint to skirt
                SetupSkirtJoint(skirtRootObject.transform, spring, 0, GetMaxDepth(skirtRootObject.transform));

                System.Action<Vrm10InstanceSpringBone.Spring, int, int, Vector3> SetupCollider = (Vrm10InstanceSpringBone.Spring spring, int skirtIndex, int adjacentIndex, Vector3 averagePoint) =>
                {
                    var skirt = skirtTargetRootObjects[skirtIndex];
                    var adjacent = skirtTargetRootObjects[adjacentIndex];

                    var allLegs = new List<UpperLegBone>
                    {
                        new UpperLegBone("Left", upperLegLeft, upperLegLeftEnd, colliderLegLeft),
                        new UpperLegBone("Right", upperLegRight, upperLegRightEnd, colliderLegRight)
                    };

                    var determinedLeg = DetermineColliderLeg(skirt.transform.position, upperLegLeft.position, upperLegRight.position);
                    var targetLegs = new List<UpperLegBone>();

                    if (determinedLeg == ColliderLeg.Left || determinedLeg == ColliderLeg.Both)
                    {
                        targetLegs.Add(allLegs[0]);
                    }
                    if (determinedLeg == ColliderLeg.Right || determinedLeg == ColliderLeg.Both)
                    {
                        targetLegs.Add(allLegs[1]);
                    }

                    foreach (var leg in targetLegs)
                    {
                        var oppositeLeg = allLegs.First(l => l.name != leg.name);

                        var name = prefix + prefixDelimiter + skirt.name + prefixDelimiter + adjacent.name + prefixDelimiter + leg.name;

                        // SpringBoneColliderGroup
                        VRM10SpringBoneColliderGroup colliderGroup = null;

                        var legIndex = leg.name == "Left" ? 0 : 1;
                        if (colliderGroupCache[skirtIndex, adjacentIndex, legIndex])
                        {
                            colliderGroup = colliderGroupCache[skirtIndex, adjacentIndex, legIndex];
                        }
                        else if (colliderGroupCache[adjacentIndex, skirtIndex, legIndex])
                        {
                            colliderGroup = colliderGroupCache[adjacentIndex, skirtIndex, legIndex];
                        }
                        else
                        {
                            // add collider to legs
                            GameObject colliderObject = AddEmpty(name, leg.colliderContainer);

                            colliderGroup = colliderObject.AddComponent<VRM10SpringBoneColliderGroup>();
                            colliderGroup.Name = name;
                            colliderGroupCache[skirtIndex, adjacentIndex, legIndex] =
                            colliderGroupCache[adjacentIndex, skirtIndex, legIndex] = colliderGroup;

                            vrm10instance.SpringBone.ColliderGroups.Add(colliderGroup);

                            // SpringBoneCollider
                            var collider = colliderObject.AddComponent<VRM10SpringBoneCollider>();
                            collider.ColliderType = VRM10SpringBoneColliderTypes.Plane;

                            var nearestPoint = GetNearestPointOnLine(leg.start.position, leg.end.position - leg.start.position, averagePoint);
                            var legCenter = (leg.start.position + leg.end.position) * 0.5f;
                            var colliderNormal = Vector3.Normalize(averagePoint - nearestPoint);

                            switch (colliderType)
                            {
                                case ColliderType.Plane:
                                    {
                                        collider.ColliderType = VRM10SpringBoneColliderTypes.Plane;
                                        var length = legRadius;
                                        collider.Offset = leg.start.InverseTransformPoint(
                                            legCenter +
                                            colliderNormal * length
                                        );
                                        collider.Normal = leg.start.InverseTransformPoint((averagePoint - nearestPoint) + leg.start.position);
                                        collider.Normal.Normalize();
                                    }
                                    break;
                                case ColliderType.CupsuleInside:
                                    {
                                        collider.ColliderType = VRM10SpringBoneColliderTypes.CapsuleInside;
                                        collider.Radius = colliderRadius;
                                        var length = legRadius + colliderRadius;
                                        collider.Offset = leg.start.InverseTransformPoint(
                                            legCenter +
                                            colliderNormal * length
                                        ) - new Vector3(0, colliderYOffset, 0);
                                        collider.Tail = collider.Offset + new Vector3(0, colliderTailYOffset + colliderYOffset, 0);
                                    }
                                    break;
                                case ColliderType.Cupsule:
                                    {
                                        collider.ColliderType = VRM10SpringBoneColliderTypes.Capsule;
                                        collider.Radius = colliderRadius;
                                        var length = legRadius - colliderRadius;
                                        collider.Offset = leg.start.InverseTransformPoint(
                                            legCenter +
                                            colliderNormal * length
                                        ) - new Vector3(0, colliderYOffset, 0);
                                        collider.Tail = collider.Offset + new Vector3(0, colliderTailYOffset + colliderYOffset, 0);
                                    }
                                    break;
                                case ColliderType.SphereInside:
                                    {
                                        collider.ColliderType = VRM10SpringBoneColliderTypes.SphereInside;
                                        collider.Radius = colliderRadius;
                                        var length = legRadius + colliderRadius;
                                        collider.Offset = leg.start.InverseTransformPoint(
                                            legCenter +
                                            colliderNormal * length
                                        );
                                    }
                                    break;
                                case ColliderType.Sphere:
                                    {
                                        collider.ColliderType = VRM10SpringBoneColliderTypes.Sphere;
                                        collider.Radius = colliderRadius;
                                        var length = legRadius - colliderRadius;
                                        collider.Offset = leg.start.InverseTransformPoint(
                                            legCenter +
                                            colliderNormal * length
                                        );
                                    }
                                    break;
                                default:
                                    throw new System.Exception("Error");
                            }

                            // 
                            colliderGroup.Colliders.Add(collider);
                        }
                        spring.ColliderGroups.Add(colliderGroup);
                    }
                };
                SetupCollider(spring, i, prevIndex, averagePointPrev);
                SetupCollider(spring, i, nextIndex, averagePointNext);
            }
        }
#endif

#if VRC_SDK_EXIST
        private void SetupVRChat(Animator animator, VRCAvatarDescriptor avatarDescriptor, GameObject skirtRoot, int skirtOffset, bool removeOnly = false)
        {
            Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            Transform upperLegLeft = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
            Transform upperLegRight = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
            Transform lowerLegLeft = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
            Transform lowerLegRight = animator.GetBoneTransform(HumanBodyBones.RightLowerLeg);

            if (!upperLegLeft || !upperLegRight || !lowerLegLeft || !lowerLegRight)
            {
                Debug.Log("Leg Bone not found");
                return;
            }

            // remove previous objects created by this tool
            var allChildren = rootObject.transform.GetComponentsInChildren<Transform>(true);
            var prefixedObjects = allChildren.Where(t => t.name.StartsWith(prefix + prefixDelimiter)).ToList();
            foreach (Transform prefixedObject in prefixedObjects)
            {
                if (prefixedObject != null)
                {
                    DestroyImmediate(prefixedObject.gameObject);
                }
            }

            // remove existing PhysBones added by this tool
            var currentSkirtTargetRootObjects = new List<GameObject>();
            GetSkirtTargetRootObjects(skirtRoot, ref currentSkirtTargetRootObjects, skirtOffset);

            foreach (var targetObject in currentSkirtTargetRootObjects)
            {
                foreach (var physBone in targetObject.GetComponents<VRCPhysBone>())
                {
                    DestroyImmediate(physBone);
                }
            }

            if (removeOnly)
            {
                return;
            }

            var skirtTargetRootObjects = new List<GameObject>();
            GetSkirtTargetRootObjects(skirtRoot, ref skirtTargetRootObjects, skirtOffset);

            skirtTargetRootObjects.Sort(delegate (GameObject a, GameObject b)
            {
                var hipsPositionA = hips.InverseTransformPoint(a.transform.position);
                var hipsPositionB = hips.InverseTransformPoint(b.transform.position);

                var radA = System.Math.Atan2(hipsPositionA.z, hipsPositionA.x);
                var radB = System.Math.Atan2(hipsPositionB.z, hipsPositionB.x);

                if (radA > radB) return -1;
                if (radA < radB) return 1;
                return 0;
            });

            Vector3[] targetPositions = new Vector3[skirtTargetRootObjects.Count];
            for (int i = 0; i < skirtTargetRootObjects.Count; ++i)
            {
                var leafs = GetLeafBones(skirtTargetRootObjects[i].transform);
                var sum = new Vector3();
                foreach (var leaf in leafs)
                {
                    sum += leaf.position;
                }
                targetPositions[i] = sum / leafs.Count;
            }

            // constraint
            var colliderContainerLeft = upperLegLeft;
            var colliderContainerRight = upperLegRight;
            if (useConstraint)
            {
                var name = prefix + prefixDelimiter;

                var colliderContainerLeftContainer = AddEmpty(name + upperLegLeft.name, upperLegLeft.parent);
                colliderContainerLeftContainer.transform.position = upperLegLeft.position;
                colliderContainerLeftContainer.transform.rotation = upperLegLeft.rotation;
                colliderContainerLeftContainer.transform.localScale = upperLegLeft.localScale;
                colliderContainerLeft = AddEmpty(name + upperLegLeft.name + ".1", colliderContainerLeftContainer.transform).transform;

                var aimConstraintLeft = colliderContainerLeft.gameObject.AddComponent<VRCAimConstraint>();
                aimConstraintLeft.Sources.Add(new VRCConstraintSource(lowerLegLeft, 1.0f));
                aimConstraintLeft.AimAxis = Vector3.up;
                aimConstraintLeft.UpAxis = Vector3.right;
                aimConstraintLeft.WorldUp = VRCAimConstraint.WorldUpType.ObjectRotationUp;
                aimConstraintLeft.WorldUpVector = Vector3.right;
                aimConstraintLeft.WorldUpTransform = colliderContainerLeftContainer.transform;
                aimConstraintLeft.IsActive = true;

                var colliderContainerRightContainer = AddEmpty(name + upperLegRight.name, upperLegRight.parent);
                colliderContainerRightContainer.transform.position = upperLegRight.position;
                colliderContainerRightContainer.transform.rotation = upperLegRight.rotation;
                colliderContainerRightContainer.transform.localScale = upperLegRight.localScale;
                colliderContainerRight = AddEmpty(name + upperLegRight.name + ".1", colliderContainerRightContainer.transform).transform;

                var aimConstraintRight = colliderContainerRight.gameObject.AddComponent<VRCAimConstraint>();
                aimConstraintRight.Sources.Add(new VRCConstraintSource(lowerLegRight, 1.0f));
                aimConstraintRight.AimAxis = Vector3.up;
                aimConstraintRight.UpAxis = Vector3.right;
                aimConstraintRight.WorldUp = VRCAimConstraint.WorldUpType.ObjectRotationUp;
                aimConstraintRight.WorldUpVector = Vector3.right;
                aimConstraintRight.WorldUpTransform = colliderContainerRightContainer.transform;
                aimConstraintRight.IsActive = true;
            }

            var colliderCache = new VRCPhysBoneCollider[skirtTargetRootObjects.Count, skirtTargetRootObjects.Count, 2];

            for (int i = 0; i < skirtTargetRootObjects.Count; ++i)
            {
                int nextIndex = (i + 1) % skirtTargetRootObjects.Count;
                int prevIndex = (i + skirtTargetRootObjects.Count - 1) % skirtTargetRootObjects.Count;
                
                GameObject skirtRootObject = skirtTargetRootObjects[i];
                Vector3 skirtRootPosition = targetPositions[i];
                Vector3 nextSkirtRootPosition = targetPositions[nextIndex];
                Vector3 prevSkirtRootPosition = targetPositions[prevIndex];

                var physBone = skirtRootObject.AddComponent<VRCPhysBone>();
                // JSON文字列からPhysBoneの全プロパティを適用
                if (!string.IsNullOrEmpty(physBoneSettingsJson))
                {
                    EditorJsonUtility.FromJsonOverwrite(physBoneSettingsJson, physBone);
                }

                var averagePointNext = (skirtRootPosition + nextSkirtRootPosition) * 0.5f;
                var averagePointPrev = (skirtRootPosition + prevSkirtRootPosition) * 0.5f;

                System.Action<VRCPhysBone, int, int, Vector3> setupCollider = 
                    (currentPhysBone, skirtIndex, adjacentIndex, averagePoint) =>
                {
                    var skirt = skirtTargetRootObjects[skirtIndex];
                    var adjacent = skirtTargetRootObjects[adjacentIndex];

                    var allLegs = new List<UpperLegBone>
                    {
                        new UpperLegBone("Left", upperLegLeft, lowerLegLeft, colliderContainerLeft),
                        new UpperLegBone("Right", upperLegRight, lowerLegRight, colliderContainerRight)
                    };

                    var determinedLeg = DetermineColliderLeg(skirt.transform.position, upperLegLeft.position, upperLegRight.position);
                    var targetLegs = new List<UpperLegBone>();

                    if (determinedLeg == ColliderLeg.Left || determinedLeg == ColliderLeg.Both)
                        targetLegs.Add(allLegs[0]);
                    if (determinedLeg == ColliderLeg.Right || determinedLeg == ColliderLeg.Both)
                        targetLegs.Add(allLegs[1]);

                    foreach (var leg in targetLegs)
                    {
                        var oppositeLeg = allLegs.First(l => l.name != leg.name);

                        var name = prefix + prefixDelimiter + skirt.name + prefixDelimiter + adjacent.name + prefixDelimiter + leg.name;
                        var legIndex = leg.name == "Left" ? 0 : 1;
                        
                        VRCPhysBoneCollider collider = colliderCache[skirtIndex, adjacentIndex, legIndex];
                        if (collider == null)
                        {
                           collider = colliderCache[adjacentIndex, skirtIndex, legIndex];
                        }

                        if (collider == null)
                        {
                            if (collider == null)
                        {
                            GameObject colliderObject = AddEmpty(name, leg.colliderContainer);
                            collider = colliderObject.AddComponent<VRCPhysBoneCollider>();
                            colliderCache[skirtIndex, adjacentIndex, legIndex] = collider;
                            colliderCache[adjacentIndex, skirtIndex, legIndex] = collider;

                            var legCenter = (leg.start.position + leg.end.position) * 0.5f;
                            var nearestPointOnLeg = GetNearestPointOnLine(leg.start.position, leg.end.position - leg.start.position, averagePoint);
                            var offsetDirection = Vector3.Normalize(averagePoint - nearestPointOnLeg);

                            collider.shapeType = (VRCPhysBoneCollider.ShapeType)physBoneColliderType;
                            collider.rotation = Quaternion.Euler(physBoneColliderRotation);

                            switch (physBoneColliderType)
                            {
                                case PhysBoneColliderType.Sphere:
                                {
                                    collider.insideBounds = true;
                                    collider.radius = physBoneColliderRadius;
                                    var offsetDistance = legRadius + collider.radius;
                                    var worldCenter = legCenter + offsetDirection * offsetDistance;
                                    colliderObject.transform.SetPositionAndRotation(leg.start.position, leg.start.rotation);
                                    collider.position = colliderObject.transform.InverseTransformPoint(worldCenter);
                                    break;
                                }
                                case PhysBoneColliderType.Capsule:
                                {
                                    collider.insideBounds = true;
                                    collider.radius = physBoneColliderRadius;
                                    var offsetDistance = legRadius + collider.radius;
                                    var worldCenter = legCenter + offsetDirection * offsetDistance;
                                    collider.height = physBoneColliderHeight > 0 ? physBoneColliderHeight : Vector3.Distance(leg.start.position, leg.end.position);
                                    colliderObject.transform.SetPositionAndRotation(leg.start.position, Quaternion.FromToRotation(Vector3.up, leg.end.position - leg.start.position));
                                    collider.position = colliderObject.transform.InverseTransformPoint(worldCenter);
                                    break;
                                }
                                case PhysBoneColliderType.Plane:
                                {
                                    collider.insideBounds = false; // Plane does not support inside bounds
                                    var worldPosition = nearestPointOnLeg + offsetDirection * legRadius;
                                    colliderObject.transform.position = worldPosition;
                                    colliderObject.transform.rotation = Quaternion.LookRotation(offsetDirection) * Quaternion.Euler(90, 0, 0);
                                    break;
                                }
                                default:
                                    throw new System.ArgumentOutOfRangeException(nameof(physBoneColliderType), physBoneColliderType, null);
                            }
                        }
                        }
                        
                        if (!currentPhysBone.colliders.Contains(collider))
                        {
                            currentPhysBone.colliders.Add(collider);
                        }
                    }
                };

                setupCollider(physBone, i, prevIndex, averagePointPrev);
                setupCollider(physBone, i, nextIndex, averagePointNext);
            }
        }
#endif

        private List<Transform> GetLeafBones(Transform root)
        {
            var result = new List<Transform>();
            GetLeafBonesRecursive(root, ref result);
            return result;
        }

        private void GetLeafBonesRecursive(Transform root, ref List<Transform> list)
        {
            var childCount = root.childCount;
            if (childCount == 0)
            {
                list.Add(root);
                return;
            }

            for (int i = 0; i < childCount; ++i)
            {
                GetLeafBonesRecursive(root.GetChild(i), ref list);
            }
        }

#if UNIVRM_EXIST
        void RemoveSkirtJoint(Transform skirt)
        {
            var name = prefix + prefixDelimiter + skirt.name;
            var children = GetChildren(skirt.gameObject);
            var prevJoints = skirt.gameObject.GetComponents<VRM10SpringBoneJoint>().ToList();

            foreach (var joint in prevJoints)
            {
                DestroyImmediate(joint);
            }

            foreach (Transform child in children)
            {
                RemoveSkirtJoint(child);
            }
        }

        void SetupSkirtJoint(Transform skirt, Vrm10InstanceSpringBone.Spring spring, int depth, int maxDepth)
        {
            var name = prefix + prefixDelimiter + skirt.name;
            var children = GetChildren(skirt.gameObject);

            var prevJoint = skirt.gameObject.GetComponent<VRM10SpringBoneJoint>();
            if (prevJoint)
            {
                spring.Joints.Add(prevJoint);
            }
            else
            {
                var joint = skirt.gameObject.AddComponent<VRM10SpringBoneJoint>();
                var curvePosition = Mathf.Min((float)depth / (float)(maxDepth - 1), 1.0f); // maxDepth = Leaf Bone (Ignore)

                joint.m_stiffnessForce = jointStiffnessForce * jointStiffnessForceCurve.Evaluate(curvePosition);
                joint.m_gravityPower = jointGravityPower * jointGravityPowerCurve.Evaluate(curvePosition);
                joint.m_gravityDir = jointGravityDir;
                joint.m_dragForce = jointDragForce * jointDragForceCurve.Evaluate(curvePosition);
                joint.m_jointRadius = jointRadius * jointRadiusCurve.Evaluate(curvePosition);
                joint.m_drawCollider = jointDrawCollider;

                spring.Joints.Add(joint);
            }

            foreach (Transform child in children)
            {
                SetupSkirtJoint(child, spring, depth + 1, maxDepth);
            }
        }
#endif

        private ColliderLeg DetermineColliderLeg(Vector3 skirt, Vector3 upperLegLeft, Vector3 upperLegRight)
        {

            var dotRight = Vector3.Dot(upperLegLeft - upperLegRight, skirt - upperLegRight);
            var dotLeft = Vector3.Dot(upperLegRight - upperLegLeft, skirt - upperLegLeft);

            if (dotRight > 0 && dotLeft > 0)
            {
                return ColliderLeg.Both;
            }

            if (
                (skirt - upperLegLeft).sqrMagnitude <
                (skirt - upperLegRight).sqrMagnitude
            )
            {
                return ColliderLeg.Left;
            }
            else
            {
                return ColliderLeg.Right;
            }

        }

        private int GetMaxDepth(Transform transform, int depth = 0)
        {
            int max = depth;
            var children = GetChildren(transform.gameObject);

            foreach (Transform child in children)
            {
                int childDepth = GetMaxDepth(child, depth + 1);
                if (childDepth > max)
                {
                    max = childDepth;
                }
            }

            return max;
        }

        public static Vector3 GetNearestPointOnLine(Vector3 linePnt, Vector3 lineDir, Vector3 pnt)
        {
            lineDir.Normalize();
            var v = pnt - linePnt;
            var d = Vector3.Dot(v, lineDir);
            return linePnt + lineDir * d;
        }

        static GameObject AddEmpty(string name, Transform parent)
        {
            GameObject newObject = new GameObject(name);

            newObject.transform.parent = parent;
            newObject.transform.localPosition = Vector3.zero;
            newObject.transform.localRotation = Quaternion.identity;
            newObject.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);

            return newObject;
        }

        static Transform[] GetChildren(GameObject go)
        {
            int count = go.transform.childCount;
            var children = new Transform[count];

            for (int i = 0; i < count; ++i)
            {
                children[i] = go.transform.GetChild(i);
            }

            return children;
        }

        private void GetSkirtTargetRootObjects(GameObject go, ref List<GameObject> skirtTargetRootObjects, int skirtOffset, int depth = 0)
        {
            if (depth == skirtOffset)
            {
                skirtTargetRootObjects.Add(go);
            }
            else
            {
                var children = GetChildren(go);
                foreach (Transform child in children)
                {
                    GetSkirtTargetRootObjects(child.gameObject, ref skirtTargetRootObjects, skirtOffset, depth + 1);
                }
            }
        }

        private void OnGUI()
        {
            Animator animator = null;
            EditorGUI.BeginChangeCheck();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                EditorGUILayout.LabelField($"{typeof(VRM10SkirtSetupWindow).Namespace} Version: " + version);
            }

            using (new GUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                using (new EditorGUI.DisabledGroupScope(!IsUnivermExist()))
                {
                    if (GUILayout.Toggle(targetSystem == TargetSystem.VRM10, "VRM1.0", EditorStyles.toolbarButton))
                    {
                        targetSystem = TargetSystem.VRM10;
                    }
                }
                using (new EditorGUI.DisabledGroupScope(!IsVrcSdkExist()))
                {
                    if (GUILayout.Toggle(targetSystem == TargetSystem.VRChat, "VRChat", EditorStyles.toolbarButton))
                    {
                        targetSystem = TargetSystem.VRChat;
                    }
                }
            }

            if (!IsUnivermExist() && !IsVrcSdkExist())
            {
                EditorGUILayout.HelpBox("VRM1.0/VRChatいずれかのアバター向けSDKがみつかりません", MessageType.Error);
                return;
            }

            if (targetSystem == TargetSystem.VRM10 && !IsUnivermExist())
            {
                EditorGUILayout.HelpBox("UniVRMがプロジェクトに見つかりません", MessageType.Warning);
            }
            if (targetSystem == TargetSystem.VRChat && !IsVrcSdkExist())
            {
                EditorGUILayout.HelpBox("VRChat SDKがプロジェクトに見つかりません", MessageType.Warning);
            }

            skirtRoot = (GameObject)EditorGUILayout.ObjectField("SkirtRoot", skirtRoot, typeof(GameObject), true);
            skirtOffset = EditorGUILayout.IntField("オフセット", skirtOffset);
            legRadius = EditorGUILayout.FloatField("UpperLeg半径", legRadius);
            useConstraint = GUILayout.Toggle(useConstraint, "Constraintによるねじれ対策");

            var errors = new List<string>();

            if (!skirtRoot)
            {
                errors.Add("SkirtRootが選択されていません");
                goto ValidationFinish;
            }
            rootObject = skirtRoot.transform.root.gameObject;
            EditorGUILayout.LabelField("ルートオブジェクト");
            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                EditorGUILayout.LabelField(rootObject ? rootObject.name : "");
            }

            if (targetSystem == TargetSystem.VRM10)
            {
#if UNIVRM_EXIST
                isJointSettingsOpen = EditorGUILayout.Foldout(isJointSettingsOpen, "Joint");
                if (isJointSettingsOpen)
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        jointStiffnessForce = EditorGUILayout.FloatField("Stiffness Force", jointStiffnessForce);
                        jointStiffnessForceCurve = EditorGUILayout.CurveField(jointStiffnessForceCurve, Color.green, new Rect(0, 0, 1, 1));
                        jointGravityPower = EditorGUILayout.FloatField("Gravity Power", jointGravityPower);
                        jointGravityPowerCurve = EditorGUILayout.CurveField(jointGravityPowerCurve, Color.green, new Rect(0, 0, 1, 1));
                        jointGravityDir = EditorGUILayout.Vector3Field("Gravity Dir", jointGravityDir);
                        jointDragForce = EditorGUILayout.FloatField("Drag Force", jointDragForce);
                        jointDragForceCurve = EditorGUILayout.CurveField(jointDragForceCurve, Color.green, new Rect(0, 0, 1, 1));
                        jointRadius = EditorGUILayout.FloatField("Radius", jointRadius);
                        jointRadiusCurve = EditorGUILayout.CurveField(jointRadiusCurve, Color.green, new Rect(0, 0, 1, 1));
                    }
                }

                isColliderSettingsOpen = EditorGUILayout.Foldout(isColliderSettingsOpen, "Collider");
                if (isColliderSettingsOpen)
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        colliderType = (ColliderType)EditorGUILayout.EnumPopup("Collider Type", colliderType);
                        switch (colliderType)
                        {
                            case ColliderType.Cupsule:
                            case ColliderType.CupsuleInside:
                                colliderRadius = EditorGUILayout.FloatField("Radius", colliderRadius);
                                colliderYOffset = EditorGUILayout.FloatField("YOffset", colliderYOffset);
                                colliderTailYOffset = EditorGUILayout.FloatField("TailYOffset", colliderTailYOffset);
                                break;
                            case ColliderType.Sphere:
                            case ColliderType.SphereInside:
                                colliderRadius = EditorGUILayout.FloatField("Radius", colliderRadius);
                                break;
                        }
                    }
                }
#endif
            }
            else if (targetSystem == TargetSystem.VRChat)
            {
#if VRC_SDK_EXIST
                isPhysBoneSettingsOpen = EditorGUILayout.Foldout(isPhysBoneSettingsOpen, "PhysBone");
                if (isPhysBoneSettingsOpen)
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        if (physBoneEditor != null)
                        {
                            // SerializedObjectを更新して最新の状態を読み込む
                            physBoneEditor.serializedObject.Update();

                            // VRCPhysBoneコンポーネントのインスペクターを描画
                            physBoneEditor.OnInspectorGUI();

                            // インスペクターでの変更をオブジェクトに適用
                            physBoneEditor.serializedObject.ApplyModifiedProperties();

                            // インスペクターでの変更をJSON文字列にシリアライズ
                            physBoneSettingsJson = EditorJsonUtility.ToJson(tempPhysBone);
                        }
                        else
                        {
                            EditorGUILayout.HelpBox("PhysBone Editor not initialized.", MessageType.Warning);
                        }
                    }
                }

                isPhysBoneColliderSettingsOpen = EditorGUILayout.Foldout(isPhysBoneColliderSettingsOpen, "PhysBone Collider");
                if (isPhysBoneColliderSettingsOpen)
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        physBoneColliderType = (PhysBoneColliderType)EditorGUILayout.EnumPopup("Collider Type", physBoneColliderType);
                        if (physBoneColliderType == PhysBoneColliderType.Sphere || physBoneColliderType == PhysBoneColliderType.Capsule)
                        {
                            physBoneColliderRadius = EditorGUILayout.FloatField("Radius", physBoneColliderRadius);
                        }
                        if (physBoneColliderType == PhysBoneColliderType.Capsule)
                        {
                            physBoneColliderHeight = EditorGUILayout.FloatField("Height", physBoneColliderHeight);
                        }
                        physBoneColliderRotation = EditorGUILayout.Vector3Field("Rotation", physBoneColliderRotation);
                    }
                }
#endif
            }

            if (!rootObject)
            {
                errors.Add("ルートオブジェクトが見つかりません");
                goto ValidationFinish;
            }

            animator = rootObject.GetComponent<Animator>();
            if (!animator)
            {
                errors.Add("アクティブオブジェクトにはAnimatorがありません");
            }

            if (targetSystem == TargetSystem.VRM10)
            {
#if UNIVRM_EXIST
                Vrm10Instance vrm10instance = rootObject.GetComponent<Vrm10Instance>();
                if (!vrm10instance)
                {
                    errors.Add("アクティブオブジェクトにはVRM10Instanceがありません");
                }
#endif
            }

            if (!skirtRoot.transform.IsChildOf(rootObject.transform))
            {
                errors.Add("SkirtRootがアクティブオブジェクトの子孫ではありません");
            }

        ValidationFinish:
            if (errors.Count == 0)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Reset"))
                    {
                        if (targetSystem == TargetSystem.VRM10)
                        {
#if UNIVRM_EXIST
                            SetupVRM(animator, rootObject.GetComponent<Vrm10Instance>(), skirtRoot, skirtOffset, true);
#endif
                        }
                        else if (targetSystem == TargetSystem.VRChat)
                        {
#if VRC_SDK_EXIST
                            SetupVRChat(animator, rootObject.GetComponent<VRCAvatarDescriptor>(), skirtRoot, skirtOffset, true);
#endif
                        }
                    }
                    if (GUILayout.Button("Run"))
                    {
                        if (targetSystem == TargetSystem.VRM10)
                        {
#if UNIVRM_EXIST
                            SetupVRM(animator, rootObject.GetComponent<Vrm10Instance>(), skirtRoot, skirtOffset, false);
#endif
                        }
                        else if (targetSystem == TargetSystem.VRChat)
                        {
#if VRC_SDK_EXIST
                            SetupVRChat(animator, rootObject.GetComponent<VRCAvatarDescriptor>(), skirtRoot, skirtOffset, false);
#endif
                        }
                    }
                }
            }
            else
            {
                GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
                labelStyle.wordWrap = true;
                using (new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUILayout.Label(
                        string.Join("\n", errors),
                        labelStyle
                    );
                }
            }

            EditorGUILayout.EndScrollView();

            if (EditorGUI.EndChangeCheck())
            {
                EditorUserSettings.SetConfigValue(prefix + "/skirtOffset", skirtOffset.ToString());
                EditorUserSettings.SetConfigValue(prefix + "/legRadius", legRadius.ToString());
                EditorUserSettings.SetConfigValue(prefix + "/useConstraint", useConstraint.ToString());

#if UNIVRM_EXIST
                EditorUserSettings.SetConfigValue(prefix + "/isJointSettingsOpen", isJointSettingsOpen.ToString());
                EditorUserSettings.SetConfigValue(prefix + "/jointStiffnessForce", jointStiffnessForce.ToString());
                EditorUserSettings.SetConfigValue(prefix + "/jointStiffnessForceCurve", JsonUtility.ToJson(new WrappedAnimationCurve(jointStiffnessForceCurve)));
                EditorUserSettings.SetConfigValue(prefix + "/jointGravityPower", jointGravityPower.ToString());
                EditorUserSettings.SetConfigValue(prefix + "/jointGravityPowerCurve", JsonUtility.ToJson(new WrappedAnimationCurve(jointGravityPowerCurve)));
                EditorUserSettings.SetConfigValue(prefix + "/jointGravityDir", JsonUtility.ToJson(jointGravityDir));
                EditorUserSettings.SetConfigValue(prefix + "/jointDragForce", jointDragForce.ToString());
                EditorUserSettings.SetConfigValue(prefix + "/jointDragForceCurve", JsonUtility.ToJson(new WrappedAnimationCurve(jointDragForceCurve)));
                EditorUserSettings.SetConfigValue(prefix + "/jointRadius", jointRadius.ToString());
                EditorUserSettings.SetConfigValue(prefix + "/jointRadiusCurve", JsonUtility.ToJson(new WrappedAnimationCurve(jointRadiusCurve)));

                EditorUserSettings.SetConfigValue(prefix + "/isColliderSettingsOpen", isColliderSettingsOpen.ToString());
                EditorUserSettings.SetConfigValue(prefix + "/colliderType", ((int)colliderType).ToString());
                EditorUserSettings.SetConfigValue(prefix + "/colliderRadius", colliderRadius.ToString());
                EditorUserSettings.SetConfigValue(prefix + "/colliderYOffset", colliderYOffset.ToString());
                EditorUserSettings.SetConfigValue(prefix + "/colliderTailYOffset", colliderTailYOffset.ToString());
#endif
#if VRC_SDK_EXIST
                EditorUserSettings.SetConfigValue(prefix + "/isPhysBoneSettingsOpen", isPhysBoneSettingsOpen.ToString());
                EditorUserSettings.SetConfigValue(prefix + "/physBoneSettingsJson", physBoneSettingsJson); // PhysBoneの全設定をJSONで保存

                // PhysBone Colliderの設定を保存
                EditorUserSettings.SetConfigValue(prefix + "/isPhysBoneColliderSettingsOpen", isPhysBoneColliderSettingsOpen.ToString());
                EditorUserSettings.SetConfigValue(prefix + "/physBoneColliderType", ((int)physBoneColliderType).ToString());
                EditorUserSettings.SetConfigValue(prefix + "/physBoneColliderRadius", physBoneColliderRadius.ToString());
                EditorUserSettings.SetConfigValue(prefix + "/physBoneColliderHeight", physBoneColliderHeight.ToString());
                EditorUserSettings.SetConfigValue(prefix + "/physBoneColliderRotation", JsonUtility.ToJson(physBoneColliderRotation));
#endif
            }
        }

        private bool IsUnivermExist()
        {
#if UNIVRM_EXIST
            return true;
#else
            return false;
#endif
        }

        private bool IsVrcSdkExist()
        {
#if VRC_SDK_EXIST
            return true;
#else
            return false;
#endif
        }
        void OnDisable()
        {
#if VRC_SDK_EXIST
            if (physBoneEditor != null)
            {
                DestroyImmediate(physBoneEditor);
                physBoneEditor = null;
            }
            if (tempPhysBone != null)
            {
                DestroyImmediate(tempPhysBone.gameObject);
                tempPhysBone = null;
            }
#endif
        }
    }
}