using UnityEngine;

namespace GPUSkinning
{
    public enum CrowdGPUSkinningQuality
    {
        Bone1,
        Bone2, 
        Bone4
    }
    
    [System.Serializable]
    public class CrowdGPUSkinningBone
    {
        [System.NonSerialized]
        public Transform transform = null;
        public Matrix4x4 bindpose;
        public int parentBoneIndex = -1;
        public int[] childrenBonesIndices = null;
        [System.NonSerialized]
        public Matrix4x4 animationMatrix;
        public string name = null;
        public string guid = null;
        public bool isExposed = false;
        [System.NonSerialized]
        private bool bindposeInverseInit = false;
        [System.NonSerialized]
        private Matrix4x4 bindposeInverse;
        public Matrix4x4 BindposeInverse
        {
            get
            {
                if(!bindposeInverseInit)
                {
                    bindposeInverse = bindpose.inverse;
                    bindposeInverseInit = true;
                }
                return bindposeInverse;
            }
        }
    }
}