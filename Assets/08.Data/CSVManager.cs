using UnityEngine;
using System.Collections.Generic;
using System.IO;
using UnityEditor;

public class CSVManager : MonoBehaviour
{
    // 데이터를 저장할 리스트 예시
    private List<YourDataClass> _dataList = new List<YourDataClass>();
    private const string _yourDataClassStr = "YourDataClass";

    // 데이터를 JSON 파일로 Resources 폴더에 저장합니다.
    public void SaveDataToJSON()
    {
        string json = JsonUtility.ToJson(_dataList);
        TextAsset textAsset = new TextAsset(json);
        string filePath = $"Assets/Resources/{_yourDataClassStr}.json";
        AssetDatabase.CreateAsset(textAsset, filePath);
        AssetDatabase.SaveAssets();
        Debug.Log("데이터 저장 완료: " + filePath);
    }
 
    // JSON 파일에서 데이터를 로드합니다.
    public void LoadDataFromJSON()
    {
        TextAsset textAsset = Resources.Load<TextAsset>(_yourDataClassStr);
        
        if (textAsset != null)
        {
            string json = textAsset.text;
            _dataList = JsonUtility.FromJson<List<YourDataClass>>(json);
            Debug.Log("데이터 로드 완료");
 
            // 로드된 데이터로 무언가를 수행합니다.
            foreach (var data in _dataList)
            {
                Debug.Log("이름: " + data.Name + ", 점수: " + data.Score);
            }
        }
        else
        {
            Debug.LogError($"경고: Resources 폴더에서 {_yourDataClassStr}.json 파일을 찾을 수 없습니다.");
        }
    }
}