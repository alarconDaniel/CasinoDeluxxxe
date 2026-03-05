using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    public float interactDistance = 3f;
    public HUDController hud;

    [Header("Opcional: referencia al menú")]
    public CasinoStatsMenu statsMenu;

    [Header("Opcional: referencia al menú de créditos")]
    public CasinoCreditsMenu creditsMenu;

    // --- ADICIONAL (no rompe lo que ya tienes) ---
    [Header("Raycast (recomendado)")]
    public Camera raycastCamera;                               // si la asignas, el rayo sale desde la cámara
    public Vector3 rayOriginOffset = new Vector3(0f, 1.6f, 0f); // si NO hay cámara, sube el origen del rayo
    public LayerMask interactLayers = ~0;                      // por defecto: Everything
    public QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

    void Update()
    {
        // Si algún menú está abierto, no interactuamos con nada.
        if ((statsMenu != null && statsMenu.IsOpen) || (creditsMenu != null && creditsMenu.IsOpen))
        {
            if (hud != null) hud.SetPrompt("");
            return;
        }

        IInteractable target = null;

        // --- Tu ray original (lo conservo) ---
        Ray ray = new Ray(transform.position, transform.forward);

        // --- ADICIONAL: preferir cámara si existe (FPS/TPP apunta mejor) ---
        if (raycastCamera != null)
        {
            ray = new Ray(raycastCamera.transform.position, raycastCamera.transform.forward);
        }
        else
        {
            // Si no hay cámara asignada, al menos subimos el origen para que no salga desde los pies
            ray = new Ray(transform.position + rayOriginOffset, transform.forward);
        }

        // --- Tu raycast (mejorado con layers + triggers configurables) ---
        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactLayers, triggerInteraction))
        {
            target = hit.collider.GetComponentInParent<IInteractable>();
        }

        if (target != null)
        {
            if (hud != null) hud.SetPrompt(target.Prompt + " (E)");
            if (Input.GetKeyDown(KeyCode.E))
                target.Interact();
        }
        else
        {
            if (hud != null) hud.SetPrompt("");
        }
    }
}