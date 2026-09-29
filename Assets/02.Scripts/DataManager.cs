using UnityEngine;
using System;
using System.Collections.Generic;

public class DataManager : MonoBehaviour
{
    private static DataManager _instance;
    public static DataManager Instance => _instance;
    public Dictionary<string, WeaponData> WeaponDatas { get; private set; }
    public Dictionary<ProjectileType, ProjectileData> ProjectileDatas { get; private set; }
    public SavePlayerStatus PlayerStatusDatas { get; private set; }

    private void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
        }

        _instance = this;

        LoadWeaponCSV();
        LoadProjectileCSV();
    }

    private void LoadWeaponCSV()
    {
        TextAsset csvData = Resources.Load("Datas/Weapon") as TextAsset;
        if (csvData == null)
        {
            Debug.LogWarning("Weapon data not found");
            return;
        }

        WeaponDatas = new Dictionary<string, WeaponData>();
        string[] datas = csvData.text.Split(new char[] { '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        for (int i = 1; i < datas.Length; i++) //pass Header
        {
            string[] row = datas[i].Split(',');
            WeaponDatas[row[0]] = new WeaponData
            {
                Name = row[1],
                BehaviourType = (FireBehaviourType)Enum.Parse(typeof(FireBehaviourType), row[2]),
                PatternType = (FirePatternType)Enum.Parse(typeof(FirePatternType), row[3]),
                ProjectileType = (ProjectileType)Enum.Parse(typeof(ProjectileType), row[4]),
                FireRate = float.Parse(row[5]),
                ProjectileCount = int.Parse(row[6]),
                Rebound = float.Parse(row[7]),
                MagazineSize = int.Parse(row[8]),
                ReloadTime = float.Parse(row[9]),
                ChargeTime = float.Parse(row[10]),
                Angle = float.Parse(row[11]),
                Range = float.Parse(row[12]),
            };
        }
    }

    private void LoadProjectileCSV()
    {
        TextAsset csvData = Resources.Load("Datas/Projectile") as TextAsset;
        if (csvData == null)
        {
            Debug.LogWarning("Projectile data not found");
            return;
        }

        ProjectileType projectileType;
        ProjectileDatas = new Dictionary<ProjectileType, ProjectileData>();
        string[] datas = csvData.text.Split(new char[] { '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        for (int i = 1; i < datas.Length; i++) //pass Header
        {
            string[] row = datas[i].Split(',');
            projectileType = (ProjectileType)Enum.Parse(typeof(ProjectileType), row[0]);
            ProjectileDatas[projectileType] = new ProjectileData
            {
                Damage = int.Parse(row[1]),
                SpeedScalar = float.Parse(row[2]),
                Durability = int.Parse(row[3]),
            };
        }
    }

    private void LoadPlayerStatusCSV()
    {
        TextAsset csvData = Resources.Load("Datas/PlayerStatus") as TextAsset;
        if (csvData == null)
        {
            Debug.LogWarning("PlayerStatus data not found");
            return;
        }

        string[] datas = csvData.text.Split(new char[] { '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        string[] row = datas[1].Split(','); //datas[0]은 열 이름 있음
        PlayerStatusDatas = new SavePlayerStatus
        (
            float.Parse(row[1]),
            float.Parse(row[2]),
            float.Parse(row[3]),
            float.Parse(row[4]),
            float.Parse(row[5]),
            float.Parse(row[6]),
            float.Parse(row[7]),
            int.Parse(row[8]),
            int.Parse(row[9]),
            int.Parse(row[10])
        );
    }
}