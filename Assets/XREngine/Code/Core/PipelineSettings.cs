using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using System.IO;
using System.Text.RegularExpressions;

using Newtonsoft.Json;

namespace XREngine
{
    public enum LightmapMode
    {
        IGNORE,
        BAKE_COMBINED,
        BAKE_SEPARATE
    }

    

    [InitializeOnLoad]
    public static class PipelineSettings
    {
        [System.Serializable]
        struct Data
        {
            public string GLTFName;
            public string XREProjectFolder;
            public bool ExportColliders;
            public bool ExportSkybox;
            public LightmapMode lightmapMode;
            public int CombinedTextureResolution;
            public void Apply()
            {
                PipelineSettings.GLTFName = GLTFName;
                PipelineSettings.XREProjectFolder = this.XREProjectFolder;
                PipelineSettings.ExportColliders = this.ExportColliders;
                PipelineSettings.ExportSkybox = this.ExportSkybox;
                PipelineSettings.lightmapMode = this.lightmapMode;
                PipelineSettings.CombinedTextureResolution = this.CombinedTextureResolution;
            }
            public void Set()
            {
                GLTFName = PipelineSettings.GLTFName;
                XREProjectFolder = PipelineSettings.XREProjectFolder;
                ExportColliders = PipelineSettings.ExportColliders;
                ExportSkybox = PipelineSettings.ExportSkybox;
                lightmapMode = PipelineSettings.lightmapMode;
                CombinedTextureResolution = PipelineSettings.CombinedTextureResolution;
            }
        }

        public static readonly string ConversionFolder = Application.dataPath + "/../Outputs/GLTF/";
        public static readonly string PipelineFolder = Application.dataPath + "/../Pipeline/";
        public static readonly string configFile = PipelineFolder + "settings.conf";

        public static string GLTFName;
        public static string XREProjectFolder;// = Application.dataPath + "/../Outputs/GLB/";
        public static string XRELocalPath => "https://localhost:8642/" + 
            Regex.Match(XREProjectFolder, @"[\w-]+[\\/]+[\w-]+[\\/]*$").Value;
        
        public static bool ExportColliders;
        public static bool ExportSkybox;

        public static LightmapMode lightmapMode;

        public static int CombinedTextureResolution = 4096;
        
        static PipelineSettings()
        {
            ReadSettingsFromConfig();
        }

        public static void ReadSettingsFromConfig()
        {
            var config = new FileInfo(configFile);
            if (!config.Exists)
            {
                return;
            }
            var data = JsonConvert.DeserializeObject<Data>
            (
                File.ReadAllText(configFile)
            );
            data.Apply();
        }

        public static void SaveSettings()
        {
            var data = new Data();
            data.Set();
            File.WriteAllText(configFile, JsonConvert.SerializeObject(data, Formatting.Indented));
        }

        internal static void ClearPipelineJunk()
        {
            Regex filter = new Regex(@".*\.(jpg|png|tga)");
            var pipelineFiles = Directory.GetFiles(PipelineFolder);
            foreach(var path in pipelineFiles)
            {
                if(filter.IsMatch(path))
                {
                    File.Delete(path);
                }
            }
        }
    }

    
}

