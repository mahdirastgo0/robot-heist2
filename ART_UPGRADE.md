# Robot Heist – Coastal Art Update

Based on Visual & Mobility Update (Unity 2022.1.24f1, Built-in Render Pipeline). Open the `unity` directory in Unity Hub and wait for model/texture importing, then Play. The Windows native DLL is retained. No network access is required for the included changes.

## Imported assets and gameplay
- `Sport car - gta v.obj`: Sport Coupe ($9,500). Original MTL absent; replacement red paint, glass, light, plastic and metal materials generated from material names.
- `68-transportation-001.zip`: Range Rover SUV ($4,500). Original OBJ uses wire colors and does not assign the supplied photos; neutral replacement car materials are used. The free rental retains the pre-existing Defender model instead of a primitive car.
- `25-mustang-13.04.2021-obj.zip`: Mustang GT ($14,500). Removed studio backdrop, lights and extremely dense bolt mesh (660,480 source faces); remaining 66,085 polygon faces. Missing MTL rebuilt with material-name-based paint, glass and metal colors.
- `41-formula-1.rar`: Formula Racer ($26,000). Uses original diffuse and normal maps and replacement metallic/smoothness settings, not the original full spec/gloss pipeline. Original artist requests attribution: **Dil Afroze Ahmad**. See `OptionalSourceModels/Formula1-credit.txt`.
- `22-trees_9_obj (1).zip`: six individually separated, UV-textured tree models with lower-detail procedural LODs. Tree trunks retain independent gameplay collision; other trees use the original fallback.
- `35-rp_nathan_animated_003_walking_max.zip`: Nathan FBX and textures. Imported as an animated decorative pedestrian on each island's promenade, using its supplied looping walk if Unity imports a playable legacy animation clip. If no clip imports, the pedestrian is skipped. Player/combat rigs are unchanged.
- `32-livingroom_fbx.rar`: living room FBX; sofa-named renderer groups used in the base with replacement fabric material where texture is absent. If no matching renderers import, the existing base sofa remains. Other room geometry is disabled to prevent walls/furniture overlapping gameplay.
- `96-couch.zip`: original Blender couch preserved in `OptionalSourceModels/couch.blend`. Blender is unavailable in the packaging environment, so this model was not converted or automatically instantiated. Export it from Blender as FBX and choose **Robot Heist > Art > Import exported couch FBX**. It then takes priority over the living-room sofa.

Vehicles use the previous arcade physics and standardized collision size. Imported wheels may be static visually because their meshes are not consistently separated and rigged. Cars are automatically fitted to the collision envelope. Exact mesh orientation and dimensions require visual verification in Unity. The previous player movement speeds (8 m/s normal, 12 m/s sprint) and jump/crouch/aim controls are preserved. The additional vehicles are stored in the existing per-server/cycle garage ownership mask. Press V in base to select a car for the next land.

## Environment
23 new 2048x2048 maps: seven upgraded albedo/normal/metallic-smoothness sets (asphalt, brick, concrete, plaster, pavement, sand and metal), facade glass albedo, and a new water normal. The color textures build on the previous project's existing textures with local noise/weathering; they are not newly downloaded photographs. Roads use smaller repeat spacing. Building glass has reflective PBR material. The Built-in coastal water shader blends two moving normals, shallow/deep colors, shoreline foam and angle-dependent reflection. It is an opaque stylized coastal shader, not a fluid simulation. Standard material fallback remains available.

## Optional online CC0 assets
Network downloads failed in the packaging environment; the following optional sources are NOT embedded.
- Kenney City Kit (Commercial), CC0: https://kenney.nl/assets/city-kit-commercial — 50 models. In Unity, **Robot Heist > Art > Install Kenney CC0 commercial buildings**, or download ZIP manually and use **Import local Kenney city ZIP**. Non-interactive apartment blocks automatically use imported models on next generation; shop layouts are retained. Model materials may need inspection after import.
- Poly Haven: https://polyhaven.com/ — previous editor action **Robot Heist > Art > Download CC0 PBR textures (2K)** can replace the included environment textures where downloads succeed.

User-supplied assets are not verified CC0; confirm your rights before redistribution/commercial release, especially the GTA-labelled asset and Renderpeople model. Third-party brand names used only to identify the supplied assets.

## Validation
- Native C++ test.cpp compiled and passed (`ALL OK`).
- All 10 new OBJ files checked for valid vertex/UV/normal references, material names and MTL diffuse paths.
- All 23 new 2K textures verified readable. Changed C# source delimiters checked; this is not a C# compilation.
- Unity Editor/Blender unavailable here. Unity model import, shader compilation, frame rate, runtime collision/animation, visual quality and optional online installers have not been tested. On first run: check Console, open garage and test each vehicle, walk around beach/bridges, verify the water shader and pedestrian, visit base and shops, check frame rate. Existing fallbacks are kept where possible.
