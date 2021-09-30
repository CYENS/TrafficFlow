using System.Collections;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Policies;
using System.IO;

public class CrossroadCarAgent : Agent
{
    public GameObject ground;
    public GameObject area;
    public GameObject target;
    public bool useVectorObs;

    int m_Score_g;
    int m_Score_a;
    int m_Score_r;
    int m_Score_ar;
    int m_Score_col;
    int m_Score_w;
    int m_Score_np;
    int m_Score_t;
    int m_Step;

    public string path;

    int m_AgentID;
    int m_SceneID;

    Rigidbody m_AgentRb;
    Material m_GroundMaterial;
    Renderer m_GroundRenderer;
    CrossroadSettings m_Settings;
    CrossroadScene m_Scene;
    CrossroadLights m_LightControl;
    CrossroadColliders m_TargetCollider;
    CrossroadColliders m_AgentCollider;

    CrossroadLane m_AgentLane;
    // CrossroadLane m_CurrentLane;
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

    // Vector3 m_Direction;
    // Vector3 m_Normal;

    public float flag;
    public int episode;

    public override void Initialize()
    {
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
        // m_CurrentLane = InitLane.GetComponent<CrossroadLane>();
        m_TargetLane = EndLane.GetComponent<CrossroadLane>();

        // m_Direction = m_CurrentLane.direction;
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

        m_AgentID = this.transform.parent.GetSiblingIndex();
        m_SceneID = this.transform.parent.parent.GetSiblingIndex();

        // m_AgentLane.SetDescriptors();
        // m_TargetLane.SetDescriptors();

        // m_AgentLane.SetTrainingBounds();

        m_AgentOffset = 0f;
        m_TargetOffset = 0f;

        m_Deviate = 0.1f;

        m_AgentLaneId = m_AgentLane.transform.GetSiblingIndex();

        m_Raysensor =
        transform.GetComponent<RayPerceptionSensorComponentBase>();
        m_DefaultMask = LayerMask.GetMask("Default", "Ground",
            "Agent", "Traffic Lights", "Lane", "Target", "No Pass");

        path =  "Assets/Data/scores.txt";
        m_Score_g = 0;
        m_Score_a = 0;
        m_Score_r = 0;
        m_Score_ar = 0;
        m_Score_col = 0;
        m_Score_w = 0;
        m_Score_np = 0;
        m_Score_t = 0;
        m_Step=0;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (useVectorObs)
        {
            // sensor.AddObservation(StepCount / (float)MaxStep);
            var dir = m_TargetTr.position - m_AgentTr.position;
            // var sine = Vector3.Cross(dir.normalized, m_AgentTr.forward);
            // var cosine = Vector3.Dot(dir.normalized, m_AgentTr.forward);
            // var sine = Vector3.Cross(m_Direction,
            //     - m_TargetLane.direction);
            // var cosine = Vector3.Dot(m_Direction,
            //     - m_TargetLane.direction);
            //
            // var deflection = Mathf.Asin(Mathf.Max(0f, Mathf.Min(1f, sine.magnitude)));
            // if ( cosine < 0f ){
            //     deflection += Mathf.PI/2;
            // }
            // if ( Vector3.Dot(sine, Vector3.up) < 0f )
            // {
            //     deflection *= -1f;
            // }

            // sensor.AddObservation(deflection/Mathf.PI);
            // sensor.AddObservation(dir.magnitude);


            sensor.AddObservation(Vector3.Dot(dir, m_AgentLane.direction)
                + Vector3.Dot(dir, m_AgentLane.normal));
            // // sensor.AddObservation(Vector3.Dot(dir, m_Direction)
            // //     + Vector3.Dot(dir, m_Normal));
            sensor.AddObservation(Vector3.Dot(dir, m_TargetLane.direction)
                + Vector3.Dot(dir, m_TargetLane.normal));


            // var disp = m_TargetTr.position - m_AgentTr.position;
            // var dist = Mathf.Abs(Vector3.Dot(disp, m_TargetLane.direction));
            //
            // var targetLaneDisp = m_TargetTr.position - m_TargetLane.enter.transform.position;
            // var targetLaneDist = Mathf.Abs(Vector3.Dot(targetLaneDisp, m_TargetLane.direction));
            //
            // if  (dist > targetLaneDist)
            // {
            //     var partialDisp = m_TargetLane.enter.transform.position - m_AgentTr.position;
            //     var partialDist = Mathf.Abs(Vector3.Dot(partialDisp, m_TargetLane.direction));
            //
            //     dist = partialDist + targetLaneDist;
            // }

            // var along = Vector3.Dot(disp, m_TargetLane.direction);
            // var offcentre = Vector3.Dot(disp, m_TargetLane.normal);
            // var along = Vector3.Dot(disp, transform.forward);
            // var offcentre = Vector3.Dot(disp, transform.right);

            // sensor.AddObservation(dist);
            // sensor.AddObservation(along);
            // sensor.AddObservation(offcentre);

            // sensor.AddObservation(disp.x);
            // sensor.AddObservation(disp.z);

            // sensor.AddObservation(Vector3.Dot(disp, m_AgentLane.direction) + Vector3.Dot(disp, m_AgentLane.normal));
            // sensor.AddObservation(along + offcentre);
        }
    }

    IEnumerator SwapGroundMaterial(Material mat, float time)
    {
        m_GroundRenderer.material = mat;
        yield return new WaitForSeconds(time);
        m_GroundRenderer.material = m_GroundMaterial;
    }

    RayPerceptionSensorComponentBase  m_Raysensor;

    IEnumerator DisableRayLayer(LayerMask mask, float time)
    {
        m_Raysensor.RayLayerMask = mask;

        yield return new WaitForSeconds(time);

        m_Raysensor.RayLayerMask = m_DefaultMask;

    }

    float m_theta = 20f;
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
                break;
            case 1:
                dirToGo = transform.forward * 1f;
                rotateTowards =
                    Vector3.RotateTowards(transform.forward, rotateDir,
                    Time.deltaTime * m_Settings.agentRotationSpeed, 0f);
                break;
            case 2:
                dirToGo = transform.forward * -1f;
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
        m_Step+=1;
    }

    void OnCollisionEnter(Collision col)
    {
        // Debug.Log("the agent collided with" + col.gameObject.name);
        if (col.gameObject == target.transform.GetChild(0).gameObject)
        {
            // Associate with magnitude & deflecLayerMask mask = LayerMask.GetMask("wait");tion observations
            SetReward(1f);
            m_Score_t += 1;
            // StartCoroutine(SwapGroundMaterial(m_Settings.goalScoredMaterial, 0.5f));
            EndEpisode();
        }
        else if (col.gameObject.CompareTag("lights_green"))
        {
            // Associate with tag "lights_green"
            // LayerMask mask = m_DefaultMask - LayerMask.GetMask("No Pass");
            // StartCoroutine(DisableRayLayer(mask, 0.5f));

            AddReward(1f / MaxStep);
            m_Score_g += 1;
            // StartCoroutine(SwapGroundMaterial(m_Settings.greenLightMaterial, 0.5f));
        }
        else if (col.gameObject.CompareTag("lights_orange"))
        {
            // Associate with tag "lights_orange"
            // LayerMask mask = m_DefaultMask - LayerMask.GetMask("No Pass");
            // StartCoroutine(DisableRayLayer(mask, 0.5f));

            AddReward(-0.8f / MaxStep);
            m_Score_a += 1;
            // StartCoroutine(SwapGroundMaterial(m_Settings.orangeLightMaterial, 0.5f));
        }
        else if (col.gameObject.CompareTag("lights_red"))
        {
            // Associate with tag "lights_red"
            // LayerMask mask = m_DefaultMask - LayerMask.GetMask("No Pass");
            // StartCoroutine(DisableRayLayer(mask, 0.5f));

            SetReward(-1f);
            m_Score_r += 1;
            // StartCoroutine(SwapGroundMaterial(m_Settings.redLightMaterial, 0.5f));
            if (CompletedEpisodes > 1e3 || !m_Settings.isTraining)
            {
                EndEpisode();
            }
        }
        else if (col.gameObject.CompareTag("lights_red_orange"))
        {
            // Associate with tag "lights_red_orange"
            // LayerMask mask = m_DefaultMask - LayerMask.GetMask("No Pass");
            // StartCoroutine(DisableRayLayer(mask, 0.5f));

            SetReward(-0.5f);
            m_Score_ar += 1;
            // StartCoroutine(SwapGroundMaterial(m_Settings.orangeLightMaterial, 0.5f));
            if (CompletedEpisodes > 1e3 || !m_Settings.isTraining)
            {
                EndEpisode();
            }
        }
        else if (col.gameObject.CompareTag("wait") &&
            Vector3.Dot(transform.forward * 1f, col.gameObject.GetComponent<CrossroadColliders>().lane.direction) <= -0f )
        {
            // Ạssociate with "wait" tag
            SetReward(-1f);
            m_Score_np += 1;
            // StartCoroutine(SwapGroundMaterial(m_Settings.lawBreakMaterial, 0.5f));
            EndEpisode();
        }
        else if (col.gameObject.CompareTag("wall"))
        {
            // Associate with "wall" tag
            SetReward(-1f);
            m_Score_w +=1 ;
            // StartCoroutine(SwapGroundMaterial(m_Settings.lawBreakMaterial, 0.5f));
            EndEpisode();
        }
        else if (col.gameObject.CompareTag("agent"))
        {
            // Associate with "agent" tag
            SetReward(-1f);
            m_Score_col +=1;
            // StartCoroutine(SwapGroundMaterial(m_Settings.lawBreakMaterial, 0.5f));
            EndEpisode();
        }
        // else if (col.gameObject.CompareTag("lane"))
        // {
        //     var lane_root = col.transform.parent.parent;
        //     m_CurrentLane = lane_root.gameObject.GetComponent<CrossroadLane>();
        //
        //     m_Direction = m_CurrentLane.direction;
        //     m_Normal = m_CurrentLane.normal;
        //
        //     // if (m_CurrentLane.transform.GetSiblingIndex() == m_TargetLaneId)
        //     // {
        //     //     m_Direction *= -1;
        //     //     m_Normal *= -1;
        //     // }
        // }
        // else if (col.gameObject.CompareTag("crossing"))
        // {
        //     m_CurrentLane = m_Scene.lanes.transform.GetChild(m_TargetLaneId).GetComponent<CrossroadLane>();
        //
        //     m_Direction = - m_CurrentLane.direction;
        //     m_Normal = -m_CurrentLane.normal;
        // }
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
        if (m_Settings.recordData)
        {
            StreamWriter writer = new StreamWriter(path, true);
            writer.WriteLine(m_SceneID + " " + m_AgentID + " " +
                CompletedEpisodes + " " +
                m_Score_g + " " +
                m_Score_a + " " +
                m_Score_r + " " +
                m_Score_ar + " " +
                m_Score_col + " " +
                m_Score_w + " " +
                m_Score_np + " " +
                m_Score_t + " " +
                m_Step);
            writer.Close();
            m_Score_g = 0;
            m_Score_a = 0;
            m_Score_r = 0;
            m_Score_ar = 0;
            m_Score_col = 0;
            m_Score_w = 0;
            m_Score_np = 0;
            m_Score_t = 0;
            m_Step = 0;
        }


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
        //     Debug.Log("episode: " + m_Scene.episode + "agent: " + this.name + "target offset" + m_TargetOffset);
        // }

        // Debug.Log("episode: " + m_Scene.episode + ", lane: " + m_AgentLane
        //     + " target lane: " + m_TargetLane);

        // Debug.Log("episode: " + m_Scene.episode + ", agent lane length: " + m_AgentLane.length
        //     + " target lane length: " + m_TargetLane.length);
        //
        // Debug.Log("episode: " + m_Scene.episode + ", agent offset: " + m_AgentOffset
        //     + " target offset: " + m_TargetOffset);

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
