using System;
using UnityEditor;
using UnityEngine;

namespace GPUSkinning
{
    public class CrowdGPUSkinningSamplerEditor : EditorWindow
    {
        private GameObject samplerGO;
        private CrowdGPUSkinningSampler sampler;
        private static CrowdGPUSkinningSamplerEditor s_window;

        private bool guiEnabled = false;
        
        private GameObject generatedObjectInstance;
        [SerializeField]
        private GameObject generatedPrefab;
        
        [MenuItem("Crowd Character Tools/GPU Skinning Animation Generator", false)]
        static void MakeWindow()
        {
            s_window = GetWindow(typeof(CrowdGPUSkinningSamplerEditor)) as CrowdGPUSkinningSamplerEditor;
        }

        private void OnEnable()
        {
            samplerGO = UnityEngine.Object.Instantiate(Resources.Load("GPUSkinningSampler")) as GameObject;
            if (samplerGO != null)
            {
                sampler = samplerGO.GetComponent<CrowdGPUSkinningSampler>();
            }
            EditorApplication.update += GenerateAnimation;
        }

        private void OnDisable()
        {
            sampler = null;
            if (samplerGO != null)
            {
                DestroyImmediate(samplerGO);
                samplerGO = null;
            }
            
            EditorApplication.update -= GenerateAnimation;
        }

        private void OnGUI()
        {
            if(sampler == null)
            {
                return;
            }

            GUI.skin.label.richText = true;
            GUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();
            }
            GUILayout.EndHorizontal();

            GameObject prefab = EditorGUILayout.ObjectField("Asset to Generate", generatedPrefab, typeof(GameObject), true) as GameObject;
            if (prefab != generatedPrefab)
            {
                generatedPrefab = prefab;
                var generatedInstance = Instantiate(generatedPrefab);
                sampler.SetupGameObjectInstance(generatedInstance);
            }
            
            // sampler.MappingAnimationClips();

            OnGUI_Sampler(sampler);

            // OnGUI_Preview(sampler);

            // if (preview != null)
            // {
            //     Repaint();
            // }
        }

        private void GenerateAnimation()
        {
            
        }
        
        private void OnGUI_Sampler(CrowdGPUSkinningSampler sampler)
        {
            guiEnabled = !Application.isPlaying;
            if (generatedPrefab == null)
            {
                return;
            }
            
            BeginBox();
            {
                GUI.enabled = guiEnabled;
                {
                    SerializedObject serializedObject = new SerializedObject(sampler);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("animName"), new GUIContent("Animation Name"));

                    GUI.enabled = false;
                    EditorGUILayout.Space();
                    EditorGUILayout.BeginHorizontal();
                    {
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("anim"), new GUIContent());
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    {
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("savedMesh"), new GUIContent());
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    {
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("savedMaterial"), new GUIContent());
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    {
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("savedShader"), new GUIContent());
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    {
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("texture"), new GUIContent());
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.Space();
                    GUI.enabled = true && guiEnabled;

                    EditorGUILayout.PropertyField(serializedObject.FindProperty("skinQuality"), new GUIContent("Quality"));
                    
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("rootBoneTransform"), new GUIContent("Root Bone"));
                }
                GUI.enabled = true;

                if (GUILayout.Button("Generate Animation"))
                {
                    sampler.BeginSample();
                    sampler.StartSample();
                }
            }
            EndBox();
        }
        
        private void BeginBox()
        {
            EditorGUILayout.BeginVertical(GUI.skin.GetStyle("Box"));
            EditorGUILayout.Space();
        }
        
        private void EndBox()
        {
            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();
        }
        
        private int lastIndentLevel = 0;
        private void BeginIndentLevel(int indentLevel)
        {
            lastIndentLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel = indentLevel;
        }

        private void EndIndentLevel()
        {
            EditorGUI.indentLevel = lastIndentLevel;
        }

    }
}
