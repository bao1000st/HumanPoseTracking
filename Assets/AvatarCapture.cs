using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mediapipe.Unity.CoordinateSystem;
using Mediapipe.Tasks.Components.Containers;
using UnityEngine.Animations.Rigging;
using System.IO;

namespace Mediapipe.Unity
{
    public class AvatarCapture : MonoBehaviour
    {
        [System.Serializable]
        public class BlenshapeInfo
        {
            public int blendshapeIndex;
            public SkinnedMeshRenderer desRenderer;
            public int desIndex;
        }
        [SerializeField] private HolisticLandmarkListAnnotationController holisticController;
        [SerializeField] private FaceLandmarkerResultAnnotationController faceController;
        [SerializeField] private Transform rig;
        public bool inverseLeftPalmFacing;
        public bool inverseRightPalmFacing;

        // model parts
        [SerializeField] private List<Transform> bodyTransforms;
        [SerializeField] private List<Transform> leftHandPartTransforms;
        [SerializeField] private List<Transform> rightHandPartTransforms;
        [SerializeField] public List<BlenshapeInfo> blendshapeInfos;


        // animation Rigs
        private Dictionary<int, Transform> _bodyRigTransforms;
        private Dictionary<int, Transform> _leftHandRigTransforms;
        private Dictionary<int, Transform> _rightHandRigTransforms;
        // landmark pairs
        private List<(int, int)> _HandLandmarkPairs = new List<(int, int)> {
            (0,9),
            (1,2), (2,3), (3,4),
            (5,6), (6,7), (7,8),
            (9,10), (10,11), (11,12),
            (13,14), (14,15), (15,16),
            (17,18), (18,19), (19,20),
        };
        private List<(int, int)> _PoseLandmarkPairs = new List<(int, int)> { // doesnt use first eleven landmarks => index will be subtracted by 11
            (0,2), (1,3), (2,4), (3,5),
        };

        void Start()
        {
            _bodyRigTransforms = CreateRigParts(_PoseLandmarkPairs, bodyTransforms, rig);
            _leftHandRigTransforms = CreateRigParts(_HandLandmarkPairs, leftHandPartTransforms, _bodyRigTransforms[4]);
            _rightHandRigTransforms = CreateRigParts(_HandLandmarkPairs, rightHandPartTransforms, _bodyRigTransforms[5]);
            rig.parent.GetComponent<RigBuilder>().Build();
        }

        void LateUpdate()
        {
            // face
            BlendshapeTracking(faceController._currentTarget.faceBlendshapes, blendshapeInfos, 100);
            // upper body
            BodyTracking(
                holisticController._currentPoseLandmarkList,
                _PoseLandmarkPairs,
                bodyTransforms,
                _bodyRigTransforms
            );
            // left hand
            HandTracking(
                holisticController._currentLeftHandLandmarkList,
                _HandLandmarkPairs,
                leftHandPartTransforms,
                _leftHandRigTransforms,
                bodyTransforms[4],
                _bodyRigTransforms[4],
                bodyTransforms[2],
                _bodyRigTransforms[2],
                inverseLeftPalmFacing
            );
            // right hand
            HandTracking(
                holisticController._currentRightHandLandmarkList,
                _HandLandmarkPairs,
                rightHandPartTransforms,
                _rightHandRigTransforms,
                bodyTransforms[5],
                _bodyRigTransforms[5],
                bodyTransforms[3],
                _bodyRigTransforms[3],
                inverseRightPalmFacing
            );
        }

        void BodyTracking(IReadOnlyList<NormalizedLandmark> landmarkList, List<(int, int)> landmarkPairs, List<Transform> partTransforms, Dictionary<int, Transform> rigTransforms)
        {
            if (landmarkList != null)
            {
                LandmarkTracking(
                    landmarkList,
                    landmarkPairs,
                    partTransforms,
                    rigTransforms,
                    11
                );
            }
        }

        void HandTracking(IReadOnlyList<NormalizedLandmark> landmarkList, List<(int, int)> landmarkPairs, List<Transform> partTransforms, Dictionary<int, Transform> rigTransforms,
        Transform wristPart, Transform wristRig, Transform foreArmPart, Transform foreArmRig, bool inversePalmFacing)
        {
            if (landmarkList != null)
            {
                var rect = holisticController.transform.parent.GetComponent<RectTransform>().rect;
                //palm facing
                var landmark0 = rect.GetPoint(landmarkList[0]);
                var landmark5 = rect.GetPoint(landmarkList[5]);
                var landmark17 = rect.GetPoint(landmarkList[17]);
                var facing = Vector3.Cross(landmark17 - landmark0, landmark0 - landmark5);
                var modifier = getModifier(landmark0, facing);
                if (inversePalmFacing)
                {
                    wristRig.LookAt(2 * wristPart.position - (wristPart.position + modifier), Vector3.up);
                    foreArmRig.LookAt(2 * foreArmPart.position - (foreArmPart.position + modifier), Vector3.up);
                }
                else
                {
                    wristRig.LookAt((wristPart.position + modifier), Vector3.up);
                    foreArmRig.LookAt((foreArmPart.position + modifier), Vector3.up);
                };
                // hand landmark tracking
                LandmarkTracking(
                    landmarkList,
                    landmarkPairs,
                    partTransforms,
                    rigTransforms,
                    0
                );
            }
        }

        void BlendshapeTracking(IReadOnlyList<Classifications> blendshapeList, List<BlenshapeInfo> blenshapeInfos, float valueModifier)
        {
            if (blendshapeList != null && blendshapeList.Count >= 1)
            {
                foreach (BlenshapeInfo info in blendshapeInfos)
                {
                    Category blenshape = blendshapeList[0].categories[info.blendshapeIndex];
                    info.desRenderer.SetBlendShapeWeight(info.desIndex, blenshape.score * valueModifier);
                }
            }
        }

        void LandmarkTracking(IReadOnlyList<NormalizedLandmark> landmarkList, List<(int, int)> landmarkPairs, List<Transform> partTransforms, Dictionary<int, Transform> rigTransforms, int indexModifier)
        {
            var rect = holisticController.transform.parent.GetComponent<RectTransform>().rect;
            for (var i = 0; i < landmarkPairs.Count; i++)
            {
                var landmarkPivot = rect.GetPoint(landmarkList[landmarkPairs[i].Item1 + indexModifier]);
                var landmarkTarget = rect.GetPoint(landmarkList[landmarkPairs[i].Item2 + indexModifier]);
                Vector3 result = partTransforms[landmarkPairs[i].Item1].position + getModifier(landmarkPivot, landmarkTarget);
                rigTransforms[landmarkPairs[i].Item2].position = result;
            }
        }

        Dictionary<int, Transform> CreateRigParts(List<(int, int)> landmarkPairs, List<Transform> partTransforms, Transform parent)
        {
            var rigTransforms = new Dictionary<int, Transform>();
            for (var i = 0; i < landmarkPairs.Count; i++)
            {
                int tipIndex = landmarkPairs[i].Item2;
                int rootIndex = landmarkPairs[i].Item1;

                var gameObject = new GameObject($"{partTransforms[tipIndex].name} Rig");
                gameObject.transform.SetParent(parent);
                gameObject.transform.position = partTransforms[tipIndex].position;
                gameObject.transform.eulerAngles = partTransforms[tipIndex].eulerAngles;

                var ikConstraint = gameObject.AddComponent<TwoBoneIKConstraint>();
                ikConstraint.data.root = partTransforms[rootIndex];
                ikConstraint.data.mid = partTransforms[tipIndex];
                ikConstraint.data.tip = partTransforms[tipIndex];
                ikConstraint.data.target = gameObject.transform;
                ikConstraint.data.targetPositionWeight = 1f;
                ikConstraint.data.targetRotationWeight = 1f;
                rigTransforms.Add(tipIndex, gameObject.transform);
            }
            return rigTransforms;
        }

        // UTILITIES---------------------------------------------------------------------------------------
        Vector3 getModifier(Vector3 landmarkPivot, Vector3 landmarkTarget)
        {
            var mirrorLandmarkTarget = new Vector3(landmarkTarget.x, landmarkTarget.y, landmarkPivot.z + (landmarkPivot.z - landmarkTarget.z));
            return Quaternion.AngleAxis(180, Vector3.up) * ((mirrorLandmarkTarget - landmarkPivot));
        }
    }
}