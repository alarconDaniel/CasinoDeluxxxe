using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    public float interactDistance = 3f;
    public HUDController hud;

    void Update()
    {
        IInteractable target = null;

        Ray ray = new Ray(transform.position, transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance))
        {
            target = hit.collider.GetComponentInParent<IInteractable>();
        }

        if (target != null)
        {
            hud.SetPrompt(target.Prompt + " (E)");
            if (Input.GetKeyDown(KeyCode.E))
                target.Interact();
        }
        else
        {
            hud.SetPrompt("");
        }
    }
}