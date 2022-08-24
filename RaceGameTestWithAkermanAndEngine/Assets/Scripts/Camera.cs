using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Camera : MonoBehaviour
{
    public GameObject Player;
    public GameObject cameraFollow;
    public float speed;
    private void Awake()
    {
        Player = GameObject.FindGameObjectWithTag("Player");
        cameraFollow = Player.transform.Find("CameraFollow").gameObject;
    }

    private void FixedUpdate()
    {
        Follow();
    }

    public void Follow()
    {
        gameObject.transform.position = Vector3.Lerp(transform.position, cameraFollow.transform.position, speed * Time.deltaTime);
        gameObject.transform.LookAt(Player.gameObject.transform.position);
    }
}
