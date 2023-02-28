using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoundaboutMoveIsland : MonoBehaviour
{
    CrossroadScene m_Scene;

    public bool deactivate;


    // Start is called before the first frame update
    void Start()
    {
        // deactivate = false;
        //
        // Transform root = this.transform.root;
        // string rootpath = "/" + root.name;
        //
        // var scene = GameObject.Find(rootpath + "/Scene");
        // m_Scene = scene.GetComponent<CrossroadScene>();
    }

    // Update is called once per frame
    void Update()
    {
        // if (m_Scene.episode % 1000 == 0)
        // {
        //     deactivate = true;
        // }
    }
}
