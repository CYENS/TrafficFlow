using UnityEngine;
using System.IO;

public class CrossroadSettings : MonoBehaviour
{
    public float agentRunSpeed;
    public float agentRotationSpeed;
    public Material goalScoredMaterial; // when a goal is scored the ground will use this material for a few seconds.
    public Material redLightMaterial; // when a red light is crossed, the ground will use this material for a few seconds.
    public Material greenLightMaterial; // when a green light is crossed, the ground will use this material for a few seconds.
    public Material orangeLightMaterial; // when an orange light is crossed, the ground will use this material for a few seconds.
    public Material lawBreakMaterial; // when breaking the law, the ground will use this material for a few seconds.
    [HideInInspector]
    public float deviate = 0.1f;

    public bool isTraining = false;
    public bool verbose = false;

    public bool writeResults = false;
    string m_ScoresFile;
    string m_SpeedProfileFile;
    string m_InfoFile;
    string m_DirectoryPath;
    bool m_NamedFile = false;

    public void createResultsFiles(){

        byte[] empty = new byte[0];

        m_DirectoryPath = "Assets/Data/Run" +
            System.DateTime.Now.ToString("yyMMdd");

        if (!Directory.Exists(m_DirectoryPath)) {
            Directory.CreateDirectory(m_DirectoryPath);
            m_ScoresFile = m_DirectoryPath + "/scores.txt";
            m_SpeedProfileFile = m_DirectoryPath + "/speed.txt";
            m_InfoFile = m_DirectoryPath + "/info.txt";
            File.WriteAllBytes(m_ScoresFile, empty);
            File.WriteAllBytes(m_SpeedProfileFile, empty);
            File.WriteAllBytes(m_InfoFile, empty);
        }
        else {
            int counter = 1;
            string update_dir_name;
            update_dir_name = m_DirectoryPath + "(" + counter.ToString() + ")";
            while (Directory.Exists(update_dir_name)) {
                counter++;
                update_dir_name = m_DirectoryPath + "(" + counter.ToString() + ")";
            }
            m_DirectoryPath = update_dir_name;
            Directory.CreateDirectory(m_DirectoryPath);
            m_ScoresFile = m_DirectoryPath + "/scores.txt";
            m_SpeedProfileFile = m_DirectoryPath + "/speed.txt";
            m_InfoFile = m_DirectoryPath + "/info.txt";
            File.WriteAllBytes(m_ScoresFile, empty);
            File.WriteAllBytes(m_SpeedProfileFile, empty);
            File.WriteAllBytes(m_InfoFile, empty);
        }
    }

    public string[] getResultsFileName(){
        if (!m_NamedFile) {
            m_NamedFile = true;
            createResultsFiles();
            StreamWriter writer = new StreamWriter(m_ScoresFile, true);
            writer.WriteLine("scene_id agent_id completed_episodes green amber red amber_red collision wall not_permitted target step_count");
            writer.Close();

            writer = new StreamWriter(m_SpeedProfileFile, true);
            writer.WriteLine("sceneID agentID episode instance speed speed**2");
            writer.Close();

            var a_scene = GameObject.Find("/CrossroadsArea");
            if (!a_scene){
                a_scene = GameObject.Find("/RoundaboutArea");
            }
            if (!a_scene){
                a_scene = GameObject.Find("/CrossroadsArea (curved)");
            }
            if (!a_scene){
                a_scene = GameObject.Find("/CrossroadsArea (narrow)");
            }
            var agentCount = 0;
            string agentModel = " ";
            foreach(Transform child in a_scene.transform)
            {
                if (child.gameObject.activeSelf && child.childCount > 0 && child.GetChild(0).tag == "agent")
                {
                    agentCount++;
                    if (agentCount == 1)
                        agentModel = child.GetChild(0).GetComponent<Unity.MLAgents.Policies.BehaviorParameters>().Model.name;
                }
            }
            writer = new StreamWriter(m_InfoFile, true);
            writer.WriteLine("model " + agentModel);
            writer.WriteLine(agentCount + " agents in the scene");
            writer.Close();
        }
        string []arr = new string[2];
        arr[0] = m_ScoresFile;
        arr[1] = m_SpeedProfileFile;
        return arr;
    }
}
