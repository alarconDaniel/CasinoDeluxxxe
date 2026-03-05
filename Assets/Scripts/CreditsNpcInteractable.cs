using UnityEngine;

public class CreditsNpcInteractable : MonoBehaviour, IInteractable
{
    [Header("Referencia al menú")]
    public CasinoCreditsMenu creditsMenu;

    public string Prompt => "Créditos";

    public void Interact()
    {
        if (creditsMenu == null)
            creditsMenu = FindFirstObjectByType<CasinoCreditsMenu>();

        if (creditsMenu != null)
            creditsMenu.SetOpen(true);
        else
            Debug.LogWarning("CreditsNpcInteractable: No encontré CasinoCreditsMenu en la escena.");
    }
}