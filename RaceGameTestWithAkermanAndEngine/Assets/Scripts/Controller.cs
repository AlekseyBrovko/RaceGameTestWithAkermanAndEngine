using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Controller : MonoBehaviour
{
    public WheelCollider[] wheels;
    public float torque = 200f;
    private void FixedUpdate()
    {
        if (Input.GetKey(KeyCode.W))
        {
            for (int i = 0; i < wheels.Length; i++)
            {
                wheels[i].motorTorque = torque;
            }
        }
        else
        {
            for (int i = 0; i < wheels.Length; i++)
            {
                wheels[i].motorTorque = 0;
            }
        }
    }
}
