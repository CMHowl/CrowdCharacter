using UnityEngine;

namespace GPUSkinning
{
    public class CrowdGPUSkinningAnimation : ScriptableObject
    {
        public string guid = null;
        public string name = null;
        public CrowdGPUSkinningBone[] bones = null;
        public int rootBoneIndex = 0;
        public CrowdGPUSkinningClip[] clips = null;
        public Bounds bounds;
        
        //Generated animation info texture
        public int textureWidth = 0;
        public int textureHeight = 0;

        public float sphereRadius = 1.0f;
    }
}
