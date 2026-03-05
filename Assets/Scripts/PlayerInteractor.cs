using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    public float interactDistance = 3f;
    public HUDController hud;

    [Header("Opcional: referencia al menú")]
    public CasinoStatsMenu statsMenu;

    [Header("Opcional: referencia al menú de créditos")]
    public CasinoCreditsMenu creditsMenu;

    void Update()
    {
        // Si algún menú está abierto, no interactuamos con nada.
        if ((statsMenu != null && statsMenu.IsOpen) || (creditsMenu != null && creditsMenu.IsOpen))
        {
            hud.SetPrompt("");
            return;
        }

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