using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class CueAimingLine : MonoBehaviour
{
    public BilliardsGameManager gameManager; // Assign this in Inspector or auto-find
    public Transform cueBall;  // Assign in inspector
    public Transform cueStick; // Assign in inspector
    public float maxDistance = 20f;
    public LayerMask collisionLayers; // Assign PoolCollision layer in Inspector

    private LineRenderer lineRenderer;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        if (!gameManager)
            gameManager = FindObjectOfType<BilliardsGameManager>();
        lineRenderer.enabled = true;
    }

    void Update()
    {
        if (lineRenderer == null || gameManager == null) return;

        if (cueBall == null || cueStick == null)
        {
            if (lineRenderer.enabled) lineRenderer.enabled = false;
            return;
        }
        if (!lineRenderer.enabled) return;

        var currentPlayerGroup = gameManager.GetCurrentPlayerGroup();

        Vector3 start = cueBall.position;
        Vector3 direction = cueStick.forward;
        direction.y = 0f;
        direction = direction.normalized;
        start.y = cueBall.position.y;

        RaycastHit hit;
        lineRenderer.positionCount = 1;
        lineRenderer.SetPosition(0, start);

        bool wrongTarget = false;

        if (Physics.Raycast(start, direction, out hit, maxDistance, collisionLayers))
        {
            Vector3 hitPoint = hit.point;
            hitPoint.y = cueBall.position.y;
            lineRenderer.positionCount = 3;
            lineRenderer.SetPosition(1, hitPoint);

            // Check if aiming at wrong ball group
            if ((hit.collider.CompareTag("SolidBall") && currentPlayerGroup == BilliardsGameManager.BallGroup.Stripes) ||
                (hit.collider.CompareTag("StripeBall") && currentPlayerGroup == BilliardsGameManager.BallGroup.Solids))
            {
                wrongTarget = true;
            }

            SetLineColor(wrongTarget ? Color.red : Color.white);

            bool isReflectable = hit.collider.gameObject.CompareTag("Rail");
            if (isReflectable)
            {
                Vector3 reflectDir = Vector3.Reflect(direction, hit.normal);
                reflectDir.y = 0f;
                reflectDir = reflectDir.normalized;
                lineRenderer.SetPosition(2, hitPoint + reflectDir * (maxDistance * 0.5f));
            }
            else
            {
                lineRenderer.positionCount = 2;
            }
        }
        else
        {
            SetLineColor(Color.white);
            Vector3 end = start + direction * maxDistance;
            end.y = cueBall.position.y;
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(1, end);
        }
    }

    private void SetLineColor(Color color)
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(color, 0), new GradientColorKey(color, 1) },
            new GradientAlphaKey[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) }
        );
        lineRenderer.colorGradient = gradient;
    }

    public void ShowAimingLine()
    {
        if (lineRenderer != null)
            lineRenderer.enabled = true;
    }
    public void HideAimingLine()
    {
        if (lineRenderer != null)
            lineRenderer.enabled = false;
    }
}
