using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class CleanHouseManagerClient : NetworkBehaviour
{
    public Transform vacuumSprite;

    private SoundManager soundManager;

    // Start is called before the first frame update
    void Start()
    {
        soundManager = GameObject.Find("SoundManager").GetComponent<SoundManager>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public override void FixedUpdateNetwork()
    {
        if (!vacuumSprite)
        {
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
    }

    /// <summary>
    /// Updates the position of the vacuum sprite to follow the player's cursor
    /// </summary>
    /// <param name="data"></param>
    private void UpdateVacuumPosition(NetworkInputData data)
    {
        vacuumSprite.transform.position = data.mousePosition;
    }
}
