#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class MapSceneTexturePostprocessor : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        string lowerPath = assetPath.ToLowerInvariant();

        if (lowerPath.Contains("resources/mapscene"))
        {
            TextureImporter importer = assetImporter as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.isReadable = true;
                importer.filterMode = FilterMode.Bilinear;
                importer.wrapMode = TextureWrapMode.Clamp;
            }
        }
    }
}
#endif
