using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace GPUSkinning
{
    public class CrowdGPUSkinningSampler : MonoBehaviour
    {
        //Instance of window
        private static CrowdGPUSkinningSampler s_samplerWindow;
        
        [HideInInspector]
        [SerializeField]
        public string animName = null;

        [HideInInspector]
        [System.NonSerialized]
        public AnimationClip animClip = null;

        [HideInInspector]
        [SerializeField]
        public AnimationClip[] animClips = null;

        [HideInInspector]
        [SerializeField]
        public CrowdGPUSkinningWrapMode[] wrapModes = null;

        [HideInInspector]
        [SerializeField]
        public int[] fpsList = null;

        [HideInInspector]
        [SerializeField]
        public bool[] rootMotionEnabled = null;

        [HideInInspector]
        [SerializeField]
        public bool[] individualDifferenceEnabled = null;

        [HideInInspector]
        [SerializeField]
        private float sphereRadius = 1.0f;

        [HideInInspector]
        [SerializeField]
        public bool createNewShader = false;

        [HideInInspector]
        [System.NonSerialized]
        public int samplingClipIndex = -1;

        [HideInInspector]
        [SerializeField]
        public TextAsset texture = null;

        [HideInInspector]
        [SerializeField]
        public CrowdGPUSkinningQuality skinQuality = CrowdGPUSkinningQuality.Bone2;

        [HideInInspector]
        [SerializeField]
        public Transform rootBoneTransform = null;

        [HideInInspector]
        [SerializeField]
        public CrowdGPUSkinningAnimation anim = null;
        
        [HideInInspector]
        [System.NonSerialized]
        public bool isSampling = false;

        [HideInInspector]
        [SerializeField]
        public Mesh savedMesh = null;

        [HideInInspector]
        [SerializeField]
        public Material savedMaterial = null;

        [HideInInspector]
        [SerializeField]
        public Shader savedShader = null;

        [HideInInspector]
        [SerializeField]
        public bool bUpdateOrNew = true;

        [SerializeField]
        public GameObject generatedPrefab;
        
        private Animation animation = null;
        private Animator animator = null;
        private RuntimeAnimatorController runtimeAnimatorController = null;
        private SkinnedMeshRenderer skinnedMeshRenderer = null;
        private CrowdGPUSkinningAnimation gpuSkinningAnimation = null;
        private CrowdGPUSkinningClip gpuSkinningClip = null;
        private Vector3 rootMotionPosition;
        private Quaternion rootMotionRotation;
        [HideInInspector]
        [System.NonSerialized]
        public int samplingTotalFrames = 0;
        [HideInInspector]
        [System.NonSerialized]
        public int samplingFrameIndex = 0;

        public const string TEMP_SAVED_ANIM_PATH = "CrowdCharacter_GPUSkinning_Temp_Save_Anim_Path";
        public const string TEMP_SAVED_MTRL_PATH = "CrowdCharacter_GPUSkinning_Temp_Save_Mtrl_Path";
        public const string TEMP_SAVED_MESH_PATH = "CrowdCharacter_GPUSkinning_Temp_Save_Mesh_Path";
        public const string TEMP_SAVED_SHADER_PATH = "CrowdCharacter_GPUSkinning_Temp_Save_Shader_Path";
        public const string TEMP_SAVED_TEXTURE_PATH = "CrowdCharacter_GPUSkinning_Temp_Save_Texture_Path";

        private GameObject gameObjectInstance;
        
        private static void ShowDialog(string msg)
        {
            EditorUtility.DisplayDialog("GPUSkinning", msg, "OK");
        }

        public void SetupGameObjectInstance(GameObject inGameObject)
        {
            gameObjectInstance = inGameObject;
        }
        
        public void BeginSample()
        {
            samplingClipIndex = 0;
        }

        public void EndSample()
        {
            samplingClipIndex = -1;
        }
        
        public bool IsSamplingProgress()
        {
            return samplingClipIndex != -1;
        }

        public bool IsAnimatorOrAnimation()
        {
            return animator != null; 
        }

        private bool IsValid()
        {
            if (isSampling)
            {
                return false;
            }

            if (string.IsNullOrEmpty(animName.Trim()))
            {
                ShowDialog("Animation name is empty.");
                return false;
            }

            if (rootBoneTransform == null)
            {
                ShowDialog("Please set Root Bone.");
                return false;
            }

            if (animClips == null || animClips.Length == 0)
            {
                ShowDialog("Please set Anim Clips.");
                return false;
            }

            return true;
        }
        
        public void StartSample()
	    {
            //TODO:Instantiate gameObject before sampling.
            if (!gameObjectInstance)
            {
                return;
            }
            
            if (!IsValid())
            {
                return;
            }
            
            animClip = animClips[samplingClipIndex];
            if (animClip == null)
		    {
                isSampling = false;
			    return;
		    }

            int numFrames = (int)(GetClipFPS(animClip, samplingClipIndex) * animClip.length);
            if(numFrames == 0)
            {
                isSampling = false;
                return;
            }

            skinnedMeshRenderer = gameObjectInstance.GetComponentInChildren<SkinnedMeshRenderer>();
		    if(skinnedMeshRenderer == null)
		    {
			    ShowDialog("Cannot find SkinnedMeshRenderer.");
			    return;
		    }
		    if(skinnedMeshRenderer.sharedMesh == null)
		    {
			    ShowDialog("Cannot find SkinnedMeshRenderer.mesh.");
			    return;
		    }

		    Mesh mesh = skinnedMeshRenderer.sharedMesh;
		    if(mesh == null)
		    {
			    ShowDialog("Missing Mesh");
			    return;
		    }

		    samplingFrameIndex = 0;

		    gpuSkinningAnimation = anim == null ? ScriptableObject.CreateInstance<CrowdGPUSkinningAnimation>() : anim;
		    gpuSkinningAnimation.name = animName;

            if(anim == null)
            {
                gpuSkinningAnimation.guid = System.Guid.NewGuid().ToString();
            }

		    List<CrowdGPUSkinningBone> bonesResult = new List<CrowdGPUSkinningBone>();
		    CollectBones(bonesResult, skinnedMeshRenderer.bones, mesh.bindposes, null, rootBoneTransform, 0);
            CrowdGPUSkinningBone[] newBones = bonesResult.ToArray();
            GenerateBonesGUID(newBones);
            if (anim != null) RestoreCustomBoneData(anim.bones, newBones);
            gpuSkinningAnimation.bones = newBones;
            gpuSkinningAnimation.rootBoneIndex = 0;

            int numClips = gpuSkinningAnimation.clips == null ? 0 : gpuSkinningAnimation.clips.Length;
            int overrideClipIndex = -1;
            //TODO:Break once matched? What about multiple overrides?
            for (int i = 0; i < numClips; ++i)
            {
                if (gpuSkinningAnimation.clips[i].name == animClip.name)
                {
                    overrideClipIndex = i;
                    break;
                }
            }

            gpuSkinningClip = new CrowdGPUSkinningClip
            {
                name = animClip.name,
                fps = GetClipFPS(animClip, samplingClipIndex),
                length = animClip.length,
                wrapMode = wrapModes[samplingClipIndex],
                frames = new CrowdGPUSkinningFrame[numFrames],
                rootMotionEnabled = rootMotionEnabled[samplingClipIndex],
                individualDifferenceEnabled = individualDifferenceEnabled[samplingClipIndex]
            };

            if(gpuSkinningAnimation.clips == null)
            {
                gpuSkinningAnimation.clips = new CrowdGPUSkinningClip[] { gpuSkinningClip };
            }
            else
            {
                if (overrideClipIndex == -1)
                {
                    List<CrowdGPUSkinningClip> clips = new List<CrowdGPUSkinningClip>(gpuSkinningAnimation.clips);
                    clips.Add(gpuSkinningClip);
                    gpuSkinningAnimation.clips = clips.ToArray();
                }
                else
                {
                    CrowdGPUSkinningClip overrideClip = gpuSkinningAnimation.clips[overrideClipIndex];
                    RestoreCustomClipData(overrideClip, gpuSkinningClip);
                    gpuSkinningAnimation.clips[overrideClipIndex] = gpuSkinningClip;
                }
            }

            SetCurrentAnimationClip();
            PrepareRecordAnimator();

            isSampling = true;
        }

        private void PrepareRecordAnimator()
        {
            if (animator != null)
            {
                int numFrames = (int)(gpuSkinningClip.fps * gpuSkinningClip.length);

                animator.applyRootMotion = gpuSkinningClip.rootMotionEnabled;
                animator.Rebind();
                animator.recorderStartTime = 0;
                animator.StartRecording(numFrames);
                for (int i = 0; i < numFrames; ++i)
                {
                    animator.Update(1.0f / gpuSkinningClip.fps);
                }
                animator.StopRecording();
                animator.StartPlayback();
            }
        }
        
        private int GetClipFPS(AnimationClip clip, int clipIndex)
        {
            return fpsList[clipIndex] == 0 ? (int)clip.frameRate : fpsList[clipIndex];
        }

        /// <summary>Collect bone results from current bone, and do these stuffs for children recursively.</summary>
        /// <param name="bonesResult">A List of bone info result.</param>
        /// <param name="bonesSmr">The bones used to skin the mesh.</param>
        /// <param name="bindposes">Bindposes. View more on https://forum.unity.com/threads/some-explanations-on-bindposes.86185/</param>
        /// <param name="parentBone">Parent's bone.</param>
        /// <param name="currentBoneTransform">Transform of current bone.</param>
        /// <param name="currentBoneIndex">Index of current bone.</param>
        private void CollectBones(List<CrowdGPUSkinningBone> bonesResult, Transform[] bonesSmr, Matrix4x4[] bindposes, CrowdGPUSkinningBone parentBone, Transform currentBoneTransform, int currentBoneIndex)
        {
            CrowdGPUSkinningBone currentBone = new CrowdGPUSkinningBone();
            bonesResult.Add(currentBone);

            //Current bone index in bonesSmr.
            int indexOfSmrBones = System.Array.IndexOf(bonesSmr, currentBoneTransform);
            
            currentBone.transform = currentBoneTransform;
            currentBone.name = currentBone.transform.gameObject.name;
            currentBone.bindpose = indexOfSmrBones == -1 ? Matrix4x4.identity : bindposes[indexOfSmrBones];
            currentBone.parentBoneIndex = parentBone == null ? -1 : bonesResult.IndexOf(parentBone);

            if(parentBone != null)
            {
                //Collecting From parent to children. Don't be afraid of not initializing childrenBonesIndices.
                parentBone.childrenBonesIndices[currentBoneIndex] = bonesResult.IndexOf(currentBone);
            }

            int numChildren = currentBone.transform.childCount;
            if(numChildren > 0)
            {
                currentBone.childrenBonesIndices = new int[numChildren];
                for(int i = 0; i < numChildren; ++i)
                {
                    CollectBones(bonesResult, bonesSmr, bindposes, currentBone, currentBone.transform.GetChild(i), i);
                }
            }
        }
        
        private void GenerateBonesGUID(CrowdGPUSkinningBone[] bones)
        {
            int numBones = (bones == null) ? 0 : bones.Length;
            for(int i = 0; i < numBones; ++i)
            {
                string boneHierarchyPath = CrowdGPUSkinningUtil.BoneHierarchyPath(bones, i);
                string guid = CrowdGPUSkinningUtil.MD5(boneHierarchyPath);
                if (bones != null)
                {
                    bones[i].guid = guid;
                }
            }
        }
        
        private void RestoreCustomBoneData(CrowdGPUSkinningBone[] bonesOrig, CrowdGPUSkinningBone[] bonesNew)
        {
            for(int i = 0; i < bonesNew.Length; ++i)
            {
                for(int j = 0; j < bonesOrig.Length; ++j)
                {
                    if(bonesNew[i].guid == bonesOrig[j].guid)
                    {
                        bonesNew[i].isExposed = bonesOrig[j].isExposed;
                        break;
                    }
                }
            }
        }
        
        private void RestoreCustomClipData(CrowdGPUSkinningClip src, CrowdGPUSkinningClip dest)
        {
            //TODO: Support animation event.
        }
        
        private void SetCurrentAnimationClip()
        {
            if (animation == null)
            {
                AnimatorOverrideController animatorOverrideController = new AnimatorOverrideController();
                AnimationClip[] clips = runtimeAnimatorController.animationClips;
                AnimationClipPair[] pairs = new AnimationClipPair[clips.Length];
                for (int i = 0; i < clips.Length; ++i)
                {
                    AnimationClipPair pair = new AnimationClipPair();
                    pairs[i] = pair;
                    pair.originalClip = clips[i];
                    pair.overrideClip = animClip;
                }
                animatorOverrideController.runtimeAnimatorController = runtimeAnimatorController;
                animatorOverrideController.clips = pairs;
                animator.runtimeAnimatorController = animatorOverrideController;
            }
        }
        
        private void Update()
	    {
		    if(!isSampling)
		    {
			    return;
		    }

            int totalFrames = (int)(gpuSkinningClip.length * gpuSkinningClip.fps);
		    samplingTotalFrames = totalFrames;

            if (samplingFrameIndex >= totalFrames)
            {
                if(animator != null)
                {
                    animator.StopPlayback();
                }

                string savePath = null;
                if (anim == null)
                {
                    savePath = EditorUtility.SaveFolderPanel("GPUSkinning Sampler Save", GetUserPreferDir(), animName);
                }
                else
                {
                    string animPath = AssetDatabase.GetAssetPath(anim);
                    savePath = new FileInfo(animPath).Directory.FullName.Replace('\\', '/');
                }

			    if(!string.IsNullOrEmpty(savePath))
			    {
				    if(!savePath.Contains(Application.dataPath.Replace('\\', '/')))
				    {
					    ShowDialog("Must select a directory in the project's Asset folder.");
				    }
				    else
				    {
					    SaveUserPreferDir(savePath);

					    string dir = "Assets" + savePath.Substring(Application.dataPath.Length);

					    string savedAnimPath = dir + "/CrowdGPUSkinning_Anim_" + animName + ".asset";
                        SetSthAboutTexture(gpuSkinningAnimation);
                        EditorUtility.SetDirty(gpuSkinningAnimation);
                        if (anim != gpuSkinningAnimation)
                        {
                            AssetDatabase.CreateAsset(gpuSkinningAnimation, savedAnimPath);
                        }
                        WriteTempData(TEMP_SAVED_ANIM_PATH, savedAnimPath);
                        anim = gpuSkinningAnimation;

                        CreateTextureMatrix(dir, anim);

                        if (samplingClipIndex == 0)
                        {
                            Mesh newMesh = CreateNewMesh(skinnedMeshRenderer.sharedMesh, "GPUSkinning_Mesh");
                            if (savedMesh != null)
                            {
                                newMesh.bounds = savedMesh.bounds;
                            }
                            string savedMeshPath = dir + "/GPUSKinning_Mesh_" + animName + ".asset";
                            AssetDatabase.CreateAsset(newMesh, savedMeshPath);
                            WriteTempData(TEMP_SAVED_MESH_PATH, savedMeshPath);
                            savedMesh = newMesh;

                            CreateShaderAndMaterial(dir);
                        }

					    AssetDatabase.Refresh();
					    AssetDatabase.SaveAssets();
				    }
			    }
                isSampling = false;
                return;
            }
            
            float time = gpuSkinningClip.length * ((float)samplingFrameIndex / totalFrames);
            CrowdGPUSkinningFrame frame = new CrowdGPUSkinningFrame();
            gpuSkinningClip.frames[samplingFrameIndex] = frame;
            frame.matrices = new Matrix4x4[gpuSkinningAnimation.bones.Length];
            if (animation == null)
            {
                animator.playbackTime = time;
                animator.Update(0);
            }
            else
            {
                animation.Stop();
                AnimationState animState = animation[animClip.name];
                if(animState != null)
                {
                    animState.time = time;
                    animation.Sample();
                    animation.Play();
                }
            }
            StartCoroutine(SamplingCoroutine(frame, totalFrames));
        }
        
        private class BoneWeightSortData : System.IComparable<BoneWeightSortData>
        {
            public int index = 0;
            public float weight = 0;

            public int CompareTo(BoneWeightSortData b)
            {
                return weight > b.weight ? -1 : 1;
            }
        }
        
        private Mesh CreateNewMesh(Mesh mesh, string meshName)
        {
            Vector3[] normals = mesh.normals;
            Vector4[] tangents = mesh.tangents;
            Color[] colors = mesh.colors;
            Vector2[] uv = mesh.uv;

            Mesh newMesh = new Mesh();
            newMesh.name = meshName;
            newMesh.vertices = mesh.vertices;
            if (normals != null && normals.Length > 0) { newMesh.normals = normals; }
            if (tangents != null && tangents.Length > 0) { newMesh.tangents = tangents; }
            if (colors != null && colors.Length > 0) { newMesh.colors = colors; }
            if (uv != null && uv.Length > 0) { newMesh.uv = uv; }

            int numVertices = mesh.vertexCount;
            BoneWeight[] boneWeights = mesh.boneWeights;
            Vector4[] uv2 = new Vector4[numVertices];
		    Vector4[] uv3 = new Vector4[numVertices];
            Transform[] smrBones = skinnedMeshRenderer.bones;
            for(int i = 0; i < numVertices; ++i)
            {
                BoneWeight boneWeight = boneWeights[i];

			    BoneWeightSortData[] weights = new BoneWeightSortData[4];
			    weights[0] = new BoneWeightSortData(){ index=boneWeight.boneIndex0, weight=boneWeight.weight0 };
			    weights[1] = new BoneWeightSortData(){ index=boneWeight.boneIndex1, weight=boneWeight.weight1 };
			    weights[2] = new BoneWeightSortData(){ index=boneWeight.boneIndex2, weight=boneWeight.weight2 };
			    weights[3] = new BoneWeightSortData(){ index=boneWeight.boneIndex3, weight=boneWeight.weight3 };
			    System.Array.Sort(weights);

			    CrowdGPUSkinningBone bone0 = GetBoneByTransform(smrBones[weights[0].index]);
                CrowdGPUSkinningBone bone1 = GetBoneByTransform(smrBones[weights[1].index]);
                CrowdGPUSkinningBone bone2 = GetBoneByTransform(smrBones[weights[2].index]);
                CrowdGPUSkinningBone bone3 = GetBoneByTransform(smrBones[weights[3].index]);

                Vector4 skinData_01 = new Vector4();
			    skinData_01.x = GetBoneIndex(bone0);
			    skinData_01.y = weights[0].weight;
			    skinData_01.z = GetBoneIndex(bone1);
			    skinData_01.w = weights[1].weight;
			    uv2[i] = skinData_01;

			    Vector4 skinData_23 = new Vector4();
			    skinData_23.x = GetBoneIndex(bone2);
			    skinData_23.y = weights[2].weight;
			    skinData_23.z = GetBoneIndex(bone3);
			    skinData_23.w = weights[3].weight;
			    uv3[i] = skinData_23;
            }
            newMesh.SetUVs(1, new List<Vector4>(uv2));
		    newMesh.SetUVs(2, new List<Vector4>(uv3));

            newMesh.triangles = mesh.triangles;
            return newMesh;
        }
            
        private IEnumerator SamplingCoroutine(CrowdGPUSkinningFrame frame, int totalFrames)
        {
		    yield return new WaitForEndOfFrame();

            CrowdGPUSkinningBone[] bones = gpuSkinningAnimation.bones;
            int numBones = bones.Length;
            for(int i = 0; i < numBones; ++i)
            {
                Transform boneTransform = bones[i].transform;
                CrowdGPUSkinningBone currentBone = GetBoneByTransform(boneTransform);
                frame.matrices[i] = currentBone.bindpose;
                do
                {
                    Matrix4x4 mat = Matrix4x4.TRS(currentBone.transform.localPosition, currentBone.transform.localRotation, currentBone.transform.localScale);
                    frame.matrices[i] = mat * frame.matrices[i];
                    if (currentBone.parentBoneIndex == -1)
                    {
                        break;
                    }
                    else
                    {
                        currentBone = bones[currentBone.parentBoneIndex];
                    }
                }
                while (true);
            }

            if(samplingFrameIndex == 0)
            {
                rootMotionPosition = bones[gpuSkinningAnimation.rootBoneIndex].transform.localPosition;
                rootMotionRotation = bones[gpuSkinningAnimation.rootBoneIndex].transform.localRotation;
            }
            else
            {
                Vector3 newPosition = bones[gpuSkinningAnimation.rootBoneIndex].transform.localPosition;
                Quaternion newRotation = bones[gpuSkinningAnimation.rootBoneIndex].transform.localRotation;
                Vector3 deltaPosition = newPosition - rootMotionPosition;
                frame.rootMotionDeltaPositionQ = Quaternion.Inverse(Quaternion.Euler(transform.forward.normalized)) * Quaternion.Euler(deltaPosition.normalized);
                frame.rootMotionDeltaPositionL = deltaPosition.magnitude;
                frame.rootMotionDeltaRotation = Quaternion.Inverse(rootMotionRotation) * newRotation;
                rootMotionPosition = newPosition;
                rootMotionRotation = newRotation;

                if(samplingFrameIndex == 1)
                {
                    gpuSkinningClip.frames[0].rootMotionDeltaPositionQ = gpuSkinningClip.frames[1].rootMotionDeltaPositionQ;
                    gpuSkinningClip.frames[0].rootMotionDeltaPositionL = gpuSkinningClip.frames[1].rootMotionDeltaPositionL;
                    gpuSkinningClip.frames[0].rootMotionDeltaRotation = gpuSkinningClip.frames[1].rootMotionDeltaRotation;
                }
            }

            ++samplingFrameIndex;
        }
        
        private void CreateTextureMatrix(string dir, CrowdGPUSkinningAnimation gpuSkinningAnim)
        {
            Texture2D texture = new Texture2D(gpuSkinningAnim.textureWidth, gpuSkinningAnim.textureHeight, TextureFormat.RGBAHalf, false, true);
            Color[] pixels = texture.GetPixels();
            int pixelIndex = 0;
            for (int clipIndex = 0; clipIndex < gpuSkinningAnim.clips.Length; ++clipIndex)
            {
                CrowdGPUSkinningClip clip = gpuSkinningAnim.clips[clipIndex];
                CrowdGPUSkinningFrame[] frames = clip.frames;
                int numFrames = frames.Length;
                for (int frameIndex = 0; frameIndex < numFrames; ++frameIndex)
                {
                    CrowdGPUSkinningFrame frame = frames[frameIndex];
                    Matrix4x4[] matrices = frame.matrices;
                    int numMatrices = matrices.Length;
                    for (int matrixIndex = 0; matrixIndex < numMatrices; ++matrixIndex)
                    {
                        Matrix4x4 matrix = matrices[matrixIndex];
                        pixels[pixelIndex++] = new Color(matrix.m00, matrix.m01, matrix.m02, matrix.m03);
                        pixels[pixelIndex++] = new Color(matrix.m10, matrix.m11, matrix.m12, matrix.m13);
                        pixels[pixelIndex++] = new Color(matrix.m20, matrix.m21, matrix.m22, matrix.m23);
                    }
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();

            string savedPath = dir + "/CrowdGPUSkinning_Texture_" + animName + ".jpg";
            using (FileStream fileStream = new FileStream(savedPath, FileMode.Create))
            {
                byte[] bytes = texture.GetRawTextureData();
                fileStream.Write(bytes, 0, bytes.Length);
                fileStream.Flush();
                fileStream.Close();
                fileStream.Dispose();
            }
            WriteTempData(TEMP_SAVED_TEXTURE_PATH, savedPath);
        }
        
        private CrowdGPUSkinningBone GetBoneByTransform(Transform transform)
        {
            CrowdGPUSkinningBone[] bones = gpuSkinningAnimation.bones;
            int numBones = bones.Length;
            for(int i = 0; i < numBones; ++i)
            {
                if(bones[i].transform == transform)
                {
                    return bones[i];
                }
            }
            return null;
        }
        
        private void SetSthAboutTexture(CrowdGPUSkinningAnimation gpuSkinningAnim)
        {
            int numPixels = 0;

            CrowdGPUSkinningClip[] clips = gpuSkinningAnim.clips;
            int numClips = clips.Length;
            for (int clipIndex = 0; clipIndex < numClips; ++clipIndex)
            {
                CrowdGPUSkinningClip clip = clips[clipIndex];
                clip.pixelSegmentation = numPixels;

                CrowdGPUSkinningFrame[] frames = clip.frames;
                int numFrames = frames.Length;
                numPixels += gpuSkinningAnim.bones.Length * 3/*treat 3 pixels as a float3x4*/ * numFrames;
            }

            CalculateTextureSize(numPixels, out gpuSkinningAnim.textureWidth, out gpuSkinningAnim.textureHeight);
        }
        
        private void CreateShaderAndMaterial(string dir)
	    {
            Shader shader = null;
            if (createNewShader)
            {
                string shaderTemplate = "GPUSkinningUnlit_Template";

                string shaderStr = ((TextAsset)Resources.Load(shaderTemplate)).text;
                shaderStr = shaderStr.Replace("_$AnimName$_", animName);
                shaderStr = SkinQualityShaderStr(shaderStr);
                string shaderPath = dir + "/GPUSKinning_Shader_" + animName + ".shader";
                File.WriteAllText(shaderPath, shaderStr);
                WriteTempData(TEMP_SAVED_SHADER_PATH, shaderPath);
                AssetDatabase.ImportAsset(shaderPath);
                shader = AssetDatabase.LoadMainAssetAtPath(shaderPath) as Shader;
            }
            else
            {
                string shaderName = "GPUSkinning/GPUSkinning_Unlit_Skin";
                        shaderName +=
                    skinQuality == CrowdGPUSkinningQuality.Bone1 ? 1 :
                    skinQuality == CrowdGPUSkinningQuality.Bone2 ? 2 :
                    skinQuality == CrowdGPUSkinningQuality.Bone4 ? 4 : 1;
                shader = Shader.Find(shaderName);
                WriteTempData(TEMP_SAVED_SHADER_PATH, AssetDatabase.GetAssetPath(shader));
            }

		    Material mtrl = new Material(shader);
		    if(skinnedMeshRenderer.sharedMaterial != null)
		    {
			    mtrl.CopyPropertiesFromMaterial(skinnedMeshRenderer.sharedMaterial);
		    }
		    string savedMtrlPath = dir + "/GPUSKinning_Material_" + animName + ".mat";
		    AssetDatabase.CreateAsset(mtrl, savedMtrlPath);
            WriteTempData(TEMP_SAVED_MTRL_PATH, savedMtrlPath);
	    }
            
        private string SkinQualityShaderStr(string shaderStr)
        {
            CrowdGPUSkinningQuality removalQuality1 = 
                skinQuality == CrowdGPUSkinningQuality.Bone1 ? CrowdGPUSkinningQuality.Bone2 : 
                skinQuality == CrowdGPUSkinningQuality.Bone2 ? CrowdGPUSkinningQuality.Bone1 : 
                skinQuality == CrowdGPUSkinningQuality.Bone4 ? CrowdGPUSkinningQuality.Bone1 : CrowdGPUSkinningQuality.Bone1;

            CrowdGPUSkinningQuality removalQuality2 = 
                skinQuality == CrowdGPUSkinningQuality.Bone1 ? CrowdGPUSkinningQuality.Bone4 : 
                skinQuality == CrowdGPUSkinningQuality.Bone2 ? CrowdGPUSkinningQuality.Bone4 : 
                skinQuality == CrowdGPUSkinningQuality.Bone4 ? CrowdGPUSkinningQuality.Bone2 : CrowdGPUSkinningQuality.Bone1;

            shaderStr = Regex.Replace(shaderStr, @"_\$" + removalQuality1 + @"[\s\S]*" + removalQuality1 + @"\$_", string.Empty);
            shaderStr = Regex.Replace(shaderStr, @"_\$" + removalQuality2 + @"[\s\S]*" + removalQuality2 + @"\$_", string.Empty);
            shaderStr = shaderStr.Replace("_$" + skinQuality, string.Empty);
            shaderStr = shaderStr.Replace(skinQuality + "$_", string.Empty);

            return shaderStr;
        }
        
        private void CalculateTextureSize(int numPixels, out int texWidth, out int texHeight)
        {
            texWidth = 1;
            texHeight = 1;
            while (true)
            {
                if (texWidth * texHeight >= numPixels) break;
                texWidth *= 2;
                if (texWidth * texHeight >= numPixels) break;
                texHeight *= 2;
            }
        }
        
        private int GetBoneIndex(CrowdGPUSkinningBone bone)
        {
            return System.Array.IndexOf(gpuSkinningAnimation.bones, bone);
        }

        public static void WriteTempData(string key, string value)
        {
            PlayerPrefs.SetString(key, value);
        }
        
        private string GetUserPreferDir()
        {
            return PlayerPrefs.GetString("CrowdGPUSkinning_UserPreferDir", Application.dataPath);
        }
        
        private void SaveUserPreferDir(string dirPath)
        {
            PlayerPrefs.SetString("CrowdGPUSkinning_UserPreferDir", dirPath);
        }
    }
}
