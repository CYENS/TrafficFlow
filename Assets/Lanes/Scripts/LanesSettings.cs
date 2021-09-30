using UnityEngine;

public class LanesSettings : MonoBehaviour
{
    public float agentRunSpeed;
    public float agentRotationSpeed;
    public Material goalScoredMaterial; // when a goal is scored the ground will use this material for a few seconds.
    public Material redLightMaterial; // when fail, the ground will use this material for a few seconds.
    public Material greenLightMaterial; // when fail, the ground will use this material for a few seconds.
    public Material orangeLightMaterial; // when fail, the ground will use this material for a few seconds.
    [HideInInspector]
    public float deviate;
}
