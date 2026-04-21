using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "PainterPalette", menuName = "TD Tools/PainterPalette")]
public class PainterPalette : ScriptableObject
{
    public List<GameObject> TerrainPrefabs = new List<GameObject>(); // List of prefabs to paint from

    /// <summary>
    /// random size of the painted prefabs
    /// </summary>
    public float minScale = 0.5f; 
    public float maxScale = 1.5f;
}
