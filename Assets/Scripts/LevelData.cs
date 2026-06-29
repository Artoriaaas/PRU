using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct EnemyPlacement
{
    public int row;
    public int column;
    public int unitTypeIndex;
}

[CreateAssetMenu(fileName = "NewLevelData", menuName = "PRU/Level Data", order = 1)]
public class LevelData : ScriptableObject
{
    public List<EnemyPlacement> enemyPlacements = new List<EnemyPlacement>();
}
