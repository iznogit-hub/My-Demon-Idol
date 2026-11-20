# College Raimon — Unity environment

Editor target: Unity 6000.2.13f1. Render packages: Unity URP / HDRP 17.2.0.
The main package opens in URP. It includes optional Built-in and HDRP adaptations.
Use one active render pipeline per project and Linear colour space.

## URP
1. Import the main package in a Unity 6000.2.13f1 URP project.
2. In Project Settings > Graphics and the active Quality level, assign the
   supplied URP pipeline asset from Assets/CollegeRaimon/RenderSettings/URP.
3. Open Assets/CollegeRaimon/Demo/CollegeRaimon_Demo.unity.

## Built-in or HDRP
Import the main package first, then open the matching .unitypackage in
Assets/CollegeRaimon/RenderPipelineSupport. Keep every file selected in its import dialog.
For Built-in, clear the Render Pipeline Asset in Graphics and each active
Quality level. For HDRP, install Unity HDRP 17.2.0 and select its supplied pipeline
asset from Assets/CollegeRaimon/RenderSettings/HDRP in those settings.
Reopen the demonstration scene after switching. Reimport the main package to
restore URP materials. Do not apply both adaptations to the same project.

URP 17.2.0 remains an installed dependency because its inactive base shaders and
settings remain in the catalogue. Installing that package does not activate URP.
This content does not change global project settings through an editor script.

## Reuse and editing
Prefabs/CollegeRaimon.prefab is the assembled environment. Materials keep their GUIDs when
an adaptation is imported; common meshes and textures are not duplicated by a
support package. Textures contains the original PNG/JPEG bytes with native import
settings. Preserve their source dimensions, colour space and sampler settings.
Source-authored non-power-of-two dimensions are retained without resampling.

SourceModels contains editable FBX geometry. Prefabs/SourceModels contains the
individual FBX-backed prefabs. Editing an FBX updates its individual prefab;
the assembled environment uses separate native mesh assets and is not rebuilt
automatically. Opaque environment surfaces receive mesh collision; visual grass
identified by the source collision policy remains nonblocking. The exact policy
and material bindings are recorded under Documentation and SourceModels.

## Shaders and source
The delivered shaders are independently authored from texture × factor × vertex
RGBA operations and Unity rendering APIs. No glTFast importer or shader code is
included or required in the delivered content. PBR uses metallic 0 / roughness 1
for this source profile; Unlit preserves the source's unlit material semantics.
Lighting across the three render pipelines is not promised pixel-identical.

Source file: CollegeRaimon.glb
Source SHA-256: 8a910d057c0a14f873d0522929b8992a32a4cbdbf2bdc9eec2a5aab9ecb2e519
Provenance: Source fournie par Theo: IE_W40_CollegeRaimon_VF.glb. Auteur et licence de redistribution non renseignes; aucun changement de provenance. Copie FINAL du 05/10/2026 filtrée selon la décision approuvée final_root_decision_20261005.json ; original conservé intact, matériaux/images/BIN conservés.

No affiliation or rights to third-party models, textures or brands are inferred.
Validation reports and render captures accompany the technical candidate.
No Asset Store publication is performed by this pipeline.
