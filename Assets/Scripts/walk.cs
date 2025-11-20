using System.Collections;
using System.Collections.Generic;
using UnityEngine;  // Poprawiona nazwa przestrzeni nazw (nie UintyEngine)

public class Player : MonoBehaviour
{
    void Start()  // "void" z małej litery
    {

    }

    void Update()  // "void" z małej litery
    {
        transform.Translate(Vector3.forward * Time.deltaTime * Input.GetAxis("Vertical"));  // Poprawione "forward" (nie "foward")
    }
}