using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Camera : MonoBehaviour
{
    private GameObject Player;
    private GameObject cameraFollow;
    private Controller controller;

    public float speed;
    private void Awake()
    {
        Player = GameObject.FindGameObjectWithTag("Player");
        cameraFollow = Player.transform.Find("CameraFollow").gameObject;
        controller = Player.GetComponent<Controller>();
    }

    private void FixedUpdate()
    {
        Follow();
    }

    public void Follow()
    {
        speed = Mathf.Lerp(speed, controller.KPH / 2, Time.deltaTime);

        gameObject.transform.position = Vector3.Lerp(transform.position, cameraFollow.transform.position, speed * Time.deltaTime);
        gameObject.transform.LookAt(Player.gameObject.transform.position);
    }
}
