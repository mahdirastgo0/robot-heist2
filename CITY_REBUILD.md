# City rebuild and map hotfix

This version is based on Robot-Heist-Coastal-Art-Fixed. Unity 2022.1.24f1 project in `unity/`.

## Included and active by default
- Formula car: changed only its imported visual yaw from 90 to 270 degrees. This turns it 180 degrees relative to the unchanged +Z driving direction.
- Map: player or occupied car tracked in world coordinates; a rectangular map sized from island extents, beach margins, and screen dimensions; consistent map projection for terrain and markers; cached directional marker textures (no GUI pivot transforms); visible cyan marker and YOU label.
- New offline, locally authored OBJ meshes: HarborResidence, SeasideHotel and UrbanLofts. They contain roof profiles, window frames/mullions/sills, balconies, railing geometry and rooftop features, with material slots mapped to existing textured materials. These are architectural game meshes, not photogrammetry or assets downloaded from the Internet. Used for non-interactive residential blocks; shops retain their usable interiors.
- The uploaded SUV, Sport and Mustang models are used for up to nine parked cars per city. Free rental uses the SUV model if no distinct Rental prefab is available. Other parked cars retain the existing model for performance.
- Shop upper-story windows have deeper frames, mullions, sills, and selected air conditioners. Existing city progression, physics, store doors, core and movement remain intact.

## Optional online models (requires working internet on your PC)
In the Unity editor, before entering Play Mode, use **Robot Heist > Art > Download free detailed buildings (Grab3D)**. This downloads two actual textured OBJ ZIP packs, Brownstone House and Colonial House, individually licensed CC0 by Grab3D:
- https://grab3d.com/models/buildings/brownstone-house/
- https://grab3d.com/models/buildings/colonial-house/
The downloads were not accessible in the build environment, so the third-party models are NOT bundled and their import is untested. If the direct download fails, select OBJ ZIP on the source page and use **Robot Heist > Art > Import detailed building OBJ ZIP**. Alternatively put complete building FBX/OBJ files and their materials/textures in `Assets/Resources/ExternalArt/City/` (not modular pieces). If externally imported city models are present, they take priority over the locally authored models for residential blocks. Imported models are fitted to the original plot; shop interiors aren't replaced. Verify third-party model licenses and material appearance in Unity. For more realistic modular building art, see Poly Haven's CC0 Modular Urban Apartments Facade by James Ray Cock, https://polyhaven.com/a/modular_urban_apartments_facade; it requires assembling individual modules and is NOT included.

## Verification and limitations
- `python validation/check_city_map.py` passed: world-to-map and inverse projection checks for sample map dimensions at 4 widths, beach margins and cardinal directions; all three OBJ meshes have valid triangle and UV indices and nondegenerate triangles.
- Native C++ `g++ -std=c++17 -O2 core.cpp test.cpp ...` passed (ALL OK).
- Unity Editor is not installed here: C# compilation, frame rate, actual visual quality, car heading and marker appearance are not runtime-confirmed. Original character FBX warning may remain. The generated architecture is not equivalent to a fully photoreal city. No changes to native core or player save format.
