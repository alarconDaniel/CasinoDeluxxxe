using UnityEngine;

public abstract class CasinoMachineBase : MonoBehaviour, IInteractable
{
    [Header("Config")]
    public string machineName = "Machine";
    public int requiredLevel = 1;

    public string Prompt => $"Jugar {machineName} (Req. Nivel {requiredLevel})";

    public void Interact()
    {
        var gm = CasinoGameManager.Instance;

        if (gm.level < requiredLevel)
        {
            gm.SetMessage($"{machineName} BLOQUEADA. Necesitas nivel {requiredLevel}.");
            return;
        }

        PlayOnce();
    }

    protected abstract void PlayOnce();
}