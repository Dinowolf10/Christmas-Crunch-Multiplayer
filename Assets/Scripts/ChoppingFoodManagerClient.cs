using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class ChoppingFoodManagerClient : NetworkBehaviour
{
    private GameManager gameManager;

    [SerializeField]
    public Transform knife;

    [SerializeField]
    private ChoppingFoodManager choppingFoodManager;

    // Start is called before the first frame update
    void Start()
    {
        if (!gameManager)
        {
            gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        }

        if (!choppingFoodManager)
        {
            choppingFoodManager = GameObject.Find("ChoppingFoodManager").GetComponent<ChoppingFoodManager>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public override void FixedUpdateNetwork()
    {
        if (!knife)
        {
            return;
        }

        if (GetInput(out NetworkInputData data))
        {
            //Debug.Log("Has input authority");
            UpdateKnife(data);

            RaycastHit2D hit = Physics2D.Raycast(data.mousePosition, Vector2.zero);
            if (hit.collider != null)
            {
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
        }
    }

    /// <summary>
    /// Updates knife position to current mouse position
    /// </summary>
    /// <param name="data"></param>
    private void UpdateKnife(NetworkInputData data)
    {
        knife.position = data.mousePosition;
    }
}
