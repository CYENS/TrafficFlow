using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrossroadLane : MonoBehaviour
{

    CrossroadSettings m_Settings;
    CrossroadScene m_Scene;

    public GameObject ground;
    public GameObject wallBottom;
    public GameObject wallBottomEnd;
    public GameObject leftShoulder;
    public GameObject rightShoulder;
    public GameObject exit;
    public GameObject enter;

    // Transform m_Bottom;
    // Transform m_BottomEnd;
    Transform m_Left;
    Transform m_Right;


    // [HideInInspector]
    public Vector3 direction;
    // [HideInInspector]
    public Vector3 normal;
    // [HideInInspector]
    public float offset;
    // [HideInInspector]
    public float length;
    // [HideInInspector]
    public Vector3 centreline;
    // [HideInInspector]
    public Vector3 targetline;
    [HideInInspector]
    public Transform bottom;
    [HideInInspector]
    public Transform bottomEnd;

    [HideInInspector]
    public CrossroadLights lightControl;

    public void SetDescriptors()
    {
        Transform root = this.transform.root;
        string rootpath = "/" + root.name;

        m_Settings = FindObjectOfType<CrossroadSettings>();

        var scene = GameObject.Find(rootpath + "/Scene");
        m_Scene = scene.GetComponent<CrossroadScene>();

        bottom = wallBottom.GetComponent<Transform>();
        bottomEnd = wallBottomEnd.GetComponent<Transform>();

        m_Left = leftShoulder.GetComponent<Transform>();
        m_Right = rightShoulder.GetComponent<Transform>();

        normal = Vector3.Normalize(m_Right.position - m_Left.position);
        normal.y = 0;
        direction = Vector3.Cross(normal, Vector3.up);

        var boundsmax = m_Left.GetComponent<Renderer>().bounds.max;
        var boundsmin = m_Left.GetComponent<Renderer>().bounds.min;
        length = Mathf.Abs(Vector3.Dot((boundsmax - boundsmin), direction));
        // Debug.Log("I am " + this.name + " " + length);

        var centre = 0.25f * (3f * leftShoulder.transform.position + rightShoulder.transform.position) - ground.transform.position;
        centreline = Vector3.Dot(centre, normal) * normal;
        targetline = - Vector3.Dot(centre, normal) * normal;

    }

    public void SetTrainingBounds()
    {
        if (m_Settings.isTraining)
        {
            offset = 0.35f * length;
            bottom.position = (bottom.position.y
            - ground.transform.position.y) * Vector3.up - offset * direction
            - 10f * direction
            + ground.transform.position;
            // Debug.Log("I am " + this.name + " " + offset);
        }
        else
        {
            offset = length;
        }

    }
}
