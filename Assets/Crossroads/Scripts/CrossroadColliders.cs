using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrossroadColliders : MonoBehaviour
{
    Collider m_Coll;

    [HideInInspector]
    public CrossroadLane lane;
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
}
