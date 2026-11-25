Robot Heist Coastal Art – hotfix 1

Close Unity and back up your project. Extract this patch into the Unity project folder (the folder containing Assets and ProjectSettings), allowing the two files under Assets to overwrite the originals. Keep the existing .meta files. Reopen Unity, wait for import, and check the Console before pressing Play.

Fixed C# CS0136 local variable name collision in GameManager.Trees.cs: the distant LOD variable is now distantVariant.
Mustang.obj: converted 66085 faces to 148654 nondegenerate triangles while preserving vertex/UV/normal references and materials. 170 irregular outlines required best-effort triangulation; 248 degenerate triangles omitted. This avoids nontriangular self-intersecting polygon input but cannot guarantee perfect appearance.
The pre-existing Survival_Character.fbx jacket warning is not changed. Unity is unavailable here, so compilation and rendering have not been verified. If a red compiler error remains, send the exact first CSxxxx error with file and line number.
