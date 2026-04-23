using UnityEngine;
using TMPro;
using System.Collections;

public class BilliardsGameManager : MonoBehaviour
{
    [SerializeField] private int currentPlayer = 0;
    public int CurrentPlayer => currentPlayer;
    public int[] playerScores = new int[2];
    public TextMeshProUGUI[] playerScoreTexts;
    public TextMeshProUGUI[] playerGroupTexts;
    public TextMeshProUGUI turnText;
    public TextMeshProUGUI timerText; // Assign in Inspector
    public GameObject cueBall;
    public Transform cueBallStartPosition;
    public CueController cueController; // Assign in Inspector

    private bool scoredThisTurn = false;
    public enum BallGroup { None, Solids, Stripes }
    [SerializeField] private BallGroup[] playerGroups = new BallGroup[2] { BallGroup.None, BallGroup.None };
    public BallGroup[] PlayerGroups => playerGroups;
    private int[] ballsPocketedCount = new int[2] { 0, 0 };
    private const int ballsPerGroup = 7;
    private Coroutine turnMessageCoroutine;
    private Coroutine timerCoroutine;
    private float turnTimeLimit = 30f;
    private float currentTurnTime;

    void Start()
    {
        ResetGame();
    }

    public void ResetGame()
    {
        playerScores[0] = 0;
        playerScores[1] = 0;
        ballsPocketedCount[0] = 0;
        ballsPocketedCount[1] = 0;
        currentPlayer = 0;
        scoredThisTurn = false;
        playerGroups[0] = BallGroup.None;
        playerGroups[1] = BallGroup.None;
        UpdateUI();
        ShowTurnMessage($"Player {currentPlayer + 1}'s turn");
        StartTurnTimer();
    }

    // TIMER SYSTEM
    public void StartTurnTimer()
    {
        if (timerCoroutine != null)
            StopCoroutine(timerCoroutine);
        currentTurnTime = turnTimeLimit;
        timerCoroutine = StartCoroutine(TurnTimerCoroutine());
    }
    private IEnumerator TurnTimerCoroutine()
    {
        while (currentTurnTime > 0)
        {
            if (timerText)
                timerText.text = $"{Mathf.CeilToInt(currentTurnTime)}";
            currentTurnTime -= Time.deltaTime;
            yield return null;
        }
        if (timerText) timerText.text = "0";
        HandleTurnTimeout();
    }
    public void StopTurnTimer()
    {
        if (timerCoroutine != null)
            StopCoroutine(timerCoroutine);
        timerCoroutine = null;
        // Do NOT clear timerText here! Let it freeze displaying the remaining time.
    }
    private void HandleTurnTimeout()
    {
        ShowTurnMessage($"Time's up! Player {currentPlayer + 1}'s turn skipped.");
        EndTurn(true);
        cueController?.ReturnCueStickImmediately();
    }
    // Call this when a cue shot is played:
    public void OnPlayerShot()
    {
        StopTurnTimer();
        // Timer will freeze, showing the time left on player's clock after the shot.
    }
    private BallGroup GetBallGroup(GameObject ball)
    {
        if (ball.CompareTag("SolidBall"))
            return BallGroup.Solids;
        else if (ball.CompareTag("StripeBall"))
            return BallGroup.Stripes;
        else
            return BallGroup.None;
    }
    public void OnBallPocketed(GameObject ball)
    {
        if (ball.CompareTag("CueBall"))
        {
            HandleCueBallFoul();
            return;
        }
        if (ball.CompareTag("EightBall"))
        {
            HandleEightBallPocketed();
            return;
        }
        BallGroup ballGroup = GetBallGroup(ball);
        if (ballGroup == BallGroup.None) return;
        if (playerGroups[0] == BallGroup.None && playerGroups[1] == BallGroup.None)
        {
            playerGroups[currentPlayer] = ballGroup;
            playerGroups[1 - currentPlayer] = (ballGroup == BallGroup.Solids) ? BallGroup.Stripes : BallGroup.Solids;
            ShowTurnMessage($"Player {currentPlayer + 1} assigned {playerGroups[currentPlayer]}!");
        }
        if (ballGroup == playerGroups[currentPlayer])
        {
            playerScores[currentPlayer]++;
            ballsPocketedCount[currentPlayer]++;
            scoredThisTurn = true;
            if (ballsPocketedCount[currentPlayer] == ballsPerGroup)
                ShowTurnMessage($"Player {currentPlayer + 1} cleared group! 8 ball to pocket");
            else
                ShowTurnMessage($"Player {currentPlayer + 1} scored! Player continues.");
        }
        else
        {
            playerScores[1 - currentPlayer]++;
            scoredThisTurn = false;
            ShowTurnMessage($"Foul! Wrong group pocketed. Player {1 - currentPlayer + 1} gets a point and ball-in-hand.");
            SwitchTurn();
        }
        UpdateUI();
    }
    private void HandleCueBallFoul()
    {
        scoredThisTurn = false;
        SwitchTurn();
        ShowTurnMessage($"Foul! Cue ball pocketed. Player {currentPlayer + 1}'s turn.");
        if (cueBall != null && cueBallStartPosition != null)
        {
            Rigidbody rb = cueBall.GetComponent<Rigidbody>();
            cueBall.transform.position = cueBallStartPosition.position;
            cueBall.transform.rotation = cueBallStartPosition.rotation;
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
        UpdateUI();
        StartTurnTimer();
        cueController?.ReturnCueStickImmediately();
    }
    private void HandleEightBallPocketed()
    {
        if (playerGroups[currentPlayer] == BallGroup.None)
        {
            ShowTurnMessage($"Player {currentPlayer + 1} pockets 8-ball too early! Player loses!");
            scoredThisTurn = false;
        }
        else
        {
            ShowTurnMessage($"Player {currentPlayer + 1} wins!");
            scoredThisTurn = false;
        }
        UpdateUI();
        StopTurnTimer();
    }
    // Pass forceEnd=true on timeout to always switch turn and reset timer
    public void EndTurn(bool forceEnd = false)
    {
        if (forceEnd || !scoredThisTurn)
        {
            SwitchTurn();
            ShowTurnMessage($"Player {currentPlayer + 1}'s turn");
        }
        else
        {
            ShowTurnMessage($"Player {currentPlayer + 1} continues!");
        }
        scoredThisTurn = false;
        UpdateUI();
        StartTurnTimer();
    }
    private void SwitchTurn()
    {
        currentPlayer = 1 - currentPlayer;
    }
    private void UpdateUI()
    {
        for (int i = 0; i < playerScores.Length; i++)
        {
            if (playerScoreTexts != null && playerScoreTexts.Length > i && playerScoreTexts[i] != null)
                playerScoreTexts[i].text = $"Player {i + 1} Score: {playerScores[i]}";
            if (playerGroupTexts != null && playerGroupTexts.Length > i && playerGroupTexts[i] != null)
            {
                if (playerGroups[i] == BallGroup.None)
                    playerGroupTexts[i].text = "Assigned: None";
                else if (playerGroups[i] == BallGroup.Solids)
                    playerGroupTexts[i].text = "Assigned: Solids (1-7)";
                else
                    playerGroupTexts[i].text = "Assigned: Stripes (9-15)";
            }
        }
    }
    private void ShowTurnMessage(string message, float duration = 2f)
    {
        if (turnMessageCoroutine != null)
            StopCoroutine(turnMessageCoroutine);
        turnMessageCoroutine = StartCoroutine(ShowTurnMessageCoroutine(message, duration));
    }
    private IEnumerator ShowTurnMessageCoroutine(string message, float duration)
    {
        turnText.text = message;
        yield return new WaitForSeconds(duration);
        turnText.text = "";
        turnMessageCoroutine = null;
    }
    public BallGroup GetCurrentPlayerGroup()
    {
        if (currentPlayer >= 0 && currentPlayer < playerGroups.Length)
            return playerGroups[currentPlayer];
        return BallGroup.None;
    }
}
