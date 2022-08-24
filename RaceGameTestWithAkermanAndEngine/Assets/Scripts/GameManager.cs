using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public Controller controller;
    public GameObject neeedle;
    private float startPosition;
    private float endPosition;
    private float desiredPosition;

    public float vehicleSpeed;


    private void Start()
    {
        controller = GameObject.FindGameObjectWithTag("Player").GetComponent<Controller>();
    }

    private void Update()
    {
        vehicleSpeed = controller.KPH;
        UpdateNeedle();
    }


    public void UpdateNeedle()
    {
        desiredPosition = startPosition - endPosition;
        float temp = vehicleSpeed / 180;
        neeedle.transform.eulerAngles = new Vector3(0, 0, (startPosition - temp * desiredPosition));
    }
}
