using Fusion;

public class MouseCursorNetworked : NetworkBehaviour
{
    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData data))
        {
            transform.position = data.mousePosition;
        }
    }

    private void OnDestroy()
    {
        Log.Debug("Destroyed mouse object");
    }
}
