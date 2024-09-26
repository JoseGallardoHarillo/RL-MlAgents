using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class confE2: Agent
{   

//////////////////Atributos//////////////////

[SerializeField]
private Transform Target;

[SerializeField]
private GameObject Ground;

[Header("Factor multiplicador de spawn")]
[SerializeField]
[Range(0f, 1f)]
private float MulFactor;

[Header("Velocidad")]
[SerializeField]
[Range(0f, 3000f)]
public float Speed;

[Header("Velocidad de giro")]
[SerializeField]
[Range(50f, 300f)]
public float TurnSpeed;

[SerializeField]
private bool Training = true;

Bounds areaBounds;
Rigidbody rb;

//////////////////Funciones//////////////////


    public override void Initialize(){
        
        //Definimos el Rigidbody del agente y los limites del entorno
        rb = GetComponent<Rigidbody>();
        areaBounds = Ground.GetComponent<Collider>().bounds;

        //MaxStep forma parte de la clase Agent
        //Si no está entrenando los pasos son infinitos, osea, no se resetea
        if(!Training) MaxStep = 0;
    }

    public override void OnEpisodeBegin(){

        //Reseteo agente
        transform.position = GetRandomSpawnPos();
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        //Reseteo item
        Target.transform.position = GetRandomSpawnPos();
    }

    public Vector3 GetRandomSpawnPos() //Coordenadas disponibles
    {
        var foundNewSpawnLocation = false;
        var randomSpawnPos = Vector3.zero;
        while (foundNewSpawnLocation == false) //Mientras no se haya encontrado una posicion
        {
            var randomPosX = Random.Range(-areaBounds.extents.x * MulFactor,
                areaBounds.extents.x * MulFactor); //Se saca una posicion x dentro del limite del entorno

            var randomPosZ = Random.Range(-areaBounds.extents.z * MulFactor,
                areaBounds.extents.z * MulFactor); //Se saca una posicion z dentro del limite del entorno

            randomSpawnPos = Ground.transform.position + new Vector3(randomPosX, 1f, randomPosZ); //Ubicamos la posicion en base al suelo
            if (Physics.CheckBox(randomSpawnPos, new Vector3(2.5f, 0.01f, 2.5f)) == false) //Si no hay colisiones con el objeto cuyas dimensiones se indica en Vector3
            {
                foundNewSpawnLocation = true;
            }
        }
        return randomSpawnPos;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        //Distancia al target
        //Float de 1 posicion
        sensor.AddObservation(
        Vector3.Distance(Target.transform.position, transform.position));

        //Direccion al target
        //Vector 3 posiciones
        sensor.AddObservation((Target.transform.position - transform.position).normalized);

        //Vector de orientacion del agente
        //Vector de 3 posiciones
        sensor.AddObservation(transform.forward);

        //En total 7 atributos
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float lForward = actions.DiscreteActions[0]; //Esta variable determina si el agente da un paso o no

        float lTurn = 0; //Esta variable determina si el agente gira, y en caso de que si, ¿en que sentido?

        if (actions.DiscreteActions[1] == 1)
        {
            lTurn = -1;
        }
        else if (actions.DiscreteActions[1] == 2)
        {
            lTurn = 1;
        }
        rb.MovePosition(transform.position +
            transform.forward * lForward * Speed * Time.deltaTime);
        transform.Rotate(transform.up * lTurn * TurnSpeed * Time.deltaTime);
        
        if(Training) AddReward(-1f / MaxStep); //Penaliza por cada paso que da
    }

    public override void Heuristic(in ActionBuffers actionsOut) //Indicaciones tuyas en caso de activar heurística
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

    private void OnTriggerEnter(Collider other) //Si hay colision
    {
        if (Training) //Si esta en fase de entrenamiento
        {
            if (other.CompareTag("Item"))
            {
                AddReward(1f); //Si choca con el item gana 1 punto
                EndEpisode();
            }
            if (other.CompareTag("Barrier"))
            {
                AddReward(-0.1f); //Si choca con la barrera pierde 0.1 punto
            }
        }

        else{
            if (other.CompareTag("Item"))
            {
                EndEpisode();
            }
        }
    }

    private void OnTriggerStay(Collider other) //Mientras colisione
    {
        if (Training) 
        {
            if (other.CompareTag("Barrier"))
            {
                AddReward(-0.05f); //Se va penalizando el tiempo que este chocando con la barrera
            }
        }
    }

}
