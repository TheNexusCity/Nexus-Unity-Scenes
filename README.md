# The Nexus
A downtown district in a massive space station. A collaborative universe we can build together.

Watch this space. We're just getting started.

## What is this?
The development repository for The Nexus project and all related planning and tracking.

The Nexus is the downtown nightlife and entertainment district on Freeside Station. Featuring live music, entertainment, shopping and much more, it's neutral ground for teams to meet and build together.

The first layer is hosted by Laguna Labs on behalf of Freeside Station Group, a DAO which will be owned by the core team, partners, builders and of course users.

All assets will be released CC0, with MIT code, for anyone to use or remix for any reason. Connection to the first layer will be curated by the Nexus community board to ensure a high quality, optimized experience for users, but teams are encouraged to host their own layers and use what we build here as a basis for what they feel is important for their community.

While this project will be delivered initially on the open source XREngine codebase, we hope to encourage teams to port and interop anywhere. XREngine is a full-stack meta-MMO solution that right now has a WebGL client, but will soon be adding support for Unreal Engine and Unity.

## Who is working on it?
Right now, it is being primarily built by partners in the XRFoundation / XREngine ecosystem, including Laguna Labs, SuperReality, Wild Capture and others.

## How do I get involved?
If you are committed to delivering a high quality, XR-compatible experience and want to participate in a shared sci-fi mashup universe with us then we will work to support you with the space and tools to build, moderate and experiment.

# Usage

## System Requirements
NodeJS >12.0
Unity >2020

## Getting Started
Export any Unity scene as a GLB with menu item XREngine->Export Scene. This will bring up an export configuration window. 

### Export Parameters
Name: name of the GLTF and GLB files. Enter without file extension.

Set Output Directory: By default, the scene will be exported into the /Outputs/GLB/ folder in the project.

Export Colliders: Toggles whether collider data will be included in export. Currently only box and mesh colliders are supported.

Export: Begins an export. Note that if you have a gameobject selected in editor, then only the selection is exported.

### Supported Components
Lights: point and direction light are currently supported.

Cameras: 

Lightmaps: lightmaps are automatically combined with the diffuse channel and reprojected onto the mesh's uv0, then exported as an unlit material. Note that this will cause issues with instanced geometry.

Colliders: box and mesh colliders are automatically configured and exported in XREngine compatible format.

LODs: LOD Groups in Unity are automatically configured and exported. 

Instancing: Any Gameobjects with share the same mesh and material will be instanced by default. Currently only meshes with one material are supported. As previously noted, baking lightmaps onto Gameobjects that share the same mesh and material will break instancing.


## Known Issues

### Materials Black After Error During Export
In general, exceptions thrown during the SeinJS export will result in all materials in the scene being black. Quickly fix this after it occurs by selecting menu item SeinJS->Restore Materials.

### No Default Materials Allowed
Every material in the scene must be a project asset that resides somewhere within the Assets folder. Materials from unity's default asset registry will cause the exporter to fail.

