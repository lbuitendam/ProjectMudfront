using UnityEditor;
using UnityEngine;

namespace Mudfront.EditorTools
{
    /// <summary>Import rules for Blender exports in Art/Models: metres, baked axes, mesh colliders on kit/cover, matte toon-ish materials.</summary>
    public class MudfrontModelImporter : AssetPostprocessor
    {
        const string Folder = "Assets/_Mudfront/Art/Models/";

        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(Folder)) return;
            var mi = (ModelImporter)assetImporter;
            mi.globalScale = 1f;
            mi.useFileScale = true;
            mi.bakeAxisConversion = true;
            mi.importAnimation = false;
            mi.importCameras = false;
            mi.importLights = false;
            mi.animationType = ModelImporterAnimationType.None;
            mi.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            mi.materialLocation = ModelImporterMaterialLocation.InPrefab;
            string file = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            // static world pieces get mesh colliders; vehicles/characters get primitive colliders in code
            mi.addCollider = file.StartsWith("SM_Kit_") || file.StartsWith("SM_Cover_");
        }

        void OnPostprocessMaterial(Material m)
        {
            if (!assetPath.StartsWith(Folder)) return;
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.08f);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
            if (m.HasProperty("_SpecularHighlights")) m.SetFloat("_SpecularHighlights", 0f);
            m.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
        }
    }
}
