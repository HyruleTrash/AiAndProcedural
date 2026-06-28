using UnityEngine;
using UnityEngine.UIElements;

namespace Generation
{
    /// <summary>
    /// Holds a reference to the current position and generated grid of rooms
    /// </summary>
    public class WorldGenSnapshot
    {
        private GridRuntime? gridRuntime;
        private Vector2Int currentPosition;

        public WorldGenSnapshot(GridRuntime? gridRuntime = null, Vector2Int currentPosition = new())
        {
            this.gridRuntime = gridRuntime;
            this.currentPosition = currentPosition;
        }

        public void Update(GridRuntime newGrid, Vector2Int newPos)
        {
            this.gridRuntime = newGrid;
            this.currentPosition = newPos;
        }
        
        /// <summary>
        /// A static function that creates a texture for a sprite renderer to use, based on the snapshot
        /// </summary>
        public static void GenDebugTex(WorldGenSnapshot snapshot, SpriteRenderer renderer)
        {
            if (snapshot.gridRuntime == null) return;
            Vector2Int size = snapshot.gridRuntime.GetWorldSize(out Vector2Int offset);
            
            // Adding a border
            size += Vector2Int.one * 200;
            offset += Vector2Int.one * 100;
                
            Texture2D worldGenTex = new(size.x, size.y, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                anisoLevel = 0
            };

            Color[] pixels = snapshot.gridRuntime.GetPixels(size, offset);
            int posIndex = (snapshot.currentPosition.y + offset.y) * size.x + snapshot.currentPosition.x + offset.x;
            pixels[posIndex] = Color.blueViolet;
            
            worldGenTex.SetPixels(pixels);
            worldGenTex.Apply();
                
            Rect rect = new(0, 0, worldGenTex.width, worldGenTex.height);
            Sprite newSprite = Sprite.Create(worldGenTex, rect, new Vector2(0.5f, 0.5f), 32f);

            renderer.sprite = newSprite;
        }
    }
}