using StructureOfArraysGenerator;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SampleScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        var array = new Vector3MultiArray(10);
        array[0] = new Vector3 { x = 10, y = 20, z = 99 };

        Debug.Log(array.x[0]);
    }

    // Update is called once per frame
    void Update()
    {

    }
}


[MultiArray(typeof(Vector3))]
public readonly partial struct Vector3MultiArray
{

}