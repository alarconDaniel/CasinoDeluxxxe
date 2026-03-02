using UnityEngine;

public class StatsNpcInteractable : MonoBehaviour, IInteractable
{
    [Header("Referencia al menú")]
    public CasinoStatsMenu statsMenu;

    public string Prompt => "Estadísticas";

    public void Interact()
    {
        if (statsMenu == null)
        {
            statsMenu = FindFirstObjectByType<CasinoStatsMenu>();
        }

        if (statsMenu != null)
            statsMenu.SetOpen(true);
        else
            Debug.LogWarning("StatsNpcInteractable: No encontré CasinoStatsMenu en la escena.");
    }
}