using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridData
{
    Dictionary<Vector3Int, PlacementData> placedObjects = new();

    public void AddObjectAt(Vector3Int gridPosition, Vector2Int objectSize, int ID, int PlacedObjectIndex, UpgradeSystem upgradeSystem)
    {
        List<Vector3Int> positionToOccupy = CalculatePositions(gridPosition, objectSize);
        PlacementData data = new PlacementData(positionToOccupy, ID, PlacedObjectIndex, upgradeSystem);
        foreach (var pos in positionToOccupy)
        {
            if (placedObjects.ContainsKey(pos))
                throw new Exception($"Dictionary already contrains this call position {pos}");
            placedObjects[pos] = data;
        }
    }

    public void RemoveObjectAt(Vector3Int gridPosition)
    {
        if (placedObjects.ContainsKey(gridPosition))
        {
            placedObjects.Remove(gridPosition);
        }
    }

    private List<Vector3Int> CalculatePositions(Vector3Int gridPosition, Vector2Int objectSize)
    {
        List<Vector3Int> returnVal = new();
        for (int x = 0; x < objectSize.x; x++)
        {
            for (int y = 0; y < objectSize.y; y++)
            {
                returnVal.Add(gridPosition + new Vector3Int(x, 0, y));
            }
        }
        return returnVal;
    }

    public bool CanPlaceObjectAt(Vector3Int gridPosition, Vector2Int objectSize)
    {
        List<Vector3Int> positionToOccupy = CalculatePositions(gridPosition, objectSize);
        foreach (var pos in positionToOccupy)
        {
            if (placedObjects.ContainsKey(pos))
                return false;
        }
        return true;
    }
    public int GetObjectIndexAt(Vector3Int gridPosition)
    {
        if (placedObjects.ContainsKey(gridPosition) == false)
            return -1;
        return placedObjects[gridPosition].PlacedObjectIndex;
    }
    public PlacementData GetPlacementDataAt(Vector3Int gridPosition)
    {
        if (placedObjects.ContainsKey(gridPosition) == false)
            return null;
        return placedObjects[gridPosition];
    }
}


public class PlacementData
{
    UpgradeSystem upgradeSystem;
    public List<Vector3Int> occupiedPositions;

    public int ID { get; private set; }

    public int PlacedObjectIndex { get; private set; }

    public int UpgradeLevel { get; private set; } 

    public PlacementData(List<Vector3Int> occupiedPositions, int iD, int placedObjectIndex, UpgradeSystem upgradeSystem)
    {
        this.occupiedPositions = occupiedPositions;
        ID = iD;
        PlacedObjectIndex = placedObjectIndex;
        UpgradeLevel = 0;
        this.upgradeSystem = upgradeSystem;
    }
    public bool UpgradeTier()
    {
        if (UpgradeLevel >= 2)
        {
            Debug.Log("Already max level");
            return false;
        }

        if (upgradeSystem == null)
        {
            Debug.LogError("upgradeSystem is NULL - was it assigned when the tower was placed?");
            return false;
        }

        UpgradeLevel++;
        upgradeSystem.Upgrade();
        return true;
    }

}