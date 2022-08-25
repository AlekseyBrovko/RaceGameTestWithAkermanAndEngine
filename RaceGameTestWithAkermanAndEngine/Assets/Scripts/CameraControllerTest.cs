using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraControllerTest : MonoBehaviour
{
    private GameObject Player;
    private GameObject cameraFollow;
    private GameObject cameraLookAt;
    private Controller controller;
    private Camera cam;

    public float speed;
    public float defaultFOV = 0;
    public float desiredFOV = 0;
    [Range(0, 5)] public float smoothTime = 0;

    private void Awake()
    {
        Player = GameObject.FindGameObjectWithTag("Player");
        cameraFollow = Player.transform.Find("CameraFollow").gameObject;
        //TODO сделать lookAt game object
        controller = Player.GetComponent<Controller>();
        cam = GetComponent<Camera>();
        defaultFOV = cam.fieldOfView;
    }

    private void FixedUpdate()
    {
        Follow();
        BoostFOV();
    }

    public void Follow()
    {
        speed = Mathf.Lerp(speed, controller.KPH / 2, Time.deltaTime);

        gameObject.transform.position = Vector3.Lerp(transform.position, cameraFollow.transform.position, speed * Time.deltaTime);
        gameObject.transform.LookAt(Player.gameObject.transform.position);
    }

    private void BoostFOV()
    {
        if (Input.GetKey(KeyCode.LeftShift))
        {
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, desiredFOV, Time.deltaTime * smoothTime);
        }
        else
        {
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, defaultFOV, Time.deltaTime * smoothTime);
        }
    }

    
}
