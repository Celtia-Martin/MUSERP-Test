using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Auxiliar class that helps in the spawning of enemies
public class SpawnerHelper : MonoBehaviour
{
    public static SpawnerHelper instance;

    private Vector2[] directions = new Vector2[] { new Vector2(0, -1), new Vector2(-1, 0), new Vector2(0, 1), new Vector2(1, 0) }; //North, East, South, West 

    private SpawnPoint[] spawnPoints;

    public enum CardinalPoints
    {
        North,
        East,
        South,
        West
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            spawnPoints = new SpawnPoint[transform.childCount];
            for (int i = 0; i < transform.childCount; i++)
            {
                spawnPoints[i] = transform.GetChild(i).GetComponent<SpawnPoint>();
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public (Vector2 direction, Vector2 position, int index) GetRandomPosition()
    {
        int index = Random.Range(0, spawnPoints.Length);
        SpawnPoint randomPoint = spawnPoints[index];

        return (directions[(int)randomPoint.cardinalPoint], randomPoint.transform.position,index);

    }
    public (Vector2 direction, Vector2 position) GetInfoFromPoint(int index)
    {
        if (index<0 || index >= spawnPoints.Length)
        {
            return (Vector2.zero, Vector2.zero);
        }
        return (directions[(int)spawnPoints[index].cardinalPoint], spawnPoints[index].transform.position);
    } 
}
