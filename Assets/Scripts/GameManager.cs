using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion;
using Fusion.Sockets;
using System;

/// <summary>
/// file: GameManager.cs
/// description: Manages the progress of the overarching game and scene loading
/// author: Nathan Ballay
/// </summary>
public class GameManager : MonoBehaviour, INetworkRunnerCallbacks
{
    // Reference set in inspector
    [SerializeField]
    private MouseCursor mouseCursor;

    // Reference set in inspector
    [SerializeField]
    private SoundManager soundManager;

    // Reference set in inspector
    [SerializeField]
    private PauseMenu pauseMenu;

    // Collections
    private HashSet<int> playedScenes = new HashSet<int>();

    // Variables
    private int gameResult;
    private int numLives;
    private int score = 0;
    private int roundNumber = 0;
    [SerializeField] float betweenWaitTime;
    private float betweenTimer;
    private int numGameManagers;

    // Networking callbacks
    private NetworkRunner _runner;
    [SerializeField] private NetworkPrefabRef _playerPrefab;
    private Dictionary<PlayerRef, NetworkObject> _spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer)
        {
            // Create a unique position for the player
            Vector3 spawnPosition = new Vector3((player.RawEncoded % runner.Config.Simulation.DefaultPlayers) * 3, 1, 0);
            NetworkObject networkPlayerObject = runner.Spawn(_playerPrefab, spawnPosition, Quaternion.identity, player);
            // Keep track of the player avatars so we can remove it when they disconnect
            _spawnedCharacters.Add(player, networkPlayerObject);
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        // Find and remove the players avatar
        if (_spawnedCharacters.TryGetValue(player, out NetworkObject networkObject))
        {
            runner.Despawn(networkObject);
            _spawnedCharacters.Remove(player);
        }
    }

    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }

    private void Awake()
    {
        numGameManagers = FindObjectsOfType<GameManager>().Length;
        if (numGameManagers != 1)
        {
            Destroy(this.gameObject);
        }
        else
        {
            DontDestroyOnLoad(this.gameObject);
        }
        numLives = 4;
    }

    // Start is called before the first frame update
    void Start()
    {
        betweenTimer = betweenWaitTime;

        roundNumber = 1;

        soundManager.PlayMainMenuSound();
    }

    // Update is called once per frame
    void Update()
    {
        if (SceneManager.GetActiveScene().buildIndex == 2)
        {
            if (betweenTimer > 0)
            {
                betweenTimer -= Time.deltaTime;
            }
            else
            {
                // Resets mouse state to normal
                mouseCursor.ResetMouseState();

                betweenTimer = betweenWaitTime;

                // If the player runs out of lives, load the game over scene
                if (numLives <= 0)
                {
                    soundManager.StopAllAudio();
                    soundManager.PlayGameOverSound();
                    SceneManager.LoadScene("GameOver");
                }
                else
                {
                    // Otherwise, load another mini game/check for the win condition
                    LoadNextScene();
                }
            }
        }
    }

    /// <summary>
    /// Picks and loads a new game that hasn't been played in this sequence
    /// </summary>
    private void LoadNextScene()
    {
        // Resets mouse state to normal
        mouseCursor.ResetMouseState();

        int idx;

        // If all games have been played, reset the played games tracker and increment the round
        if (playedScenes.Count == 7)
        {
            playedScenes.Clear();
            roundNumber++;
        }

        // If the player has completed two rounds, then they win
        if (roundNumber == 3)
        {
            soundManager.StopAllAudio();
            soundManager.PlayGameVictorySound();
            SceneManager.LoadScene("GameWon");
        }
        // Otherwise, load the next mini game
        else
        {
            // Find the index of a game that hasn't been played yet
            while (playedScenes.Contains(idx = UnityEngine.Random.Range(3, 10)))
            {
                continue;
            }

            // Add the scene index to the list of played games
            playedScenes.Add(idx);

            // Load the new game
            SceneManager.LoadScene(idx);
        }
    }

    /// <summary>
    /// Reinitializes round count and number of lives
    /// </summary>
    public void ResetGameState()
    {
        SetGameResult(0);
        numLives = 4;
        playedScenes.Clear();
        roundNumber = 1;
    }

    /// <summary>
    /// Loads the scene that is displayed between mini games
    /// </summary>
    private void LoadBetweenScene()
    {
        SceneManager.LoadScene("BetweenGames");

        if (gameResult == 2)
        {
            soundManager.PlaySantaSound();
        }

        mouseCursor.ResetMouseState();
    }

    /// <summary>
    /// Adjusts game state based on a player winning a mini game
    /// </summary>
    public void WonMiniGame()
    {
        soundManager.PlayMinigameWinSound();
        StartCoroutine(WinPause());
    }

    IEnumerator WinPause()
    {
        yield return new WaitForSeconds(2f);
        SetGameResult(2);
        IncrementScore();
        LoadBetweenScene();
    }
    /// <summary>
    /// Adjusts game state based on a player losing a mini game
    /// </summary>
    public void LostMiniGame()
    {
        soundManager.PlayMinigameLoseSound();
        StartCoroutine(LosePause());
    }

    IEnumerator LosePause()
    {
        yield return new WaitForSeconds(2f);
        SetGameResult(1);
        DeductLife();
        LoadBetweenScene();
    }

    public void PlayMainMenuSound()
    {
        soundManager.PlayMainMenuSound();
    }

    public void PlayMainGameplaySound()
    {
        soundManager.PlayMainGameplaySound();
    }

    /// <summary>
    /// Tell the SoundManager to stop all audio
    /// </summary>
    public void StopAudio()
    {
        soundManager.StopAllAudio();
    }

    public bool isGamePaused()
    {
        return pauseMenu.isPaused();
    }

    /// <summary>
    /// Sets the mini game result based on win/loss
    /// </summary>
    /// <param name="result">1 for loss, 2 for win</param>
    private void SetGameResult(int result)
    {
        gameResult = result;
    }

    /// <summary>
    /// Getter for the current game result
    /// </summary>
    /// <returns></returns>
    public int GetGameResult()
    {
        return gameResult;
    }

    /// <summary>
    /// Removes a life from the player
    /// </summary>
    private void DeductLife()
    {
        numLives--;
    }

    /// <summary>
    /// Getter for the current number of lives
    /// </summary>
    /// <returns></returns>
    public int GetLives()
    {
        return numLives;
    }

    /// <summary>
    /// Adds one to the player's score
    /// </summary>
    private void IncrementScore()
    {
        score++;
    }

    /// <summary>
    /// Getter for the current player score
    /// </summary>
    /// <returns></returns>
    public int GetScore()
    {
        return score;
    }

    /// <summary>
    /// Getter for the current round number
    /// </summary>
    /// <returns></returns>
    public int GetRoundNumber()
    {
        return roundNumber;
    }

    async void StartGame(GameMode mode)
    {
        // Create the Fusion runner and let it know that we will be providing user input
        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;

        // Start or join (depends on gamemode) a session with a specific name
        await _runner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            SessionName = "TestRoom",
            Scene = SceneManager.GetActiveScene().buildIndex,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });
    }

    public void HostCoopGame()
    {
        StartGame(GameMode.Host);
    }

    public void JoinCoopGame()
    {
        StartGame(GameMode.Client);
    }
}
