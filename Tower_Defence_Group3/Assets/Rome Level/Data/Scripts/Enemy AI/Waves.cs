using UnityEngine;

[CreateAssetMenu(fileName = "Waves", menuName = "Scriptable Objects/Waves")]

public class Waves : ScriptableObject
{
    public bool mixGroups = false;
    public AIGroup[] aiGroups;
}
[System.Serializable]
public class AIGroup
{
    public GameObject aiPrefab;
    public int aiCount;
    public float spawnRate;
}
