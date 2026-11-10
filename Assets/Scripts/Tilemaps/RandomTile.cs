using System;
using UnityEngine;
using UnityEngine.Tilemaps;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Tilemaps
{
    /// <summary>
    /// Random Tile estándar de 2D Tilemap Extras.
    /// Selecciona de manera pseudoaleatoria y determinista un sprite de una lista según la posición en la cuadrícula.
    /// </summary>
    [Serializable]
    [CreateAssetMenu(fileName = "New Random Tile", menuName = "2D/Tiles/Random Tile")]
    public class RandomTile : Tile
    {
        [Tooltip("Lista de sprites a seleccionar aleatoriamente (ej. hielo_01..12).")]
        [SerializeField]
        public Sprite[] m_Sprites;

        public override void GetTileData(Vector3Int location, ITilemap tileMap, ref TileData tileData)
        {
            base.GetTileData(location, tileMap, ref tileData);

            if (m_Sprites != null && m_Sprites.Length > 0)
            {
                long hash = location.x;
                hash = (hash * 397) ^ location.y;
                hash = (hash * 397) ^ location.z;
                hash = (hash * 397) ^ (long)tileData.color.GetHashCode();
                int index = (int)Math.Abs(hash % m_Sprites.Length);
                tileData.sprite = m_Sprites[index];
            }

            // Preservar el tipo de collider configurado en el Tile (Sprite por defecto)
            tileData.colliderType = this.colliderType;
        }

#if UNITY_EDITOR
        [MenuItem("Assets/Create/2D/Tiles/Random Tile")]
        public static void CreateRandomTile()
        {
            string path = EditorUtility.SaveFilePanelInProject("Save Random Tile", "New Random Tile", "asset", "Save Random Tile", "Assets");
            if (string.IsNullOrEmpty(path))
                return;

            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<RandomTile>(), path);
        }
#endif
    }
}
