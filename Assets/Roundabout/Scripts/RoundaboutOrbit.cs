using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoundaboutOrbit : MonoBehaviour
{
    public Vector3 stream;
    public Vector3 normal;

    public void SetDescriptors()
    {
        var boundsmaxZ = Vector3.Dot(this.GetComponent<Renderer>().bounds.max, Vector3.forward)*Vector3.forward;
        var boundsminZ = Vector3.Dot(this.GetComponent<Renderer>().bounds.min, Vector3.forward)*Vector3.forward;

        var boundsmaxX = Vector3.Dot(this.GetComponent<Renderer>().bounds.max, Vector3.right)*Vector3.right;
        var boundsminX = Vector3.Dot(this.GetComponent<Renderer>().bounds.min, Vector3.right)*Vector3.right;

        normal = Vector3.Normalize(boundsmaxZ - boundsminZ + boundsmaxX - boundsminX);

        stream = Vector3.Cross(normal, Vector3.up);
    }

    void Start()
    {
        SetDescriptors();
    }
}
