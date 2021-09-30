using System.Collections;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Policies;

public class LanesCarAgent : Agent
{
    public GameObject ground;
    public GameObject area;
    public GameObject wallTop;
    public GameObject wallBottom;
    public GameObject wallTopEnd;
    public GameObject wallBottomEnd;
    public GameObject leftShoulder;
    public GameObject rightShoulder;
    public GameObject target;
    public GameObject sign;
    public GameObject wait;
    public bool useVectorObs;

    Rigidbody m_AgentRb;
    Material m_GroundMaterial;
    Renderer m_GroundRenderer;
    LanesSettings m_Settings;
    LanesScene m_Scene;

    Transform m_AgentTr;
    Transform m_TargetTr;

    Vector3 m_Direction;
    Vector3 m_Normal;

    Transform m_Top;
    Transform m_Bottom;
    Transform m_TopEnd;
    Transform m_BottomEnd;
    Transform m_Sign;

    Vector3 m_LaneCentreline;
    float m_AgentOffset;
    float m_SignOffset;
    float m_TargetOffset;
    int m_Selection;
    float m_Deviate;
    float m_RandOffset;

    public float flag;

    public override void Initialize()
    {
        flag = 0;

        Transform root = this.transform.root;
        string rootpath = "/" + root.name;

        m_Settings = FindObjectOfType<LanesSettings>();

        var scene = GameObject.Find(rootpath + "/Scene");
        m_Scene = scene.GetComponent<LanesScene>();
        // m_Scene.InitialiseScene();

        m_AgentRb = GetComponent<Rigidbody>();
        m_GroundRenderer = ground.GetComponent<Renderer>();
        m_GroundMaterial = m_GroundRenderer.material;

        m_AgentTr = GetComponent<Transform>();
        m_TargetTr = target.GetComponent<Transform>();

        m_Sign = sign.GetComponent<Transform>();

        m_Top = wallTop.GetComponent<Transform>();
        m_Bottom = wallBottom.GetComponent<Transform>();
        m_TopEnd = wallTopEnd.GetComponent<Transform>();
        m_BottomEnd = wallBottomEnd.GetComponent<Transform>();

        m_Direction = Vector3.Normalize(m_Top.position - m_Bottom.position);
        m_Normal = Vector3.Cross(m_Direction, Vector3.up);

        m_AgentOffset = -Vector3.Dot(m_Sign.position - m_Bottom.position, m_Direction) + 5f;
        m_SignOffset = m_Scene.signOffset;

        m_TargetOffset = Vector3.Dot(m_Top.position - m_Sign.position, m_Direction) - 2f;

        var centreline = 0.5f * (rightShoulder.transform.position + leftShoulder.transform.position) - ground.transform.position;
        m_LaneCentreline = Vector3.Dot(centreline, m_Normal) * m_Normal;

        m_Deviate = 0.1f;

        wait.transform.position = m_LaneCentreline - 0.01f * Vector3.up + (m_SignOffset - 4f) * m_Direction
            + ground.transform.position;

    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (useVectorObs)
        {
            sensor.AddObservation(StepCount / (float)MaxStep);
            sensor.AddObservation((m_TargetTr.position - m_AgentTr.position).magnitude);
        }
    }

    IEnumerator TargetScoredSwapGroundMaterial(Material mat, float time)
    {
        m_GroundRenderer.material = mat;
        yield return new WaitForSeconds(time);
        m_GroundRenderer.material = m_GroundMaterial;
    }

    float m_theta = 20f;
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
                // dirToGo = transform.forward * 0.1f;
                // dirToGo = Mathf.Sin(m_theta)* transform.forward * 1f;
                break;
            case 4:
                rotateDir = transform.up * -1f;
                // dirToGo = transform.forward * 0.1f;
                // dirToGo = Mathf.Sin(m_theta)* transform.forward * 1f;
                break;
        }
        transform.Rotate(rotateDir, Time.deltaTime * m_theta);
        m_AgentRb.AddForce(dirToGo * m_Settings.agentRunSpeed, ForceMode.VelocityChange);
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        AddReward(-1f / MaxStep);
        MoveAgent(actionBuffers.DiscreteActions);
    }

    void OnCollisionEnter(Collision col)
    {
        // if (col.gameObject.CompareTag("target"))
        if (col.gameObject == target.transform.GetChild(0).gameObject)
        {
            SetReward(1f);
            StartCoroutine(TargetScoredSwapGroundMaterial(m_Settings.goalScoredMaterial, 0.5f));
            EndEpisode();
        }
        else if (col.gameObject.CompareTag("wait"))
        {
            m_Selection = m_Scene.m_Selection;
            switch (m_Selection)
            {
                case 0: //green
                    AddReward(0.5f / MaxStep);
                    StartCoroutine(TargetScoredSwapGroundMaterial(m_Settings.greenLightMaterial, 0.5f));
                    break;
                case 1: //orange
                    AddReward(-0.2f / MaxStep);
                    StartCoroutine(TargetScoredSwapGroundMaterial(m_Settings.orangeLightMaterial, 0.5f));
                    break;
                case 2: //red
                    SetReward(-1f);
                    StartCoroutine(TargetScoredSwapGroundMaterial(m_Settings.redLightMaterial, 0.5f));
                    if (CompletedEpisodes > 7e2 || !m_Scene.isTraining)
                    {
                        EndEpisode();
                    }
                    break;
                case 3: // red-orange
                    SetReward(-0.5f);
                    StartCoroutine(TargetScoredSwapGroundMaterial(m_Settings.orangeLightMaterial, 0.5f));
                    if (CompletedEpisodes > 1e3 || !m_Scene.isTraining)
                    {
                        EndEpisode();
                    }
                    break;
            }
        }
        else if (col.gameObject.CompareTag("wall"))
        {
            SetReward(-0.5f);
            StartCoroutine(TargetScoredSwapGroundMaterial(m_Settings.redLightMaterial, 0.5f));
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
        m_TargetOffset = Vector3.Dot(m_Top.position - m_Sign.position, m_Direction) - 2f;
        m_AgentOffset = - Vector3.Dot(m_Sign.position - m_Bottom.position, m_Direction) + 5f + Random.Range(-1f, 7f);
        if (!m_Scene.isTraining){
            m_AgentOffset = Random.Range(Vector3.Dot(m_Bottom.position - m_Sign.position, m_Direction)*0.85f, Vector3.Dot(m_Bottom.position - m_Sign.position, m_Direction)*0.3f);
            m_TargetOffset = Random.Range(Vector3.Dot(m_Top.position - m_Sign.position, m_Direction)*0.4f, Vector3.Dot(m_Top.position - m_Sign.position, m_Direction)*0.9f);
        }
        else
        {
            if (CompletedEpisodes > 2e3 &&
            Vector3.Dot(m_Sign.position - m_Bottom.position, m_Direction) <
            Vector3.Dot(m_Sign.position - m_BottomEnd.position, m_Direction))
            {
                flag = 1;
                // m_Deviate *= 1.01f;
                m_Bottom.position -= m_Deviate * m_Direction;
            }
            if (Vector3.Dot(m_Sign.position - m_Bottom.position, m_Direction) >=
            Vector3.Dot(m_Sign.position - m_BottomEnd.position, m_Direction))
            {
                flag = 2;
                m_Bottom.position = m_BottomEnd.position;
            }

            m_TargetOffset = Vector3.Dot(m_Top.position - m_Sign.position, m_Direction) - 2f;
            m_AgentOffset = - Vector3.Dot(m_Sign.position - m_Bottom.position, m_Direction) + 5f + Random.Range(-1f, 3f);

            if (CompletedEpisodes > 3e3)
            {
                flag = 3;
                // m_AgentOffset = Random.Range(Vector3.Dot(m_Bottom - m_Sign, m_Direction) + 5f, - 15f);
                m_AgentOffset = Random.Range(Vector3.Dot(m_Bottom.position - m_Sign.position, m_Direction)*0.85f, Vector3.Dot(m_Bottom.position - m_Sign.position, m_Direction)*0.3f);

                m_TargetOffset = Random.Range(Vector3.Dot(m_Top.position - m_Sign.position, m_Direction)*0.4f, Vector3.Dot(m_Top.position - m_Sign.position, m_Direction)*0.9f);
            }
            if (m_TargetOffset > m_Scene.realOffset - 2f)
            {
                flag += 10;
                m_TargetOffset = m_Scene.realOffset - 2f;
            }
            if (m_AgentOffset < - m_Scene.realOffset + 5f)
            {
                flag += 100;
                m_AgentOffset = - m_Scene.realOffset + 5f;
            }
        }
    }
    public override void OnEpisodeBegin()
    {
        m_Scene.episode++;
        ResetOffsets();

        // LightsPlacement();
        m_RandOffset = m_AgentOffset + Random.Range(-4f, 4f);
        transform.position = m_LaneCentreline +
            Random.Range(-1.5f, 1.5f) * m_Normal + 1f * Vector3.up +
            m_RandOffset * m_Direction
            + ground.transform.position;

        var orientation = Mathf.Atan2(m_Direction.x, m_Direction.z) *
            Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, orientation, 0f);
        m_AgentRb.velocity *= 0f;

        target.transform.position = 0.5f * Vector3.up + m_TargetOffset * m_Direction + area.transform.position;
    }
}
