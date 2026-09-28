using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    private static PlayerManager _instance;
    public static PlayerManager Instance => _instance;

    public PlayerStatus PlayerStatus { private set; get; }
    private SavePlayerStatus _savePlayerStatus;

    private void Awake()
    {
        if (_instance != null) //인스턴스의 유일성 보장
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
    }

    public void GetWeapon(WeaponData weaponData)
    {
        //todo 
    }

    public void GetItem()
    {
        //todo 
    }

    public void MonsterHit()
    {
        //todo 
    }


    public void BulletHit()
    {
        //todo 
    }

    private void Save()
    {
    }

    private void Load()
    {
    }
}