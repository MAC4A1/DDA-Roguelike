using UnityEngine;
using UnityEngine.SceneManagement;

public class ExitButton : MonoBehaviour
{
    [SerializeField] private PlayerController player;
    [SerializeField] private GameController game;
    [SerializeField] private GameObject screen;

    public void OnPress(string buttonName)
    {
        if (buttonName == "Exit") { game.ExitGame(); SceneManager.LoadScene("MainMenu"); }//StartCoroutine(game.ExitGame()); SceneManager.LoadScene("MainMenu"); }
        else if (buttonName == "Performance") { game.DownloadPerformanceData(); }
        else if (buttonName == "DDA") { game.DownloadDDAData(); }
        else if (buttonName == "Resume") { player.OnPause(); }
        else if (buttonName == "Menu") screen.SetActive(false);
    }
}
