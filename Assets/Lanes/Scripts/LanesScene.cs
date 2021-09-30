using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LanesScene : MonoBehaviour
{
    [HideInInspector]
    public float signOffset;

    public GameObject ground;
    public GameObject wallTop;
    public GameObject wallBottom;
    public GameObject wallDivider;
    public GameObject lightsGreen;
    public GameObject lightsOrange;
    public GameObject lightsRed;
    public GameObject lightsRedOrange;

    public bool isTraining = false;

    Vector3 m_Direction;

    Transform m_Top;
    Transform m_Bottom;
    Transform m_Divider;
    Vector3 m_LaneCentreline;

    [HideInInspector]
    public int m_Selection;

    int m_TimeStep;
    int m_Timer;
    int m_TimeStamp;

    [HideInInspector]
    public int episode;
    int m_EpisodeStamp;

    // [HideInInspector]
    public float realOffset;


    void Awake()
    {
        episode = 0;
        m_EpisodeStamp = episode;

        signOffset = 0f;//Random.Range(-5f, 5f);

        m_Top = wallTop.transform;
        m_Bottom = wallBottom.transform;
        m_Divider = wallDivider.transform;

        m_Direction = Vector3.Normalize(m_Top.position - m_Bottom.position);

        m_TimeStep = 0;
        m_Selection = Random.Range(0, 1);

        realOffset = 0.5f * Vector3.Dot(m_Top.position - m_Bottom.position, m_Direction);

        LightsPlacement();

        if (isTraining)
        {
            m_Divider.localScale =
                new Vector3(m_Divider.localScale.x, 4f, m_Divider.localScale.z);
            m_Top.position = m_Top.position.y * Vector3.up + 0.35f * realOffset * m_Direction + ground.transform.position;
            m_Bottom.position = m_Bottom.position.y * Vector3.up -0.35f * realOffset * m_Direction + ground.transform.position;
        }

    }

    void FixedUpdate()
    {
        m_TimeStep ++;
        if (m_TimeStep > m_TimeStamp + m_Timer)
        {
            m_Selection = (m_Selection + 1) % 4;
            LightsPlacement();
        }

        if (episode > 3e3 && m_EpisodeStamp < episode)
        {
            if (m_Divider.localScale.y > 1f)
            {
                m_Divider.localScale -= new Vector3(0f, 0.01f, 0f);
            }
            else
            {
                m_Divider.localScale = new Vector3(m_Divider.localScale.x, 0.03f, m_Divider.localScale.z);
            }
        }
        m_EpisodeStamp = episode;
    }

    void LightsPlacement()
    {
        m_TimeStamp = m_TimeStep;
        switch (m_Selection)
        {
            case 0: //green
                m_Timer = Random.Range(300, 500);
                lightsGreen.transform.position =
                    5f * Vector3.up + signOffset * m_Direction
                    + ground.transform.position;
                lightsRed.transform.position =
                    -1000f * Vector3.up + signOffset * m_Direction
                    + ground.transform.position;
                lightsOrange.transform.position =
                    -1000f * Vector3.up + signOffset * m_Direction
                    + ground.transform.position;
                lightsRedOrange.transform.position =
                    -1000f * Vector3.up + signOffset * m_Direction
                    + ground.transform.position;
                break;
            case 1: // orange
                m_Timer = 200;
                lightsGreen.transform.position =
                    -1000f * Vector3.up + signOffset * m_Direction
                    + ground.transform.position;
                lightsRed.transform.position =
                    -1000f * Vector3.up + signOffset * m_Direction
                    + ground.transform.position;
                lightsOrange.transform.position =
                    5f * Vector3.up + signOffset * m_Direction
                    + ground.transform.position;
                lightsRedOrange.transform.position =
                    -1000f * Vector3.up + signOffset * m_Direction
                    + ground.transform.position;
                break;
            case 2: //red
                m_Timer = Random.Range(300, 500);
                lightsGreen.transform.position =
                    -1000f * Vector3.up + signOffset * m_Direction
                    + ground.transform.position;
                lightsRed.transform.position =
                    5f * Vector3.up + signOffset * m_Direction
                    + ground.transform.position;
                lightsOrange.transform.position =
                    -1000f * Vector3.up + signOffset * m_Direction
                    + ground.transform.position;
                lightsRedOrange.transform.position =
                    -1000f * Vector3.up + signOffset * m_Direction
                    + ground.transform.position;
                break;
            case 3: //red-orange
                m_Timer = 100;
                lightsGreen.transform.position =
                    -1000f * Vector3.up + signOffset * m_Direction
                    + ground.transform.position;
                lightsRed.transform.position =
                    -1000f * Vector3.up + signOffset * m_Direction
                    + ground.transform.position;
                lightsOrange.transform.position =
                    -1000f * Vector3.up + signOffset * m_Direction
                    + ground.transform.position;
                lightsRedOrange.transform.position =
                    5f * Vector3.up + signOffset * m_Direction
                    + ground.transform.position;
                break;
        }
    }
}
