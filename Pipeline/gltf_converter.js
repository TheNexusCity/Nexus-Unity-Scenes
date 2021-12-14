var tool = require('gltf-import-export')

args = process.argv.slice(2)
name = args[0]
const inputGLTF = "../Outputs/GLTF/" + name + ".gltf";
const newFile = "../Outputs/GLB/" + name + ".glb";

tool.ConvertGltfToGLB(inputGLTF, newFile)
