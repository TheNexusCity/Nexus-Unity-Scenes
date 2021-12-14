using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.IO;
using System.Text.RegularExpressions;


namespace XREngine
{
    public class PipelineSettings
    {

        public static string ConversionFolder = Application.dataPath + "/../Outputs/GLTF/";
        public static string DefaultExportFolder = Application.dataPath + "/../Outputs/GLB/";

        public static string PipelineFolder = Application.dataPath + "/../Pipeline/";
        public static bool ExportColliders;

        public static bool CombineLightmaps;

        public static int CombinedTextureResolution = 2048;

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

