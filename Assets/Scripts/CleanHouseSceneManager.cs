using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

/// <summary>
/// file: CleanHouseSceneManager.cs
/// description: Manages the progress and completion of the Clean House Mini Game
/// author: Nathan Ballay
/// </summary>
public class CleanHouseSceneManager : NetworkBehaviour
{
    // Prefabs
    [SerializeField] List<GameObject> dirtPrefabs = new List<GameObject>();
    [SerializeField] List<GameObject> dirtNetworkedPrefabs = new List<GameObject>();
    [SerializeField] GameObject dirtParticlePrefab;
    [SerializeField] GameObject timerPrefab;
    [SerializeField] GameObject vacuumSpritePrefab;

    // References
    [SerializeField] GameObject cleanHouseManagerClientPrefab;
    [SerializeField] CleanHouseManagerClient cleanHouseManagerClient;
    [SerializeField] GameObject vacuumSprite;
    [SerializeField] GameObject vacuumSpriteClient;
    [SerializeField] GameObject sparkle1;
    [SerializeField] GameObject sparkle2;
    private Timer timer;
    private GameManager gameManager;
    private SoundManager soundManager;
    private NetworkRunner runner;
    [SerializeField]
    public Transform clientVacuum;
    [SerializeField] [Networked] public NetworkObject clientVacuumNetworked { get; set; }

    // Variables
    private GameObject dirt;
    private bool isGameWon = false;
    private bool isTimeUp = false;
    private bool isWaiting = false;
    private bool needToSpawnClientManager = false;
    private bool needToSpawnClientVacuum = false;
    [SerializeField] [Networked] public bool needToSpawnDirt { get; set; } = true;
    int roundNumber;
    [SerializeField] [Networked] public bool isGameWonNetworked { get; set; } = false;
    [SerializeField] [Networked] public bool wonOrLost { get; set; } = false;

    // Start is called before the first frame update
    void Start()
    {
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();

        runner = gameManager.GetRunner();

        soundManager = GameObject.Find("SoundManager").GetComponent<SoundManager>();

        soundManager.PlayVacuumSound();

        roundNumber = gameManager.GetRoundNumber();

        if (!runner)
        {
            if (roundNumber == 1)
            {
                dirt = Instantiate(dirtPrefabs[0], Vector3.zero, Quaternion.identity);
            }
            else
            {
                dirt = Instantiate(dirtPrefabs[Random.Range(1, 3)], Vector3.zero, Quaternion.identity);
            }
        }
        else
        {
            needToSpawnClientManager = true;
            needToSpawnClientVacuum = true;
        }
        
        timer = timerPrefab.GetComponent<Timer>();

    }

    // Update is called once per frame
    void Update()
    {
        if (gameManager.isGamePaused())
        {
            return;
        }

        CheckTime();

        if (runner)
        {
            return;
        }

        UpdateVacuumPosition();

        if (dirt.transform.childCount == 0 && !isTimeUp && !isWaiting)
        {
            soundManager.PlaySparkleSound();
            ActivateSparkles();
            isWaiting = true;
            isGameWon = true;
            timer.StopBarDrain();
            gameManager.WonMiniGame();
        }

        if (isTimeUp && !isGameWon && !isWaiting)
        {
            isWaiting = true;
            gameManager.LostMiniGame();
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (needToSpawnClientManager && runner.IsServer)
        {
            List<PlayerRef> spawnedCharacters = gameManager.GetSpawnedCharacters();
            cleanHouseManagerClient = runner.Spawn(cleanHouseManagerClientPrefab, Vector2.zero, Quaternion.identity, inputAuthority: spawnedCharacters[1]).GetComponent<CleanHouseManagerClient>();
            needToSpawnClientManager = false;
            return;
        }

        if (needToSpawnClientVacuum && runner.IsServer)
        {
            List<PlayerRef> spawnedCharacters = gameManager.GetSpawnedCharacters();
            clientVacuum = runner.Spawn(vacuumSpritePrefab, Vector2.zero, Quaternion.identity, inputAuthority: spawnedCharacters[1]).transform;
            clientVacuumNetworked = clientVacuum.GetComponent<NetworkObject>();
            needToSpawnClientVacuum = false;
            cleanHouseManagerClient.vacuumSprite = clientVacuum;
            Debug.Log("Spawned client vacuum");
            return;
        }

        if (runner.IsClient && wonOrLost && !isWaiting)
        {
            if (isGameWonNetworked)
            {
                ActivateSparkles();
                timer.StopBarDrain();
                gameManager.WonMiniGame();
            }
            else
            {
                gameManager.LostMiniGame();
            }
            isWaiting = true;
            wonOrLost = false;
            return;
        }

        if (runner.IsClient)
        {
            if (!clientVacuum)
            {
                clientVacuum = clientVacuumNetworked.GetComponent<Transform>();
            }
            return;
        }

        if (needToSpawnDirt)
        {
            /*if (roundNumber == 1)
            {
                dirt = runner.Spawn(dirtNetworkedPrefabs[0], Vector3.zero, Quaternion.identity).gameObject;
            }
            else
            {
                dirt = runner.Spawn(dirtNetworkedPrefabs[Random.Range(1, 3)], Vector3.zero, Quaternion.identity).gameObject;
            }*/
            dirt = runner.Spawn(dirtNetworkedPrefabs[Random.Range(1, 3)], Vector3.zero, Quaternion.identity).gameObject;
            needToSpawnDirt = false;
            return;
        }

        if (GetInput(out NetworkInputData data))
        {
            UpdateVacuumPosition(data);

            RaycastHit2D hit = Physics2D.Raycast(data.mousePosition, Vector2.zero);
            if (hit.collider != null)
            {
                //Debug.Log("Hit an object object", hit.collider.gameObject);
                if (hit.collider.tag == "Dirt")
                {
                    Debug.Log("Removing dirt", hit.collider.gameObject);
                    soundManager.PlayVacuumSuckSound();
                    Destroy(hit.collider.gameObject);
                    return;
                }
            }
        }
        else
        {
            if (runner.IsServer)
            {
                Debug.Log("Assigning input authority");
                GetComponent<NetworkObject>().AssignInputAuthority(runner.LocalPlayer);
            }
        }

        if (dirt.transform.childCount == 0 && !isTimeUp && !isWaiting)
        {
            soundManager.PlaySparkleSound();
            ActivateSparkles();
            isWaiting = true;
            isGameWon = true;
            wonOrLost = true;
            isGameWonNetworked = true;
            timer.StopBarDrain();
            gameManager.WonMiniGame();
        }

        if (isTimeUp && !isGameWon && !isWaiting)
        {
            isWaiting = true;
            wonOrLost = true;
            isGameWonNetworked = false;
            gameManager.LostMiniGame();
        }
    }

    /// <summary>
    /// Updates the position of the vacuum sprite to follow the player's cursor
    /// </summary>
    private void UpdateVacuumPosition()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3 newPos = new Vector3(mousePos.x, mousePos.y, 0);
        vacuumSprite.transform.position = newPos;
    }

    /// <summary>
    /// Updates the position of the vacuum sprite to follow the player's cursor
    /// </summary>
    /// <param name="data"></param>
    private void UpdateVacuumPosition(NetworkInputData data)
    {
        vacuumSprite.transform.position = data.mousePosition;
    }

    private void ActivateSparkles()
    {
        sparkle1.SetActive(true);
        sparkle2.SetActive(true);
    }
 
    /// <summary>
    /// Checks if the timer has run out 
    /// </summary>
    private void CheckTime()
    {
        isTimeUp = timer.IsTimeUp();
    }
}