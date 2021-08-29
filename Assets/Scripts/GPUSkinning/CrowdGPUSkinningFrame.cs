using UnityEngine;

namespace GPUSkinning
{
    [System.Serializable]
    public class CrowdGPUSkinningFrame
    {
        public Matrix4x4[] matrices = null;
        public Quaternion rootMotionDeltaPositionQ;
        public float rootMotionDeltaPositionL;
        public Quaternion rootMotionDeltaRotation;

        [System.NonSerialized]
        private bool rootMotionInverseInit = false;
        [System.NonSerialized]
        private Matrix4x4 rootMotionInverse;
        public Matrix4x4 RootMotionInverse(int rootBoneIndex)
        {
            if (!rootMotionInverseInit)
            {
                rootMotionInverse = matrices[rootBoneIndex].inverse;
                rootMotionInverseInit = true;
            }
            return rootMotionInverse;
        }
    }
}
