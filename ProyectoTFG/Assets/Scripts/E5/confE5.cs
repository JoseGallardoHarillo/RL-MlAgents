using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using System.Collections;

public class confE5 : Agent
{
    //////////////////Atributos//////////////////

[SerializeField]
private bool Training = true;
[SerializeField]
private GameObject MyKey; //Gráficamente aparecerá cuando se obtenga la llave por parte del agente
[SerializeField]
private GameObject Key;
[SerializeField]
private GameObject Target;
[SerializeField]
private GameObject Room; 
[SerializeField]
private GameObject Ground;
[SerializeField]
private GameObject Door; 

private AgentSettings M_AgentSettings; //Ajustes
Bounds areaBounds;
Rigidbody rb;
bool IHaveAKey = false; //Indica si el agente tiene la llave

//////////////////Funciones//////////////////
    
    public override void Initialize()
    {
        M_AgentSettings = FindObjectOfType<AgentSettings>();
        rb = GetComponent<Rigidbody>();
        MyKey.SetActive(false);
        IHaveAKey = false;

        //MaxStep forma parte de la clase Agent
        if (!Training) MaxStep = 0; //Si no está entrenando no hay pasos de entrenamiento
    }

    public override void OnEpisodeBegin()
    {
        //Reseteo propiedades
        MyKey.SetActive(false);
        IHaveAKey = false;

        //Reseteo llave
         Key.transform.position = GetRandomSpawnPos(Ground);
         Key.SetActive(true);

        //Reseteo Item
         Target.transform.position = GetRandomSpawnPos(Room);

        //Reseteo Agente
        transform.position = GetRandomSpawnPos(Ground); 
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        Door.SetActive(true);
    }

    public Vector3 GetRandomSpawnPos(GameObject Base) //Coordenadas disponibles
    {   
        areaBounds = Base.GetComponent<Collider>().bounds;
        var foundNewSpawnLocation = false;
        var randomSpawnPos = Vector3.zero;
        while (foundNewSpawnLocation == false) //Mientras no se haya encontrado una posicion
        {
            var randomPosX = Random.Range(-areaBounds.extents.x * M_AgentSettings.SpawnAreaMarginMultiplier,
                areaBounds.extents.x * M_AgentSettings.SpawnAreaMarginMultiplier); //Se saca una posicion x dentro del limite del entorno

            var randomPosZ = Random.Range(-areaBounds.extents.z * M_AgentSettings.SpawnAreaMarginMultiplier,
                areaBounds.extents.z * M_AgentSettings.SpawnAreaMarginMultiplier); //Se saca una posicion z dentro del limite del entorno

            randomSpawnPos = Base.transform.position + new Vector3(randomPosX, 1f, randomPosZ); //Ubicamos la posicion en base al suelo
            if (Physics.CheckBox(randomSpawnPos, new Vector3(2.5f, 0.01f, 2.5f)) == false) //Si no hay colisiones con el objeto cuyas dimensiones se indica en Vector3
            {
                foundNewSpawnLocation = true;
            }
        }
        return randomSpawnPos;
    }

    public override void OnActionReceived(ActionBuffers actions)
    {

        float lForward = actions.DiscreteActions[0];

        float lTurn = 0;

        if (actions.DiscreteActions[1] == 1)
        {
            lTurn = -1;
        }
        else if (actions.DiscreteActions[1] == 2)
        {
            lTurn = 1;
        }
        rb.MovePosition(transform.position +
            transform.forward * lForward * M_AgentSettings.AgentRunSpeed * Time.deltaTime);
        transform.Rotate(transform.up * lTurn * M_AgentSettings.AgentRotationSpeed * Time.deltaTime);

        if(Training) AddReward(-1f / MaxStep); //Penaliza por cada paso que da
    }

    void OnTriggerEnter(Collider col)
    {
        //Si el agente se encuentra la llave, este la coge
        if (col.CompareTag("Key"))
        {
            if(Training) AddReward(20f);
            MyKey.SetActive(true);
            IHaveAKey = true;
            col.gameObject.SetActive(false);
        }

        if (col.CompareTag("Door"))
        {
            if (IHaveAKey) //Se abre la puerta
            {
                MyKey.SetActive(false);
                IHaveAKey = false;
                if(Training) AddReward(30f);
                Door.SetActive(false);
            }
        }

        if (col.CompareTag("Item"))
        {
            if(Training) AddReward(50f);
            EndEpisode();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
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
}
