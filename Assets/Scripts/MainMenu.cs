using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class MainMenu : MonoBehaviour
{
    private UIDocument _uiDocument;

    private Button _playButton;
    private Button _aboutButton;
    private Button _exitButton;

    [SerializeField] private GameObject loadingScreen;
    [SerializeField] private GameObject aboutScreen;

    private void Awake()
    {
        _uiDocument = GetComponent<UIDocument>();
        if (_uiDocument == null)
        {
            Debug.LogError("No UIDocument found on MainMenuManager.");
        }

        _playButton = _uiDocument.rootVisualElement.Q<Button>("Play");
        _playButton.RegisterCallback<ClickEvent>(LoadGameScene);
        _aboutButton = _uiDocument.rootVisualElement.Q<Button>("Help");
        _aboutButton.RegisterCallback<ClickEvent>(LoadAboutMenu);
        _exitButton = _uiDocument.rootVisualElement.Q<Button>("Exit");
        _exitButton.RegisterCallback<ClickEvent>(ExitGame);
    }

    private void LoadGameScene(ClickEvent evt)
    {
        loadingScreen.SetActive(true);
        SceneManager.LoadScene("SampleScene");
    }

    private void LoadAboutMenu(ClickEvent evt) 
    {
        aboutScreen.SetActive(true);
    }

    private void ExitGame(ClickEvent evt)
    {
        Application.Quit();
    }
}
