using UnityEngine;

public class YahtzeeTableMachine : CasinoMachineBase
{
    [Header("Refs")]
    public YahtzeeController controller;

    protected override void PlayOnce()
    {
        if (controller == null)
        {
            controller = FindFirstObjectByType<YahtzeeController>();
        }

        if (controller == null)
        {
            Debug.LogWarning("YahtzeeTableMachine: No encontré YahtzeeController.");
            return;
        }

        controller.TryStartMatch();
    }
}