using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Policies;
using System.IO;


public class Profiling : MonoBehaviour
{
    CrossroadSettings m_Settings;
    CrossroadCarAgent m_CarAgent;

    public int agentID;
    public int sceneID;

    string m_ScoresFile;
    string m_SpeedProfileFile;

    public int[] scores = new int[9];
    public float speed;

    int c_green = 0;
    int c_amber = 1;
    int c_red = 2;
    int c_amberred = 3;
    int c_col_agent = 4;
    int c_col_wall = 5;
    int c_nopass = 6;
    int c_target = 7;

    int instance = 0;
    int continous_t = 0;
    int last_t = 0;
    int last_ep = 0;

    public Transform m_TargetTr;
    public Rigidbody m_AgentRb;

    // Start is called before the first frame update
    void Start()
    {
        m_Settings = FindObjectOfType<CrossroadSettings>();
        m_CarAgent = this.GetComponent<CrossroadCarAgent>();

        agentID = this.transform.parent.GetSiblingIndex();
        sceneID = this.transform.parent.parent.GetSiblingIndex();

        if (m_Settings.writeResults){
            string []arr = new string[2];
            arr = m_Settings.getResultsFileName();
            m_ScoresFile = arr[0];
            m_SpeedProfileFile = arr[1];

            // m_ScoresFile = m_Settings.getResultsFileName();
        }

        m_AgentRb = this.GetComponent<Rigidbody>();
        m_TargetTr = this.transform.parent.GetChild(1);
    }


    // Update is called once per frame
    void Update()
    {
        continous_t++;
        if (last_ep < m_CarAgent.CompletedEpisodes){
            last_ep++;
            last_t = continous_t;
        }
        instance = continous_t - last_ep + 1;

        if ( m_Settings.writeResults ){
            speed = m_AgentRb.velocity.magnitude;
            StreamWriter writer = new StreamWriter(m_SpeedProfileFile, true);
            writer.WriteLine(sceneID + " " + agentID + " " +
                m_CarAgent.CompletedEpisodes + " " + instance + " " +
                speed + " " + speed*speed);
            writer.Close();
        }
    }

    public void WriteEpStatistics(int completed_episodes, int tot_steps)
    {

        if ( m_Settings.writeResults && (tot_steps > 0) ){
            StreamWriter writer = new StreamWriter(m_ScoresFile, true);
            writer.WriteLine(sceneID + " " + agentID + " " +
                completed_episodes + " " +
                scores[c_green] + " " +
                scores[c_amber] + " " +
                scores[c_red] + " " +
                scores[c_amberred] + " " +
                scores[c_col_agent] + " " +
                scores[c_col_wall] + " " +
                scores[c_nopass] + " " +
                scores[c_target] + " " +
                tot_steps);
            writer.Close();
        }
        scores = new int[9];
    }

    public void IncrementTarget(){
        scores[c_target]++;
    }

    public void IncrementGreen(){
        scores[c_green]++;
    }

    public void IncrementAmber(){
            scores[c_amber]++;
    }

    public void IncrementRed(){
        scores[c_red]++;
    }

    public void IncrementAmberRed(){
        scores[c_amberred]++;
    }

    public void IncrementNoPass()
    {
        scores[c_nopass]++;
    }

    public void IncrementWallCol(){
        scores[c_col_wall]++;
    }

    public void IncrementAgentCol(){
        scores[c_col_agent]++;
    }

}
