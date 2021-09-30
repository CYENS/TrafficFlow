using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrossroadScene : MonoBehaviour
{
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
    }
}
