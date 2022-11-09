using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// file: MoveOrnament.cs
/// description: Allows player to drag and drop ornaments onto the tree using the mouse
/// authors: Nathan Ballay, Zaven Kazandjian
/// </summary>
public class MoveOrnament : NetworkBehaviour
{
    // References
    [SerializeField] GameObject tree;
    [SerializeField] Timer timer;

    // Vectors
    private Vector2 initialPosition;
    private Vector2 mousePosition;
    [SerializeField] [Networked(OnChanged = nameof(OnOrnamentPositionChanged))] public Vector2 ornamentPosition { get; set; }


    // References
    private GameManager gameManager;
    private NetworkRunner runner;

    // Variables
    private float deltaX, deltaY;
    [SerializeField] private bool locked = false;
    //[SerializeField] private bool isGettingDragged = false;
    [SerializeField] [Networked] public bool isGettingDragged { get; set; } = false;

    // Start is called before the first frame update
    void Start()
    {
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();

        runner = gameManager.GetRunner();

        initialPosition = transform.position;
        ornamentPosition = transform.position;
    }

    //public override void FixedUpdateNetwork()
    //{
    //    if (GetInput(out NetworkInputData data))
    //    {
    //        if (data.mouseDown)
    //        {
    //            //RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
    //            RaycastHit2D hit = Physics2D.Raycast(data.mousePosition, Vector2.zero);
    //            if (hit.collider)
    //            {
    //                Debug.Log("Hit an object object", this.gameObject);
    //                if (hit.collider == this.gameObject)
    //                {
    //                    Debug.Log("Dragging ornament, updating ornament position:", this.gameObject);
    //                    //mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
    //                    mousePosition = data.mousePosition;
    //                    //transform.position = new Vector2(mousePosition.x - deltaX, mousePosition.y - deltaY);
    //                    SetDeltaOffset(data.mousePosition);
    //                    ornamentPosition = new Vector2(mousePosition.x - deltaX, mousePosition.y - deltaY);
    //                    Debug.Log("New ornament position: " + ornamentPosition, this.gameObject);
    //                }
    //            }
    //            //Debug.Log("Player has input authority");
    //            /*if (isGettingDragged)
    //            {
    //                Debug.Log("Dragging ornament, updating ornament position:", this.gameObject);
    //                //mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
    //                mousePosition = data.mousePosition;
    //                //transform.position = new Vector2(mousePosition.x - deltaX, mousePosition.y - deltaY);
    //                ornamentPosition = new Vector2(mousePosition.x - deltaX, mousePosition.y - deltaY);
    //                Debug.Log("New ornament position: " + ornamentPosition, this.gameObject);
    //            }*/
    //        }
    //    }
    //    else
    //    {
    //        Debug.Log("No input authority on this object");
    //    }

    //    /*if (runner)
    //    {
    //        if (runner.IsServer)
    //        {
    //            transform.position = ornamentPosition;
    //        }
    //    }*/

    //    //Debug.Log("Updating transform position with ornament position");
    //    //transform.position = ornamentPosition;
    //}

public static void OnOrnamentPositionChanged(Changed<MoveOrnament> changed)
    {
        changed.Behaviour.OnOrnamentPositionChanged();
    }

    private void OnOrnamentPositionChanged()
    {
        if (runner)
        {
            if (runner.IsServer)
            {
                Debug.Log("Updating transform position with ornament position: " + ornamentPosition);
                transform.position = ornamentPosition;
            }
        }
    }

    public void SetDeltaOffset(Vector3 currentMousePosition)
    {
        deltaX = currentMousePosition.x - transform.position.x;
        deltaY = currentMousePosition.y - transform.position.y;
    }

    public float GetDeltaX()
    {
        return deltaX;
    }

    public float GetDeltaY()
    {
        return deltaY;
    }

    /// <summary>
    /// Calculates mouse movement delta while the button is held down
    /// </summary>
    /*private void OnMouseDown()
    {
        if (runner)
        {
            return;
        }
        if (!gameManager.isGamePaused())
        {
            deltaX = Camera.main.ScreenToWorldPoint(Input.mousePosition).x - transform.position.x;
            deltaY = Camera.main.ScreenToWorldPoint(Input.mousePosition).y - transform.position.y;
        }
    }*/
    
    /// <summary>
    /// Redraws the ornament at the new mouse location
    /// </summary>
    /*private void OnMouseDrag()
    {
        if (runner)
        {
            //return;
            isGettingDragged = true;
        }
        else if (!locked && !gameManager.isGamePaused())
        {
            mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            transform.position = new Vector2(mousePosition.x - deltaX, mousePosition.y - deltaY);
        }
    }*/

    /// <summary>
    /// Moves ornament back to initial position if placed off screen
    /// </summary>
    /*private void OnMouseUp()
    {
        if (runner)
        {
            //return;
            isGettingDragged = false;
        }

        if (!GetComponent<Renderer>().isVisible)
        {
            transform.position = initialPosition;
        }
    }*/

    /// <summary>
    /// Getter to see if the ornament has been properly placed
    /// </summary>
    /// <returns></returns>
    public bool isLocked()
    {
        return locked;
    }

    /// <summary>
    /// Locks the ornament from being picked up
    /// </summary>
    public void LockOrnament()
    {
        locked = true;
    }
}
