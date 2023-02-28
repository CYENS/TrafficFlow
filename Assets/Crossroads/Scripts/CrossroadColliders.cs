using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrossroadColliders : MonoBehaviour
{
    // [HideInInspector]
    public CrossroadLane lane;
    public Vector3 stream;
    public Vector3 current = Vector3.zero;
    public bool occupied = false;

    public void GetCollider()
    {
        RaycastHit hit;
        Vector3 ray = transform.TransformDirection(Vector3.up);

        int layerMask = 1 << 9;
        if (Physics.Raycast(transform.position, ray, out hit, 1000, layerMask))
        {
            // print(this.name + ": hit object " + hit.collider.gameObject.name);
            var col = hit.collider.gameObject;
            lane = col.transform.parent.parent.GetComponent<CrossroadLane>();
            occupied = true;
            // print(this.name + ": hit object " + col.name + " of lane " + col.transform.parent.parent.name  );
        }
        else
        {
            occupied = false;
        }
    }
    public void GetLaneStream(Vector3 init, Vector3 final)
    {
        RaycastHit hit;
        Vector3 ray = transform.TransformDirection(Vector3.up);

        if (Physics.Raycast(transform.position, ray, out hit, 1000))
        {
            // print(this.name + ": hit object " + hit.collider.gameObject.name);
            var col = hit.collider.gameObject;
            // print(this.name + ": hit object " + col.name);

            if (col.gameObject.CompareTag("crossing"))
            {
                // if (current.magnitude != 0f)
                // {
                //   stream = current + final;
                // }
                // else
                // {
                //   stream = init + final;
                // }
                // stream.Normalize();
                stream = Vector3.zero;
                // print(this.name + ": hit object " + col.name);
            }
            else
            {
              lane = col.transform.parent.parent.GetComponent<CrossroadLane>();
              occupied = true;
              if (col.gameObject.CompareTag("left_lane"))
              {
                  // print(this.name + ": hit object " + col.name + " of lane " + col.transform.parent.parent.name  );

                  stream = lane.direction;
              }
              else if (col.gameObject.CompareTag("right_lane"))
              {
                  // print(this.name + ": hit object " + col.name + " of lane " + col.transform.parent.parent.name  );
                  stream = -lane.direction;
              }
              current = stream;
            }

        }
        else
        {
            occupied = false;
        }
    }
}
