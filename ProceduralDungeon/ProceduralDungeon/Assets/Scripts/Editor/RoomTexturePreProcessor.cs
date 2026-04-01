using System;
using System.IO;
using Generation;
using UnityEditor;
using UnityEngine;

public class RoomTexturePreProcessor : AssetPostprocessor
{
    private const string TargetFolder = "Assets/Rooms";

    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(TargetFolder)) return;
        
        var importer = (TextureImporter)assetImporter;

        importer.textureType = TextureImporterType.Default;
        importer.textureShape = TextureImporterShape.Texture2D;
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency = true;
        importer.isReadable = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.anisoLevel = 1;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
    }
    
    private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        foreach (string path in importedAssets)
        {
            if (!path.StartsWith(TargetFolder) || !IsTextureFile(path)) continue;
            RoomTileLookup.roomDataHasBeenUpdated.Invoke();
            Debug.Log("New rooms have been added, or have updated, make sure to save dirty room lists!");
            break;
        }
    }
    
    private static bool IsTextureFile(string path)
    {
        var extension = Path.GetExtension(path).ToLower();
        return extension == ".png" || extension == ".jpg" || extension == ".jpeg";
    }
}