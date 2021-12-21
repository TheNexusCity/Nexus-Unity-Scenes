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

        public string fileName;
        public string ConversionPath => Path.Combine(PipelineSettings.ConversionFolder, fileName);
        

        public string exportFolder;
        public string ExportPath
        {
            get
            {
                if(exportFolder == null)
                {
                    exportFolder = PipelineSettings.DefaultExportFolder;
                }
                if(fileName == null)
                {
                    fileName = "";
                }
                Regex extensionCheck = new Regex(".*glb");
                if(!extensionCheck.IsMatch(fileName))
                {
                    return Path.Combine(exportFolder, fileName + ".glb");
                }
                return Path.Combine(exportFolder, fileName); ;
            }
        }
        

        private void OnEnable()
        {
            fileName = EditorSceneManager.GetActiveScene().name;
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
            EditorGUILayout.LabelField("Current Output Path: ", ExportPath);
            fileName = EditorGUILayout.TextField("Name:", fileName);

            if (GUILayout.Button("Set Output Directory"))
            {
                exportFolder = EditorUtility.SaveFolderPanel("Output Directory", exportFolder, "");
            }
            GUILayout.Space(8);
            PipelineSettings.ExportColliders = EditorGUILayout.Toggle("Export Colliders", PipelineSettings.ExportColliders);
            GUILayout.Space(8);
            PipelineSettings.lightmapMode = (LightmapMode)EditorGUILayout.EnumPopup("Lightmap Mode", PipelineSettings.lightmapMode);


            if(GUILayout.Button("Export"))
            {
                Export();
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


        private void Export()
        {
            

            DirectoryInfo directory = new DirectoryInfo(PipelineSettings.ConversionFolder);
            if(!directory.Exists)
            {
                Directory.CreateDirectory(PipelineSettings.ConversionFolder);
            }

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

            //set exporter path
            ExporterSettings.Export.name = fileName;
            ExporterSettings.Export.folder = PipelineSettings.ConversionFolder;

            FormatForExportingLODs();

            if(PipelineSettings.ExportColliders)
            {
                FormatForExportingColliders();
            }

            //convert materials to SeinPBR
            StandardToSeinPBR.AllToSeinPBR();

            exporter.Export();

            if(PipelineSettings.ExportColliders)
            {
               CleanUpExportingColliders();
            }

            CleanupExportingLODs();

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

            if (SystemInfo.operatingSystem.ToLower().Contains("mac"))
            {
                //mac
                proc.StandardInput.WriteLine(string.Format("\"/usr/local/bin/node\" gltf_converter.js {0}", fileName));
            }
            else
            {
                //windows
                proc.StandardInput.WriteLine(string.Format("\"C:/Program Files/nodejs/node.exe\" gltf_converter.js {0}", fileName));
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
