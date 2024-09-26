using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using System.Collections.Generic;
using System.Collections;

//Para usar .All
using System.Linq;

public class confE10 : Agent
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

private AgentSettings M_AgentSettings; //Ajustes
Bounds areaBounds;
Rigidbody rb;
bool IHaveAKey = false;
bool InRoom;


//Los items, las habitaciones a las que corresponden, las habitaciones en si, y las puertas, 
//van relacionadas con el orden en el que aparecen en las listas

[SerializeField]
public List<GameObject> DoorList = new List<GameObject>(); //La posicion de cada puerta corresponde a la posicion de cada habitacion de la que pertenece
[SerializeField]
public List<GameObject> RoomList = new List<GameObject>(); //Solo se utiliza para saber que items estan en que habitaciones

List<GameObject> TargetListCopia;
List<Collider> CollRoomList = new List<Collider>(); //Lista de colliders de cada habitacion
List<int> IteminRoomsList = new List<int>(); //Distinto a E8: Lista de items que se han detectado que estan en habitaciones y no en el pasillo
//////////////////Funciones//////////////////
    
    public override void Initialize()
    {
        M_AgentSettings = FindObjectOfType<AgentSettings>();
        rb = GetComponent<Rigidbody>();
        MyKey.SetActive(false);
        IHaveAKey = false;

        //Añadimos los colliders de cada habitacion
        foreach(var room in RoomList){
            CollRoomList.Add(room.GetComponent<Collider>());
        }

        TargetListCopia = new List<GameObject>(TargetList);

        if (!Training) MaxStep = 0;
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

        //Reseteo llave
        foreach(var it in TargetList){
            it.SetActive(true);
        }

        //Se reparten las llaves por el area
         foreach(var key in KeyList){
            key.transform.position = GetRandomSpawnPos();
            key.SetActive(true);
         }

        //Reseteo Agente
        transform.position = GetRandomSpawnPos(); 
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        //ReseteoItem
        IteminRoomsList.Clear(); //Al principio no sabemos que items estan en habitaciones

        foreach(var item in TargetList){ //Para cada item de la lista de items a obtener
            Collider col_item = item.GetComponent<Collider>();
            item.transform.position = GetRandomSpawnPosItem(); //Colocamos aleatoriamente los items

            foreach(var col in CollRoomList){ //Para cada collider de las habitaciones
                if(col_item.bounds.Intersects(col.bounds)){ //Si el collider del item actual esta en contacto con la habitacion actual
                    IteminRoomsList.Add(CollRoomList.IndexOf(col)); //[item1,item2.....itemn],donde el valor de cada uno de la habitacion a la que corresponden
                    //Importante mencionar que esto es así porque el orden de las habitaciones en la lista de habitaciones y el orden de los colliders de estos es el mismo
                    break;
                }

                else if(CollRoomList.IndexOf(col) == (CollRoomList.Count - 1)){ //Si ha llegado a la ultima habitacion y no ha sido detectada en una
                    IteminRoomsList.Add(-1); //Esta en el pasillo principal
                }
            }
            //Con este foreach ahora disponemos de una lista que nos indicara las habitaciones en las que estan los items en el episodio actual
         }

        //Activacion de las puertas
        foreach(var door in DoorList){
            door.SetActive(true);
        }
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
                areaBounds.extents.x * M_AgentSettings.SpawnAreaMarginMultiplier); //Se saca una posicion x dentro del limite del entorno

            var randomPosZ = Random.Range(-areaBounds.extents.z * M_AgentSettings.SpawnAreaMarginMultiplier,
                areaBounds.extents.z * M_AgentSettings.SpawnAreaMarginMultiplier); //Se saca una posicion z dentro del limite del entorno

            randomSpawnPos = Ground.transform.position + new Vector3(randomPosX, 1f, randomPosZ); //Ubicamos la posicion en base al suelo
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
        sensor.AddObservation(IteminRoomsList.ConvertAll(n => (float)n)); //Tiene tantos atributos como elementos/items haya en la lista
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
        //Si el agente se encuentra la llave, este la coge
        if (col.CompareTag("key"))
        {
            if((IteminRoomsList.All(n => (n == -1))) || IHaveAKey == false){ //Si no hay ningun item en una habitacion o ya se tiene una llave en poseción
                 AddReward(-20f); //No es necesaria la llave o ya tienes una acaparador
            }

            else{
                AddReward(20f);
            }

            if(MyKey.activeSelf != true) MyKey.SetActive(true); //Si no tengo ya la animacion de la llave en el agente ent0onces se la pongo
            if(IHaveAKey != true) IHaveAKey = true; //Si no tenía ninguna llave entonces ahora si indico que al menos tengo una
            col.gameObject.SetActive(false);
        }

        if (col.CompareTag("door"))
        {
            if (IHaveAKey) //Si tengo una llave al menos
            {   
                //Puerta 0..4
                if(IteminRoomsList.All(n => (n != DoorList.IndexOf(col.gameObject)))){ //Si ningun item esta en la habitacion que le corresponde
                 AddReward(-30f); //No es necesaria la llave 
                }

                else{
                 AddReward(30f);
                }

                //Ya no tengo llaves
                MyKey.SetActive(false);
                IHaveAKey = false; 
                    
                col.gameObject.SetActive(false);
            }
        }

        if (col.CompareTag("Item"))
        {

            AddReward(30f);

            //Lo descartamos de las listas
            IteminRoomsList.RemoveAt(TargetList.IndexOf(col.gameObject));
            TargetList.Remove(col.gameObject);
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
