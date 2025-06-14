using UnityEngine;

public class UIController : MonoBehaviour
{
    public void Exit()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.ExitPlaymode();
        #endif

        Application.Quit();
    }
}
