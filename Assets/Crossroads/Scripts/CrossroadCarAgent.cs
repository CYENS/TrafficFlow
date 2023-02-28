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
    public GameObject m_Skin;

    Transform m_TargetTr;

    Transform m_Sign;

    public float m_AgentOffset;
    public float m_TargetOffset;
    int m_Selection;
    float m_Deviate;
    float m_RandOffset;

    int m_AgentLaneId;
    int m_TargetLaneId;

    bool m_Reposition;
    bool m_CarryForwards;

    LayerMask m_DefaultMask;

    Vector3 m_Direction;
    // Vector3 m_Normal;

    public float flag;
    public int episode;
    public int idle;
    public int forw;
    public int back;
    public int right;
    public int left;

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
        m_Skin = this.transform.Find("Skin").gameObject;

        m_GroundRenderer = ground.GetComponent<Renderer>();
        m_GroundMaterial = m_GroundRenderer.material;

        m_TargetTr = target.GetComponent<Transform>();

        m_AgentCollider = GetComponent<CrossroadColliders>();
        m_TargetCollider =  target.transform.GetChild(0).GetComponent<CrossroadColliders>();

        m_AgentLane = InitLane.GetComponent<CrossroadLane>();
        m_CurrentLane = InitLane.GetComponent<CrossroadLane>();
        m_TargetLane = EndLane.GetComponent<CrossroadLane>();

        m_Direction = m_CurrentLane.direction;

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


        // Assign rendering colour to agent
        var randcolour = Color.HSVToRGB(Random.Range(0f, 0.9f), Random.Range(0.80f, 0.90f), Random.Range(0.80f, 0.90f));

        foreach (Transform skin in m_Skin.transform)
        {
            var agentRenderer = skin.gameObject.GetComponent<MeshRenderer>();
            agentRenderer.material.SetColor("_Color", randcolour);
            var targetRenderer = m_TargetTr.GetChild(0).GetComponent<MeshRenderer>();
            targetRenderer.material.SetColor("_Color", randcolour);
        }

        // var scale_x = Mathf.Abs(m_AgentLane.direction.x)*Random.Range(0f, 0.5f)
        //     + Mathf.Abs(m_AgentLane.normal.x)*Random.Range(0f, 0.2f);
        // var scale_z = Mathf.Abs(m_AgentLane.direction.z)*Random.Range(0f, 0.5f)
        //     + Mathf.Abs(m_AgentLane.normal.z)*Random.Range(0f, 0.2f);
        // var scaleChange = new Vector3(scale_x, 0f,scale_z);
        // transform.localScale += scaleChange;

        m_Reposition = true;
        m_CarryForwards = false;

        if (m_Settings.isTraining || m_Settings.boundedTraining)
        {
            m_Settings.connectLanes = false;
        }
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

    // Get rid of this?
    public float Deviation()
    {
        float dev = 0f;
        if (m_AgentCollider.stream.magnitude == 0)
        {
            drawgizmos = true;
            var goaldir = m_TargetLane.enter.transform.position - m_AgentLane.exit.transform.position;
            var goalnormal = Vector3.Cross(Vector3.up, Vector3.Normalize(goaldir));
            var agentpos = transform.position - m_AgentLane.exit.transform.position;
            dev = Vector3.Dot(agentpos, goalnormal);
        }
        else if (m_AgentCollider.occupied)
        {
            drawgizmos = false;
            var agentpos = transform.position - m_AgentCollider.lane.centreline;
            var centrenormal = Vector3.Dot(m_AgentCollider.lane.direction, m_AgentCollider.stream) * m_AgentCollider.lane.normal;
            dev = Vector3.Dot(agentpos, centrenormal);
        }

        return dev;
    }


    public override void CollectObservations(VectorSensor sensor)
    {
        if (useVectorObs)
        {
            var dir = m_TargetTr.position - transform.position;

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

    float m_Theta = 1f;
    Vector3 rotateDir = Vector3.zero;
    Vector3 motionDir = Vector3.zero;

    public void MoveAgent(ActionSegment<int> act)
    {
        var dirToGo = Vector3.zero;
        // rotateDir = Vector3.zero;

        var action = act[0];

        var rotateTowards = transform.forward;
        Quaternion rotation;

        switch (action)
        {
            case 0:
                idle++;
                break;
            case 1:
                dirToGo = transform.forward * 1f;
                rotateTowards =
                    Vector3.RotateTowards(transform.forward, rotateDir,
                    Time.deltaTime * m_Settings.agentRotationSpeed, 0f);
                motionDir = transform.forward * 1f;
                // AddReward( -0.005f * m_Settings.agentRunSpeed *
                //     m_Settings.agentRunSpeed / MaxStep );
                forw++;
                break;
            case 2:
                dirToGo = transform.forward * -0.2f;
                rotateTowards =
                    Vector3.RotateTowards(transform.forward, rotateDir,
                    Time.deltaTime * m_Settings.agentRotationSpeed, 0f);
                AddReward(-1.0f / MaxStep);
                motionDir = transform.forward * 1f;
                // AddReward( -0.01f * 0.2f * 0.2f * m_Settings.agentRunSpeed *
                //     m_Settings.agentRunSpeed / MaxStep );
                back++;
                break;
            case 3:
                dirToGo = transform.forward * 0f;

                rotateDir = Mathf.Sin(m_Theta)* transform.forward * 1f
                    + Mathf.Cos(m_Theta)* transform.right * 1f;

                rotateTowards =
                    Vector3.RotateTowards(transform.forward, rotateDir,
                    Time.deltaTime * m_Settings.agentRotationSpeed, 0f);

                // AddReward( -0.01f * 0.167f * m_Settings.agentRotationSpeed *
                //     m_Settings.agentRotationSpeed / MaxStep );
                right++;
                break;
            case 4:
                dirToGo = transform.forward * 0f;

                rotateDir = Mathf.Sin(m_Theta)* transform.forward * 1f
                    + Mathf.Cos(m_Theta)* transform.right * -1f;

                rotateTowards =
                    Vector3.RotateTowards(transform.forward, rotateDir,
                    Time.deltaTime * m_Settings.agentRotationSpeed, 0f);

                // AddReward( -0.01f * 0.167f * m_Settings.agentRotationSpeed *
                //     m_Settings.agentRotationSpeed / MaxStep );
                left++;
                break;
        }

        rotation = Quaternion.LookRotation(rotateTowards);
        m_AgentRb.MoveRotation(rotation);
        m_AgentRb.MovePosition(m_AgentRb.position
            + dirToGo * m_Settings.agentRunSpeed * Time.deltaTime);

        var returnorientation = Mathf.Atan2( motionDir.x,
            motionDir.z) * Mathf.Rad2Deg;
        // m_Skin.transform.rotation = Quaternion.Euler(0f, returnorientation, 0f);
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // Learn intrinsically the sense of time
        AddReward(-0.1f / MaxStep);
        MoveAgent(actionBuffers.DiscreteActions);

        m_AgentCollider.GetLaneStream(m_AgentLane.direction, -m_TargetLane.direction);
        // if (Vector3.Dot(transform.forward * 1f, m_AgentCollider.stream) > 0.01f )
        // {
        //     AddReward(0.3f / MaxStep);
        //     // AddReward(Mathf.Exp(-StepCount) - 1);
        // }

        if (Vector3.Dot(transform.forward * 1f, m_AgentCollider.stream) <- 0.01f )
        {
            m_Steps = StepCount;
            EndEpisode();
            // AddReward(Mathf.Exp(-StepCount) - 1);
        }
    }

    void OnCollisionEnter(Collision col)
    {
        // Debug.Log("the agent collided with" + col.gameObject.name);
        if (col.gameObject == target.transform.GetChild(0).gameObject)
        {
            // Associate with magnitude & deflection observations
            // LayerMask mask = LayerMask.GetMask("wait");
            SetReward(1f);
            m_Profiling.IncrementTarget();
            m_CarryForwards = true;
            m_Reposition = false;
            StartCoroutine(SwapGroundMaterial(m_Settings.goalScoredMaterial, 0.5f));
            m_Steps = StepCount;
            EndEpisode();
        }
        else if (col.gameObject.CompareTag("lights_green"))
        {
            // Associate with tag "lights_green"
            // LayerMask mask = m_DefaultMask - LayerMask.GetMask("No Pass");
            // StartCoroutine(DisableRayLayer(mask, 0.5f));
            m_Profiling.IncrementGreen();
            AddReward(1f / MaxStep);
            StartCoroutine(SwapGroundMaterial(m_Settings.greenLightMaterial, 0.5f));
        }
        else if (col.gameObject.CompareTag("lights_orange"))
        {
            // Associate with tag "lights_orange"
            // LayerMask mask = m_DefaultMask - LayerMask.GetMask("No Pass");
            // StartCoroutine(DisableRayLayer(mask, 0.5f));

            // AddReward(-1f / MaxStep);
            m_Profiling.IncrementAmber();
            StartCoroutine(SwapGroundMaterial(m_Settings.orangeLightMaterial, 0.5f));
        }
        else if (col.gameObject.CompareTag("lights_red"))
        {
            // Associate with tag "lights_red"
            // LayerMask mask = m_DefaultMask - LayerMask.GetMask("No Pass");
            // StartCoroutine(DisableRayLayer(mask, 0.5f));
            m_Profiling.IncrementRed();
            SetReward(-1f);
            StartCoroutine(SwapGroundMaterial(m_Settings.redLightMaterial, 0.5f));
            if (CompletedEpisodes > 1e3 || !m_Settings.boundedTraining)
            {
                m_CarryForwards = true;
                m_Reposition = false;
                m_Steps = StepCount;
                EndEpisode();
            }
        }
        else if (col.gameObject.CompareTag("lights_red_orange"))
        {
            // Associate with tag "lights_red_orange"
            // LayerMask mask = m_DefaultMask - LayerMask.GetMask("No Pass");
            // StartCoroutine(DisableRayLayer(mask, 0.5f));
            SetReward(-0.5f);
            m_Profiling.IncrementAmberRed();
            StartCoroutine(SwapGroundMaterial(m_Settings.orangeLightMaterial, 0.5f));
            if (CompletedEpisodes > 1e3 || !m_Settings.boundedTraining)
            {
                m_CarryForwards = true;
                m_Reposition = false;
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
            m_CarryForwards = false;
            m_Reposition = true;
            StartCoroutine(SwapGroundMaterial(m_Settings.lawBreakMaterial, 0.5f));
            m_Steps = StepCount;
            EndEpisode();
        }
        else if (col.gameObject.CompareTag("orbit") &&
            Vector3.Dot(transform.forward * 1f, col.gameObject.GetComponent<RoundaboutOrbit>().stream) <= -0f )
        {
            // Ạssociate with "wait" tag
            SetReward(-1f);
            m_Profiling.IncrementNoPass();
            m_CarryForwards = false;
            m_Reposition = true;
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
            m_CarryForwards = false;
            m_Reposition = true;
            if (!col.transform.parent.gameObject.CompareTag("intersection_component") )
            {
                // var new_lane = col.transform.parent.parent.parent.gameObject;
                // m_AgentLane = new_lane.GetComponent<CrossroadLane>();
                //
                m_AgentLaneId = m_AgentLane.transform.GetSiblingIndex();
            }
            StartCoroutine(SwapGroundMaterial(m_Settings.lawBreakMaterial, 0.5f));
            m_Steps = StepCount;
            EndEpisode();
        }
        else if (col.gameObject.CompareTag("agent"))
        {
            // Associate with "agent" tag
            SetReward(-1f);
            m_Profiling.IncrementAgentCol();
            m_CarryForwards = false;
            m_Reposition = true;
            StartCoroutine(SwapGroundMaterial(m_Settings.lawBreakMaterial, 0.5f));
            m_Steps = StepCount;
            EndEpisode();
        }
        else if (col.gameObject.CompareTag("lane"))
        {
            m_CurrentLane = col.transform.parent.parent.GetComponent<CrossroadLane>();
            if (m_CurrentLane != m_TargetLane && m_Settings.isTraining)
            {
                m_CarryForwards = false;
                m_Reposition = true;
                m_Steps = StepCount;
                EndEpisode();
            }
        }
        if (col.gameObject.CompareTag("transport") && col.transform.root != area.transform)
        {
            if (m_Settings.connectLanes)
            {
                area = col.transform.root.gameObject;
                ground = col.transform.root.GetChild(0).gameObject;

                // root = this.transform.root;
                string rootpath = "/" + area.transform.name;
                // Debug.Log(rootpath);

                var scene = GameObject.Find(rootpath + "/Scene");
                m_Scene = scene.GetComponent<CrossroadScene>();

                var new_lane = col.transform.parent.parent.gameObject;
                m_AgentLane = new_lane.GetComponent<CrossroadLane>();
                m_AgentLaneId = m_AgentLane.transform.GetSiblingIndex();

                m_CarryForwards = true;
                m_Reposition = false;
            }
            else
            {
                m_CarryForwards = false;
                m_Reposition = true;
            }
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
        // else if (Input.GetKey(KeyCode.LeftArrow))
        // {
        //     Debug.Log("Look Left");
        //     discreteActionsOut[0] = 5;
        // }
        // else if (Input.GetKey(KeyCode.RightArrow))
        // {
        //     Debug.Log("Look Right");
        //     discreteActionsOut[0] = 6;
        // }
    }

    void ResetOffsets()
    {
        if (!m_Settings.boundedTraining){
            m_AgentOffset = Random.Range(- m_AgentLane.offset * 0.85f, -m_AgentLane.offset * 0.4f);
            m_TargetOffset = Random.Range(m_TargetLane.offset * 0.4f, m_TargetLane.offset * 0.9f);
        }
        else
        {
            // Debug.Log(m_AgentLane.offset + " " + m_AgentLane.length);
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
        forw = 0;
        back = 0;
        right = 0;
        left = 0;

        m_Profiling.WriteEpStatistics(CompletedEpisodes, m_Steps);

        m_Scene.episode++;
        episode = CompletedEpisodes;

        var current_position = transform.position - ground.transform.position;

        if (!m_Settings.connectLanes)
        {
            transform.position -= 1000f * Vector3.up;
            m_Reposition = true;
            m_CarryForwards = false;
        }

        if (!m_CarryForwards)
        {
            m_TargetLaneId = Random.Range(0,
                m_Scene.lanes.transform.childCount - 1);

            if (m_TargetLaneId >= m_AgentLaneId)
            {
                m_TargetLaneId += 1;
            }

            // if (this.transform.parent.GetSiblingIndex()==2)
            // {
            //     m_TargetLaneId = (m_AgentLaneId + 3) % 4;
            // }

            m_TargetLane = m_Scene.lanes.transform.GetChild(m_TargetLaneId).GetComponent<CrossroadLane>();
        }

        // Debug.Log(m_TargetLane.direction + " " + targetLaneId);

        // m_AgentCollider.GetCollider();
        // m_TargetCollider.GetCollider();
        // Debug.Log(m_TargetCollider.occupied);

        // m_AgentLane = m_AgentCollider.lane;
        // if (m_TargetCollider.occupied)
        // {
        //     // Debug.Log(m_TargetCollider.lane);
        //     m_TargetLane = m_TargetCollider.lane;
        // }
        // else
        // {
        //     Debug.Logm_AgentOffset("episode: " + m_Scene.episode + "agent: " + this.name + "target offset" + m_TargetOffset);
        // }

        // Debug.Log("episode: " + m_Scene.episode + ", lane: " + m_AgentLane
        //     + " target lane: " + m_TargetLane);

        // Debug.Log("episode: " + m_Scene.episode + ", agent lane length: " + m_AgentLane.length
        //     + " target lane length: " + m_TargetLane.length);
        //
        // Debug.Log("episode: " + m_Scene.episode + ", agent offset: " + m_AgentOffset
        //     + " target offset: " + m_TargetOffset);

        if (!m_CarryForwards)
        {
            ResetOffsets();
        }
        else
        {
            // m_TargetOffset = m_TargetLane.offset * 0.99f;
            m_TargetOffset = 50f * 0.99f;
            m_CarryForwards = false;
        }

        // if (CompletedEpisodes < 3e2  && m_Sett-restrictedDirings.boundedTraining)
        // {
        //     m_AgentOffset += CompletedEpisodes/3e1f - 7f;
        // }

        if (m_Reposition)
        {
            m_RandOffset = m_AgentOffset + Random.Range(-4f, 0f);
            transform.position = m_AgentLane.centreline
                + Random.Range(-1.5f, 1.5f) * m_AgentLane.normal
                // - 10f * m_AgentLane.direction
                + m_RandOffset * m_AgentLane.direction
                // + 0f * Vector3.up
                + current_position.y * Vector3.up
                + ground.transform.position;

            var orientation_a = Mathf.Atan2( m_AgentLane.direction.x,
                m_AgentLane.direction.z) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, orientation_a, 0f);
            m_AgentRb.velocity = 1f * m_AgentLane.direction;

            m_Reposition = false;
        }

        rotateDir = transform.forward;
        motionDir = transform.forward;

        // if (CompletedEpisodes < 3e2  && m_Settings.boundedTraining)
        // {
        //     m_TargetOffset += CompletedEpisodes/3e1f - 10f;
        // }
        target.transform.position = m_TargetLane.targetline
            - m_TargetOffset * m_TargetLane.direction
            // - 10f * m_TargetLane.direction
            - 0.012f*Vector3.up
            + ground.transform.position;
        var orientation_t = Mathf.Atan2( m_TargetLane.direction.x,
            m_TargetLane.direction.z) * Mathf.Rad2Deg;
        target.transform.rotation = Quaternion.Euler(0f, orientation_t, 0f);

        // Debug.Log("episode: " + m_Scene.episode + ", agent offset: "
        //     + m_AgentOffset + " rand offset: " + m_RandOffset);
    }
}
