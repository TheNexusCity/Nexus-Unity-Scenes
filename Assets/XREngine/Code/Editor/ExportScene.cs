using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.EditorCoroutines.Editor;
using UnityEngine;
using SeinJS;
using System.Text.RegularExpressions;
using System.Diagnostics;
using XREngine.RealityPack;
using System.Linq;

namespace XREngine
{
    public class ExportScene : EditorWindow
    {

        string defaultMatPath = @"Assets/XREngine/Content/Materials/Block.mat";

        [MenuItem("XREngine/Export Scene")]
        static void Init()
        {
            ExportScene window = (ExportScene)EditorWindow.GetWindow(typeof(ExportScene));
            window.Show();
        }

        Exporter exporter;

        public string ConversionPath => Path.Combine(PipelineSettings.ConversionFolder, PipelineSettings.GLTFName);
        
        

        public string ExportPath
        {
            get
            {
                string exportFolder = PipelineSettings.XREProjectFolder + "/assets/";

                return exportFolder;
            }
        }
        
        private void OnFocus()
        {
            if(exporter == null)
            {
                exporter = new Exporter();
            }

            Config.Load();
            if (!Utils.inited)
            {
                Utils.Init();
            }

            ExtensionManager.Init();
        }

        private void OnGUI()
        {
            PipelineSettings.GLTFName = EditorGUILayout.TextField("Name:", PipelineSettings.GLTFName);

            if (GUILayout.Button("Set Output Directory"))
            {
                PipelineSettings.XREProjectFolder = EditorUtility.SaveFolderPanel("Output Directory", PipelineSettings.XREProjectFolder, "");
            }
            GUILayout.Space(8);
            GUILayout.Label("Export Components:");
            PipelineSettings.ExportColliders = EditorGUILayout.Toggle("Colliders", PipelineSettings.ExportColliders);
            PipelineSettings.ExportSkybox = EditorGUILayout.Toggle("Skybox", PipelineSettings.ExportSkybox);
            PipelineSettings.ExportEnvmap = EditorGUILayout.Toggle("Envmap", PipelineSettings.ExportEnvmap);
            GUILayout.Space(8);
            PipelineSettings.lightmapMode = (LightmapMode)EditorGUILayout.EnumPopup("Lightmap Mode", PipelineSettings.lightmapMode);
            
            GUILayout.Space(16);
            if(GUILayout.Button("Save Settings as Default"))
            {
                PipelineSettings.SaveSettings();
            }
            GUILayout.Space(16);
            if(PipelineSettings.XREProjectFolder != null)
            {
                if (GUILayout.Button("Export"))
                {
                    Export();
                }
            }
           
        }

        Dictionary<Transform, string> lodRegistry;
        private void FormatForExportingLODs()
        {
            lodRegistry = new Dictionary<Transform, string>();
            LODGroup[] lodGroups = GameObject.FindObjectsOfType<LODGroup>();
            foreach(var lodGroup in lodGroups)
            {
                Transform tr = lodGroup.transform;
                lodRegistry.Add(tr, tr.name);
                tr.name += "_LODGroup";
            }
        }

        private void CleanupExportingLODs()
        {
            if(lodRegistry != null)
            {
                foreach(var kv in lodRegistry)
                {
                    kv.Key.name = kv.Value;
                }
                lodRegistry = null;
            }
        }

        private void FormatForExportingSkybox()
        {
            if (PipelineSettings.ExportSkybox)
            {
                var skyMat = RenderSettings.skybox;
                var cubemap = skyMat.GetTexture("_Tex") as Cubemap;
                string srcPath = AssetDatabase.GetAssetPath(cubemap);
                string srcName = Regex.Match(srcPath, @"(?<=.*/)\w*(?=\.hdr)").Value;
                string nuPath = Path.Combine(PipelineSettings.XREProjectFolder, "cubemap");
                var cubemapDir = new DirectoryInfo(nuPath);
                if (!cubemapDir.Exists)
                {
                    cubemapDir.Create();
                }

                CubemapFace[] faces = Enumerable.Range(0, 6).Select((i) => (CubemapFace)i).ToArray();
                string[] fNames = new string[]
                {
                    "negx",
                    "posx",
                    "posy",
                    "negy",
                    
                    "posz",
                    "negz"
                    
                    /*"negz",
                    "posz"*/
                };
                Texture2D[] faceTexes = faces.Select((x, i) =>
                {
                    Texture2D result = new Texture2D(cubemap.width, cubemap.height);// cubemap.format, false);
                    var pix = cubemap.GetPixels(x);
                    System.Array.Reverse(pix);
                    result.SetPixels(pix);
                    result.Apply();

                    string facePath = string.Format("{0}/{1}.jpg", nuPath, fNames[i]);
                    File.WriteAllBytes(facePath, result.EncodeToJPG());
                    return result;
                }).ToArray();

                GameObject skyboxGO = new GameObject("__skybox__");
                skyboxGO.AddComponent<SkyBox>();
            }
        }
            
        private void CleanupExportingSkybox()
        {
            var skyboxes = FindObjectsOfType<SkyBox>();
            for(int i = 0; i < skyboxes.Length; i++)
            {
                DestroyImmediate(skyboxes[i].gameObject);
            }
        }

        private void FormatForExportingEnvmap()
        {
            GameObject envmapGO = new GameObject("__envmap__");
            envmapGO.AddComponent<Envmap>();
        }

        private void CleanupExportEnvmap()
        {
            var envmaps = FindObjectsOfType<Envmap>();
            for (int i = 0; i < envmaps.Length; i++)
            {
                DestroyImmediate(envmaps[i].gameObject);
            }
        }
        /// <summary>
        /// Formats the scene to correctly export colliders to match XREngine colliders spec
        /// </summary>
        GameObject cRoot;
        private void FormatForExportingColliders()
        {
            cRoot = new GameObject("Colliders", typeof(ColliderParent));
            //Dictionary<Collider, Transform> parents = new Dictionary<Collider, Transform>();
            Material defaultMat = AssetDatabase.LoadMainAssetAtPath(defaultMatPath) as Material;
            Collider[] colliders = GameObject.FindObjectsOfType<Collider>();
            foreach(var collider in colliders)
            {
                Transform xform = collider.transform;
                Vector3 position = xform.position;
                Quaternion rotation = xform.rotation;
                Vector3 scale = xform.lossyScale;
                //parents[collider] = xform;
                if (collider.GetType() == typeof(BoxCollider))
                {
                    var box = (BoxCollider)collider;
                    GameObject clone = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    clone.name = xform.gameObject.name + "__COLLIDER__";
                    clone.transform.position = position;
                    clone.transform.rotation = rotation;
                    clone.transform.localScale = scale;



                    clone.transform.position += clone.transform.localToWorldMatrix.MultiplyVector(box.center);
                    Vector3 nuScale = clone.transform.localScale;
                    nuScale.x *= box.size.x;
                    nuScale.y *= box.size.y;
                    nuScale.z *= box.size.z;
                    clone.transform.localScale = nuScale;
                    MeshRenderer rend = clone.GetComponent<MeshRenderer>();
                    //rend.lightmapIndex = -1;
                    rend.material = defaultMat;
                    clone.transform.SetParent(cRoot.transform, true);
                }
                else
                {
                    GameObject clone = Instantiate(xform.gameObject, cRoot.transform, true);
                    clone.transform.position = position;
                    clone.transform.rotation = rotation;
                    clone.name += "__COLLIDER__";
                    MeshRenderer rend = clone.GetComponent<MeshRenderer>();
                    rend.lightmapIndex = -1;
                }
                
                
                
                
            }
        }
        private void CleanUpExportingColliders()
        {
            if(cRoot)
            {
                DestroyImmediate(cRoot);
            }
        }

        struct MeshRestore
        {
            public List<Mesh> remove;
            public Mesh restore;
            public MeshRestore(List<Mesh> _remove, Mesh _restore)
            {
                remove = _remove;
                restore = _restore;
            }
        }
        private void Export()
        {
            

            DirectoryInfo directory = new DirectoryInfo(PipelineSettings.ConversionFolder);
            if(!directory.Exists)
            {
                Directory.CreateDirectory(PipelineSettings.ConversionFolder);
            }
            string exportFolder = Path.Combine(PipelineSettings.XREProjectFolder, "assets");
            DirectoryInfo outDir = new DirectoryInfo(exportFolder);
            if(!outDir.Exists)
            {
                Directory.CreateDirectory(exportFolder);
            }
                
            var files = directory.GetFiles();
            var subDirectories = directory.GetDirectories();

            //delete files in pipeline folder to make way for new export
            foreach(var file in files)
            {
                file.Delete();
            }

            foreach(var subDir in subDirectories)
            {
                subDir.Delete(true);
            }

            List<MeshRestore> restorer = new List<MeshRestore>();
            //set exporter path
            ExporterSettings.Export.name = PipelineSettings.GLTFName;
            ExporterSettings.Export.folder = PipelineSettings.ConversionFolder;
            
            //split up meshes due to gltf export bug
            /*
            var multiMeshes = FindObjectsOfType<MeshFilter>()
                .Select((filt) => new System.Tuple<GameObject, Mesh>(filt.gameObject, filt.sharedMesh))
                .Where((mesh) => Regex.IsMatch(AssetDatabase.GetAssetPath(mesh.Item2), @".*\.glb"));
            foreach(var multiMesh in multiMeshes)
            {
                List<Mesh> remove = new List<Mesh>();
                
                Mesh firstSubmesh = null;
                for(int i = 0; i < multiMesh.Item2.subMeshCount; i++)
                {
                    Mesh nuMesh = MeshExtension.GetSubmesh(multiMesh.Item2, i);
                    string nuPath = PipelineSettings.PipelineAssetsFolder.Replace(Application.dataPath, "Assets") + multiMesh.Item1.name + "_" + i + ".asset";
                    AssetDatabase.CreateAsset(nuMesh, nuPath);
                    AssetDatabase.Refresh();
                    if (i > 0)
                    {
                        GameObject nuGO = GameObject.Instantiate(multiMesh.Item1, multiMesh.Item1.transform);
                        nuGO.GetComponent<MeshFilter>().sharedMesh = nuMesh;
                    }
                    else firstSubmesh = nuMesh;
                    remove.Add(nuMesh);
                }
                restorer.Add(new MeshRestore(remove, multiMesh.Item2));
                multiMesh.Item1.GetComponent<MeshFilter>().sharedMesh = firstSubmesh;
            }
            */
            FormatForExportingLODs();

            if(PipelineSettings.ExportColliders)
            {
                FormatForExportingColliders();
            }

            if(PipelineSettings.ExportSkybox)
            {
                FormatForExportingSkybox();
            }

            if(PipelineSettings.ExportEnvmap)
            {
                FormatForExportingEnvmap();
            }

            

            //convert materials to SeinPBR
            StandardToSeinPBR.AllToSeinPBR();
            try
            {
                exporter.Export();
            } catch (System.NullReferenceException e)
            {
                UnityEngine.Debug.LogWarning(e);
            }
            

            if(PipelineSettings.ExportColliders)
            {
               CleanUpExportingColliders();
            }

            CleanupExportingLODs();

            if(PipelineSettings.ExportSkybox)
            {
                CleanupExportingSkybox();
            }

            if(PipelineSettings.ExportEnvmap)
            {
                CleanupExportEnvmap();
            }
            /*
            foreach (var restore in restorer)
            {
                var subGOs = FindObjectsOfType<Transform>().Where((tr) =>
                    tr.GetComponent<MeshFilter>() &&
                    restore.remove.Contains(tr.GetComponent<MeshFilter>().sharedMesh)
                    ).Select((tr) => tr.gameObject).ToList();
                for (int i = 0; i < subGOs.Count; i++)
                {
                    var subGO = subGOs[i];
                    if (i == 0)
                        subGO.GetComponent<MeshFilter>().sharedMesh = restore.restore;
                    else
                        DestroyImmediate(subGO);
                }
            }*/

            //restore materials
            StandardToSeinPBR.RestoreMaterials();

            //now execute the GLTF conversion script in the Pipeline folder


            var cmd = new ProcessStartInfo();

            if (SystemInfo.operatingSystem.ToLower().Contains("mac"))
            {
                //for mac
                cmd.FileName = "/bin/bash";
                cmd.UseShellExecute = false;
                cmd.RedirectStandardError = true;
                cmd.RedirectStandardInput = true;
                cmd.RedirectStandardOutput = true;
                cmd.WindowStyle = ProcessWindowStyle.Hidden;

            }
            else
            {
                //windows
                cmd.UseShellExecute = false;
                //cmd.Verb = "runas";
                cmd.RedirectStandardInput = true;
                cmd.RedirectStandardOutput = true;
                cmd.RedirectStandardError = true;
                cmd.WindowStyle = ProcessWindowStyle.Hidden;

                cmd.FileName = "CMD.exe";
            }

            

            Process proc = new Process();
            proc.StartInfo = cmd;
            proc.Start();
            proc.StandardInput.AutoFlush = true;
            proc.StandardInput.WriteLine(string.Format("cd {0}", PipelineSettings.PipelineFolder));
            proc.StandardInput.Flush();
            string fileName = PipelineSettings.GLTFName;
            if (SystemInfo.operatingSystem.ToLower().Contains("mac"))
            {
                //mac
                proc.StandardInput.WriteLine(string.Format("\"/usr/local/bin/node\" gltf_converter.js {0} \"{1}\"", fileName, ExportPath));
            }
            else
            {
                //windows
                proc.StandardInput.WriteLine(string.Format("\"C:/Program Files/nodejs/node.exe\" gltf_converter.js {0} \"{1}\"", fileName, ExportPath));
            }
                
            
            proc.StandardInput.Flush();
            proc.StandardInput.Close();
            //proc.WaitForExit();
            UnityEngine.Debug.Log(proc.StandardOutput.ReadToEnd());
            UnityEngine.Debug.Log(proc.StandardError.ReadToEnd());
            //OpenInFileBrowser.Open(exportFolder);
        }
    }

}
