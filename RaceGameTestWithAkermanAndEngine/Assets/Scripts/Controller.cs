using System.Collections;
using System.Collections.Generic;
using System;
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

    internal enum GearBoxType
    {
        Manual,
        Automatic
    }
    [SerializeField] private GearBoxType gearBoxType;

    public GameObject wheelMeshes, wheelColliders;
    public AnimationCurve enginePower;                          //кривая мощности двигателя
    public float[] Gears;                                       //массив шестеренок с их передаточными числами
    public int gearNum = 0;                                     //текущая передача
    public float smoothTime = 0.01f;
    private InputManager inputManager;
    private WheelCollider[] wheels = new WheelCollider[4];      //массив колайдеров
    private GameObject[] wheelsMeshes = new GameObject[4];      //массив мешей колёс
    public float[] forwardSlip = new float[4];                  //статистика скольжения продольного
    public float[] sideSlip = new float[4];                     //статистика скольжения поперечного
    private Rigidbody rigidbody;
    private GameObject centerOfMass;                

    public float KPH;                                           //статистика километров в час
    public float breakPower = 1000;                             //сила торможения
    public float radius = 6;                                    //для формулы акермана, радиус разворота
    public float downForceValue = 50f;                          //прижимная сила
    //public float motorTorque = 200f;
    //public float steeringMax = 4;
    public float tempo;                                         //угол дрифта
    public float handBrakeFrictionMultiplier = 2f;              //для дрифта
    public WheelFrictionCurve sideWaysFriction;                 //для дрифта
    public WheelFrictionCurve forwardFriction;                  //для дрифта
    public float handBrakeFriction;                             //для дрифта
    private float frictionMultiplier = 3f;                      //для дрифта
    private float currSpeed;                                    //текущая скорость                
    public float totalPower;                                    //переменная для рассчета мощности            
    public float engineRPM;                                     //тахометр, обороты двигателя текущие
    public float wheelsRpm;                                     
    public float maxEngineRpm = 5000f;   //для рассчета мощности
    public float maxGearBoxRpm = 5000f;     //нужно для коробки передач
    public float minGearBoxRpm = 2500f;     //нужно для коробки передач
    public bool reverce;

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

    private void Update()
    {
        GearBox();
    }

    private void FixedUpdate()
    {
        AddDownForce();
        AnimateWheels();
        MoveVehicle();
        SteerVehicle();
        GetFriction();
        CalculateEnginePower();

        AirResistanceForce();

        Break();
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
            {                                                   //totalPower/4
                wheels[i].motorTorque = inputManager.vertical * (totalPower / 4);
            }
        }
        else if (driveType == DriveType.RearWheelDrive)
        {
            for (int i = 2; i < wheels.Length; i++)
            {
                wheels[i].motorTorque = inputManager.vertical * (totalPower / 2) * Time.fixedTime;
            }
        }
        else if (driveType == DriveType.FrontWheelDrive)
        {
            for (int i = 0; i < wheels.Length - 2; i++)
            {
                wheels[i].motorTorque = inputManager.vertical * (totalPower / 2);
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

        //TODO ТУТ БУЛЕВА ЧТО ЕСЛИ ВПЕРЕД, ТО тормозим 
        /*if (inputManager.vertical < 0 && !reverce)
        {
            foreach (var wheel in wheels)
            {
                wheel.brakeTorque = breakPower;
            }
        }
        else
        {
            foreach (var wheel in wheels)
            {
                wheel.brakeTorque = 0;
            }
        }*/
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
        if (!inputManager.handBrake)
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
        //из этих значений можно брать скольжение фронтальное и боковое
        //нужно, если есть фронтальное скольжение как то его решать
        for (int i = 0; i < wheelsMeshes.Length; i++)
        {
            WheelHit wheelHit;
            wheels[i].GetGroundHit(out wheelHit);
            forwardSlip[i] = (float)Math.Round(wheelHit.forwardSlip, 2);
        }
        for (int i = 0; i < wheelsMeshes.Length; i++)
        {
            WheelHit wheelHit;
            wheels[i].GetGroundHit(out wheelHit);
            sideSlip[i] = (float)Math.Round(wheelHit.sidewaysSlip, 2);
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

    private void CalculateEnginePower()
    {
        WheelRPM();
        //totalPower = enginePower.Evaluate(engineRPM) * (Gears[gearNum]) * inputManager.vertical;
        if (inputManager.vertical > 0)
        {
            totalPower = enginePower.Evaluate(engineRPM) * 3.6f * inputManager.vertical;
        }


        float velocity = 0.0f;

        if (inputManager.vertical != 0)
        {
            rigidbody.drag = 0.005f;
        }
        if (inputManager.vertical == 0)
        {
            rigidbody.drag = 0.1f;
        }


        if (engineRPM >= maxEngineRpm)
        {
            engineRPM = Mathf.SmoothDamp(engineRPM, maxEngineRpm - 500, ref velocity, 0.05f);
        }
        else
        {
            engineRPM = Mathf.SmoothDamp(engineRPM, 1000 + (Mathf.Abs(wheelsRpm) * 3.6f * (Gears[gearNum])), ref velocity, smoothTime);      //3,6 это число редуктора моста
        }
    }

    private void WheelRPM()
    {
        float sum = 0;
        int R = 0;
        for (int i = 0; i < 4; i++)
        {
            sum += wheels[i].rpm;
            R++;
        }
        wheelsRpm = (R != 0) ? sum / R : 0;

        //????
        //if (wheelsRpm < 0 && !reverce) reverce = true;
        //if (wheelsRpm > 0 && reverce) reverce = false;

    }

    private void GearBox()
    {
        if (!IsGrounded()) return;
        if (gearBoxType == GearBoxType.Automatic)
        {
            if (engineRPM > maxGearBoxRpm && gearNum < Gears.Length - 1)
            {
                gearNum++;
                //вообще спорно костыльная фигня щас будет
                SwitchingTheGearBox();
            }
            if (engineRPM < minGearBoxRpm && gearNum > 0)
            {
                gearNum--;
                SwitchingTheGearBox();
            }
        }
        else if (gearBoxType == GearBoxType.Manual)
        {
            if (Input.GetKeyDown(KeyCode.E) && gearNum < Gears.Length - 1)
            {
                gearNum++;
                SwitchingTheGearBox();
            }
            if (Input.GetKeyDown(KeyCode.Q) && gearNum > 0)
            {
                gearNum--;
                SwitchingTheGearBox();
            }
        }
    }

    private void AirResistanceForce()
    {
        var v = rigidbody.velocity.magnitude;
        var p = 1.225f;
        var cd = 0.47f;
        var s = 2f;     //Pi*R*R

        var direction = -rigidbody.velocity.normalized;
        var forceAmount = (p * v * v * s * cd) / 2f;
        rigidbody.AddForce(direction * forceAmount * Time.deltaTime);
    }

    private void MotorBrake()
    {
        if (inputManager.vertical == 0 && (KPH <= 10 || KPH >= -10))
        {
            breakPower = 10f;
        }
        else
        {
            breakPower = 0f;
        }
    }

    private bool IsGrounded()
    {
        if (wheels[0].isGrounded && wheels[1].isGrounded && wheels[2].isGrounded && wheels[3].isGrounded)
            return true;
        else
            return false;
    }

    public void Break()
    {
        if (inputManager.vertical < 0 && !reverce)
        {
            breakPower = (KPH >= 10) ? 1000 : 100;
        }
        else
        {
            breakPower = 0;
        }
    }

    private void SwitchingTheGearBox()
    {
        StartCoroutine(SwitchingTheGearBoxCor());
    }

    IEnumerator SwitchingTheGearBoxCor()
    {
        float switchingBreak = 5000f;
        float switchngGearBoxTimer = 0.5f;
        wheels[2].motorTorque = 0;
        wheels[3].motorTorque = 0;
        wheels[2].brakeTorque = switchingBreak;
        wheels[3].brakeTorque = switchingBreak;
        yield return new WaitForSeconds(switchngGearBoxTimer);
        wheels[2].brakeTorque = 0;
        wheels[3].brakeTorque = 0;
    }
}
