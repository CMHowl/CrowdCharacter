using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GPUSkinning
{
    public enum CrowdGPUSkinningWrapMode
    {
        Once, 
        Loop
    }

    public class CrowdGPUSkinningClip
    {
        public string name = null;
        public float length = 0.0f;
        public int fps = 0;
        public CrowdGPUSkinningWrapMode wrapMode = CrowdGPUSkinningWrapMode.Once;
        public CrowdGPUSkinningFrame[] frames = null;
        public int pixelSegmentation = 0;
        public bool rootMotionEnabled = false;
        public bool individualDifferenceEnabled = false;
    }
}