using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GridPos
{
    public float X;
    public float Y;

    public GridPos(float x, float y)
    {
        X = x;
        Y = y;
    }

    public static List<GridPos> CreateGridInfo(LootSpawnInfo lootSpawnInfo)
    {
        List<GridPos> gridInfo = new List<GridPos>();

        float startOfX = lootSpawnInfo.SpawnPointLeftUp.x;
        float startOfY = lootSpawnInfo.SpawnPointLeftUp.y;
        float endOfX = lootSpawnInfo.SpawnPointRightDown.x;
        float endOfY = lootSpawnInfo.SpawnPointRightDown.y;

        float indexOfX = startOfX;
        float indexOfY = startOfY;

        GridPos grid;
        while (indexOfY >= endOfY)
        {
            while (indexOfX <= endOfX)
            {
                grid = new GridPos(indexOfX, indexOfY);
                gridInfo.Add(grid);
                indexOfX += 0.25f;
            }

            indexOfX = startOfX;
            indexOfY -= 0.25f;
        }

        return gridInfo;
    }

    public static Vector3 GetPosition(List<GridPos> gridList, Vector2 checksize, LayerMask targetMask)
    {
        int targetIndex;
        int count;
        Collider2D hit;

        List<GridPos> currentGridInfoList = gridList.ToList();
        Vector3 targetPosition = new Vector3(0, 0, 0);
        int repeat = currentGridInfoList.Count;

        for (int index = 0; index < repeat; index++)
        {
            count = currentGridInfoList.Count;
            targetIndex = UnityEngine.Random.Range(0, count);
            targetPosition.x = currentGridInfoList[targetIndex].X;
            targetPosition.y = currentGridInfoList[targetIndex].Y;

            currentGridInfoList.RemoveAt(targetIndex);

            hit = Physics2D.OverlapBox(
                targetPosition,
                checksize,
                0,
                targetMask);

            if (hit == null)
            {
                break;
            }
        }

        return targetPosition;
    }
}