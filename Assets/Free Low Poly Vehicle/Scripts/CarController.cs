/*
MESSAGE FROM CREATOR: This script was coded by Mena. You can use it in your games either these are commercial or
personal projects. You can even add or remove functions as you wish. However, you cannot sell copies of this
script by itself, since it is originally distributed as a free product.
I wish you the best for your project. Good luck!

P.S: If you need more cars, you can check my other vehicle assets on the Unity Asset Store, perhaps you could find
something useful for your game. Best regards, Mena.
*/

using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Free_Low_Poly_Vehicle.Scripts
{
    public class CarController : MonoBehaviour
    {

        //CAR SETUP

        [Space(20)]
        //[Header("CAR SETUP")]
        [Space(10)]
        [Range(20, 190)]
        public int maxSpeed = 90; //The maximum speed that the car can reach in km/h.
        [Range(10, 120)]
        public int maxReverseSpeed = 45; //The maximum speed that the car can reach while going on reverse in km/h.
        [Range(1, 10)]
        public int accelerationMultiplier = 2; // How fast the car can accelerate. 1 is a slow acceleration and 10 is the fastest.
        [Space(10)]
        [Range(10, 45)]
        public int maxSteeringAngle = 27; // The maximum angle that the tires can reach while rotating the steering wheel.
        [Range(0.1f, 1f)]
        public float steeringSpeed = 0.5f; // How fast the steering wheel turns.
        [Space(10)]
        [Range(100, 600)]
        public int brakeForce = 350; // The strength of the wheel brakes.
        [Range(1, 10)]
        public int decelerationMultiplier = 2; // How fast the car decelerates when the user is not using the throttle.
        [Range(1, 10)]
        public int handbrakeDriftMultiplier = 5; // How much grip the car loses when the user hit the handbrake.
        [Space(10)]
        public Vector3 bodyMassCenter; // This is a vector that contains the center of mass of the car. I recommend to set this value
        // in the points x = 0 and z = 0 of your car. You can select the value that you want in the y axis,
        // however, you must notice that the higher this value is, the more unstable the car becomes.
        // Usually the y value goes from 0 to 1.5.

        //WHEELS

        //[Header("WHEELS")]

        /*
    The following variables are used to store the wheels' data of the car. We need both the mesh-only game objects and wheel
    collider components of the wheels. The wheel collider components and 3D meshes of the wheels cannot come from the same
    game object; they must be separate game objects.
    */
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

        //PARTICLE SYSTEMS

        [Space(20)]
        //[Header("EFFECTS")]
        [Space(10)]
        //The following variable lets you to set up particle systems in your car
        public bool useEffects = false;

        // The following particle systems are used as tire smoke when the car drifts.
        [FormerlySerializedAs("RLWParticleSystem")] public ParticleSystem rlwParticleSystem;
        [FormerlySerializedAs("RRWParticleSystem")] public ParticleSystem rrwParticleSystem;

        [FormerlySerializedAs("RLWTireSkid")] [Space(10)]
        // The following trail renderers are used as tire skids when the car loses traction.
        public TrailRenderer rlwTireSkid;
        [FormerlySerializedAs("RRWTireSkid")] public TrailRenderer rrwTireSkid;

        //SPEED TEXT (UI)

        [Space(20)]
        //[Header("UI")]
        [Space(10)]
        //The following variable lets you to set up a UI text to display the speed of your car.
        public bool useUI = false;
        public Text carSpeedText; // Used to store the UI object that is going to show the speed of the car.

        //SOUNDS

        [Space(20)]
        //[Header("Sounds")]
        [Space(10)]
        //The following variable lets you to set up sounds for your car such as the car engine or tire screech sounds.
        public bool useSounds = false;
        public AudioSource carEngineSound; // This variable stores the sound of the car engine.
        public AudioSource tireScreechSound; // This variable stores the sound of the tire screech (when the car is drifting).
        private float initialCarEngineSoundPitch; // Used to store the initial pitch of the car engine sound.

        //CONTROLS

        [Space(20)]
        //[Header("CONTROLS")]
        [Space(10)]
        //The following variables lets you to set up touch controls for mobile devices.
        public bool useTouchControls = false;
        public GameObject throttleButton;
        private PrometeoTouchInput throttlePti;
        public GameObject reverseButton;
        private PrometeoTouchInput reversePti;
        public GameObject turnRightButton;
        private PrometeoTouchInput turnRightPti;
        public GameObject turnLeftButton;
        private PrometeoTouchInput turnLeftPti;
        public GameObject handbrakeButton;
        private PrometeoTouchInput handbrakePti;

        //CAR DATA

        [HideInInspector]
        public float carSpeed; // Used to store the speed of the car.
        [HideInInspector]
        public bool isDrifting; // Used to know whether the car is drifting or not.
        [HideInInspector]
        public bool isTractionLocked; // Used to know whether the traction of the car is locked or not.

        //PRIVATE VARIABLES

        /*
    IMPORTANT: The following variables should not be modified manually since their values are automatically given via script.
    */
        private Rigidbody carRigidbody; // Stores the car's rigidbody.
        private float steeringAxis; // Used to know whether the steering wheel has reached the maximum value. It goes from -1 to 1.
        private float throttleAxis; // Used to know whether the throttle has reached the maximum value. It goes from -1 to 1.
        private float driftingAxis;
        private float localVelocityZ;
        private bool deceleratingCar;
/*
    The following variables are used to store information about sideways friction of the wheels (such as
    extremumSlip,extremumValue, asymptoteSlip, asymptoteValue and stiffness). We change this values to
    make the car to start drifting.
    */
        private WheelFrictionCurve fLwheelFriction;
        private float flWextremumSlip;
        private WheelFrictionCurve fRwheelFriction;
        private float frWextremumSlip;
        private WheelFrictionCurve rLwheelFriction;
        private float rlWextremumSlip;
        private WheelFrictionCurve rRwheelFriction;
        private float rrWextremumSlip;

        // Start is called before the first frame update
        private void Start()
        {
            //In this part, we set the 'carRigidbody' value with the Rigidbody attached to this
            //gameObject. Also, we define the center of mass of the car and we set the friction
            //values of the wheels.
            carRigidbody = gameObject.GetComponent<Rigidbody>();
            carRigidbody.centerOfMass = bodyMassCenter;
            SetValues();

            //If we are using sounds, we set the initial value for the car engine sound
            if (useSounds)
            {
                initialCarEngineSoundPitch = carEngineSound.pitch;
            }

            //If we are using touch controls, we check if they've been setup correctly
            if (!useTouchControls) return;
            if (throttleButton == null || reverseButton == null || turnRightButton == null || turnLeftButton == null || handbrakeButton == null)
            {
                Debug.LogError("The buttons for the touch controls are not assigned. For this reason, the touch controls will be disabled.");
                useTouchControls = false;
            }
            else
            {
                throttlePti = throttleButton.GetComponent<PrometeoTouchInput>();
                reversePti = reverseButton.GetComponent<PrometeoTouchInput>();
                turnRightPti = turnRightButton.GetComponent<PrometeoTouchInput>();
                turnLeftPti = turnLeftButton.GetComponent<PrometeoTouchInput>();
                handbrakePti = handbrakeButton.GetComponent<PrometeoTouchInput>();
            }

        }

        // Update is called once per frame
        private void Update()
        {
            //CAR DATA
            //We set the value of the 'carSpeed' variable according to the speed of the car rigidbody.
            //(We multiply the value by 3.6 to convert it from m/s to km/h)
            carSpeed = carRigidbody.linearVelocity.magnitude * 3.6f;

            //We set the value of the 'localVelocityZ' variable according to the value of the local 'z' velocity of the car rigidbody.
            localVelocityZ = transform.InverseTransformDirection(carRigidbody.linearVelocity).z;

            //We set the value of the 'localVelocityX' variable according to the value of the local 'x' velocity of the car rigidbody.

            //CAR CONTROLS
            //We call the method that manages the car controls.
            CarControl();

            //CAR SOUNDS
            //If we are using sounds, we call the method that manages the car sounds.
            if (useSounds)
            {
                CarSounds();
            }

            //CAR EFFECTS
            //If we are using effects, we call the methods that manage the particle systems and the trail renderers.
            if (useEffects)
            {
                CarEffects();
            }

            //CAR UI
            //If we are using UI, we call the method that manages the speed text.
            if (useUI)
            {
                CarUI();
            }

        }

        // This method converts the car speed to mph from km/h

        // This method manages the car controls based on the user inputs.
        void CarControl()
        {

            if (useTouchControls)
            {
                //If we are using touch controls, we control the car with the buttons defined in the script.
                if (throttlePti.buttonPressed)
                {
                    GoForward();
                }
                if (reversePti.buttonPressed)
                {
                    GoReverse();
                }
                if (turnRightPti.buttonPressed)
                {
                    TurnRight();
                }
                if (turnLeftPti.buttonPressed)
                {
                    TurnLeft();
                }
                if (handbrakePti.buttonPressed)
                {
                    Handbrake();
                }
                if (!handbrakePti.buttonPressed)
                {
                    RecoverTraction();
                }
                if (!throttlePti.buttonPressed && !reversePti.buttonPressed)
                {
                    ThrottleOff();
                }
                if (!reversePti.buttonPressed && !throttlePti.buttonPressed)
                {
                    ThrottleOff();
                }
                if (!turnRightPti.buttonPressed && !turnLeftPti.buttonPressed && steeringAxis != 0f)
                {
                    ResetSteeringAxis();
                }
            }
            else
            {
                //If we are not using touch controls, we control the car with the keyboard.
                //(The inputs are defined in the Input Manager)
                if (Input.GetAxis("Vertical") > 0)
                {
                    GoForward();
                }
                if (Input.GetAxis("Vertical") < 0)
                {
                    GoReverse();
                }
                if (Input.GetAxis("Horizontal") > 0)
                {
                    TurnRight();
                }
                if (Input.GetAxis("Horizontal") < 0)
                {
                    TurnLeft();
                }
                if (Input.GetKey(KeyCode.Space))
                {
                    Handbrake();
                }
                if (Input.GetKeyUp(KeyCode.Space))
                {
                    RecoverTraction();
                }
                if (Input.GetAxis("Vertical") == 0)
                {
                    ThrottleOff();
                }
                if (Input.GetAxis("Horizontal") == 0 && steeringAxis != 0f)
                {
                    ResetSteeringAxis();
                }
            }

            //We call the method that update the wheels' position and rotation.
            AnimateWheels();

        }

        // --- ЦЕ ВИПРАВЛЕНА ФУНКЦІЯ ---
        void FixedUpdate()
        {
            // --- ПОЛНОСТЬЮ ОБНОВЛЕННАЯ ЛОГИКА ---
            
            // 1. ПРИМЕНЯЕМ СИЛУ (УСКОРЕНИЕ)
            // Мы используем 'motorTorque' (крутящий момент) из Инспектора,
            // а не 'motorForce' из кода.
            // 'throttleAxis' - это нажатие 'W' (1) или 'S' (-1).
            // Мы применяем крутящий момент к КОЛЕСАМ, а не силу к кузову.
            // Это гораздо более реалистичный подход.
            
            // Применяем крутящий момент к задним колесам (если у вас задний привод)
            // или ко всем (если полный)
            float torque = throttleAxis * maxSpeed * accelerationMultiplier * 50f; // 'maxSpeed' и 'accelerationMultiplier' из Инспектора
            
            rearLeftCollider.motorTorque = torque;
            rearRightCollider.motorTorque = torque;
            
            // Если у вас полный привод, добавьте и передние:
            // frontLeftCollider.motorTorque = torque;
            // frontRightCollider.motorTorque = torque;

            // --- КОНЕЦ ГЛАВНОГО ИСПРАВЛЕНИЯ ---


            //If the car speed is higher than the max speed...
            if (carSpeed > maxSpeed)
            {
                carRigidbody.linearVelocity = (maxSpeed / 3.6f) * carRigidbody.linearVelocity.normalized;
            }

            //If the car speed is higher than the max reverse speed...
            if (carSpeed > maxReverseSpeed && localVelocityZ < 0)
            {
                carRigidbody.linearVelocity = (maxReverseSpeed / 3.6f) * carRigidbody.linearVelocity.normalized;
            }

            //We assign the 'steeringAxis' value to the 'steerAngle'...
            frontLeftCollider.steerAngle = steeringAxis * maxSteeringAngle;
            frontRightCollider.steerAngle = steeringAxis * maxSteeringAngle;

            //We check if the car is drifting...
            if (isDrifting)
            {
                frontLeftCollider.brakeTorque = brakeForce * handbrakeDriftMultiplier * driftingAxis;
                frontRightCollider.brakeTorque = brakeForce * handbrakeDriftMultiplier * driftingAxis;
                rearLeftCollider.brakeTorque = brakeForce * handbrakeDriftMultiplier * driftingAxis;
                rearRightCollider.brakeTorque = brakeForce * handbrakeDriftMultiplier * driftingAxis;
            }
            else
            {
                switch (throttleAxis)
                {
                    case < 0 when localVelocityZ > 0 && !isDrifting:
                    case > 0 when localVelocityZ < 0 && !isDrifting:
                        frontLeftCollider.brakeTorque = brakeForce;
                        frontRightCollider.brakeTorque = brakeForce;
                        rearLeftCollider.brakeTorque = brakeForce;
                        rearRightCollider.brakeTorque = brakeForce;
                        break;
                    default:
                        frontLeftCollider.brakeTorque = 0;
                        frontRightCollider.brakeTorque = 0;
                        rearLeftCollider.brakeTorque = 0;
                        rearRightCollider.brakeTorque = 0;
                        break;
                }
            }

            //If the car is decelerating...
            if (deceleratingCar)
            {
                // Убираем AddForce отсюда, так как торможение уже обрабатывается выше
                // carRigidbody.AddForce(transform.forward * (localVelocityZ * 10f * decelerationMultiplier));
            }
        }
        // --- КІНЕЦЬ ВИПРАВЛЕНОЇ ФУНКЦІЇ ---

        // This method manages the sounds of the car.
        private void CarSounds()
        {

            //We set the pitch of the 'carEngineSound' according to the car speed.
            carEngineSound.pitch = initialCarEngineSoundPitch + (carSpeed / maxSpeed) / 3;

            //If the car is drifting, we play the 'tireScreechSound'.
            if (isDrifting)
            {
                if (!tireScreechSound.isPlaying)
                {
                    tireScreechSound.Play();
                }
            }
            //If the car is not drifting, we stop playing the 'tireScreechSound'.
            else
            {
                if (tireScreechSound.isPlaying)
                {
                    tireScreechSound.Stop();
                }
            }

        }

        // This method manages the particle systems and trail renderers of the car.
        private void CarEffects()
        {

            //If the car is drifting, we play the particle systems and we enable the trail renderers.
            if (isDrifting)
            {
                if (!rlwParticleSystem.isPlaying)
                {
                    rlwParticleSystem.Play();
                }
                if (!rrwParticleSystem.isPlaying)
                {
                    rrwParticleSystem.Play();
                }
                rlwTireSkid.emitting = true;
                rrwTireSkid.emitting = true;
            }
            //If the car is not drifting, we stop playing the particle systems and we disable the trail renderers.
            else
            {
                if (rlwParticleSystem.isPlaying)
                {
                    rlwParticleSystem.Stop();
                }
                if (rrwParticleSystem.isPlaying)
                {
                    rrwParticleSystem.Stop();
                }
                rlwTireSkid.emitting = false;
                rrwTireSkid.emitting = false;
            }

        }

        // This method manages the speed text.
        private void CarUI()
        {
            //We set the 'carSpeedText' value according to the car speed.
            carSpeedText.text = ((int)carSpeed).ToString();
        }

        // This method sets the friction values of the wheels.
        private void SetValues()
        {
            fLwheelFriction = frontLeftCollider.sidewaysFriction;
            flWextremumSlip = fLwheelFriction.extremumSlip;
            fRwheelFriction = frontRightCollider.sidewaysFriction;
            frWextremumSlip = fRwheelFriction.extremumSlip;
            rLwheelFriction = rearLeftCollider.sidewaysFriction;
            rlWextremumSlip = rLwheelFriction.extremumSlip;
            rRwheelFriction = rearRightCollider.sidewaysFriction;
            rrWextremumSlip = rRwheelFriction.extremumSlip;
        }

        // This method is used to make the car to go forward.
        private void GoForward()
        {
            throttleAxis = 1;
            deceleratingCar = false;
        }

        // This method is used to make the car to go in reverse.
        private void GoReverse()
        {
            throttleAxis = -1;
            deceleratingCar = false;
        }

        // This method is used to make the car to turn right.
        private void TurnRight()
        {
            steeringAxis = steeringAxis + (Time.deltaTime * 10f * steeringSpeed);
            if (steeringAxis > 1f)
            {
                steeringAxis = 1f;
            }
        }

        // This method is used to make the car to turn left.
        private void TurnLeft()
        {
            steeringAxis = steeringAxis - (Time.deltaTime * 10f * steeringSpeed);
            if (steeringAxis < -1f)
            {
                steeringAxis = -1f;
            }
        }

        // This method is used to make the car to decelerate.
        private void ThrottleOff()
        {
            throttleAxis = 0;
            deceleratingCar = true;
        }

        // This method is used to reset the steering axis.
        private void ResetSteeringAxis()
        {
            if (steeringAxis > 0f)
            {
                steeringAxis = steeringAxis - (Time.deltaTime * 10f * steeringSpeed);
            }
            if (steeringAxis < 0f)
            {
                steeringAxis = steeringAxis + (Time.deltaTime * 10f * steeringSpeed);
            }
            if (Mathf.Abs(frontLeftCollider.steerAngle) < 1f)
            {
                steeringAxis = 0f;
            }
        }

        // This method is used to make the car to drift.
        private void Handbrake()
        {
            driftingAxis = driftingAxis + (Time.deltaTime * 1f);
            if (driftingAxis > 1f)
            {
                driftingAxis = 1f;
            }
            isDrifting = true;
            //We modify the friction values of the wheels to make the car to drift.
            fLwheelFriction.extremumSlip = flWextremumSlip * handbrakeDriftMultiplier;
            frontLeftCollider.sidewaysFriction = fLwheelFriction;
            fRwheelFriction.extremumSlip = frWextremumSlip * handbrakeDriftMultiplier;
            frontRightCollider.sidewaysFriction = fRwheelFriction;
            rLwheelFriction.extremumSlip = rlWextremumSlip * handbrakeDriftMultiplier;
            rearLeftCollider.sidewaysFriction = rLwheelFriction;
            rRwheelFriction.extremumSlip = rrWextremumSlip * handbrakeDriftMultiplier;
            rearRightCollider.sidewaysFriction = rRwheelFriction;
        }

        // This method is used to make the car to recover traction.
        private void RecoverTraction()
        {
            isDrifting = false;
            driftingAxis = 0f;
            //We restore the friction values of the wheels.
            fLwheelFriction.extremumSlip = flWextremumSlip;
            frontLeftCollider.sidewaysFriction = fLwheelFriction;
            fRwheelFriction.extremumSlip = frWextremumSlip;
            frontRightCollider.sidewaysFriction = fRwheelFriction;
            rLwheelFriction.extremumSlip = rlWextremumSlip;
            rearLeftCollider.sidewaysFriction = rLwheelFriction;
            rRwheelFriction.extremumSlip = rrWextremumSlip;
            rearRightCollider.sidewaysFriction = rRwheelFriction;
        }

        //This method is used to update the wheels' position and rotation.
        private void AnimateWheels()
        {
            frontLeftCollider.GetWorldPose(out var wheelPosition, out var wheelRotation);
            frontLeftMesh.transform.position = wheelPosition;
            frontLeftMesh.transform.rotation = wheelRotation;

            frontRightCollider.GetWorldPose(out wheelPosition, out wheelRotation);
            frontRightMesh.transform.position = wheelPosition;
            frontRightMesh.transform.rotation = wheelRotation;

            rearLeftCollider.GetWorldPose(out wheelPosition, out wheelRotation);
            rearLeftMesh.transform.position = wheelPosition;
            rearLeftMesh.transform.rotation = wheelRotation;

            rearRightCollider.GetWorldPose(out wheelPosition, out wheelRotation);
            rearRightMesh.transform.position = wheelPosition;
            rearRightMesh.transform.rotation = wheelRotation;
        }

    }
}