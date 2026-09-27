using UnityEngine;
using System;
using System.Collections.Generic;

public class DataManager : MonoBehaviour 
{
    private static DataManager _instance;
    public static DataManager Instance => _instance;

    public Dictionary<string, WeaponData> WeaponDatas { get; private set; }
    public Dictionary<ProjectileType, ProjectileData> ProjectileDatas { get; private set; }
    
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
        for (int i = 1; i < datas.Length; i++)//pass Header
        {
            string[] row = datas[i].Split(',');
            WeaponDatas[row[0]] = new WeaponData
            {
                Name = row[1],
                Type = (FireType)Enum.Parse(typeof(FireType), row[2]),
                FireRate = float.Parse(row[3]),
                ProjectileCount = int.Parse(row[4]),
                ProjectileType = (ProjectileType)Enum.Parse(typeof(ProjectileType), row[5]),
                Rebound = float.Parse(row[6]),
                MagazineSize = int.Parse(row[7]),
                ReloadTime = float.Parse(row[8]),
                ChargeTime = float.Parse(row[9]),
                Angle = float.Parse(row[10]),
                Range = float.Parse(row[11]),
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
        for (int i = 1; i < datas.Length; i++)//pass Header
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
}