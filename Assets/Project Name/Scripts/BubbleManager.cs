using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using System.Collections;
using UnityEngine.XR;

public class BubbleManager : MonoBehaviour
{
    public HapticImpulsePlayer leftControllerImpulse;
    public GameObject spherePrefab; // Assign a sphere prefab in the inspector
    public Transform stylusTip;    // Assign the stylus tip transform in the inspector
    public AudioSource audioSource; // Assign an AudioSource component in the inspector
    public AudioClip chargeSound; // Assign the sound for charging in the inspector
    public AudioClip fireSound;   // Assign the sound for firing in the inspector
    private InputDevice stylus;

    [Header("Haptic Parameters")]
    public float initialAmplitude = 0.2f; // Starting intensity
    public float maxAmplitude = 1.0f;     // Maximum intensity
    public float initialDuration = 1.0f;  // Starting duration
    public float minDuration = 0.1f;      // Shortest duration
    public float pauseTime = 1.0f;        // Pause between haptic impulses
    public int steps = 10;                // Number of steps in the charging sequence
    public float fireForce = 10f;         // Force to fire the sphere into space
    public float linearChargeTime = 3f;  // Time to charge linearly from 0 to 1

    private GameObject spawnedSphere;     // Reference to the spawned sphere

    public enum ChargeMode { Stepped, Linear }
    public ChargeMode chargeMode = ChargeMode.Stepped;

    void Awake()
    {
        InputDevices.deviceConnected += OnInputDeviceConnected;
    }

    public void SwitchCharging()
    {
        // Toggle between Stepped and Linear charging modes
        chargeMode = chargeMode == ChargeMode.Stepped ? ChargeMode.Linear : ChargeMode.Stepped;
    }

    public void StartChargeHaptics()
    {
        if (leftControllerImpulse == null || spherePrefab == null || stylusTip == null || audioSource == null)
        {
            Debug.LogError("HapticImpulsePlayer, SpherePrefab, StylusTip, or AudioSource is not assigned.");
            return;
        }

        switch (chargeMode)
        {
            case ChargeMode.Stepped:
                StartCoroutine(SteppedChargeRoutine());
                break;

            case ChargeMode.Linear:
                StartCoroutine(LinearChargeRoutine());
                break;

            default:
                Debug.LogError("Unknown charge mode selected.");
                break;
        }
    }

    private IEnumerator SteppedChargeRoutine()
    {
        float amplitudeStep = (maxAmplitude - initialAmplitude) / steps;
        float durationStep = (initialDuration - minDuration) / steps;

        float currentAmplitude = initialAmplitude;
        float currentDuration = initialDuration;

        // Spawn the sphere at the stylus tip
        spawnedSphere = Instantiate(spherePrefab, stylusTip.position, Quaternion.identity, stylusTip.transform);
        float fullSize = spawnedSphere.transform.localScale.x;
        spawnedSphere.transform.localScale = Vector3.zero;

        for (int i = 0; i <= steps; i++)
        {
            // Play charge sound with adjusted amplitude
            PlayChargeSound(currentAmplitude);

            // Send haptic impulse
            leftControllerImpulse.SendHapticImpulse(currentAmplitude, currentDuration);
            stylus.SendHapticImpulse(0, currentAmplitude, currentDuration);

            // Grow the sphere's size based on the current amplitude
            float scale = Mathf.Lerp(0, 1, currentAmplitude / maxAmplitude);
            spawnedSphere.transform.localScale = (Vector3.one * fullSize) * scale;

            // Pause for the current duration
            yield return new WaitForSeconds(currentDuration);

            // Increase amplitude and decrease duration
            currentAmplitude += amplitudeStep;
            currentDuration -= durationStep;
        }

        // Fire the sphere into space
        FireSphere();
    }

    private IEnumerator LinearChargeRoutine()
    {
        // Spawn the sphere at the stylus tip
        spawnedSphere = Instantiate(spherePrefab, stylusTip.position, Quaternion.identity, stylusTip.transform);
        float fullSize = spawnedSphere.transform.localScale.x;
        spawnedSphere.transform.localScale = Vector3.zero;

        float elapsedTime = 0f;

        while (elapsedTime < linearChargeTime)
        {
            float progress = elapsedTime / linearChargeTime;
            float currentAmplitude = Mathf.Lerp(0, maxAmplitude, progress);

            // Play charge sound with adjusted amplitude
            PlayChargeSound(currentAmplitude);

            // Send haptic impulse
            leftControllerImpulse.SendHapticImpulse(currentAmplitude, Time.deltaTime);
            stylus.SendHapticImpulse(0, currentAmplitude, Time.deltaTime);

            // Grow the sphere's size based on the current amplitude
            spawnedSphere.transform.localScale = (Vector3.one * fullSize) * progress;

            elapsedTime += Time.deltaTime;
            yield return null; // Wait for the next frame
        }

        // Ensure it's fully charged
        spawnedSphere.transform.localScale = Vector3.one * fullSize;

        // Fire the sphere into space
        FireSphere();
    }

    private void FireSphere()
    {
        if (spawnedSphere != null)
        {
            spawnedSphere.transform.parent = null;
            Rigidbody rb = spawnedSphere.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = spawnedSphere.AddComponent<Rigidbody>();
            }

            // Play fire sound
            PlayFireSound();

            rb.AddForce(-stylusTip.up * fireForce, ForceMode.Impulse);
        }
    }

    private void PlayChargeSound(float amplitude)
    {
        if (chargeSound != null && audioSource != null)
        {
            audioSource.clip = chargeSound;
            audioSource.volume = Mathf.Clamp(amplitude, 0.1f, 1f); // Scale volume with amplitude
            if (!audioSource.isPlaying)
            {
                audioSource.Play();
            }
        }
    }

    private void PlayFireSound()
    {
        if (fireSound != null && audioSource != null)
        {
            audioSource.clip = fireSound;
            audioSource.volume = 1f; // Full volume for fire sound
            audioSource.Play();
        }
    }

    void OnInputDeviceConnected(InputDevice newDevice)
    {
        bool isStylus = newDevice.name.ToLower().Contains("logitech");
        if (isStylus)
            stylus = newDevice;
    }
}
