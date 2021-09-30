using System.Collections;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Policies;

public class BasicCarAgent : Agent
{
    public GameObject ground;
    public GameObject area;
    public GameObject wallTop;
    public GameObject wallBottom;
    public GameObject target;
    public GameObject lights_green;
    public GameObject lights_orange;
    public GameObject lights_red;
    public GameObject lights_red_orange;
    public GameObject wait;
    public bool useVectorObs;
    public float block_offset = -1f;
    public float target_offset = -1f;

    Rigidbody m_AgentRb;
    Material m_GroundMaterial;
    Renderer m_GroundRenderer;
    BasicSettings m_BasicSettings;

    Transform m_AgentTr;
    Transform m_TargetTr;

    float m_Top;
    float m_Bottom;
    float m_AgentOffset;
    float m_BlockOffset;
    float m_BlockRandomZ;
    float m_TargetOffset;
    float m_TargetZ;
    int m_Selection;
    int m_Timer;
    int m_TimeStamp;
    float m_Deviate;

    public override void Initialize()
    {
        m_BasicSettings = FindObjectOfType<BasicSettings>();
        m_AgentRb = GetComponent<Rigidbody>();
        m_GroundRenderer = ground.GetComponent<Renderer>();
        m_GroundMaterial = m_GroundRenderer.material;

        m_AgentTr = GetComponent<Transform>();
        m_TargetTr = target.GetComponent<Transform>();

        m_Bottom = wallBottom.transform.position.z - ground.transform.position.z;
        m_AgentOffset = m_Bottom + 10f;
        m_BlockOffset = m_Bottom + 25f;
        m_TargetOffset = m_Bottom + 48f;

        m_Top = wallTop.transform.position.z - ground.transform.position.z;
        m_TargetZ = m_Top - 2f;
        m_Deviate = 0.01f;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (useVectorObs)
        {
            sensor.AddObservation(StepCount / (float)MaxStep);
        }
    }

    IEnumerator TargetScoredSwapGroundMaterial(Material mat, float time)
    {
        m_GroundRenderer.material = mat;
        yield return new WaitForSeconds(time);
        m_GroundRenderer.material = m_GroundMaterial;
    }

    float m_theta = 60f;
    public void MoveAgent(ActionSegment<int> act)
    {
        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;

        var action = act[0];
        switch (action)
        {
            case 0:
                break;
            case 1:
                dirToGo = transform.forward * 1f;
                break;
            case 2:
                dirToGo = transform.forward * -1f;
                break;
            // TODO: restricted turns
            case 3:
                rotateDir = transform.up * 1f;
                // dirToGo = Mathf.Sin(m_theta)* transform.forward * 1f;
                break;
            case 4:
                rotateDir = transform.up * -1f;
                // dirToGo = Mathf.Sin(m_theta)* transform.forward * 1f;
                break;
        }
        transform.Rotate(rotateDir, Time.deltaTime * m_theta);
        m_AgentRb.AddForce(dirToGo * m_BasicSettings.agentRunSpeed, ForceMode.VelocityChange);
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        AddReward(-1f / MaxStep);
        MoveAgent(actionBuffers.DiscreteActions);

        if (StepCount > m_TimeStamp + m_Timer)
        {
            m_Selection = (m_Selection + 1) % 4;
            LightsPlacement();
        }
    }

    void OnCollisionEnter(Collision col)
    {
        if (col.gameObject.CompareTag("target"))
        {
            SetReward(1f);
            StartCoroutine(TargetScoredSwapGroundMaterial(m_BasicSettings.goalScoredMaterial, 0.5f));
            EndEpisode();
        }
        else if (col.gameObject.CompareTag("wait"))
        {
            switch (m_Selection)
            {
                case 0: //green
                    AddReward(0.2f / MaxStep);
                    StartCoroutine(TargetScoredSwapGroundMaterial(m_BasicSettings.greenLightMaterial, 0.5f));
                    break;
                case 1: //orange
                    AddReward(-0.5f / MaxStep);
                    StartCoroutine(TargetScoredSwapGroundMaterial(m_BasicSettings.orangeLightMaterial, 0.5f));
                    break;
                case 2: //red
                    SetReward(-1f); // TODO: gradualy increase (in magnitude) this, make it -1
                    StartCoroutine(TargetScoredSwapGroundMaterial(m_BasicSettings.redLightMaterial, 0.5f));
                    EndEpisode();
                    break;
                case 3: // red-orange
                    SetReward(-0.5f);
                    StartCoroutine(TargetScoredSwapGroundMaterial(m_BasicSettings.orangeLightMaterial, 0.5f));
                    EndEpisode();
                    break;
            }
        }
        else if (col.gameObject.CompareTag("wall"))
        {
            SetReward(-0.5f);
            StartCoroutine(TargetScoredSwapGroundMaterial(m_BasicSettings.redLightMaterial, 0.5f));
            EndEpisode();
        }

    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        discreteActionsOut[0] = 0;
        if (Input.GetKey(KeyCode.D))
        {
            discreteActionsOut[0] = 3;
        }
        else if (Input.GetKey(KeyCode.W))
        {
            discreteActionsOut[0] = 1;
        }
        else if (Input.GetKey(KeyCode.A))
        {
            discreteActionsOut[0] = 4;
        }
        else if (Input.GetKey(KeyCode.S))
        {
            discreteActionsOut[0] = 2;
        }
    }

    void ResetOffsets()
    {
        if (block_offset > 0 && target_offset < m_TargetZ && block_offset < target_offset)
        {
            m_BlockOffset = block_offset;
            m_TargetOffset = target_offset;
        }
        else
        {
            if (m_TargetOffset < m_TargetZ && CompletedEpisodes > 1e3)
            {
                m_Deviate *= 1.1f;
                m_BlockOffset += m_Deviate;
                m_TargetOffset += m_Deviate;
            }
            if (CompletedEpisodes > 2e3)
            {
                m_BlockOffset = Random.Range(m_Bottom + 25f, m_Top - 20f);
                m_TargetOffset = Random.Range(m_Bottom + 48f, m_Top - 2f);
            }
            if (m_TargetOffset >= m_TargetZ)
            {
                m_TargetOffset = m_TargetZ;
            }
        }
    }
    public override void OnEpisodeBegin()
    {
        ResetOffsets();

        m_BlockRandomZ = Random.Range(-5f, 5f);

        m_Selection = Random.Range(0, 4);
        LightsPlacement();
        wait.transform.position =
            new Vector3(0f, -0.01f, -4f + m_BlockOffset + m_BlockRandomZ)
            + ground.transform.position;

        transform.position = new Vector3(0f + Random.Range(-3f, 3f),
            1f, m_AgentOffset + Random.Range(-5f, 5f))
            + ground.transform.position;
        transform.rotation = Quaternion.Euler(0f, Random.Range(-20f, 20f), 0f);
        m_AgentRb.velocity *= 0f;


        target.transform.position = new Vector3(0f, 0.5f, m_TargetOffset) + area.transform.position;
    }

    void LightsPlacement()
    {
        m_TimeStamp = StepCount;
        switch (m_Selection)
        {
            case 0: //green
                m_Timer = Random.Range(300, 500);
                lights_green.transform.position =
                    new Vector3(0f, 5f, m_BlockOffset + m_BlockRandomZ)
                    + ground.transform.position;
                lights_red.transform.position =
                    new Vector3(0f, -1000f, m_BlockOffset + m_BlockRandomZ)
                    + ground.transform.position;
                lights_orange.transform.position =
                    new Vector3(0f, -1000f, m_BlockOffset + m_BlockRandomZ)
                    + ground.transform.position;
                lights_red_orange.transform.position =
                    new Vector3(0f, -1000f, m_BlockOffset + m_BlockRandomZ)
                    + ground.transform.position;
                break;
            case 1: // orange
                m_Timer = 200;
                lights_green.transform.position =
                    new Vector3(0f, -1000f, m_BlockOffset + m_BlockRandomZ)
                    + ground.transform.position;
                lights_red.transform.position =
                    new Vector3(0f, -1000f, m_BlockOffset + m_BlockRandomZ)
                    + ground.transform.position;
                lights_orange.transform.position =
                    new Vector3(0f, 5f, m_BlockOffset + m_BlockRandomZ)
                    + ground.transform.position;
                lights_red_orange.transform.position =
                    new Vector3(0f, -1000f, m_BlockOffset + m_BlockRandomZ)
                    + ground.transform.position;
                break;
            case 2: //red
                m_Timer = Random.Range(300, 500);
                lights_green.transform.position =
                    new Vector3(0f, -1000f, m_BlockOffset + m_BlockRandomZ)
                    + ground.transform.position;
                lights_red.transform.position =
                    new Vector3(0f, 5f, m_BlockOffset + m_BlockRandomZ)
                    + ground.transform.position;
                lights_orange.transform.position =
                    new Vector3(0f, -1000f, m_BlockOffset + m_BlockRandomZ)
                    + ground.transform.position;
                lights_red_orange.transform.position =
                    new Vector3(0f, -1000f, m_BlockOffset + m_BlockRandomZ)
                    + ground.transform.position;
                break;
            case 3: //red-orange
                m_Timer = 100;
                lights_green.transform.position =
                    new Vector3(0f, -1000f, m_BlockOffset + m_BlockRandomZ)
                    + ground.transform.position;
                lights_red.transform.position =
                    new Vector3(0f, -1000f, m_BlockOffset + m_BlockRandomZ)
                    + ground.transform.position;
                lights_orange.transform.position =
                    new Vector3(0f, -1000f, m_BlockOffset + m_BlockRandomZ)
                    + ground.transform.position;
                lights_red_orange.transform.position =
                    new Vector3(0f, 5f, m_BlockOffset + m_BlockRandomZ)
                    + ground.transform.position;
                break;
        }
    }
}
