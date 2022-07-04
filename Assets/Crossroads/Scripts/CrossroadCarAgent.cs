using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Policies;

public class CrossroadCarAgent : Agent
{
    Profiling m_Profiling;
    public GameObject ground;
    public GameObject area;
    public GameObject target;
    public bool useVectorObs;

    Rigidbody m_AgentRb;
    Material m_GroundMaterial;
    Renderer m_GroundRenderer;
    CrossroadSettings m_Settings;
    CrossroadScene m_Scene;
    CrossroadLights m_LightControl;
    CrossroadColliders m_TargetCollider;
    CrossroadColliders m_AgentCollider;

    CrossroadLane m_AgentLane;
    CrossroadLane m_CurrentLane;
    CrossroadLane m_TargetLane;

    public GameObject InitLane;
    public GameObject EndLane;

    Transform m_AgentTr;
    Transform m_TargetTr;

    Transform m_Sign;

    float m_AgentOffset;
    float m_TargetOffset;
    int m_Selection;
    float m_Deviate;
    float m_RandOffset;

    int m_AgentLaneId;
    int m_TargetLaneId;

    LayerMask m_DefaultMask;

    Vector3 m_Direction;

    public float flag;
    public int episode;
    public int idle;

    int m_Steps = 0;

    public override void Initialize()
    {
        m_Profiling = GetComponent<Profiling>();

        flag = 0;

        Transform root = this.transform.root;
        string rootpath = "/" + root.name;

        m_Settings = FindObjectOfType<CrossroadSettings>();

        var scene = GameObject.Find(rootpath + "/Scene");
        m_Scene = scene.GetComponent<CrossroadScene>();

        var light_control = GameObject.Find(rootpath + "/LightsControl");
        m_LightControl = light_control.GetComponent<CrossroadLights>();
        m_LightControl.ResetLights();

        m_AgentRb = GetComponent<Rigidbody>();

        m_GroundRenderer = ground.GetComponent<Renderer>();
        m_GroundMaterial = m_GroundRenderer.material;

        m_AgentTr = GetComponent<Transform>();
        m_TargetTr = target.GetComponent<Transform>();

        m_AgentCollider = GetComponent<CrossroadColliders>();
        m_TargetCollider =  target.transform.GetChild(0).GetComponent<CrossroadColliders>();

        m_AgentLane = InitLane.GetComponent<CrossroadLane>();
        m_CurrentLane = InitLane.GetComponent<CrossroadLane>();
        m_TargetLane = EndLane.GetComponent<CrossroadLane>();

        m_Direction = m_CurrentLane.direction;
        // m_Normal = m_CurrentLane.normal;

        CrossroadLane arbitrary_lane;
        if (this.transform.GetSiblingIndex() == 0)
        {
            foreach (Transform lane in m_Scene.lanes.transform)
            {
                arbitrary_lane = lane.GetComponent<CrossroadLane>();
                arbitrary_lane.SetDescriptors();
                arbitrary_lane.SetTrainingBounds();
            }
        }

        m_AgentOffset = 0f;
        m_TargetOffset = 0f;

        m_Deviate = 0.1f;

        m_AgentLaneId = m_AgentLane.transform.GetSiblingIndex();

        m_Raysensor =
        transform.GetComponent<RayPerceptionSensorComponentBase>();
        m_DefaultMask = LayerMask.GetMask("Default", "Ground",
            "Agent", "Traffic Lights", "Lane", "Target", "No Pass");

        var randcolour = Color.HSVToRGB(Random.Range(0f, 0.85f), Random.Range(0.30f, 0.40f), Random.Range(0.75f, 0.85f));

        var agentRenderer = transform.GetChild(0).GetComponent<MeshRenderer>();
        agentRenderer.material.SetColor("_Color", randcolour);
        var targetRenderer = m_TargetTr.GetChild(0).GetComponent<MeshRenderer>();
        targetRenderer.material.SetColor("_Color", randcolour);
    }

    public bool drawgizmos=false;

    void OnDrawGizmos()
    {
        if (drawgizmos)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(m_TargetLane.enter.transform.position, m_AgentLane.exit.transform.position);
        }
    }

    public float Deviation()
    {
        float dev = 0f;
        if (m_AgentCollider.stream.magnitude == 0)
        {
            drawgizmos = true;
            var goaldir = m_TargetLane.enter.transform.position - m_AgentLane.exit.transform.position;
            var goalnormal = Vector3.Cross(Vector3.up, Vector3.Normalize(goaldir));
            var agentpos = m_AgentTr.position - m_AgentLane.exit.transform.position;
            dev = Vector3.Dot(agentpos, goalnormal);
        }
        else if (m_AgentCollider.occupied)
        {
            drawgizmos = false;
            var agentpos = m_AgentTr.position - m_AgentCollider.lane.centreline;
            var centrenormal = Vector3.Dot(m_AgentCollider.lane.direction, m_AgentCollider.stream) * m_AgentCollider.lane.normal;
            dev = Vector3.Dot(agentpos, centrenormal);
        }

        return dev;
    }


    public override void CollectObservations(VectorSensor sensor)
    {
        if (useVectorObs)
        {
            // sensor.AddObservation(StepCount / (float)MaxStep);
            var dir = m_TargetTr.position - m_AgentTr.position;

            sensor.AddObservation(Vector3.Dot(dir, m_AgentLane.direction)
                + Vector3.Dot(dir, m_AgentLane.normal));
            sensor.AddObservation(Vector3.Dot(dir, m_TargetLane.direction)
                + Vector3.Dot(dir, m_TargetLane.normal));

        }
    }

    IEnumerator SwapGroundMaterial(Material mat, float time)
    {
        if (m_Settings.verbose){
            m_GroundRenderer.material = mat;
            yield return new WaitForSeconds(time);
            m_GroundRenderer.material = m_GroundMaterial;
        }
    }

    RayPerceptionSensorComponentBase  m_Raysensor;

    IEnumerator DisableRayLayer(LayerMask mask, float time)
    {
        m_Raysensor.RayLayerMask = mask;

        yield return new WaitForSeconds(time);

        m_Raysensor.RayLayerMask = m_DefaultMask;

    }

    float m_theta = 1f;
    Vector3 rotateDir = Vector3.zero;
    public void MoveAgent(ActionSegment<int> act)
    {
        var dirToGo = Vector3.zero;
        // var rotateDir = Vector3.zero;

        var action = act[0];

        var rotateTowards = transform.forward;
        Quaternion rotation;

        switch (action)
        {
            case 0:
                idle+=1;
                break;
            case 1:
                dirToGo = transform.forward * 1f;
                rotateTowards =
                    Vector3.RotateTowards(transform.forward, rotateDir,
                    Time.deltaTime * m_Settings.agentRotationSpeed, 0f);
                break;
            case 2:
                dirToGo = transform.forward * -0.2f;
                rotateTowards =
                    Vector3.RotateTowards(transform.forward, rotateDir,
                    Time.deltaTime * m_Settings.agentRotationSpeed, 0f);
                // m_theta = 0;
                break;
            // TODO: restricted turns
            case 3:
                dirToGo = transform.forward * 0f;

                rotateDir = Mathf.Sin(m_theta)* transform.forward * 1f
                    + Mathf.Cos(m_theta)* transform.right * 1f;

                rotateTowards =
                    Vector3.RotateTowards(transform.forward, rotateDir,
                    Time.deltaTime * m_Settings.agentRotationSpeed, 0f);
                break;
            case 4:
                dirToGo = transform.forward * 0f;
                rotateDir = Mathf.Sin(m_theta)* transform.forward * 1f
                    + Mathf.Cos(m_theta)* transform.right * -1f;

                rotateTowards =
                    Vector3.RotateTowards(transform.forward, rotateDir,
                    Time.deltaTime * m_Settings.agentRotationSpeed, 0f);
                break;
        }

        rotation = Quaternion.LookRotation(rotateTowards);
        m_AgentRb.MoveRotation(rotation);
        m_AgentRb.MovePosition(m_AgentRb.position
            + dirToGo * m_Settings.agentRunSpeed * Time.deltaTime);
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // Learn intrinsically the sense of time
        AddReward(-0.1f / MaxStep);
        MoveAgent(actionBuffers.DiscreteActions);

        m_AgentCollider.GetLaneStream(m_AgentLane.direction, -m_TargetLane.direction);

        if (Vector3.Dot(transform.forward * 1f, m_AgentCollider.stream) <- 0.01f )
        {
            m_Steps = StepCount;
            EndEpisode();
        }
    }

    void OnCollisionEnter(Collision col)
    {
        // Debug.Log("the agent collided with" + col.gameObject.name);
        if (col.gameObject == target.transform.GetChild(0).gameObject)
        {
            // Associate with magnitude & deflection observations
            SetReward(1f);
            m_Profiling.IncrementTarget();
            StartCoroutine(SwapGroundMaterial(m_Settings.goalScoredMaterial, 0.5f));
            m_Steps = StepCount;
            EndEpisode();
        }
        else if (col.gameObject.CompareTag("lights_green"))
        {
            // Associate with tag "lights_green"
            // StartCoroutine(DisableRayLayer(mask, 0.5f));
            m_Profiling.IncrementGreen();
            AddReward(1f / MaxStep);
            StartCoroutine(SwapGroundMaterial(m_Settings.greenLightMaterial, 0.5f));
        }
        else if (col.gameObject.CompareTag("lights_orange"))
        {
            // Associate with tag "lights_orange"
            // StartCoroutine(DisableRayLayer(mask, 0.5f));

            // AddReward(-1f / MaxStep);
            m_Profiling.IncrementAmber();
            StartCoroutine(SwapGroundMaterial(m_Settings.orangeLightMaterial, 0.5f));
        }
        else if (col.gameObject.CompareTag("lights_red"))
        {
            // Associate with tag "lights_red"
            // StartCoroutine(DisableRayLayer(mask, 0.5f));
            m_Profiling.IncrementRed();
            SetReward(-1f);
            StartCoroutine(SwapGroundMaterial(m_Settings.redLightMaterial, 0.5f));
            if (CompletedEpisodes > 1e3 || !m_Settings.isTraining)
            {
                m_Steps = StepCount;
                EndEpisode();
            }
        }
        else if (col.gameObject.CompareTag("lights_red_orange"))
        {
            // Associate with tag "lights_red_orange"
            // StartCoroutine(DisableRayLayer(mask, 0.5f));

            SetReward(-0.5f);
            m_Profiling.IncrementAmberRed();
            StartCoroutine(SwapGroundMaterial(m_Settings.orangeLightMaterial, 0.5f));
            if (CompletedEpisodes > 1e3 || !m_Settings.isTraining)
            {
                m_Steps = StepCount;
                EndEpisode();
            }
        }
        else if (col.gameObject.CompareTag("wait") &&
            Vector3.Dot(transform.forward * 1f, col.gameObject.GetComponent<CrossroadColliders>().lane.direction) <= -0f )
        {
            // Ạssociate with "wait" tag
            SetReward(-1f);
            m_Profiling.IncrementNoPass();
            StartCoroutine(SwapGroundMaterial(m_Settings.lawBreakMaterial, 0.5f));
            m_Steps = StepCount;
            EndEpisode();
        }
        else if (col.gameObject.CompareTag("wall") ||
            col.gameObject.CompareTag("wall_centre"))
        {
            // Associate with "wall" tag
            SetReward(-1f);
            m_Profiling.IncrementWallCol();
            StartCoroutine(SwapGroundMaterial(m_Settings.lawBreakMaterial, 0.5f));
            m_Steps = StepCount;
            EndEpisode();
        }
        else if (col.gameObject.CompareTag("agent"))
        {
            // Associate with "agent" tag
            SetReward(-1f);
            m_Profiling.IncrementAgentCol();
            StartCoroutine(SwapGroundMaterial(m_Settings.lawBreakMaterial, 0.5f));
            m_Steps = StepCount;
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
        // m_TargetOffset = m_TargetLane.offset - 2f;
        // m_AgentOffset = - m_AgentLane.offset + 5f + Random.Range(-1f, 7f);
        if (!m_Settings.isTraining){
            m_AgentOffset = Random.Range(- m_AgentLane.offset * 0.85f, -m_AgentLane.offset * 0.4f);
            m_TargetOffset = Random.Range(m_TargetLane.offset * 0.4f, m_TargetLane.offset * 0.9f);
        }
        else
        {
            if (CompletedEpisodes > 6e2 && m_AgentLane.offset < m_AgentLane.length)
            {
                flag = 1;
                m_AgentLane.bottom.position -= m_Deviate *  m_AgentLane.direction;
                m_AgentLane.offset += m_Deviate;
            }
            if (m_AgentLane.offset >= m_AgentLane.length)
            {
                flag = 2;
                m_AgentLane.bottom.position = m_AgentLane.bottomEnd.position;
                m_AgentLane.offset = m_AgentLane.length;
            }

            m_TargetOffset = m_TargetLane.offset - 2f - Random.Range( 0f, 1f);
            m_AgentOffset = - m_AgentLane.offset + 5f + Random.Range(-1f, 3f);

            if (CompletedEpisodes > 1e3)
            {
                flag = 3;
                m_AgentOffset = Random.Range(- m_AgentLane.offset * 0.85f, -m_AgentLane.offset * 0.3f);

                m_TargetOffset = Random.Range(m_TargetLane.offset * 0.4f, m_TargetLane.offset * 0.9f);
            }
            if (m_TargetOffset > m_TargetLane.offset - 2f)
            {
                flag += 10;
                m_TargetOffset = m_TargetLane.offset - 2f;
            }
            if (m_AgentOffset < - m_AgentLane.offset + 5f)
            {
                flag += 100;
                m_AgentOffset = - m_AgentLane.offset + 5f;
            }
        }
    }
    public override void OnEpisodeBegin()
    {
        idle = 0;

        m_Profiling.WriteEpStatistics(CompletedEpisodes, m_Steps);

        m_Scene.episode++;
        episode = CompletedEpisodes;

        transform.position -= 1000f * Vector3.up;

        m_TargetLaneId = Random.Range(0,
            m_Scene.lanes.transform.childCount - 1);
        if (m_TargetLaneId >= m_AgentLaneId)
        {
            m_TargetLaneId += 1;
        }

        m_TargetLane = m_Scene.lanes.transform.GetChild(m_TargetLaneId).GetComponent<CrossroadLane>();

        ResetOffsets();

        m_RandOffset = m_AgentOffset + Random.Range(-4f, 0f);
        transform.position = m_AgentLane.centreline
            + Random.Range(-1.5f, 1.5f) * m_AgentLane.normal
            - 10f * m_AgentLane.direction
            + m_RandOffset * m_AgentLane.direction
            + 0f * Vector3.up
            + ground.transform.position;

        var orientation = Mathf.Atan2( m_AgentLane.direction.x,
            m_AgentLane.direction.z) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, orientation, 0f);
        m_AgentRb.velocity = 1f * m_AgentLane.direction;

        rotateDir = transform.forward;

        if (CompletedEpisodes < 3e2  && m_Settings.isTraining)
        {
            m_TargetOffset += CompletedEpisodes/3e1f - 10f;
        }
        target.transform.position = m_TargetLane.targetline
            - m_TargetOffset * m_TargetLane.direction
            - 10f * m_TargetLane.direction
            + ground.transform.position;
        orientation = Mathf.Atan2( m_TargetLane.direction.x,
            m_TargetLane.direction.z) * Mathf.Rad2Deg;
        target.transform.rotation = Quaternion.Euler(0f, orientation, 0f);

        // Debug.Log("episode: " + m_Scene.episode + ", agent offset: "
        //     + m_AgentOffset + " rand offset: " + m_RandOffset);
    }
}
