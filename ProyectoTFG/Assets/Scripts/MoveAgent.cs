using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveAgent : MonoBehaviour
{
    Rigidbody _rb = null;
    public float speed = 400;

    void Start()
    {
        _rb = GetComponent<Rigidbody>(); //Almacenamos el rigidbody del jugador 
    }

    void FixedUpdate() //Damos el movimiento
    {
        float moveHorizontal = Input.GetAxis("Horizontal");
        float moveVertical = Input.GetAxis("Vertical");

        Vector3 movement = new Vector3(moveHorizontal, 0f, moveVertical);
        _rb.AddForce(movement * speed * Time.deltaTime);

    }
}
