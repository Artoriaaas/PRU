using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class TextureImportHelper
{
    static TextureImportHelper()
    {
        EditorApplication.delayCall += () => {
            // Apply transparency filtering to jpg files first if they exist
            MakeTransparent("Assets/Resources/CustomUI/QuocTuan.jpg", "Assets/Resources/CustomUI/QuocTuan.png");
            MakeTransparent("Assets/Resources/CustomUI/ToaDo.jpg", "Assets/Resources/CustomUI/ToaDo.png");

            string[] paths = {
                "Assets/Resources/CustomUI/ThanhTongL.png",
                "Assets/Resources/CustomUI/ThanhTongR.png",
                "Assets/Resources/CustomUI/QuocTuan.png",
                "Assets/Resources/CustomUI/ToaDo.png"
            };
            foreach (var path in paths)
            {
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null && (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single))
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.alphaIsTransparency = true;
                    importer.SaveAndReimport();
                    Debug.Log("Configured " + path + " as Sprite (Single)");
                }
            }
        };
    }

    private static void MakeTransparent(string srcPath, string dstPath)
    {
        if (!System.IO.File.Exists(srcPath)) return;

        // First configure src as readable texture
        TextureImporter importer = AssetImporter.GetAtPath(srcPath) as TextureImporter;
        if (importer != null)
        {
            importer.isReadable = true;
            importer.textureType = TextureImporterType.Default; // Temporary to read raw bytes
            importer.SaveAndReimport();
        }

        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(srcPath);
        if (tex != null)
        {
            int w = tex.width;
            int h = tex.height;
            Texture2D transparentTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] pixels = tex.GetPixels();
            for (int i = 0; i < pixels.Length; i++)
            {
                Color c = pixels[i];
                // Black pixel filter
                if (c.r < 0.08f && c.g < 0.08f && c.b < 0.08f)
                {
                    pixels[i] = Color.clear;
                }
            }
            transparentTex.SetPixels(pixels);
            transparentTex.Apply();

            byte[] pngData = transparentTex.EncodeToPNG();
            System.IO.File.WriteAllBytes(dstPath, pngData);
            AssetDatabase.ImportAsset(dstPath);

            // Delete the old .jpg
            AssetDatabase.DeleteAsset(srcPath);
            Debug.Log("Converted " + srcPath + " to transparent " + dstPath);
        }
    }
}
