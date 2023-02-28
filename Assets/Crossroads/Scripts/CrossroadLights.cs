using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrossroadLights : MonoBehaviour
{
    public GameObject area;

    public GameObject[] signs;

    // [HideInInspector]
    public int selection;
    public int master;

    int m_TimeStep = -1;
    int m_Timer;
    int m_TimeStamp;

    public void ResetLights()
    {
        master = Random.Range(0, 1);
        selection = Random.Range(0, 1);

        m_TimeStep = 0;

        foreach (GameObject sign in signs)
        {
            foreach (Transform light_component in sign.transform)
            {
                if (!light_component.CompareTag("wait"))
                {
                    light_component.transform.position =
                        -1000f * Vector3.up
                        + area.transform.position;
                    if (light_component.CompareTag("sign_green"))
                    {
                        light_component.SetSiblingIndex(0);
                    }
                    if (light_component.CompareTag("sign_orange"))
                    {
                        light_component.SetSiblingIndex(1);
                    }
                    if (light_component.CompareTag("sign_red"))
                    {
                        light_component.SetSiblingIndex(2);
                    }
                    if (light_component.CompareTag("sign_red_orange"))
                    {
                        light_component.SetSiblingIndex(3);
                    }
                }
                else
                {
                    var waitCollider =
                        light_component.GetComponent<CrossroadColliders>();
                    waitCollider.GetCollider();
                }
            }
        }

        LightsSync();
        LightsPlacement();

    }

    void FixedUpdate()
    {
        if (m_TimeStep >= 0){
            m_TimeStep ++;
            if (m_TimeStep > m_TimeStamp + m_Timer)
            {
                selection = (selection + 1) % 4;
                LightsSync();
                LightsPlacement();
                if (selection == 2)
                {
                    master = (master + 1) % 2;
                }
            }
        }
    }

    void LightsSync()
    {
        m_TimeStamp = m_TimeStep;
        switch (selection)
        {
            case 0: //green
                m_Timer = Random.Range(300, 500);
                break;
            case 1: // orange
                m_Timer = 200;
                break;
            case 2: //red
                // m_Timer = Random.Range(300, 500);
                m_Timer = 40;
                break;
            case 3: //red-orange
                m_Timer = 100;
                break;
        }
    }

    void LightsPlacement()
    {
        int previous = (selection + 3) % 4;
        signs[master].transform.GetChild(previous).position =
            -1000f * Vector3.up + area.transform.position;
        signs[master].transform.GetChild(selection).position =
            area.transform.position;

        signs[(master + 1) % 2].transform.GetChild(2).position =
            area.transform.position;
    }
}
