using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class Knife : MonoBehaviour
{
    [SerializeField]
    private ChoppingFoodManager choppingFoodManager;

    [SerializeField]
    private Transform slashTransform;

    [SerializeField]
    private Animator slashAnimator;

    [SerializeField]
    private float mouseX, mouseY;

    [SerializeField]
    private GameManager gameManager;

    private NetworkRunner runner;

    // Start is called before the first frame update
    void Start()
    {
        if (!choppingFoodManager)
        {
            choppingFoodManager = GameObject.Find("ChoppingFoodManager").GetComponent<ChoppingFoodManager>();
        }

        if (!slashTransform)
        {
            slashTransform = GameObject.Find("Slash").transform;
        }

        if (!slashAnimator)
        {
            slashAnimator = GameObject.Find("Slash").GetComponent<Animator>();
        }

        if (!gameManager)
        {
            gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        }

        runner = gameManager.GetRunner();
    }

    // Update is called once per frame
    void Update()
    {
        if (!runner)
        {
            UpdateMouseInput();
        }
    }

    private void UpdateMouseInput()
    {
        mouseX = Input.GetAxisRaw("Mouse X");
        mouseY = Input.GetAxisRaw("Mouse Y");
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!runner)
        {
            Transform collisionTransform = collision.transform;

            if (collisionTransform.gameObject.tag == "ChoppableFood" && (Mathf.Abs(mouseX) > 0 || Mathf.Abs(mouseY) > 0))
            {
                ChopFruit(collision.gameObject);
            }
            else if (collisionTransform.gameObject.tag == "SantaHat")
            {
                Debug.Log("Hit " + collisionTransform.gameObject.name);

                ChopHat(collision.gameObject);
            }
        }
    }

    private IEnumerator Slash()
    {
        if (runner)
        {
            slashTransform.position = this.transform.position;
        }
        else
        {
            Vector2 mousePos = choppingFoodManager.GetMousePosition();

            slashTransform.position = new Vector2(mousePos.x, mousePos.y);
        }

        if (mouseX < 0 || mouseY > 0)
        {
            slashTransform.rotation = Quaternion.Euler(0, 0, 180f);
        }

        //float angle = Mathf.Atan2(slashTransform.position.y, slashTransform.position.x) * Mathf.Rad2Deg;

        //slashTransform.rotation = Quaternion.Euler(0, 0, angle);

        slashAnimator.SetBool("isSlashing", true);

        yield return new WaitForSeconds(0.25f);

        slashAnimator.SetBool("isSlashing", false);
    }

    public void ChopFruit(GameObject fruit)
    {
        slashAnimator.SetBool("isSlashing", false);
        StopCoroutine("Slash");
        StartCoroutine("Slash");

        fruit.GetComponent<ChoppableFood>().GetChopped();

        fruit.GetComponent<BoxCollider2D>().enabled = false;

        fruit.GetComponent<SpriteRenderer>().enabled = false;

        fruit.GetComponentInChildren<ParticleSystem>().Play();

        //choppingFoodManager.slashAudio.Play();
        choppingFoodManager.getSoundManager().PlaySlashSound();
        choppingFoodManager.getSoundManager().PlaySplatSound();

        choppingFoodManager.RemoveFoodToChop(fruit.GetComponent<Rigidbody2D>());
    }

    public void ChopHat(GameObject hat)
    {
        slashAnimator.SetBool("isSlashing", false);
        StopCoroutine("Slash");
        StartCoroutine("Slash");

        hat.GetComponent<ChoppableFood>().GetChopped();

        hat.GetComponent<BoxCollider2D>().enabled = false;

        hat.GetComponent<SpriteRenderer>().enabled = false;

        choppingFoodManager.getSoundManager().PlaySlashSound();
        choppingFoodManager.getSoundManager().PlayWrongChoiceSound();

        choppingFoodManager.LoseGame();
    }
}
