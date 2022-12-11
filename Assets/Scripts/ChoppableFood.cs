using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChoppableFood : NetworkBehaviour
{
    // Reference set in inspector
    [SerializeField]
    private GameObject piece1, piece2;

    [SerializeField]
    private float choppedForce = 0.1f;

    public bool hasBeenChopped = false;
    [SerializeField] [Networked] public bool needsToBeChopped { get; set; } = false;

    private ChoppingFoodManager choppingFoodManager;

    private void Start()
    {
        if (!choppingFoodManager)
        {
            choppingFoodManager = GameObject.Find("ChoppingFoodManager").GetComponent<ChoppingFoodManager>();
        }
    }

    public void GetChopped()
    {
        piece1.SetActive(true);
        piece2.SetActive(true);

        piece1.GetComponent<Rigidbody2D>().AddForce(new Vector2(1.0f * choppedForce, 0.3f), ForceMode2D.Impulse);
        piece2.GetComponent<Rigidbody2D>().AddForce(new Vector2(-1.0f * choppedForce, 0.3f), ForceMode2D.Impulse);

        if (Runner.IsServer)
        {
            needsToBeChopped = true;
            hasBeenChopped = true;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (needsToBeChopped && Runner.IsClient)
        {
            if (this.tag == "ChoppableFood" && !hasBeenChopped)
            {
                choppingFoodManager.clientKnife.GetComponent<Knife>().ChopFruit(this.gameObject);
            }
            else if (this.tag == "SantaHat" && !hasBeenChopped)
            {
                choppingFoodManager.clientKnife.GetComponent<Knife>().ChopHat(this.gameObject);
            }
            needsToBeChopped = false;
            hasBeenChopped = true;
        }
    }
}
