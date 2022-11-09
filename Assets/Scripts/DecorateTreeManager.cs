using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

/// <summary>
/// file: DecorateTreeManager
/// description: Manages the progress and completion of the Decorate Tree Mini Game
/// author: Nathan Ballay
/// </summary>
public class DecorateTreeManager : NetworkBehaviour
{
    // Prefabs
    [SerializeField] GameObject ornamentPrefab;
    [SerializeField] GameObject ornamentNetworkedPrefab;
    [SerializeField] GameObject timerPrefab;

    // References
    [SerializeField] GameObject sparkle1;
    [SerializeField] GameObject sparkle2;
    private Timer timer;
    private GameManager gameManager;
    private SoundManager soundManager;
    [SerializeField] [Networked] NetworkObject clientDecorateTreeManager { get; set; }

    // Collections
    private List<GameObject> ornaments = new List<GameObject>();
    [SerializeField] private List<NetworkObject> ornamentsNetworked = new List<NetworkObject>();
    [SerializeField] [Networked, Capacity(5)] private NetworkArray<NetworkObject> ornamentsNetworkedSync { get; }
    [SerializeField] List<GameObject> treeBlocks = new List<GameObject>();
    [SerializeField] List<Sprite> sprites = new List<Sprite>();

    // Variables
    private bool networkedOrnamentsSpawned = false;
    private bool isGameWon = false;
    private bool isTimeUp = false;
    private bool isWaiting = false;
    private GameObject newOrnament;
    private NetworkObject newOrnamentNetworked;
    [SerializeField] float startingX;
    [SerializeField] float startingY;
    [SerializeField] float spawnVariability;
    private int numOrnaments;
    private NetworkRunner runner;
    [SerializeField] [Networked] public bool clientNeedsToSyncOrnaments { get; set; } = false;
    [SerializeField] [Networked] public bool clientDecorateTreeManagerNeedsToSync { get; set; } = true;
    [SerializeField] private bool playerDraggingOrnament = false;
    [SerializeField] private MoveOrnament currentDraggedOrnament;
    private Vector2 mousePosition;

    // Start is called before the first frame update
    void Start()
    {
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();

        soundManager = GameObject.Find("SoundManager").GetComponent<SoundManager>();

        runner = gameManager.GetRunner();

        timer = timerPrefab.GetComponent<Timer>();

        if (runner)
        {
            networkedOrnamentsSpawned = false;
            //Debug.Log(runner.LocalPlayer);
            //Debug.Log(GetComponent<NetworkObject>());
            //GetComponent<NetworkObject>().AssignInputAuthority(runner.LocalPlayer);
            //List<PlayerRef> spawnedCharacters = gameManager.GetSpawnedCharacters();
            //GetComponent<NetworkObject>().AssignInputAuthority(spawnedCharacters[0]);
        }
        else
        {
            networkedOrnamentsSpawned = true;
            SpawnOrnaments();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (gameManager.isGamePaused() || !networkedOrnamentsSpawned || clientNeedsToSyncOrnaments)
        {
            return;
        }

        CheckOrnamentPlacement();
        CheckComplete();
        CheckTime();

        if (isGameWon && !isTimeUp && !isWaiting)
        {
            soundManager.PlaySparkleSound();
            LockAllOrnaments();
            ActivateSparkles();
            isWaiting = true;
            timer.StopBarDrain();
            gameManager.WonMiniGame();
        }

        if (isTimeUp && !isGameWon && !isWaiting)
        {
            LockAllOrnaments();
            isWaiting = true;
            gameManager.LostMiniGame();
        }
    }

    public override void FixedUpdateNetwork()
    {
        /*if (clientDecorateTreeManager != null)
        {
            if (runner.IsClient)
            {
                Debug.Log("Set client decorate tree manager");
                clientDecorateTreeManager = this.gameObject.GetComponent<NetworkObject>();
            }
            return;
        }
        else if (clientDecorateTreeManagerNeedsToSync)
        {
            if (runner.IsServer)
            {
                List<PlayerRef> spawnedCharacters = gameManager.GetSpawnedCharacters();
                clientDecorateTreeManager.AssignInputAuthority(spawnedCharacters[1]);
                clientDecorateTreeManagerNeedsToSync = false;
            }
        }*/

        if (!networkedOrnamentsSpawned)
        {
            Debug.Log("Creating network ornaments");
            SpawnOrnaments();
            return;
        }

        if (clientNeedsToSyncOrnaments)
        {
            if (runner.IsClient)
            {
                Debug.Log("Client needs to sync ornaments");
                SyncOrnaments();
            }
            clientNeedsToSyncOrnaments = false;
            return;
        }

        //Debug.Log("Trying to get input", this.gameObject);
        if (GetInput(out NetworkInputData data))
        {
            //Debug.Log("Got input", this.gameObject);
            if (data.mouseDown)
            {
                if (playerDraggingOrnament)
                {
                    Debug.Log("Dragging ornament", currentDraggedOrnament);
                    Debug.Log("Setting position: " + data.mousePosition);
                    //currentDraggedOrnament.SetDeltaOffset(data.mousePosition);
                    //currentDraggedOrnament.ornamentPosition = new Vector2(mousePosition.x - currentDraggedOrnament.GetDeltaX(), mousePosition.y - currentDraggedOrnament.GetDeltaY());
                    currentDraggedOrnament.ornamentPosition = data.mousePosition;
                    return;
                }
                else
                {
                    RaycastHit2D hit = Physics2D.Raycast(data.mousePosition, Vector2.zero);
                    if (hit.collider != null)
                    {
                        Debug.Log("Hit an object object", this.gameObject);
                        if (hit.collider.tag == "Ornament")
                        {
                            Debug.Log("Dragging ornament, updating ornament position:", this.gameObject);
                            //mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                            mousePosition = data.mousePosition;
                            //transform.position = new Vector2(mousePosition.x - deltaX, mousePosition.y - deltaY);
                            MoveOrnament moveOrnamnet = hit.collider.GetComponent<MoveOrnament>();
                            currentDraggedOrnament = moveOrnamnet;
                            moveOrnamnet.SetDeltaOffset(data.mousePosition);
                            moveOrnamnet.ornamentPosition = new Vector2(mousePosition.x - moveOrnamnet.GetDeltaX(), mousePosition.y - moveOrnamnet.GetDeltaY());
                            Debug.Log("New ornament position: " + moveOrnamnet.ornamentPosition, moveOrnamnet.gameObject);
                            playerDraggingOrnament = true;
                            moveOrnamnet.isGettingDragged = true;
                            return;
                        }
                    }
                }
            }
            playerDraggingOrnament = false;
            if (currentDraggedOrnament != null)
            {
                currentDraggedOrnament.isGettingDragged = false;
                currentDraggedOrnament = null;
            }
        }
        else
        {
            if (runner.IsServer)
            {
                Debug.Log("Assigning input authority");
                List<PlayerRef> spawnedCharacters = gameManager.GetSpawnedCharacters();
                GetComponent<NetworkObject>().AssignInputAuthority(spawnedCharacters[1]);
                //GetComponent<NetworkObject>().AssignInputAuthority(runner.LocalPlayer);
            }
        }
    }

    public void UpdateClientInput(NetworkInputData data)
    {
        if (playerDraggingOrnament)
        {
            Debug.Log("Dragging ornament", currentDraggedOrnament);
            Debug.Log("Setting position: " + data.mousePosition);
            //currentDraggedOrnament.SetDeltaOffset(data.mousePosition);
            //currentDraggedOrnament.ornamentPosition = new Vector2(mousePosition.x - currentDraggedOrnament.GetDeltaX(), mousePosition.y - currentDraggedOrnament.GetDeltaY());
            currentDraggedOrnament.ornamentPosition = data.mousePosition;
            return;
        }
        else
        {
            RaycastHit2D hit = Physics2D.Raycast(data.mousePosition, Vector2.zero);
            if (hit.collider != null)
            {
                Debug.Log("Hit an object object", this.gameObject);
                if (hit.collider.tag == "Ornament")
                {
                    Debug.Log("Dragging ornament, updating ornament position:", this.gameObject);
                    //mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    mousePosition = data.mousePosition;
                    //transform.position = new Vector2(mousePosition.x - deltaX, mousePosition.y - deltaY);
                    MoveOrnament moveOrnamnet = hit.collider.GetComponent<MoveOrnament>();
                    currentDraggedOrnament = moveOrnamnet;
                    moveOrnamnet.SetDeltaOffset(data.mousePosition);
                    moveOrnamnet.ornamentPosition = new Vector2(mousePosition.x - moveOrnamnet.GetDeltaX(), mousePosition.y - moveOrnamnet.GetDeltaY());
                    Debug.Log("New ornament position: " + moveOrnamnet.ornamentPosition, moveOrnamnet.gameObject);
                    playerDraggingOrnament = true;
                    moveOrnamnet.isGettingDragged = true;
                    return;
                }
            }
        }
        playerDraggingOrnament = false;
    }

    /// <summary>
    /// Spawns ornaments with some variability and fills the collection
    /// </summary>
    private void SpawnOrnaments()
    {
        int roundNumber = gameManager.GetRoundNumber();

        if (roundNumber == 1)
        {
            numOrnaments = 3;
        }
        else if (roundNumber == 2)
        {
            numOrnaments = 5;
        }

        if (runner)
        {
            if (runner.IsServer)
            {
                List<PlayerRef> spawnedCharacters = gameManager.GetSpawnedCharacters();
                for (int i = 0; i < numOrnaments; i++)
                {
                    newOrnamentNetworked = runner.Spawn(ornamentNetworkedPrefab, RandomSpawnLocation(), Quaternion.identity, inputAuthority: spawnedCharacters[0], InitializeRandomSpriteBeforeSpawn);
                    //newOrnamentNetworked = runner.Spawn(ornamentNetworkedPrefab, RandomSpawnLocation(), Quaternion.identity, inputAuthority: null, InitializeRandomSpriteBeforeSpawn);
                    Debug.Log(newOrnamentNetworked);
                    // Add ornament to networked ornaments list
                    ornamentsNetworked.Add(newOrnamentNetworked);
                    ornamentsNetworkedSync.Set(i, newOrnamentNetworked);
                }
            }

            networkedOrnamentsSpawned = true;
            clientNeedsToSyncOrnaments = true;

           // SyncOrnaments();

            /*for (int i = 0; i < numOrnaments; i++)
            {
                ornamentsNetworked[i].GetComponent<SpriteRenderer>().sprite = ChooseRandomSprite();
            }*/
        }
        else
        {
            for (int i = 0; i < numOrnaments; i++)
            {
                newOrnament = Instantiate(ornamentPrefab, RandomSpawnLocation(), Quaternion.identity);
                newOrnament.GetComponent<SpriteRenderer>().sprite = ChooseRandomSprite();

                // Add ornament to ornaments list
                ornaments.Add(newOrnament);
            }
        }
    }

    /// <summary>
    /// Syncs networked ornaments list with the networked ornaments property array
    /// </summary>
    private void SyncOrnaments()
    {
        Debug.Log("Syncing ornaments for client");
        for (int i = 0; i < numOrnaments; i++)
        {
            ornamentsNetworked.Add(ornamentsNetworkedSync.Get(i));
            ornamentsNetworked[i].GetComponent<SpriteRenderer>().sprite = ChooseRandomSprite();
            Debug.Log("Synced: " + ornamentsNetworkedSync.Get(i).name);
        }
    }

    /// <summary>
    /// Generates a random locaiton for ornaments to spawn at
    /// </summary>
    /// <returns>Partially randomized Vector2</returns>
    private Vector3 RandomSpawnLocation()
    {
        float XPos = Random.Range(startingX - spawnVariability, startingX + spawnVariability);
        float YPos = Random.Range(startingY - spawnVariability, startingY + spawnVariability);

        return new Vector2(XPos, YPos);
    }

    /// <summary>
    /// Returns a random toy sprite from the list
    /// </summary>
    /// <returns>random Sprite</returns>
    private Sprite ChooseRandomSprite()
    {
        int idx = Random.Range(0, sprites.Count - 1);

        return sprites[idx];
    }

    /// <summary>
    /// 
    /// </summary>
    private void InitializeRandomSpriteBeforeSpawn(NetworkRunner runner, NetworkObject obj)
    {
        obj.GetComponent<SpriteRenderer>().sprite = ChooseRandomSprite();
    }

    /// <summary>
    /// Locks all ornaments at their current location
    /// </summary>
    private void LockAllOrnaments()
    {
        if (runner)
        {
            foreach (NetworkObject ornament in ornamentsNetworked)
            {
                ornament.GetComponent<MoveOrnament>().LockOrnament();
            }
        }
        else
        {
            foreach (GameObject ornament in ornaments)
            {
                ornament.GetComponent<MoveOrnament>().LockOrnament();
            }
        }    
    }

    /// <summary>
    /// Determines if ornaments have been placed at their proper location and provides feedback if so
    /// </summary>
    private void CheckOrnamentPlacement()
    {
        if (runner)
        {
            for (int i = 0; i < ornamentsNetworked.Count; i++)
            {
                for (int j = 0; j < treeBlocks.Count; j++)
                {
                    // If the player drops and unlocked ornament over the tree, lock it in place
                    if (AABBCollisionNetworked(ornamentsNetworked[i], treeBlocks[j]) && !ornamentsNetworked[i].GetComponent<MoveOrnament>().isLocked() && !Input.GetMouseButton(0))
                    {
                        soundManager.PlayGrabSound();
                        ornamentsNetworked[i].GetComponent<MoveOrnament>().LockOrnament();
                    }
                }
            }
        }
        else
        {
            for (int i = 0; i < ornaments.Count; i++)
            {
                for (int j = 0; j < treeBlocks.Count; j++)
                {
                    // If the player drops and unlocked ornament over the tree, lock it in place
                    if (AABBCollision(ornaments[i], treeBlocks[j]) && !ornaments[i].GetComponent<MoveOrnament>().isLocked() && !Input.GetMouseButton(0))
                    {
                        soundManager.PlayGrabSound();
                        ornaments[i].GetComponent<MoveOrnament>().LockOrnament();
                    }
                }
            }
        }
    }

    /// <summary>
    /// Checks for a collision between two objects using the AABB method
    /// </summary>
    /// <param name="o1">First game object being considered for a collision</param>
    /// <param name="o2">Second game object being considered for a collision</param>
    /// <returns>boolean determining whether a collision has occurred</returns>
    public bool AABBCollision(GameObject o1, GameObject o2)
    {
        // Get references to bounds of both objects
        Bounds bounds1 = o1.GetComponent<SpriteRenderer>().bounds;
        Bounds bounds2 = o2.GetComponent<SpriteRenderer>().bounds;

        // Find all mins and maxes of o1
        float minX1 = bounds1.min.x;
        float maxX1 = bounds1.max.x;
        float minY1 = bounds1.min.y;
        float maxY1 = bounds1.max.y;

        // Find all mins and maxes of o2
        float minX2 = bounds2.min.x;
        float maxX2 = bounds2.max.x;
        float minY2 = bounds2.min.y;
        float maxY2 = bounds2.max.y;

        // Check all necessary conditions for a collision
        bool cond1 = minX2 < maxX1;
        bool cond2 = maxX2 > minX1;
        bool cond3 = maxY2 > minY1;
        bool cond4 = minY2 < maxY1;

        // Determine if collision has occurred
        if (cond1 && cond2 && cond3 && cond4)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Checks for a collision between two objects using the AABB method
    /// </summary>
    /// <param name="o1">The networked object being considered for a collision</param>
    /// <param name="o2">Second game object being considered for a collision</param>
    /// <returns>boolean determining whether a collision has occurred</returns>
    public bool AABBCollisionNetworked(NetworkObject o1, GameObject o2)
    {
        // Get references to bounds of both objects
        Bounds bounds1 = o1.GetComponent<SpriteRenderer>().bounds;
        Bounds bounds2 = o2.GetComponent<SpriteRenderer>().bounds;

        // Find all mins and maxes of o1
        float minX1 = bounds1.min.x;
        float maxX1 = bounds1.max.x;
        float minY1 = bounds1.min.y;
        float maxY1 = bounds1.max.y;

        // Find all mins and maxes of o2
        float minX2 = bounds2.min.x;
        float maxX2 = bounds2.max.x;
        float minY2 = bounds2.min.y;
        float maxY2 = bounds2.max.y;

        // Check all necessary conditions for a collision
        bool cond1 = minX2 < maxX1;
        bool cond2 = maxX2 > minX1;
        bool cond3 = maxY2 > minY1;
        bool cond4 = minY2 < maxY1;

        // Determine if collision has occurred
        if (cond1 && cond2 && cond3 && cond4)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Checks whether all ornaments have been properly placed
    /// </summary>
    private void CheckComplete()
    {
        bool isComplete = true;

        if (runner)
        {
            foreach (NetworkObject ornament in ornamentsNetworked)
            {
                if (!ornament.GetComponent<MoveOrnament>().isLocked())
                {
                    isComplete = false;
                    break;
                }
            }
        }
        else
        {
            foreach (GameObject ornament in ornaments)
            {
                if (!ornament.GetComponent<MoveOrnament>().isLocked())
                {
                    isComplete = false;
                    break;
                }
            }
        }

        isGameWon = isComplete;
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
