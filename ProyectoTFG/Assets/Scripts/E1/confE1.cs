using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class confE1 : Agent
{

//////////////////Atributos//////////////////

[SerializeField]
private float Fmov = 200;

[SerializeField]
private Transform Target;

[SerializeField]
private GameObject Ground;

[SerializeField]
[Range(0f, 1f)]
private float MulFactor;

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

    public override void CollectObservations(VectorSensor sensor){
        //Calcular cuanto nos queda hasta el objetivo
        Vector3 distance = Target.position - transform.position;

        //Un vector ocupa 3 atributos por observacion
        sensor.AddObservation(distance.normalized);
    }

    public override void OnActionReceived(ActionBuffers actions){
        //Construimos un vector con el vector recibido.
        Vector3 move = new Vector3(actions.ContinuousActions[0],
        0f, actions.ContinuousActions[1]);
        //Sumamos el vector construido como fuerza 
        rb.AddForce(move * Fmov * Time.deltaTime);
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
