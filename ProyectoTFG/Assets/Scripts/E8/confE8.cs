using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using System.Collections.Generic;
using System.Collections;

//Para usar .All
using System.Linq;

public class confE8 : Agent
{ 

    //////////////////Atributos//////////////////

[SerializeField]
private bool Training = true;
[SerializeField]
private GameObject MyKey;
[SerializeField]
private List<GameObject> KeyList = new List<GameObject>();
[SerializeField]
public List<GameObject> TargetList = new List<GameObject>();
[SerializeField]
private GameObject Ground;
[SerializeField]
private GameObject Door; 

private AgentSettings M_AgentSettings; //Ajustes
Bounds areaBounds;
Rigidbody rb;
bool IHaveAKey = false;
bool InRoom;

List<GameObject> TargetListCopia;
List<int> IteminRoomList = new List<int>(); //Lista de items donde 0 Significa fuera y 1 significa dentro (inRoom)

//////////////////Funciones//////////////////


    
    public override void Initialize()
    {
        M_AgentSettings = FindObjectOfType<AgentSettings>();
        rb = GetComponent<Rigidbody>();
        MyKey.SetActive(false);
        IHaveAKey = false;

        TargetListCopia = new List<GameObject>(TargetList);

        //MaxStep forma parte de la clase Agent
        if (!Training) MaxStep = 0; //Si no está entrenando no hay pasos de entrenamiento
    }

    public override void OnEpisodeBegin()
    {
        
        //Reseteo propiedades
        MyKey.SetActive(false);
        IHaveAKey = false;

        //Evaluacion episodio anterior

        if(TargetList.Count !=0){
            AddReward(-100f);
        }

        //Recarga de la lista de items y puertas, ya que se irán eliminando
        TargetList.Clear();
        TargetList.AddRange(TargetListCopia);

        foreach(var it in TargetList){
            it.SetActive(true);
        }

        //Se reparten las llaves por el área
         foreach(var key in KeyList){
            key.transform.position = GetRandomSpawnPos();
            key.SetActive(true);
         }

        

        //Reseteo Agente
        transform.position = GetRandomSpawnPos(); 
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        //ReseteoItem

        IteminRoomList.Clear(); //Al principio no sabemos qué items están dentro o fuera

        foreach(var item in TargetList){ //Para cada item de la lista de items a obtener
            Collider col_item = item.GetComponent<Collider>();
            item.transform.position = GetRandomSpawnPosItem(); //Colocamos aleatoriamente los items

            InRoom = Physics.CheckBox(item.transform.position, new Vector3(2.5f, 0.01f, 2.5f), transform.rotation, LayerMask.GetMask("Rooml"));

            if(InRoom == true){
                IteminRoomList.Add(1); //Dentro
            }
            else{
                IteminRoomList.Add(0); //Fuera
            }
        }
        //Tras este bucle tendremos una lista IteminRoomList con un valor 0 o 1 en cada casilla correspondiente a cada Item (Coge el mismo orden que TargetList)

        Door.SetActive(true);
    }


    public Vector3 GetRandomSpawnPos() //Coordenadas disponibles
    {
        areaBounds = Ground.GetComponent<Collider>().bounds;
        var foundNewSpawnLocation = false;
        var randomSpawnPos = Vector3.zero;
        while (foundNewSpawnLocation == false) //Mientras no se haya encontrado una posición
        {
            var randomPosX = Random.Range(-areaBounds.extents.x * M_AgentSettings.SpawnAreaMarginMultiplier,
                areaBounds.extents.x * M_AgentSettings.SpawnAreaMarginMultiplier); //Se saca una posición x dentro del límite del entorno

            var randomPosZ = Random.Range(-areaBounds.extents.z * M_AgentSettings.SpawnAreaMarginMultiplier,
                areaBounds.extents.z * M_AgentSettings.SpawnAreaMarginMultiplier); //Se saca una posición z dentro del límite del entorno

            randomSpawnPos = Ground.transform.position + new Vector3(randomPosX, 1f, randomPosZ); //Ubicamos la posición en base al suelo
            if (Physics.CheckBox(randomSpawnPos, new Vector3(2.5f, 0.01f, 2.5f)) == false) //Si no hay colisiones con el objeto cuyas dimensiones se indica en Vector3
            {
                foundNewSpawnLocation = true;
            }
        }
        return randomSpawnPos;
    }

    public Vector3 GetRandomSpawnPosItem() //Coordenadas disponibles
    {   
        int layerMask = ~(1 << 6); //Máscara para la etiqueta nº6 (Rooml)
        areaBounds = Ground.GetComponent<Collider>().bounds;
        var foundNewSpawnLocation = false;
        var randomSpawnPos = Vector3.zero;
        while (foundNewSpawnLocation == false)
        {
            var randomPosX = Random.Range(-areaBounds.extents.x * M_AgentSettings.SpawnAreaMarginMultiplier,
                areaBounds.extents.x * M_AgentSettings.SpawnAreaMarginMultiplier); //Se saca una posición x dentro del límite del entorno

            var randomPosZ = Random.Range(-areaBounds.extents.z * M_AgentSettings.SpawnAreaMarginMultiplier,
                areaBounds.extents.z * M_AgentSettings.SpawnAreaMarginMultiplier); //Se saca una posición z dentro del límite del entorno

            randomSpawnPos = Ground.transform.position + new Vector3(randomPosX, 1f, randomPosZ); //Ubicamos la posición en base al suelo
            if (Physics.CheckBox(randomSpawnPos, new Vector3(2.5f, 0.01f, 2.5f), transform.rotation, layerMask) == false) //Si no hay colisiones IGNORANDO a los objetos de etiqueta 6.
            {
                foundNewSpawnLocation = true;
            }
        }
        return randomSpawnPos;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(IHaveAKey);
        sensor.AddObservation(IteminRoomList.ConvertAll(n => (float)n)); //Tiene tantos atributos como elementos/items haya en la lista
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

        if (Training) AddReward(-1f / MaxStep); //Penaliza por cada paso que da
    }

    void OnTriggerEnter(Collider col)
    { 
        if (col.CompareTag("Key"))
        {   
            if(IHaveAKey){ //Si ya tengo una llave
                if(Training) AddReward(-20);
            }

            else{
                if(Training){
                    if((IteminRoomList.All(n => (n == 0))) || IHaveAKey == false){ //Si no hay ningun item en una habitacion (0) o ya se tiene una llave en poseción
                        AddReward(-20f); //No es necesaria la llave o ya tienes una acaparador
                    }

                    else{
                        AddReward(20f);
                    }
                }
            }
            
            if(MyKey.activeSelf != true) MyKey.SetActive(true); //Si no tengo ya la animacion de la llave en el agente entonces se la pongo
            if(IHaveAKey != true) IHaveAKey = true; //Si no tenía ninguna llave entonces ahora si indico que al menos tengo una
            col.gameObject.SetActive(false);
        }

        if (col.CompareTag("Door"))
        {
            if (IHaveAKey) //Si tengo una llave al menos
            {   
                if(Training){
                    if((IteminRoomList.All(n => (n == 0))) || IHaveAKey == false){ //Si no hay ningun item en una habitacion (0) o ya se tiene una llave en poseción
                        AddReward(-30f); //No es necesaria la llave o ya tienes una acaparador
                    }

                    else{
                        AddReward(30f);
                    }
                }

                //Ya no tengo llave
                MyKey.SetActive(false);
                IHaveAKey = false; 
                
                col.gameObject.SetActive(false);
            }

            else{
                if(Training) AddReward(-30);
            }
        }

        if (col.CompareTag("Item"))
        {

            AddReward(30f);
            IteminRoomList.RemoveAt(TargetList.IndexOf(col.gameObject)); //Descarto el valor correspondiente al item que se ha eliminado
            TargetList.Remove(col.gameObject); //Descarto el item que se ha eliminado
            col.gameObject.SetActive(false);

            if(TargetList.Count == 0){ //Si obtenemos todos los items
                AddReward(50f); //enhorabuena, has ganado
                EndEpisode(); //SE FINALIZA
            }
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
