using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class ChoppingFoodManager : NetworkBehaviour
{
    [SerializeField]
    private Camera cam;

    [SerializeField]
    private GameObject knifePrefab;

    [SerializeField]
    private Transform knife;

    [SerializeField]
    public Transform clientKnife;

    [SerializeField] [Networked] public NetworkObject clientKnifeNetworked { get; set; }

    [SerializeField]
    private GameObject choppingFoodManagerClientPrefab;

    [SerializeField]
    private ChoppingFoodManagerClient choppingFoodManagerClient;

    private Vector2 mousePos;

    [SerializeField]
    private float throwForce = 1f;

    [SerializeField]
    public AudioSource slashAudio;

    // Populated in editor
    [SerializeField]
    private List<Rigidbody2D> foods;

    [SerializeField]
    private List<Rigidbody2D> foodsNetworked;

    // Populated in editor
    [SerializeField]
    private List<Rigidbody2D> santaHats;

    // Populated in editor
    [SerializeField]
    private List<Rigidbody2D> santaHatsNetworked;

    [SerializeField]
    private List<Rigidbody2D> foodsToChop;

    [SerializeField]
    private List<Rigidbody2D> foodsToChopNetworked;

    private bool isCutting = false;
    private bool isWaiting = false;
    private bool needToSpawnClientManager = false;
    private bool needToSpawnClientKnife = false;

    private GameManager gameManager;
    private SoundManager soundManager;

    private NetworkRunner runner;

    [SerializeField] [Networked] public bool needToThrowFood { get; set; } = true;

    private float delayFruitVisibleCheck = 3.0f;
    private bool canCheckFruitVisibility = false;

    [SerializeField] [Networked] public bool isGameWonNetworked { get; set; } = false;
    [SerializeField] [Networked] public bool wonOrLost { get; set; } = false;

    // Start is called before the first frame update
    private void Start()
    {
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();

        soundManager = GameObject.Find("SoundManager").GetComponent<SoundManager>();

        runner = gameManager.GetRunner();

        if (!runner)
        {
            ThrowFoodUp();
        }
        else
        {
            needToSpawnClientManager = true;
            needToSpawnClientKnife = true;
        }
    }

    // Update is called once per frame
    private void Update()
    {
        if (isWaiting || gameManager.isGamePaused())
        {
            return;
        }

        if (!runner)
        {
            UpdateKnife();
        }

        /*if (Input.GetMouseButtonDown(0))
        {
            isCutting = true;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isCutting = false;
        }

        if (isCutting)
        {
            CheckObjectHit();
        }*/
    }

    public override void FixedUpdateNetwork()
    {
        if (runner.IsClient && wonOrLost && !isWaiting)
        {
            if (isGameWonNetworked)
            {
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

        if (needToSpawnClientManager && runner.IsServer)
        {
            List<PlayerRef> spawnedCharacters = gameManager.GetSpawnedCharacters();
            choppingFoodManagerClient = runner.Spawn(choppingFoodManagerClientPrefab, Vector2.zero, Quaternion.identity, inputAuthority: spawnedCharacters[1]).GetComponent<ChoppingFoodManagerClient>();
            needToSpawnClientManager = false;
            return;
        }

        if (needToSpawnClientKnife && runner.IsServer)
        {
            List<PlayerRef> spawnedCharacters = gameManager.GetSpawnedCharacters();
            clientKnife = runner.Spawn(knifePrefab, Vector2.zero, Quaternion.identity, inputAuthority: spawnedCharacters[1]).transform;
            clientKnifeNetworked = clientKnife.GetComponent<NetworkObject>();
            needToSpawnClientKnife = false;
            choppingFoodManagerClient.knife = clientKnife;
            return;
        }

        if (runner.IsClient)
        {
            if (!clientKnife)
            {
                clientKnife = clientKnifeNetworked.GetComponent<Transform>();
            }
            return;
        }

        if (needToThrowFood)
        {
            Debug.Log("Throw food up called in fixed update");
            ThrowFoodUp();
            needToThrowFood = false;
            StartCoroutine("StartDelayFruitVisibleCheck");
            return;
        }

        if (GetInput(out NetworkInputData data))
        {
            UpdateKnife(data);

            RaycastHit2D hit = Physics2D.Raycast(data.mousePosition, Vector2.zero);
            if (hit.collider != null)
            {
                //Debug.Log("Hit an object object", hit.collider.gameObject);
                if (hit.collider.tag == "ChoppableFood")
                {
                    Debug.Log("Starting chop on this fruit", hit.collider.gameObject);
                    if (!hit.collider.GetComponent<ChoppableFood>().hasBeenChopped)
                    {
                        knife.GetComponent<Knife>().ChopFruit(hit.collider.gameObject);
                    }
                    return;
                }
                else if (hit.collider.tag == "SantaHat")
                {
                    Debug.Log("Starting chop on this santa hat", hit.collider.gameObject);
                    if (!hit.collider.GetComponent<ChoppableFood>().hasBeenChopped)
                    {
                        knife.GetComponent<Knife>().ChopHat(hit.collider.gameObject);
                    }
                    return;
                }
            }

            if (canCheckFruitVisibility && !isWaiting)
            {
                for (int i = 0; i < foodsToChopNetworked.Count; i++)
                {
                    Debug.Log(foodsToChopNetworked[i].GetComponent<Renderer>().isVisible, foodsToChopNetworked[i]);

                    if (!foodsToChopNetworked[i].GetComponent<Renderer>().isVisible && !foodsToChopNetworked[i].GetComponent<ChoppableFood>().hasBeenChopped)
                    {
                        Debug.Log("Lost game");
                        wonOrLost = true;
                        isGameWonNetworked = false;
                        isWaiting = true;
                        LoseGame();
                        return;
                    }
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
    }

    /// <summary>
    /// Updates knife position to current mouse position
    /// </summary>
    private void UpdateKnife()
    {
        mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
        knife.position = new Vector2(mousePos.x, mousePos.y);
    }

    /// <summary>
    /// Updates knife position to current mouse position
    /// </summary>
    /// <param name="data"></param>
    private void UpdateKnife(NetworkInputData data)
    {
        knife.position = data.mousePosition;
    }

    private void ThrowFoodUp()
    {
        int roundNumber = gameManager.GetRoundNumber();
        //int roundNumber = 2;
        int i = 0;
        Rigidbody2D f;

        if (roundNumber == 1)
        {
            i = 2;
        }
        else if (roundNumber == 2)
        {
            i = 4;
        }

        Debug.Log("Throwing up food");

        if (runner)
        {
            Debug.Log("There is a runner");
            if (runner.IsServer)
            {
                Debug.Log("Runner is the server");
                while (i > 0)
                {
                    Debug.Log("Looping through");
                    santaHatsNetworked[i - 1].AddForce(new Vector2(Random.Range(-1.0f, 1.0f), Random.Range(0.75f, 1.5f) * throwForce), ForceMode2D.Impulse);
                    f = foodsNetworked[Random.Range(0, foodsNetworked.Count)];
                    f.AddForce(new Vector2(Random.Range(-1.5f, 1.5f), Random.Range(0.8f, 1.2f) * throwForce), ForceMode2D.Impulse);
                    Debug.Log(f.velocity, f);
                    Debug.Log(f.name, f);
                    foodsToChopNetworked.Add(f);
                    foodsNetworked.Remove(f);
                    i--;
                }
            }
        }
        else
        {
            while (i > 0)
            {
                santaHats[i - 1].AddForce(new Vector2(Random.Range(-1.0f, 1.0f), Random.Range(0.75f, 1.5f) * throwForce), ForceMode2D.Impulse);
                f = foods[Random.Range(0, foods.Count)];
                f.AddForce(new Vector2(Random.Range(-1.5f, 1.5f), Random.Range(0.8f, 1.2f) * throwForce), ForceMode2D.Impulse);
                Debug.Log(f.name);
                foodsToChop.Add(f);
                foods.Remove(f);
                i--;
            }
        }
    }

    /// <summary>
    /// Checks if an object was hit, if there was an object then check if the object is an object to get
    /// </summary>
    private void CheckObjectHit()
    {
        RaycastHit2D hit = Physics2D.Raycast(cam.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);

        if (hit)
        {
            if (hit.transform.gameObject.tag == "ChoppableFood")
            {
                Debug.Log("Hit " + hit.transform.gameObject.name);

                hit.transform.GetComponent<ChoppableFood>().GetChopped();

                hit.transform.GetComponent<BoxCollider2D>().enabled = false;

                hit.transform.GetComponent<Renderer>().enabled = false;

                foodsToChop.Remove(hit.transform.GetComponent<Rigidbody2D>());

                if (foodsToChop.Count == 0 && !isWaiting)
                {
                    isWaiting = true;
                    gameManager.WonMiniGame();
                }
            }
            else if (hit.transform.gameObject.tag == "SantaHat" && !isWaiting)
            {
                Debug.Log("Hit " + hit.transform.gameObject.name);

                isWaiting = true;
                gameManager.LostMiniGame();
            }
        }
    }

    public void RemoveFoodToChop(Rigidbody2D rb)
    {
        if (runner)
        {
            foodsToChopNetworked.Remove(rb);

            if (runner.IsServer)
            {
                if (foodsToChopNetworked.Count == 0 && !isWaiting)
                {
                    wonOrLost = true;
                    isGameWonNetworked = true;
                    isWaiting = true;
                    gameManager.WonMiniGame();
                }
            }
        }
        else
        {
            foodsToChop.Remove(rb);

            if (foodsToChop.Count == 0 && !isWaiting)
            {
                isWaiting = true;
                gameManager.WonMiniGame();
            }
        }
    }

    public void LoseGame()
    {
        isWaiting = true;
        gameManager.LostMiniGame();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (foodsToChop.Contains(collision.GetComponent<Rigidbody2D>()) && !isWaiting && !runner)
        {
            Debug.Log("Hit " + collision.transform.gameObject.name);

            isWaiting = true;
            gameManager.LostMiniGame();
        }
    }

    private IEnumerator StartDelayFruitVisibleCheck()
    {
        while (delayFruitVisibleCheck > 0)
        {
            delayFruitVisibleCheck -= Time.deltaTime;
            yield return null;
        }

        if (delayFruitVisibleCheck <= 0)
        {
            canCheckFruitVisibility = true;
        }
    }

    public Vector2 GetMousePosition()
    {
        return mousePos;
    }

    public SoundManager getSoundManager()
    {
        return soundManager;
    }
}
