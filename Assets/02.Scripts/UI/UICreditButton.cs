using UnityEngine;
using UnityEngine.SceneManagement;

public class UICreditButton : MonoBehaviour
{
    private const string MainMenuScene = "03.MainMenu";
    private const string CreditScene = "06.CreditScene";

    public void CloseCreditScene()
    {
        SceneManager.LoadScene(MainMenuScene);
    }

    public void OpenCreditScene()
    {
        SceneManager.LoadScene(CreditScene);
    }
}