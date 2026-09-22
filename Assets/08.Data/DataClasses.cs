using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class YourDataClass
{
    // CSV 데이터 구조를 나타내는 필드들을 추가합니다.
    public string Name; // 이름
    public int Score; // 점수
    // 필요한 만큼 더 많은 필드를 추가하세요.
}

[System.Serializable]
public class ExampleDataClass
{
    // CSV 데이터 구조를 나타내는 필드들을 추가합니다.
    public bool IsEdible;
    public float MoveSpeed;
    // List는 추가적인 파싱 스크립트가 필요합니다! 필요하시다면 어떻게 해야 할지 잘 고민해 보세요. AI는 답을 알고있다..
    public List<int> IngredientList;
    // 필요한 만큼 더 많은 필드를 추가하세요.
}

// 밑으로 똑같은 양식으로 추가해주시면 됩니다. 내부 필드는 필요하신대로 추가 수정 삭제하시면 됩니다.