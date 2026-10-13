using System.Collections.Generic;

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
                indexOfX += 0.5f;
            }

            indexOfX = startOfX;
            indexOfY -= 0.5f;
        }

        return gridInfo;
    }
}