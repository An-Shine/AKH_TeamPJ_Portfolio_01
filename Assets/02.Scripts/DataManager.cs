using UnityEngine;
using System;
using System.Collections.Generic;

public class DataManager : MonoBehaviour 
{
    private static  DataManager _instance;
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
        for (int i = 0; i < datas.Length; i++)
        {
            string[] row = datas[i].Split(',');
            WeaponDatas[row[0]] = new WeaponData
            {
                ShootingTerm =  float.Parse(row[1]),
                ProjectileCount = int.Parse(row[2]),
                ProjectileType = (ProjectileType)Enum.Parse(typeof(ProjectileType), row[3]),
                Rebound = float.Parse(row[4]),
                MagazineSize = int.Parse(row[5]),
                ReloadTime = float.Parse(row[6]),
                ChargeTime = float.Parse(row[7])
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
        for (int i = 0; i < datas.Length; i++)
        {
            string[] row = datas[i].Split(',');
            projectileType = (ProjectileType)Enum.Parse(typeof(ProjectileType), row[0]);
            ProjectileDatas[projectileType] = new ProjectileData
            {
                Damage = int.Parse(row[1]),
                SpeedScalar = float.Parse(row[2]),
                Range = float.Parse(row[3])
            };
        }
    }
}