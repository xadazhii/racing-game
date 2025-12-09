using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MyCarController : MonoBehaviour
{
    #region CAR SETUP
    [Space(20)]
    [Header("CAR SETUP")]
    [Space(10)]
    public int maxSpeed = 300;
    public int maxReverseSpeed = 60;
    public int accelerationMultiplier = 30;

    [Space(10)]
    public int maxSteeringAngle = 35;
    [Range(0.1f, 1f)]
    public float steeringSpeed = 0.5f;

    [Space(10)]
    public int brakeForce = 800;
    public int decelerationMultiplier = 2;
    public int handbrakeDriftMultiplier = 5;

    [Space(10)]
    public Vector3 bodyMassCenter;
    #endregion

    #region POWER-UPS SETUP
    [Space(20)]
    [Header("POWER-UPS")]
    [Tooltip("Multiplier for nitro speed boost (default: 3x)")]
    public int nitroMultiplier = 3;

    [Tooltip("Left nitro particle effect")]
    public ParticleSystem nitroLeftParticles;

    [Tooltip("Right nitro particle effect")]
    public ParticleSystem nitroRightParticles;

    [Header("Oil Settings")]
    [Tooltip("Oil slick prefab to drop behind car")]
    public GameObject oilSlickPrefab;

    [Tooltip("Position where oil spawns (create empty GameObject behind car)")]
    public Transform oilSpawnPoint;

    [Header("Freeze Settings")]
    [Tooltip("Prefab for the freeze projectile")]
    public GameObject freezeRayPrefab;
    [Tooltip("Position where projectile spawns (create empty GameObject in front of car)")]
    public Transform firePoint;

    [Header("Shield Settings")]
    [Tooltip("Shield visual effect (Sphere around car)")]
    public GameObject shieldVisual;

    [Space(10)]
    [Header("Status (Read Only)")]
    public bool isNitroOn = false;
    public bool isShielded = false;
    public bool isFrozen = false;
    public float currentSpeedFactor = 1.0f;
    #endregion

    #region WHEELS
    [Header("WHEELS")]
    public GameObject frontLeftMesh;
    public WheelCollider frontLeftCollider;

    [Space(10)]
    public GameObject frontRightMesh;
    public WheelCollider frontRightCollider;

    [Space(10)]
    public GameObject rearLeftMesh;
    public WheelCollider rearLeftCollider;

    [Space(10)]
    public GameObject rearRightMesh;
    public WheelCollider rearRightCollider;
    #endregion

    #region EFFECTS
    [Header("EFFECTS")]
    public bool useEffects = true;
    public ParticleSystem RLWParticleSystem;
    public ParticleSystem RRWParticleSystem;
    public TrailRenderer RLWTireSkid;
    public TrailRenderer RRWTireSkid;

    [Header("UI Effects")]
    public GameObject nitroOverlay;
    public GameObject frozenOverlay;
    #endregion

    #region SOUNDS
    [Header("Sounds")]
    public bool useSounds = false;
    public AudioSource carEngineSound;
    public AudioSource tireScreechSound;
    private float initialCarEngineSoundPitch;
    #endregion

    #region UI
    [Header("UI")]
    public bool useUI = false;
    public Text carSpeedText;
    #endregion

    #region CONTROLS
    [Header("CONTROLS")]
    public bool useTouchControls = false;
    public GameObject throttleButton;
    private PrometeoTouchInput throttlePTI;
    public GameObject reverseButton;
    private PrometeoTouchInput reversePTI;
    public GameObject turnRightButton;
    private PrometeoTouchInput turnRightPTI;
    public GameObject turnLeftButton;
    private PrometeoTouchInput turnLeftPTI;
    public GameObject handbrakeButton;
    private PrometeoTouchInput handbrakePTI;
    #endregion

    #region PRIVATE VARIABLES
    [HideInInspector] public float carSpeed;
    [HideInInspector] public bool isDrifting;
    [HideInInspector] public bool isTractionLocked;
    [HideInInspector] public bool isFinished = false;

    private Rigidbody carRigidbody;
    private float steeringAxis;
    private float throttleAxis;
    private float driftingAxis;
    private float localVelocityZ;
    private float localVelocityX;
    private bool deceleratingCar;
    private bool touchControlsSetup = false;

    private WheelFrictionCurve FLwheelFriction;
    private float FLWextremumSlip;
    private WheelFrictionCurve FRwheelFriction;
    private float FRWextremumSlip;
    private WheelFrictionCurve RLwheelFriction;
    private float RLWextremumSlip;
    private WheelFrictionCurve RRwheelFriction;
    private float RRWextremumSlip;
    #endregion

    #region UNITY LIFECYCLE
    void Start()
    {
        useEffects = true;
        carRigidbody = gameObject.GetComponent<Rigidbody>();
        carRigidbody.centerOfMass = bodyMassCenter;

        InitializeWheelFriction();
        InitializeAudio();
        InitializeUI();
        InitializeEffects();
        InitializeTouchControls();
        InitializeVisuals();
    }

    void Update()
    {
        carSpeed = (2 * Mathf.PI * frontLeftCollider.radius * frontLeftCollider.rpm * 60) / 1000;
        localVelocityX = transform.InverseTransformDirection(carRigidbody.linearVelocity).x;
        localVelocityZ = transform.InverseTransformDirection(carRigidbody.linearVelocity).z;

        if (isFinished || isFrozen)
        {
            isNitroOn = false;
            if (nitroOverlay) nitroOverlay.SetActive(false);

            ThrottleOff();

            if (!deceleratingCar)
            {
                InvokeRepeating("DecelerateCar", 0f, 0.1f);
                deceleratingCar = true;
            }

            AnimateWheelMeshes();
            CarEffects();
            return;
        }

        if (!useTouchControls)
        {
            HandleKeyboardInput();
        }
        else if (touchControlsSetup)
        {
            HandleTouchInput();
        }

        AnimateWheelMeshes();
        CarEffects();
    }
    #endregion

    #region INITIALIZATION METHODS
    private void InitializeWheelFriction()
    {
        FLwheelFriction = new WheelFrictionCurve();
        FLwheelFriction.extremumSlip = frontLeftCollider.sidewaysFriction.extremumSlip;
        FLWextremumSlip = frontLeftCollider.sidewaysFriction.extremumSlip;
        FLwheelFriction.extremumValue = frontLeftCollider.sidewaysFriction.extremumValue;
        FLwheelFriction.asymptoteSlip = frontLeftCollider.sidewaysFriction.asymptoteSlip;
        FLwheelFriction.asymptoteValue = frontLeftCollider.sidewaysFriction.asymptoteValue;
        FLwheelFriction.stiffness = frontLeftCollider.sidewaysFriction.stiffness;

        FRwheelFriction = new WheelFrictionCurve();
        FRwheelFriction.extremumSlip = frontRightCollider.sidewaysFriction.extremumSlip;
        FRWextremumSlip = frontRightCollider.sidewaysFriction.extremumSlip;
        FRwheelFriction.extremumValue = frontRightCollider.sidewaysFriction.extremumValue;
        FRwheelFriction.asymptoteSlip = frontRightCollider.sidewaysFriction.asymptoteSlip;
        FRwheelFriction.asymptoteValue = frontRightCollider.sidewaysFriction.asymptoteValue;
        FRwheelFriction.stiffness = frontRightCollider.sidewaysFriction.stiffness;

        RLwheelFriction = new WheelFrictionCurve();
        RLwheelFriction.extremumSlip = rearLeftCollider.sidewaysFriction.extremumSlip;
        RLWextremumSlip = rearLeftCollider.sidewaysFriction.extremumSlip;
        RLwheelFriction.extremumValue = rearLeftCollider.sidewaysFriction.extremumValue;
        RLwheelFriction.asymptoteSlip = rearLeftCollider.sidewaysFriction.asymptoteSlip;
        RLwheelFriction.asymptoteValue = rearLeftCollider.sidewaysFriction.asymptoteValue;
        RLwheelFriction.stiffness = rearLeftCollider.sidewaysFriction.stiffness;

        RRwheelFriction = new WheelFrictionCurve();
        RRwheelFriction.extremumSlip = rearRightCollider.sidewaysFriction.extremumSlip;
        RRWextremumSlip = rearRightCollider.sidewaysFriction.extremumSlip;
        RRwheelFriction.extremumValue = rearRightCollider.sidewaysFriction.extremumValue;
        RRwheelFriction.asymptoteSlip = rearRightCollider.sidewaysFriction.asymptoteSlip;
        RRwheelFriction.asymptoteValue = rearRightCollider.sidewaysFriction.asymptoteValue;
        RRwheelFriction.stiffness = rearRightCollider.sidewaysFriction.stiffness;
    }

    private void InitializeAudio()
    {
        if (carEngineSound != null)
        {
            initialCarEngineSoundPitch = carEngineSound.pitch;
        }

        if (useSounds)
        {
            InvokeRepeating("CarSounds", 0f, 0.1f);
        }
        else
        {
            if (carEngineSound != null) carEngineSound.Stop();
            if (tireScreechSound != null) tireScreechSound.Stop();
        }
    }

    private void InitializeUI()
    {
        if (useUI)
        {
            InvokeRepeating("CarSpeedUI", 0f, 0.1f);
        }
        else if (carSpeedText != null)
        {
            carSpeedText.text = "0";
        }
    }

    private void InitializeEffects()
    {
        if (!useEffects)
        {
            if (RLWParticleSystem != null) RLWParticleSystem.Stop();
            if (RRWParticleSystem != null) RRWParticleSystem.Stop();
            if (RLWTireSkid != null) RLWTireSkid.emitting = false;
            if (RRWTireSkid != null) RRWTireSkid.emitting = false;
        }
    }

    private void InitializeTouchControls()
    {
        if (useTouchControls)
        {
            if (throttleButton != null && reverseButton != null &&
                turnRightButton != null && turnLeftButton != null && handbrakeButton != null)
            {
                throttlePTI = throttleButton.GetComponent<PrometeoTouchInput>();
                reversePTI = reverseButton.GetComponent<PrometeoTouchInput>();
                turnLeftPTI = turnLeftButton.GetComponent<PrometeoTouchInput>();
                turnRightPTI = turnRightButton.GetComponent<PrometeoTouchInput>();
                handbrakePTI = handbrakeButton.GetComponent<PrometeoTouchInput>();
                touchControlsSetup = true;
            }
            else
            {
                Debug.LogWarning("Touch controls not setup properly!");
            }
        }
    }

    private void InitializeVisuals()
    {
        if (nitroLeftParticles != null) nitroLeftParticles.Stop();
        if (nitroRightParticles != null) nitroRightParticles.Stop();

        if (shieldVisual != null) shieldVisual.SetActive(false);
        if (nitroOverlay != null) nitroOverlay.SetActive(false);
        if (frozenOverlay != null) frozenOverlay.SetActive(false);
    }
    #endregion

    #region INPUT HANDLING
    private void HandleKeyboardInput()
    {
        if (Input.GetKey(KeyCode.W))
        {
            CancelInvoke("DecelerateCar");
            deceleratingCar = false;
            GoForward();
        }
        if (Input.GetKey(KeyCode.S))
        {
            CancelInvoke("DecelerateCar");
            deceleratingCar = false;
            GoReverse();
        }
        if (Input.GetKey(KeyCode.A))
        {
            TurnLeft();
        }
        if (Input.GetKey(KeyCode.D))
        {
            TurnRight();
        }
        if (Input.GetKey(KeyCode.Space))
        {
            CancelInvoke("RecoverTraction");
            Handbrake();
        }

        if (!Input.GetKey(KeyCode.W) && !Input.GetKey(KeyCode.S))
        {
            ThrottleOff();
            if (!deceleratingCar)
            {
                InvokeRepeating("DecelerateCar", 0f, 0.1f);
                deceleratingCar = true;
            }
        }
        if (!Input.GetKey(KeyCode.A) && !Input.GetKey(KeyCode.D))
        {
            ResetSteeringAngle();
        }
        if (!Input.GetKey(KeyCode.Space))
        {
            RecoverTraction();
        }
    }

    private void HandleTouchInput()
    {
        if (throttlePTI.buttonPressed)
        {
            CancelInvoke("DecelerateCar");
            deceleratingCar = false;
            GoForward();
        }
        if (reversePTI.buttonPressed)
        {
            CancelInvoke("DecelerateCar");
            deceleratingCar = false;
            GoReverse();
        }
        if (turnLeftPTI.buttonPressed)
        {
            TurnLeft();
        }
        if (turnRightPTI.buttonPressed)
        {
            TurnRight();
        }
        if (handbrakePTI.buttonPressed)
        {
            CancelInvoke("RecoverTraction");
            Handbrake();
        }

        if (!throttlePTI.buttonPressed && !reversePTI.buttonPressed)
        {
            ThrottleOff();
            if (!deceleratingCar)
            {
                InvokeRepeating("DecelerateCar", 0f, 0.1f);
                deceleratingCar = true;
            }
        }
        if (!turnLeftPTI.buttonPressed && !turnRightPTI.buttonPressed)
        {
            ResetSteeringAngle();
        }
        if (!handbrakePTI.buttonPressed)
        {
            RecoverTraction();
        }
    }
    #endregion

    #region CAR CONTROLS (Physics)
    public void TurnLeft()
    {
        steeringAxis = steeringAxis - (Time.deltaTime * 10f * steeringSpeed);
        if (steeringAxis < -1f)
        {
            steeringAxis = -1f;
        }
        float steeringAngle = steeringAxis * maxSteeringAngle;
        frontLeftCollider.steerAngle = Mathf.Lerp(frontLeftCollider.steerAngle, steeringAngle, steeringSpeed);
        frontRightCollider.steerAngle = Mathf.Lerp(frontRightCollider.steerAngle, steeringAngle, steeringSpeed);
    }

    public void TurnRight()
    {
        steeringAxis = steeringAxis + (Time.deltaTime * 10f * steeringSpeed);
        if (steeringAxis > 1f)
        {
            steeringAxis = 1f;
        }
        float steeringAngle = steeringAxis * maxSteeringAngle;
        frontLeftCollider.steerAngle = Mathf.Lerp(frontLeftCollider.steerAngle, steeringAngle, steeringSpeed);
        frontRightCollider.steerAngle = Mathf.Lerp(frontRightCollider.steerAngle, steeringAngle, steeringSpeed);
    }

    public void ResetSteeringAngle()
    {
        if (steeringAxis < 0f)
        {
            steeringAxis = steeringAxis + (Time.deltaTime * 10f * steeringSpeed);
        }
        else if (steeringAxis > 0f)
        {
            steeringAxis = steeringAxis - (Time.deltaTime * 10f * steeringSpeed);
        }
        if (Mathf.Abs(frontLeftCollider.steerAngle) < 1f)
        {
            steeringAxis = 0f;
        }
        float steeringAngle = steeringAxis * maxSteeringAngle;
        frontLeftCollider.steerAngle = Mathf.Lerp(frontLeftCollider.steerAngle, steeringAngle, steeringSpeed);
        frontRightCollider.steerAngle = Mathf.Lerp(frontRightCollider.steerAngle, steeringAngle, steeringSpeed);
    }

    public void GoForward()
    {
        if (Mathf.Abs(localVelocityX) > 2.5f) isDrifting = true;
        else isDrifting = false;

        throttleAxis = throttleAxis + (Time.deltaTime * 3f);
        if (throttleAxis > 1f) throttleAxis = 1f;

        if (localVelocityZ < -1f)
        {
            Brakes();
        }
        else
        {
            if (Mathf.RoundToInt(carSpeed) < maxSpeed)
            {
                float speedFactor = (accelerationMultiplier * 50f) * throttleAxis * currentSpeedFactor;

                if (isNitroOn) speedFactor *= nitroMultiplier;

                frontLeftCollider.brakeTorque = 0;
                frontLeftCollider.motorTorque = speedFactor;
                frontRightCollider.brakeTorque = 0;
                frontRightCollider.motorTorque = speedFactor;
                rearLeftCollider.brakeTorque = 0;
                rearLeftCollider.motorTorque = speedFactor;
                rearRightCollider.brakeTorque = 0;
                rearRightCollider.motorTorque = speedFactor;
            }
            else
            {
                frontLeftCollider.motorTorque = 0;
                frontRightCollider.motorTorque = 0;
                rearLeftCollider.motorTorque = 0;
                rearRightCollider.motorTorque = 0;
            }
        }
    }

    public void GoReverse()
    {
        if (Mathf.Abs(localVelocityX) > 2.5f) isDrifting = true;
        else isDrifting = false;

        throttleAxis = throttleAxis - (Time.deltaTime * 3f);
        if (throttleAxis < -1f) throttleAxis = -1f;

        if (localVelocityZ > 1f)
        {
            Brakes();
        }
        else
        {
            if (Mathf.Abs(Mathf.RoundToInt(carSpeed)) < maxReverseSpeed)
            {
                float speedFactor = (accelerationMultiplier * 50f) * throttleAxis * currentSpeedFactor;

                frontLeftCollider.brakeTorque = 0;
                frontLeftCollider.motorTorque = speedFactor;
                frontRightCollider.brakeTorque = 0;
                frontRightCollider.motorTorque = speedFactor;
                rearLeftCollider.brakeTorque = 0;
                rearLeftCollider.motorTorque = speedFactor;
                rearRightCollider.brakeTorque = 0;
                rearRightCollider.motorTorque = speedFactor;
            }
            else
            {
                frontLeftCollider.motorTorque = 0;
                frontRightCollider.motorTorque = 0;
                rearLeftCollider.motorTorque = 0;
                rearRightCollider.motorTorque = 0;
            }
        }
    }

    public void ThrottleOff()
    {
        frontLeftCollider.motorTorque = 0;
        frontRightCollider.motorTorque = 0;
        rearLeftCollider.motorTorque = 0;
        rearRightCollider.motorTorque = 0;
    }

    public void DecelerateCar()
    {
        if (Mathf.Abs(localVelocityX) > 2.5f) isDrifting = true;
        else isDrifting = false;

        if (throttleAxis != 0f)
        {
            if (throttleAxis > 0f) throttleAxis = throttleAxis - (Time.deltaTime * 10f);
            else if (throttleAxis < 0f) throttleAxis = throttleAxis + (Time.deltaTime * 10f);
            if (Mathf.Abs(throttleAxis) < 0.15f) throttleAxis = 0f;
        }

        carRigidbody.linearVelocity = carRigidbody.linearVelocity * (1f / (1f + (0.025f * decelerationMultiplier)));
        frontLeftCollider.motorTorque = 0;
        frontRightCollider.motorTorque = 0;
        rearLeftCollider.motorTorque = 0;
        rearRightCollider.motorTorque = 0;

        if (carRigidbody.linearVelocity.magnitude < 0.25f)
        {
            carRigidbody.linearVelocity = Vector3.zero;
            CancelInvoke("DecelerateCar");
        }
    }

    public void Brakes()
    {
        frontLeftCollider.brakeTorque = brakeForce;
        frontRightCollider.brakeTorque = brakeForce;
        rearLeftCollider.brakeTorque = brakeForce;
        rearRightCollider.brakeTorque = brakeForce;
    }

    public void Handbrake()
    {
        CancelInvoke("RecoverTraction");
        driftingAxis = driftingAxis + (Time.deltaTime);
        float secureStartingPoint = driftingAxis * FLWextremumSlip * handbrakeDriftMultiplier;

        if (secureStartingPoint < FLWextremumSlip) driftingAxis = FLWextremumSlip / (FLWextremumSlip * handbrakeDriftMultiplier);
        if (driftingAxis > 1f) driftingAxis = 1f;

        if (Mathf.Abs(localVelocityX) > 2.5f) isDrifting = true;
        else isDrifting = false;

        if (driftingAxis < 1f)
        {
            FLwheelFriction.extremumSlip = FLWextremumSlip * handbrakeDriftMultiplier * driftingAxis;
            frontLeftCollider.sidewaysFriction = FLwheelFriction;

            FRwheelFriction.extremumSlip = FRWextremumSlip * handbrakeDriftMultiplier * driftingAxis;
            frontRightCollider.sidewaysFriction = FRwheelFriction;

            RLwheelFriction.extremumSlip = RLWextremumSlip * handbrakeDriftMultiplier * driftingAxis;
            rearLeftCollider.sidewaysFriction = RLwheelFriction;

            RRwheelFriction.extremumSlip = RRWextremumSlip * handbrakeDriftMultiplier * driftingAxis;
            rearRightCollider.sidewaysFriction = RRwheelFriction;
        }
        isTractionLocked = true;
        DriftCarPS();
    }

    public void RecoverTraction()
    {
        isTractionLocked = false;
        driftingAxis = driftingAxis - (Time.deltaTime / 1.5f);
        if (driftingAxis < 0f) driftingAxis = 0f;

        if (FLwheelFriction.extremumSlip > FLWextremumSlip)
        {
            FLwheelFriction.extremumSlip = FLWextremumSlip * handbrakeDriftMultiplier * driftingAxis;
            frontLeftCollider.sidewaysFriction = FLwheelFriction;
            FRwheelFriction.extremumSlip = FRWextremumSlip * handbrakeDriftMultiplier * driftingAxis;
            frontRightCollider.sidewaysFriction = FRwheelFriction;
            RLwheelFriction.extremumSlip = RLWextremumSlip * handbrakeDriftMultiplier * driftingAxis;
            rearLeftCollider.sidewaysFriction = RLwheelFriction;
            RRwheelFriction.extremumSlip = RRWextremumSlip * handbrakeDriftMultiplier * driftingAxis;
            rearRightCollider.sidewaysFriction = RRwheelFriction;
            Invoke("RecoverTraction", Time.deltaTime);
        }
        else if (FLwheelFriction.extremumSlip < FLWextremumSlip)
        {
            FLwheelFriction.extremumSlip = FLWextremumSlip;
            frontLeftCollider.sidewaysFriction = FLwheelFriction;
            FRwheelFriction.extremumSlip = FRWextremumSlip;
            frontRightCollider.sidewaysFriction = FRwheelFriction;
            RLwheelFriction.extremumSlip = RLWextremumSlip;
            rearLeftCollider.sidewaysFriction = RLwheelFriction;
            RRwheelFriction.extremumSlip = RRWextremumSlip;
            rearRightCollider.sidewaysFriction = RRwheelFriction;
            driftingAxis = 0f;
        }
    }

    public void AnimateWheelMeshes()
    {
        try
        {
            Quaternion FLWRotation; Vector3 FLWPosition;
            frontLeftCollider.GetWorldPose(out FLWPosition, out FLWRotation);
            frontLeftMesh.transform.position = FLWPosition;
            frontLeftMesh.transform.rotation = FLWRotation;

            Quaternion FRWRotation; Vector3 FRWPosition;
            frontRightCollider.GetWorldPose(out FRWPosition, out FRWRotation);
            frontRightMesh.transform.position = FRWPosition;
            frontRightMesh.transform.rotation = FRWRotation;

            Quaternion RLWRotation; Vector3 RLWPosition;
            rearLeftCollider.GetWorldPose(out RLWPosition, out RLWRotation);
            rearLeftMesh.transform.position = RLWPosition;
            rearLeftMesh.transform.rotation = RLWRotation;

            Quaternion RRWRotation; Vector3 RRWPosition;
            rearRightCollider.GetWorldPose(out RRWPosition, out RRWRotation);
            rearRightMesh.transform.position = RRWPosition;
            rearRightMesh.transform.rotation = RRWRotation;
        }
        catch (Exception ex) { Debug.LogWarning(ex); }
    }
    #endregion

    #region EFFECTS & AUDIO
    public void DriftCarPS()
    {
        if (useEffects)
        {
            try
            {
                if (isDrifting)
                {
                    if (RLWParticleSystem != null && !RLWParticleSystem.isPlaying) RLWParticleSystem.Play();
                    if (RRWParticleSystem != null && !RRWParticleSystem.isPlaying) RRWParticleSystem.Play();
                }
                else
                {
                    if (RLWParticleSystem.isPlaying) RLWParticleSystem.Stop();
                    if (RRWParticleSystem.isPlaying) RRWParticleSystem.Stop();
                }
            }
            catch (Exception ex) { Debug.LogWarning(ex); }

            try
            {
                if ((isTractionLocked || Mathf.Abs(localVelocityX) > 5f) && Mathf.Abs(carSpeed) > 12f)
                {
                    if (RLWTireSkid != null) RLWTireSkid.emitting = true;
                    if (RRWTireSkid != null) RRWTireSkid.emitting = true;
                }
                else
                {
                    if (RLWTireSkid != null) RLWTireSkid.emitting = false;
                    if (RRWTireSkid != null) RRWTireSkid.emitting = false;
                }
            }
            catch (Exception ex) { Debug.LogWarning(ex); }
        }
        else if (!useEffects)
        {
            if (RLWParticleSystem != null) RLWParticleSystem.Stop();
            if (RRWParticleSystem != null) RRWParticleSystem.Stop();
            if (RLWTireSkid != null) RLWTireSkid.emitting = false;
            if (RRWTireSkid != null) RRWTireSkid.emitting = false;
        }
    }

    private void CarEffects()
    {
        DriftCarPS();
        if (isNitroOn)
        {
            if (nitroLeftParticles != null && !nitroLeftParticles.isPlaying) nitroLeftParticles.Play();
            if (nitroRightParticles != null && !nitroRightParticles.isPlaying) nitroRightParticles.Play();
        }
        else
        {
            if (nitroLeftParticles != null && nitroLeftParticles.isPlaying) nitroLeftParticles.Stop();
            if (nitroRightParticles != null && nitroRightParticles.isPlaying) nitroRightParticles.Stop();
        }
    }

    public void CarSounds()
    {
        if (useSounds)
        {
            try
            {
                if (carEngineSound != null)
                {
                    float engineSoundPitch = initialCarEngineSoundPitch + (Mathf.Abs(carRigidbody.linearVelocity.magnitude) / 25f);
                    carEngineSound.pitch = engineSoundPitch;
                    if (isNitroOn) carEngineSound.pitch += 0.5f;
                }
                if ((isDrifting) || (isTractionLocked && Mathf.Abs(carSpeed) > 12f))
                {
                    if (!tireScreechSound.isPlaying) tireScreechSound.Play();
                }
                else if ((!isDrifting) && (!isTractionLocked || Mathf.Abs(carSpeed) < 12f))
                {
                    tireScreechSound.Stop();
                }
            }
            catch (Exception ex) { Debug.LogWarning(ex); }
        }
        else if (!useSounds)
        {
            if (carEngineSound != null && carEngineSound.isPlaying) carEngineSound.Stop();
            if (tireScreechSound != null && tireScreechSound.isPlaying) tireScreechSound.Stop();
        }
    }

    public void CarSpeedUI()
    {
        if (useUI)
        {
            try
            {
                float absoluteCarSpeed = Mathf.Abs(carSpeed);
                if (carSpeedText != null) carSpeedText.text = Mathf.RoundToInt(absoluteCarSpeed).ToString();
            }
            catch (Exception ex) { Debug.LogWarning(ex); }
        }
    }
    #endregion

    #region POWER-UP METHODS
    public void ActivateNitro(float duration)
    {
        StartCoroutine(NitroRoutine(duration));
    }

    private IEnumerator NitroRoutine(float duration)
    {
        isNitroOn = true;
        if (nitroOverlay != null) nitroOverlay.SetActive(true);
        Debug.Log("🚀 NITRO ACTIVATED!");

        yield return new WaitForSeconds(duration);

        isNitroOn = false;
        if (nitroOverlay != null) nitroOverlay.SetActive(false);
        Debug.Log("Nitro deactivated");
    }

    public void ActivateShield(float duration)
    {
        StartCoroutine(ShieldRoutine(duration));
    }

    private IEnumerator ShieldRoutine(float duration)
    {
        isShielded = true;
        if (shieldVisual != null) shieldVisual.SetActive(true);
        Debug.Log("🛡️ SHIELD ACTIVATED!");

        yield return new WaitForSeconds(duration);

        isShielded = false;
        if (shieldVisual != null) shieldVisual.SetActive(false);
        Debug.Log("Shield deactivated");
    }

    public void DropOil()
    {
        if (oilSlickPrefab != null && oilSpawnPoint != null)
        {
            RaycastHit hit;
            Vector3 spawnPos = oilSpawnPoint.position;

            if (Physics.Raycast(oilSpawnPoint.position, Vector3.down, out hit, 5.0f))
            {
                spawnPos = hit.point + Vector3.up * 0.05f;
            }

            GameObject oil = Instantiate(oilSlickPrefab, spawnPos, Quaternion.identity);
            oil.tag = "Player";

            Debug.Log("💧 OIL DROPPED!");
        }
        else
        {
            Debug.LogWarning("Cannot drop oil - Check Inspector assignments!");
        }
    }

    public void ShootFreezeRay()
    {
        if (freezeRayPrefab != null)
        {
            Vector3 spawnPos = transform.position + transform.forward * 3f + Vector3.up;
            if (firePoint != null) spawnPos = firePoint.position;

            Instantiate(freezeRayPrefab, spawnPos, transform.rotation);
            Debug.Log("❄️ Freeze Ray Shot!");
        }
    }

    public void ActivateGlobalFreeze(float duration)
    {
        if (GlobalFreeze.instance != null)
        {
            GlobalFreeze.instance.FreezeAllBots(duration);
            Debug.Log("❄️ GLOBAL FREEZE ACTIVATED!");
        }
        else
        {
            Debug.LogWarning("GlobalFreeze script not found in scene!");
        }
    }

    public void ApplyFreeze(float duration)
    {
        if (isShielded)
        {
            Debug.Log("🛡️ Shield blocked Freeze!");
            return;
        }
        StartCoroutine(FreezeRoutine(duration));
    }

    private IEnumerator FreezeRoutine(float duration)
    {
        isFrozen = true;
        if (frozenOverlay != null) frozenOverlay.SetActive(true);

        if (carRigidbody != null) carRigidbody.linearVelocity = Vector3.zero;

        Debug.Log("🥶 FROZEN!");
        yield return new WaitForSeconds(duration);

        isFrozen = false;
        if (frozenOverlay != null) frozenOverlay.SetActive(false);
        Debug.Log("Melted.");
    }

    public void ApplySlow(float slowFactor, float duration)
    {
        if (isShielded)
        {
            Debug.Log("🛡️ Shield blocked Oil!");
            return;
        }
        StartCoroutine(SlowRoutine(slowFactor, duration));
    }

    private IEnumerator SlowRoutine(float factor, float duration)
    {
        currentSpeedFactor = factor;

        if (carRigidbody != null) carRigidbody.linearVelocity *= factor;

        Debug.Log("🐌 SLOWED DOWN!");
        yield return new WaitForSeconds(duration);

        currentSpeedFactor = 1.0f;
        Debug.Log("Speed returned to normal");
    }
    #endregion
}
