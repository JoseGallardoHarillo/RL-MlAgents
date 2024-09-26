using UnityEngine;

public class AgentSettings : MonoBehaviour
{

[Header("Velocidad")]
[SerializeField]
public float AgentRunSpeed; //Velocidad del agente

[Header("Velocidad de giro")]
[SerializeField]
public float AgentRotationSpeed; //Velocidad de rotación del agente

[Header("Factor multiplicador de spawn")]
[SerializeField]
public float SpawnAreaMarginMultiplier; //Factor multiplicador de spawn

}
