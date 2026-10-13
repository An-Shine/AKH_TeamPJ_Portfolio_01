using System.Collections.Generic;

public class GridPos
{
    public int X;
    public int Y;

    public GridPos(int x, int y)
    {
        X = x;
        Y = y;
    }

    public static List<GridPos> CreateGridInfo(LootSpawnInfo lootSpawnInfo)
    {
        List<GridPos> gridInfo = new List<GridPos>();

        int startOfX = lootSpawnInfo.SpawnPointLeftUp.x;
        int startOfY = lootSpawnInfo.SpawnPointLeftUp.y;
        int endOfX = lootSpawnInfo.SpawnPointRightDown.x;
        int endOfY = lootSpawnInfo.SpawnPointRightDown.y;

        int indexOfX = startOfX;
        int indexOfY = startOfY;

        GridPos grid;
        while (indexOfY >= endOfY)
        {
            while (indexOfX <= endOfX)
            {
                grid = new GridPos(indexOfX, indexOfY);
                gridInfo.Add(grid);
                indexOfX += 1;
            }

            indexOfX = startOfX;
            indexOfY -= 1;
        }

        return gridInfo;
    }
}