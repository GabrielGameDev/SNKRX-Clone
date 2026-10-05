using UnityEngine;
using UnityEngine.InputSystem;

public class InputController : MonoBehaviour
{
    public InputActionReference moveAction;

    public Vector2 Movement { get; private set; }

    private void OnEnable()
    {
        if (moveAction != null && moveAction.action != null)
        {
            moveAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (moveAction != null && moveAction.action != null)
        {
            moveAction.action.Disable();
        }
    }

    private void Update()
    {
        if (moveAction != null && moveAction.action != null)
        {
            Movement = moveAction.action.ReadValue<Vector2>();
        }
    }
}
