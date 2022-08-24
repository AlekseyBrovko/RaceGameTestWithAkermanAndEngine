using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Controller : MonoBehaviour
{
    internal enum DriveType
    {
        FrontWheelDrive,
        RearWheelDrive,
        AllWheelDrive
    }

    [SerializeField] private DriveType driveType;
    public GameObject wheelMeshes, wheelColliders;
    private InputManager inputManager;
    private WheelCollider[] wheels = new WheelCollider[4];
    private GameObject[] wheelsMeshes = new GameObject[4];
    public float[] slip = new float[4];
    private Rigidbody rigidbody;
    private GameObject centerOfMass;

    public float KPH;
    public float breakPower = 1000;
    public float radius = 6;            //для формулы акермана, радиус разворота
    public float downForceValue = 50f;
    public float motorTorque = 200f;
    public float steeringMax = 4;
    public float tempo;                 //угол дрифта
    public float handBrakeFrictionMultiplier = 2f;
    public WheelFrictionCurve sideWaysFriction;
    public WheelFrictionCurve forwardFriction;
    public float handBrakeFriction;
    private float frictionMultiplier = 3f;
    public float currSpeed;

    #region Stats
    private float VehicleSpeed;
    private float LF_rpm;
    private float RF_rpm;
    private float LB_rpm;
    private float RB_rpm;
    #endregion

    private void Start()
    {
        GetObjectsOnStart();
    }


    private void FixedUpdate()
    {
        AddDownForce();
        AnimateWheels();
        MoveVehicle();
        SteerVehicle();
        GetFriction();

        Drifting();

        Stats();
    }

    public void AnimateWheels()
    {
        Vector3 wheelPosition = Vector3.zero;
        Quaternion wheelRotation = Quaternion.identity;

        for (int i = 0; i < 4; i++)
        {
            wheels[i].GetWorldPose(out wheelPosition, out wheelRotation);
            wheelsMeshes[i].transform.position = wheelPosition;
            wheelsMeshes[i].transform.rotation = wheelRotation;
        }
    }

    private void GetObjectsOnStart()
    {
        inputManager = GetComponent<InputManager>();
        rigidbody = GetComponent<Rigidbody>();
        centerOfMass = GameObject.Find("CenterOfMass");
        rigidbody.centerOfMass = centerOfMass.transform.localPosition;

        wheelMeshes = GameObject.Find("WheelMeshes");
        wheelsMeshes[0] = wheelMeshes.transform.GetChild(0).gameObject;
        wheelsMeshes[1] = wheelMeshes.transform.GetChild(1).gameObject;
        wheelsMeshes[2] = wheelMeshes.transform.GetChild(2).gameObject;
        wheelsMeshes[3] = wheelMeshes.transform.GetChild(3).gameObject;

        wheelColliders = GameObject.Find("WheelColliders");
        wheels[0] = wheelColliders.transform.GetChild(0).gameObject.GetComponent<WheelCollider>();
        wheels[1] = wheelColliders.transform.GetChild(1).gameObject.GetComponent<WheelCollider>();
        wheels[2] = wheelColliders.transform.GetChild(2).gameObject.GetComponent<WheelCollider>();
        wheels[3] = wheelColliders.transform.GetChild(3).gameObject.GetComponent<WheelCollider>();
    }

    private void MoveVehicle()
    {
        float totalDrive;

        if (driveType == DriveType.AllWheelDrive)
        {
            for (int i = 0; i < wheels.Length; i++)
            {
                wheels[i].motorTorque = inputManager.vertical * (motorTorque / 4);
            }
        }
        else if (driveType == DriveType.RearWheelDrive)
        {
            for (int i = 2; i < wheels.Length; i++)
            {
                wheels[i].motorTorque = inputManager.vertical * (motorTorque / 2) * Time.fixedTime;
            }
        }
        else if (driveType == DriveType.FrontWheelDrive)
        {
            for (int i = 0; i < wheels.Length - 2; i++)
            {
                wheels[i].motorTorque = inputManager.vertical * (motorTorque / 2);
            }
        }
        KPH = rigidbody.velocity.magnitude * 3.6f;

        if (inputManager.handBrake)
        {
            wheels[2].brakeTorque = wheels[3].brakeTorque = breakPower;
        }
        else
        {
            wheels[2].brakeTorque = wheels[3].brakeTorque = 0;
        }
    }

    private void SteerVehicle()
    {
        //годнота подъехала, формула акермана для рулевых колёс
        if (inputManager.horizontal > 0)
        {
            //rear track size is set to 1.5f        wheel base has been set to 2.55f
            wheels[0].steerAngle = Mathf.Rad2Deg * Mathf.Atan(2.55f / (radius + (1.5f / 2))) * inputManager.horizontal;
            wheels[1].steerAngle = Mathf.Rad2Deg * Mathf.Atan(2.55f / (radius - (1.5f / 2))) * inputManager.horizontal;
        }
        else if (inputManager.horizontal < 0)
        {
            wheels[0].steerAngle = Mathf.Rad2Deg * Mathf.Atan(2.55f / (radius - (1.5f / 2))) * inputManager.horizontal;
            wheels[1].steerAngle = Mathf.Rad2Deg * Mathf.Atan(2.55f / (radius + (1.5f / 2))) * inputManager.horizontal;
        }
        else
        {
            wheels[0].steerAngle = 0;
            wheels[1].steerAngle = 0;
        }
    }

    private void AddDownForce()
    {
        rigidbody.AddForce(-transform.up * downForceValue * rigidbody.velocity.magnitude);
    }

    private void Drifting()
    {
        currSpeed = KPH;
        if (inputManager.handBrake)
        {
            sideWaysFriction = wheels[0].sidewaysFriction;
            forwardFriction = wheels[0].forwardFriction;

            float velocity = 0;
            sideWaysFriction.extremumValue = sideWaysFriction.asymptoteValue = Mathf.SmoothDamp(sideWaysFriction.asymptoteValue, handBrakeFriction, ref velocity, 0.05f * Time.deltaTime);
            forwardFriction.extremumValue = forwardFriction.asymptoteValue = Mathf.SmoothDamp(forwardFriction.asymptoteValue, handBrakeFriction, ref velocity, 0.05f * Time.deltaTime);

            for (int i = 2; i < 4; i++)
            {
                wheels[i].sidewaysFriction = sideWaysFriction;
                wheels[i].forwardFriction = forwardFriction;
            }

            sideWaysFriction.extremumValue = sideWaysFriction.asymptoteValue = 1.5f;
            forwardFriction.extremumValue = forwardFriction.asymptoteValue = 1.5f;

            for (int i = 0; i < 2; i++)
            {
                wheels[i].sidewaysFriction = sideWaysFriction;
                wheels[i].forwardFriction = forwardFriction;
            }

            //CarManager.spining = (inputManager.handBrake) ? true : false;



            for (int i = 2; i < 4; i++)
            {
                WheelHit wheelHit;

                wheels[i].GetGroundHit(out wheelHit);

                if (wheelHit.sidewaysSlip < 0)
                {
                    tempo = (1 + -inputManager.horizontal) * Mathf.Abs(wheelHit.sidewaysSlip * handBrakeFrictionMultiplier);
                }
                if (wheelHit.sidewaysSlip < 0.5f)
                {
                    tempo = 0.5f;
                }

                if (wheelHit.sidewaysSlip > 0)
                {
                    tempo = (1 + inputManager.horizontal) * Mathf.Abs(wheelHit.sidewaysSlip * handBrakeFrictionMultiplier);
                }
                if (wheelHit.sidewaysSlip > 0.5f)
                {
                    tempo = 0.5f;
                }
                if (wheelHit.sidewaysSlip < 0.99f || wheelHit.sidewaysSlip > 0.99f)
                {
                    velocity = 0;
                    handBrakeFriction = Mathf.SmoothDamp(handBrakeFriction, tempo * 3, ref velocity, 0.1f * Time.deltaTime);
                }
                else
                {
                    handBrakeFriction = tempo;
                }
            }
        }
        if(!inputManager.handBrake)
        {
            forwardFriction = wheels[0].forwardFriction;
            sideWaysFriction = wheels[0].sidewaysFriction;
            
            forwardFriction.extremumValue = forwardFriction.asymptoteValue = ((currSpeed * frictionMultiplier) / 300) + 1;
            sideWaysFriction.extremumValue = sideWaysFriction.asymptoteValue = ((currSpeed * frictionMultiplier) / 300) + 1;
            
            for (int i = 0; i < 4; i++)
            {
                wheels[i].sidewaysFriction = sideWaysFriction;
                wheels[i].forwardFriction = forwardFriction;
            }
        }
    }

    private void GetFriction()
    {
        //TODO сделать и для продольного и поперечного скольжения

        for (int i = 0; i < wheelsMeshes.Length; i++)
        {
            WheelHit wheelHit;
            wheels[i].GetGroundHit(out wheelHit);
            slip[i] = wheelHit.forwardSlip;
        }
    }

    private void Stats()
    {
        VehicleSpeed = rigidbody.velocity.magnitude;
        LF_rpm = Mathf.CeilToInt(wheels[0].rpm);
        RF_rpm = Mathf.CeilToInt(wheels[1].rpm);
        LB_rpm = Mathf.CeilToInt(wheels[2].rpm);
        RB_rpm = Mathf.CeilToInt(wheels[3].rpm);
    }
}
