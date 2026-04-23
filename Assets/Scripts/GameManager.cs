using UnityEngine;

public class GameManager : MonoBehaviour
{
    public CueController cueController;
    public BilliardsGameManager billiardsGameManager;

    void Start()
    {
        if (cueController != null)
        {
            cueController.OnCueReturned += HandleCueReturned;
        }
    }

    private void HandleCueReturned()
    {
        billiardsGameManager?.EndTurn();
    }

    void OnDestroy()
    {
        if (cueController != null)
        {
            cueController.OnCueReturned -= HandleCueReturned;
        }
    }
}
