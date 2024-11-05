using System.IO.Enumeration;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarController : MonoBehaviour
{
    public enum CarType
    {
        FrontWheelDrive,
        RearWheelDrive,
        FourWheelDrive
    }
    public CarType carType = CarType.FrontWheelDrive;

    public enum ControlMode
    {
        Keyboard, 
        Button
    };

    public ControlMode control;

    [Header("Wheel GameObject Meshs")]
    public GameObject FrontWheelLeft;
    public GameObject FrontWheelRight;  
    public GameObject BackWheelLeft;
    public GameObject BackWheelRight;

    [Header("WheelCollider")]
    public WheelCollider FrontWheelLeftCollider;
    public WheelCollider FrontWheelRightCollider;  
    public WheelCollider BackWheelLeftCollider;
    public WheelCollider BackWheelRightCollider;

    [Header("Movement, steering and braking")]
    private float currentSpeed;
    public float maximumMotorTorque;
    public float maximumSteeringAngle = 20f;
    public float maximumSpeed;
    public float brakePower;
    public Transform COM;
    float carSpeed;
    float carSpeedConverted;
    float motorTorque;
    float tireAngle;
    float vertical = 0f;
    float horizontal = 0f;
    bool handBrake = false;
    Rigidbody carRigidbody;

    [Header("Sounds & Effects")]
    public ParticleSystem[] smokeEffects;
    private bool smokeEffectEnabled = true;
    public TrailRenderer[] trailRenderers;

    public AudioSource engineSound;
    public AudioClip engineClip;

    void Start()
    {
        carRigidbody = GetComponent<Rigidbody>();

        if(carRigidbody != null)
        {
            carRigidbody.centerOfMass = COM.localPosition;
        }

        engineSound.loop = true;
        engineSound.playOnAwake = false;
        engineSound.volume = 0.5f;
        engineSound.pitch = 1f;

        engineSound.Play();
        engineSound.Pause();
    }

    void Update()
    {
        GetInputs();
        CalculateCarMovement();
        CalculateCarSteering();

        ApplyTransformToWheels();
    }

    void GetInputs()
    {
        if(control == ControlMode.Keyboard)
        {
            horizontal = Input.GetAxis("Horizontal");
            vertical = Input.GetAxisRaw("Vertical");
        }
    }

    void CalculateCarMovement()
    {
        carSpeed = carRigidbody.velocity.magnitude;
        carSpeedConverted = MathF.Round(carSpeed * 3.6f);

        // Apply braking
        if(Input.GetKey(KeyCode.Space))
           handBrake = true;
        else
            handBrake = false;

        if(handBrake)
        {
            motorTorque = 0f;
            ApplyBrake();
            EnableTrailEffect(true);
            if(!smokeEffectEnabled)
            {
                EnableSmokeEffect(true);
                smokeEffectEnabled = true;
            }
        }
        else
        {
            ReleaseBrake();

            if(carSpeedConverted < maximumSpeed)
                motorTorque = maximumMotorTorque * -vertical;
            else
                motorTorque = 0f;

            EnableTrailEffect(false);
            if(smokeEffectEnabled)
            {
                EnableSmokeEffect(false);
                smokeEffectEnabled = false;
            }

            if(carSpeedConverted > 0 || handBrake)
            {
                engineSound.UnPause();

                float gearRatio = currentSpeed / maximumSpeed;
                int numberOfGears = 6;
                int currentGear = Mathf.Clamp(Mathf.FloorToInt(gearRatio * (numberOfGears)) + 1, 1, numberOfGears);

                float pitchMutiplier = 0.5f + 0.5f * (currentSpeed / maximumSpeed);
                float volumeMultiplier = 0.2f + 0.8f * (currentSpeed / maximumSpeed);

                engineSound.pitch = Mathf.Lerp(0.5f, 1.0f, pitchMutiplier) * currentGear;
                engineSound.volume = volumeMultiplier;
            }
            else
            {
                engineSound.UnPause();
                engineSound.pitch = 0.5f;
                engineSound.volume = 0.2f;
            }
        }

        ApplyMotorTorque();
    }

    void CalculateCarSteering()
    {
        tireAngle = maximumSteeringAngle * horizontal;
        FrontWheelLeftCollider.steerAngle = tireAngle;
        FrontWheelRightCollider.steerAngle = tireAngle;
    }

    void ApplyMotorTorque()
    {
        if(carType == CarType.FrontWheelDrive)
        {
            FrontWheelLeftCollider.motorTorque = motorTorque;
            FrontWheelRightCollider.motorTorque = motorTorque;           
        }

        else if(carType == CarType.RearWheelDrive)
        {
            BackWheelLeftCollider.motorTorque = motorTorque;
            BackWheelRightCollider.motorTorque = motorTorque;           
        }

        else if(carType == CarType.FourWheelDrive)
        {
            FrontWheelLeftCollider.motorTorque = motorTorque;
            FrontWheelRightCollider.motorTorque = motorTorque;
            BackWheelLeftCollider.motorTorque = motorTorque;
            BackWheelRightCollider.motorTorque = motorTorque;           
        }
    }


    void ApplyBrake()
    {
        FrontWheelLeftCollider.brakeTorque = brakePower;
        FrontWheelRightCollider.brakeTorque = brakePower;
        BackWheelLeftCollider.brakeTorque = brakePower;
        BackWheelRightCollider.brakeTorque = brakePower;
    }

     void ReleaseBrake()
    {
        FrontWheelLeftCollider.brakeTorque = 0;
        FrontWheelRightCollider.brakeTorque = 0;
        BackWheelLeftCollider.brakeTorque = 0;
        BackWheelRightCollider.brakeTorque = 0;
    }

    public void ApplyTransformToWheels()
    {
        Vector3 position;
        Quaternion rotation;

        FrontWheelLeftCollider.GetWorldPose(out position, out rotation);
        FrontWheelLeft.transform.position = position;
        FrontWheelLeft.transform.rotation = rotation;

        FrontWheelRightCollider.GetWorldPose(out position, out rotation);
        FrontWheelRight.transform.position = position;
        FrontWheelRight.transform.rotation = rotation;

        BackWheelLeftCollider.GetWorldPose(out position, out rotation);
        BackWheelLeft.transform.position = position;
        BackWheelLeft.transform.rotation = rotation;

        BackWheelRightCollider.GetWorldPose(out position, out rotation);
        BackWheelRight.transform.position = position;
        BackWheelRight.transform.rotation = rotation;
    }

    private void EnableSmokeEffect(bool enable)
    {
        foreach(ParticleSystem smokeEffect in smokeEffects)
        {
            if(enable)
            {
                smokeEffect.Play();
            }
            else
            {
                smokeEffect.Stop();
            }
        }
    }

    private void EnableTrailEffect(bool enable)
    {
        foreach(TrailRenderer trailRenderer in trailRenderers)
        {
            trailRenderer.emitting = enable;
        }
    }




}