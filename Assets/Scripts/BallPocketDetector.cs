using UnityEngine;

public class BallPocketDetector : MonoBehaviour
{
    public BilliardsGameManager gameManager;

    private void Start()
    {
        if (gameManager == null)
            gameManager = FindObjectOfType<BilliardsGameManager>();
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if the collided object has any valid ball tag
        if (other.CompareTag("CueBall") || other.CompareTag("EightBall") || other.CompareTag("SolidBall") || other.CompareTag("StripeBall"))
        {
            gameManager.OnBallPocketed(other.gameObject);

            // Disable the ball after pocketing except for cue ball which is handled differently in gameManager
            if (!other.CompareTag("CueBall"))
            {
                other.gameObject.SetActive(false);
            }
        }
    }
}
