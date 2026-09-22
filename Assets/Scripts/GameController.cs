using System.IO;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class GameController : MonoBehaviour
{
    public GameObject playerObject;
    public Camera mainCamera;
    public GameObject gameUI;
    public GameObject gameOverScreen;

    [SerializeField] private Grid gridModel;
    [SerializeField] private MapView mapView;
    [SerializeField] private BackgroundView backgroundView;
    [SerializeField] private MapFSM mapFSM;
    [SerializeField] private DDA dda;
    [SerializeField] private PlayerController player;
    [SerializeField] private DataLogger logger;
    [SerializeField] private GameObject levelScreen;

    [SerializeField] private TMP_Text timeText, levelText, timeClearText, goldText, killText, floorText;
    [SerializeField] private TMP_Text floorReachedText, totalTimeText, totalGoldText, totalKillText;

    [SerializeField]
    private bool useDynamicDifficulty = true;

    private DDASettings currentDifficulty = DDASettings.CreateDefault();

    private float floorStartTime;
    private bool changingLevel;

    //public TextMeshPro timeText, goldText, killText, floorText, scoreText;

    [HideInInspector] public int gold;

    private bool gameStarted = false;
    private bool isPlaying = false;
    private bool gameOver = false;
    private bool ddaRunning = false;
    private bool paused = false;
    private int score;

    private float totalTime, currentTime;
    private float timeCount;

    private int hours, minutes;
    private float seconds;

    private int floor;

    private float floorCompletionTime;

    private List<DDAData> ddaHistory;
    private List<PlayerPerformanceData> performanceHistory;
    private bool finalPerformanceLogged = false;

    [SerializeField] private AudioSource source;
    [SerializeField] private AudioClip playerDeathAudio;

    void Start()
    {
        floor = 1;
        ddaHistory = new List<DDAData>();
        performanceHistory = new List<PlayerPerformanceData>();
        ApplyDDA(currentDifficulty);
        floorText.text = "Floor: " + floor;
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log("Is Playing: " + isPlaying);
        Debug.Log("Game is Over: " + gameOver);
        Debug.Log("DDA is Running: " + ddaRunning);
        if (!paused)
        {
            totalTime += Time.deltaTime;
            currentTime += Time.deltaTime;

            UpdateTimerDisplay();
        }
        if (!isPlaying && !gameOver && !ddaRunning && !gameStarted) StartGame();
    }

    private void UpdateTimerDisplay()
    {
        if (timeText == null)
            return;

        int hours = Mathf.FloorToInt(currentTime / 3600f);

        int minutes = Mathf.FloorToInt(currentTime / 60f) % 60;

        float seconds = currentTime % 60f;

        timeText.text = $"{hours:00}:{minutes:00}:{seconds:00.00}";
    }

    void ApplyDDA(DDASettings settings)
    {
        player.ApplyDDA(settings);
        gridModel.ApplyDDA(settings);

        Debug.Log(
            "DDA: Applied difficulty:\n" +
            $"Monster HP: {settings.monsterHP}\n" +
            $"Monster damage: {settings.monsterAttack}\n" +
            $"Monster spawn rate: {settings.monsterSpawnRate:F2}\n" +
            $"Hazard damage: {settings.hazardDamage}\n" +
            $"Hazard spawn rate: {settings.hazardSpawnRate:F2}"
        );
        
        ddaHistory.Add(new DDAData(floor, settings));

        Debug.Log(ddaHistory.ToArray());
    }

    void StartGame()
    {
        gameStarted = true;
        isPlaying = true;
        //iterate through the grid to find the entrance
        Vector3 entrance = gridModel.GetEntrancePos();
        playerObject.transform.position = entrance;
        player.playerPosition = gridModel.EntrancePosition;
        floorStartTime = Time.time;
    }

    public void PauseGame()
    {
        paused = !paused;
    }

    public void SaveData()
    {
        string logDirectory = Path.Combine(Application.persistentDataPath, "Logs");

        string versionDir;

        if (useDynamicDifficulty)
        {
            versionDir = Path.Combine(logDirectory, "Version-A");
        } 
        else
        {
            versionDir = Path.Combine(logDirectory, "Version-B");
        }

        Directory.CreateDirectory(versionDir);

        int run = 1;

        //keep incrementing run until it reaches a run number that's not being used
        while (File.Exists(Path.Combine(versionDir, $"dda_run_{run}.csv")) || File.Exists(Path.Combine(versionDir, $"performance_run_{run}.csv")))
        {
            run++;
        }

        string ddaPath = Path.Combine(versionDir, $"dda_run_{run}.csv");

        string performancePath = Path.Combine(versionDir, $"performance_run_{run}.csv");

        logger.ExportDDAToCSV(ddaHistory, ddaPath);
        logger.ExportPerformanceToCSV(performanceHistory, performancePath);

        Debug.Log($"Logs saved to: {versionDir}");
    }

    public void DownloadData()
    {
        LogFinalPerformance();

        string version;

        if (useDynamicDifficulty)
            version = "Version-A";
        else
            version = "Version-B";

        logger.DownloadPerformanceCSV(
            performanceHistory,
            $"{version}_performance.csv"
        );

        logger.DownloadDDACSV(
            ddaHistory,
            $"{version}_dda.csv"
        );
    }

    public void DownloadPerformanceData()
    {
        LogFinalPerformance();

        string version =
            useDynamicDifficulty ? "Version-A" : "Version-B";

        logger.DownloadPerformanceCSV(
            performanceHistory,
            $"{version}_performance.csv"
        );
    }

    public void DownloadDDAData()
    {
        string version =
            useDynamicDifficulty ? "Version-A" : "Version-B";

        logger.DownloadDDACSV(
            ddaHistory,
            $"{version}_dda.csv"
        );
    }

    private void LogFinalPerformance()
    {
        if (finalPerformanceLogged)
            return;

        performanceHistory.Add(
            new PlayerPerformanceData(
                floor,
                player.currentHealth,
                player.maxHealth,
                player.attack,
                player.gold,
                currentTime,
                totalTime,
                player.kills
            )
        );

        finalPerformanceLogged = true;
    }

    public void ExitGame()
    {
        LogFinalPerformance();

        SaveData();

        //yield return new WaitForSeconds(3);
    }

    public void EndGame()
    {
        source.PlayOneShot(playerDeathAudio);
        gameOver = true;
        isPlaying = false;
        mainCamera.transform.SetParent(null);
        gameUI.SetActive(false);
        GameObject.Destroy(playerObject);
        ShowEndScreen();
    }

    //to fix: the floor loading ui should show time spent, gold and kills obtained in the current floor instead of the entire run
    public async void NextLevel()
    {
        if (changingLevel || gameOver) return;

        Debug.Log("Level Screen On");
        levelScreen.SetActive(true);

        changingLevel = true;
        ddaRunning = true;
        isPlaying = false;

        floorCompletionTime = currentTime;

        levelText.text = "Floor " + floor + " cleared!";
        performanceHistory.Add(new PlayerPerformanceData(floor, player.currentHealth, player.maxHealth, player.attack, player.gold, floorCompletionTime, totalTime, player.kills));

        floor++;

        floorText.text = "Floor: " + floor;

        timeClearText.text = "Time taken: " + floorCompletionTime.ToString("F2") + " seconds";
        goldText.text = "Total gold collected: " + player.gold;
        killText.text = "Total monsters defeated: " + player.kills;

        await Awaitable.WaitForSecondsAsync(0.5f);

        if (useDynamicDifficulty)
        {
            DDASettings newDifficulty = await dda.RequestDifficulty(
                    floorCompletionTime,
                    player.currentHealth,
                    player.maxHealth,
                    player.attack,
                    player.gold,
                    floor,
                    currentDifficulty
                );

            ApplyDDA(newDifficulty);
            currentDifficulty = newDifficulty;
        }

        mapFSM.Restart();
        Vector3 entrance = gridModel.GetEntrancePos();
        playerObject.transform.position = entrance;

        changingLevel = false;
        ddaRunning = false;
        isPlaying = true;
        floorStartTime = Time.time;

        currentTime = 0.0f;

        Debug.Log("Level Screen Off");
        levelScreen.SetActive(false);
        player.gameLoading = false;
    }

    public void RenderMap()
    {
        backgroundView.RenderMapView();
        mapView.RenderMapView();
    }

    
    private void ShowEndScreen()
    {
        totalTimeText.text = "Total Time Taken: " + totalTime.ToString("F2") + " seconds";
        totalGoldText.text = "Score: " + player.gold;
        totalKillText.text = "Monsters Killed: " + player.kills;
        floorReachedText.text = "Floor Reached: " + floor;
        gameOverScreen.SetActive(true);
    }
    
}
