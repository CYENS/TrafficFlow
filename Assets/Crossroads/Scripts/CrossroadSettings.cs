using UnityEngine;

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
    public bool recordData = false;
}
