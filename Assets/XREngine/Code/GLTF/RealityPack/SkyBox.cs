using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace XREngine.GLTF
{
    public class SkyBox : RPComponent
    {
        public override string Type => base.Type + ".skybox";

        public override JProperty Serialized => new JProperty("extras", new JObject(
            new JProperty(Type + ".backgroundType", 1),
            new JProperty(Type + ".cubemapPath", PipelineSettings.XRELocalPath + "/cubemap/"),
            new JProperty("realitypack.entity", transform.name)
        ));
    }

}
