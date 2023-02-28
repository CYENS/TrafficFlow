using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrossroadScene : MonoBehaviour
{
    public GameObject area;

    public GameObject[] wallDividers;
    public GameObject lanes;

    Transform m_Divider;

    int m_Timer;
    int m_TimeStamp;

    [HideInInspector]
    public int episode;
    int m_EpisodeStamp;

    [HideInInspector]
    public CrossroadLights lightControl;

    CrossroadSettings m_Settings;

    public bool movingIsland;
    GameObject m_Guide;
    GameObject m_Island;
    Vector3 m_MoveDir;
    bool m_Activated;

    void Awake()
    {
        m_Settings = FindObjectOfType<CrossroadSettings>();

        episode = 0;
        m_EpisodeStamp = episode;

        // if (m_Settings.isTraining)
        // {
        //     foreach (GameObject divider in wallDividers)
        //     {
        //         divider.transform.localScale =
        //             new Vector3(divider.transform.localScale.x, 4f,
        //             divider.transform.localScale.z);
        //     }
        // }

        Transform root = this.transform.root;
        string rootpath = "/" + root.name;

        if (m_Settings.isTraining && movingIsland)
        {
            m_Guide  = GameObject.Find(rootpath + "/Islands/StreamGuide");
            m_Island  = GameObject.Find(rootpath + "/Islands/CentralIsland");
            m_Guide.GetComponent<RoundaboutOrbit>().SetDescriptors();
            m_MoveDir = m_Guide.GetComponent<RoundaboutOrbit>().normal;
        }
        m_Activated = false;

    }

    void FixedUpdate()
    {
        // if (episode > 3e3 && m_EpisodeStamp < episode)
        // {
        //     foreach (GameObject divider in wallDividers)
        //     {
        //         if (divider.transform.localScale.y > 1f)
        //         {
        //             divider.transform.localScale -= new Vector3(0f, 0.01f, 0f);
        //         }
        //         else
        //         {
        //             divider.transform.localScale = new Vector3(divider.transform.localScale.x, 0.03f, divider.transform.localScale.z);
        //         }
        //     }
        // }
        m_EpisodeStamp = episode;

        if (episode % 300000 == 0 )
        {
            m_Activated = true;
        }
        if (episode%1000000 == 0 && m_Settings.isTraining && movingIsland && m_Activated)
        // if (episode%10 == 0 && m_Settings.isTraining && movingIsland)
        {
            m_Activated = false;
            if (Vector3.Magnitude(m_Island.transform.localPosition - m_Island.transform.localPosition.y * Vector3.up) > 1f)
            {
                m_Guide.transform.position += 2.5f * m_MoveDir;
                m_Island.transform.position += 2.5f * m_MoveDir;
            }
        }
    }
}
