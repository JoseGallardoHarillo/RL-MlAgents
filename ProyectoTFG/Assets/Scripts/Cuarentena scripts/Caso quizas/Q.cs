using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using System.Collections;

public class Q : Agent
{   
    [HideInInspector]
    public Bounds areaBounds;

    public bool _training = true;

    public GameObject MyKey; //Gráficamente aparecerá cuando se obtenga la llave por parte del agente
    public GameObject Key; //La llave
    public GameObject Target;
    public GameObject Ground;
    public GameObject Door;

    public bool IHaveAKey; //Indica si el agente tiene la llave

    private AgentSettings m_AgentSettings; //Ajustes
    private Rigidbody m_AgentRb;
    //private VerificadorItem ver_item;

    bool InRoom;

    
    public override void Initialize()
    {
        m_AgentSettings = FindObjectOfType<AgentSettings>();
        m_AgentRb = GetComponent<Rigidbody>();
        //ver_item = Target.GetComponent<VerificadorItem>();
        MyKey.SetActive(false);
        IHaveAKey = false;

        //MaxStep forma parte de la clase Agent
        if (!_training) MaxStep = 0; //Si no está entrenando no hay pasos de entrenamiento
    }

    public override void OnEpisodeBegin()
    {
        //Reseteo propiedades
        MyKey.SetActive(false);
        IHaveAKey = false;

        //Reseteo llave
         Key.transform.position = GetRandomSpawnPos(Ground);
         Key.SetActive(true);

        

        //Reseteo Agente
        transform.position = GetRandomSpawnPos(Ground); 
        m_AgentRb.velocity = Vector3.zero;
        m_AgentRb.angularVelocity = Vector3.zero;

        //ReseteoItem

         Target.transform.position = GetRandomSpawnPosItem(Ground);

         InRoom = Physics.CheckBox(Target.transform.position, new Vector3(2.5f, 0.01f, 2.5f), transform.rotation, LayerMask.GetMask("Rooml"));


        Door.SetActive(true);
        //InRoom = ver_item.IsinRoom();
    }

    public Vector3 GetRandomSpawnPos(GameObject Base) //Coordenadas disponibles
    {   
        areaBounds = Base.GetComponent<Collider>().bounds;
        var foundNewSpawnLocation = false;
        var randomSpawnPos = Vector3.zero;
        while (foundNewSpawnLocation == false)
        {
            var randomPosX = Random.Range(-areaBounds.extents.x * m_AgentSettings.SpawnAreaMarginMultiplier,
                areaBounds.extents.x * m_AgentSettings.SpawnAreaMarginMultiplier);

            var randomPosZ = Random.Range(-areaBounds.extents.z * m_AgentSettings.SpawnAreaMarginMultiplier,
                areaBounds.extents.z * m_AgentSettings.SpawnAreaMarginMultiplier);
            randomSpawnPos = Base.transform.position + new Vector3(randomPosX, 1f, randomPosZ);
            if (Physics.CheckBox(randomSpawnPos, new Vector3(2.5f, 0.01f, 2.5f)) == false)
            {
                foundNewSpawnLocation = true;
            }
        }
        return randomSpawnPos;
    }

    public Vector3 GetRandomSpawnPosItem(GameObject Base) //Coordenadas disponibles
    {   
        int layerMask = ~(1 << 6);
        areaBounds = Base.GetComponent<Collider>().bounds;
        var foundNewSpawnLocation = false;
        var randomSpawnPos = Vector3.zero;
        while (foundNewSpawnLocation == false)
        {
            var randomPosX = Random.Range(-areaBounds.extents.x * m_AgentSettings.SpawnAreaMarginMultiplier,
                areaBounds.extents.x * m_AgentSettings.SpawnAreaMarginMultiplier);

            var randomPosZ = Random.Range(-areaBounds.extents.z * m_AgentSettings.SpawnAreaMarginMultiplier,
                areaBounds.extents.z * m_AgentSettings.SpawnAreaMarginMultiplier);
            randomSpawnPos = Base.transform.position + new Vector3(randomPosX, 1f, randomPosZ);
            if (Physics.CheckBox(randomSpawnPos, new Vector3(2.5f, 0.01f, 2.5f), transform.rotation, layerMask) == false)
            {
                foundNewSpawnLocation = true;
            }
        }
        return randomSpawnPos;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(IHaveAKey);
        sensor.AddObservation(InRoom);
        //Distancia a la key.

        //Float de 1 posicion.
        sensor.AddObservation(
        Vector3.Distance(Key.transform.position, transform.position));
        //Dirección al target.
        //Vector 3 posiciones. 
        sensor.AddObservation(
            (Key.transform.position - transform.position).normalized);
        
        //Distancia al item.

        //Float de 1 posicion.
        sensor.AddObservation(
        Vector3.Distance(Target.transform.position, transform.position));
        //Dirección al target.
        //Vector 3 posiciones. 
        sensor.AddObservation(
            (Target.transform.position - transform.position).normalized);


        //Vector del señor, donde mira.
        //Vector de 3 posiciones. 
        sensor.AddObservation(
            transform.forward);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {

        //COMPORTAMIENTO
        //POCICIÓN 0:
        //POCICIÓN 1: [0,1] Grio a la izquierda 

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
        m_AgentRb.MovePosition(transform.position +
            transform.forward * lForward * m_AgentSettings.AgentRunSpeed * Time.deltaTime);
        transform.Rotate(transform.up * lTurn * m_AgentSettings.AgentRotationSpeed * Time.deltaTime);
        AddReward(-1f / MaxStep); //Penaliza por cada paso que da
    }

    void OnTriggerEnter(Collider col)
    {//print("ON TRIGGER ENTER"); 
        //Si el agente se encuentra la llave, este la coge
        //print(InRoom);
        if (col.CompareTag("key"))
        {

            if(InRoom ==true){ //Si esta verdaderamente en la habitacion
                AddReward(20f);
                MyKey.SetActive(true);
                IHaveAKey = true;
                col.gameObject.SetActive(false);
            }
            else{
                AddReward(-20f);
                MyKey.SetActive(true);
                IHaveAKey = true;
                col.gameObject.SetActive(false);
            }
            //print("LLAVEE");
            
        }

        if (col.CompareTag("door"))
        {
            if (IHaveAKey) //Se abre la puerta?
            {
                if(InRoom == true){
                    //print("GANASTE!!!!!!!!!!!!!!!!!");
                    MyKey.SetActive(false);
                    IHaveAKey = false;
                    AddReward(30f);
                    Door.SetActive(false);
                }

                else{
                    //print("GANASTE!!!!!!!!!!!!!!!!!");
                    MyKey.SetActive(false);
                    IHaveAKey = false;
                    AddReward(-30f);
                    Door.SetActive(false);
                }
                
            }
        }

        if (col.CompareTag("Item"))
        {
            AddReward(50f);
            //ver_item.reinicio();
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
