using Fusion;
using Unity;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MouseCursorNetworked : NetworkBehaviour
{
    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData data))
        {
            transform.position = data.mousePosition;
            if (data.mouseDown && Runner.IsServer && SceneManager.GetActiveScene().name == "DecorateTree")
            {
                Debug.Log("Getting mouse input");
                GameObject.Find("SceneManager").GetComponent<DecorateTreeManager>().UpdateClientInput(data);
                //RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
                /*RaycastHit2D hit = Physics2D.Raycast(data.mousePosition, Vector2.zero);
                if (hit.collider != null)
                {
                    Debug.Log("Target Position: " + hit.collider.gameObject.transform.position);
                    if (hit.collider.tag == "Ornament")
                    {
                        Debug.Log("Dragging ornament, updating ornament position:", this.gameObject);
                        //mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                        //mousePosition = data.mousePosition;
                        //transform.position = new Vector2(mousePosition.x - deltaX, mousePosition.y - deltaY);
                        MoveOrnament moveOrnamnet = hit.collider.GetComponent<MoveOrnament>();
                        //moveOrnamnet.SetDeltaOffset(Input.mousePosition);
                        //moveOrnamnet.ornamentPosition = new Vector2(Input.mousePosition.x - moveOrnamnet.GetDeltaX(), Input.mousePosition.y - moveOrnamnet.GetDeltaY());
                        moveOrnamnet.ornamentPosition = new Vector2(data.mousePosition.x, data.mousePosition.y);
                        Debug.Log("New ornament position: " + moveOrnamnet.ornamentPosition, moveOrnamnet.gameObject);
                              
                    }
                }*/
            }
        }
    }

    private void OnDestroy()
    {
        Log.Debug("Destroyed mouse object");
    }
}
