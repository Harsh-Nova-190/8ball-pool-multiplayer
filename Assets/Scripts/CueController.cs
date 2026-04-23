using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class CueController : MonoBehaviour
{
    public event Action OnCueReturned;
    public Transform cuePivot;
    public Transform cueStick;
    public Rigidbody cueBall;
    public Slider aimSlider;
    public Slider powerSlider;
    public float distanceFromBall = 1.5f;
    public float maxForce = 20f;
    public float shootSpeed = 10f;
    private float currentPower = 0f;
    private bool isStriking = false;
    private bool canShoot = true;
    private Vector3 normalPosition;
    private Vector3 pulledBackPosition;
    public CueAimingLine cueAimingLine;
    public BilliardsGameManager billiardsGameManager; // Assign in Inspector or automatically

    void Start()
    {
        aimSlider.minValue = 0f;
        aimSlider.maxValue = 360f;
        powerSlider.minValue = 0f;
        powerSlider.maxValue = 1f;
        var trigger = powerSlider.gameObject.AddComponent<EventTrigger>();
        var up = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
        up.callback.AddListener((_) => ShootCueBall());
        trigger.triggers.Add(up);
        if (cueAimingLine != null)
            cueAimingLine.ShowAimingLine();
    }
    void Update()
    {
        Vector3 aimDir = GetAimDirection();
        if (!isStriking && canShoot)
        {
            normalPosition = cueBall.position - aimDir * distanceFromBall;
            normalPosition.y = cueBall.position.y;
            float powerValue = powerSlider.value;
            pulledBackPosition = normalPosition - aimDir * powerValue * 8f;
            cuePivot.position = pulledBackPosition;
            Vector3 lookDir = cueBall.position - cuePivot.position;
            lookDir.y = 0f;
            cuePivot.rotation = Quaternion.LookRotation(lookDir);
            cueStick.localRotation = Quaternion.identity;
        }
        else if (isStriking)
        {
            Vector3 hitPos = cueBall.position - aimDir * 0.2f;
            hitPos.y = cueBall.position.y;
            cuePivot.position = Vector3.MoveTowards(cuePivot.position, hitPos, shootSpeed * Time.deltaTime);
            Vector3 lookDir = cueBall.position - cuePivot.position;
            lookDir.y = 0f;
            cuePivot.rotation = Quaternion.LookRotation(lookDir);
            cueStick.localRotation = Quaternion.identity;
            if (Vector3.Distance(cuePivot.position, hitPos) < 0.01f)
            {
                cueBall.AddForce(aimDir * currentPower * maxForce, ForceMode.Impulse);
                isStriking = false;
                canShoot = false;
                if (cueAimingLine != null)
                    cueAimingLine.HideAimingLine();
                billiardsGameManager?.OnPlayerShot(); // Stop timer on shot
                StartCoroutine(ReturnCueStickAfterBallsStop());
            }
        }
    }
    private IEnumerator ReturnCueStickAfterBallsStop()
    {
        while (BallsAreMoving())
        {
            yield return null;
        }
        yield return StartCoroutine(ReturnCueStick());
    }
    private bool BallsAreMoving(float threshold = 0.05f)
    {
        string[] ballTags = { "SolidBall", "StripeBall", "CueBall" };
        foreach (string tag in ballTags)
        {
            GameObject[] balls = GameObject.FindGameObjectsWithTag(tag);
            foreach (var ball in balls)
            {
                Rigidbody rb = ball.GetComponent<Rigidbody>();
                if (rb && rb.linearVelocity.sqrMagnitude > threshold * threshold)
                    return true;
            }
        }
        return false;
    }
    public IEnumerator ReturnCueStick()
    {
        if (cueAimingLine != null)
            cueAimingLine.HideAimingLine();
        float duration = 6f;
        float elapsed = 0f;
        Vector3 startPos = cuePivot.position;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cuePivot.position = Vector3.Lerp(startPos, normalPosition, elapsed / duration);
            yield return null;
        }
        cuePivot.position = normalPosition;
        canShoot = true;
        if (cueAimingLine != null)
            cueAimingLine.ShowAimingLine();
        OnCueReturned?.Invoke();
    }
    public void ReturnCueStickImmediately()
    {
        StopAllCoroutines();
        StartCoroutine(ReturnCueStick());
    }
    Vector3 GetAimDirection()
    {
        float angle = aimSlider.value * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle));
    }
    void ShootCueBall()
    {
        if (!canShoot) return;
        currentPower = powerSlider.value;
        isStriking = true;
        StartCoroutine(SnapSliderBack());
    }
    IEnumerator SnapSliderBack()
    {
        float duration = 0f;
        float startValue = powerSlider.value;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            powerSlider.value = Mathf.Lerp(startValue, powerSlider.minValue, elapsed / duration);
            yield return null;
        }
        powerSlider.value = powerSlider.minValue;
    }
}
