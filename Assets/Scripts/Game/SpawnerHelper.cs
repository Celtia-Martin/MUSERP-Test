using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnerHelper : MonoBehaviour
{
    public static SpawnerHelper instance;

    private float maxPosX;
    private float maxPosY;

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
            maxPosX = Camera.main.pixelWidth;
            maxPosY = Camera.main.pixelHeight;
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
        //    float x=0;
        //    float y=0;
        //    CardinalPoints randomPoint = (CardinalPoints)Random.Range(0, 4);
        //    switch (randomPoint)
        //    {
        //        case CardinalPoints.North:
        //            y = maxPosY;
        //            x = Random.Range(-maxPosX, maxPosX);
        //            break;
        //        case CardinalPoints.East:
        //            x = maxPosX;
        //            y = Random.Range(-maxPosY, maxPosY);
        //            break;
        //        case CardinalPoints.South:
        //            y = 0;
        //            x = Random.Range(-maxPosX, maxPosX);
        //            break;
        //        case CardinalPoints.West:
        //            x = 0;
        //            y = Random.Range(-maxPosY, maxPosY);
        //            break;
        //    }
        //    Vector3 position = Camera.main.ScreenToWorldPoint(new Vector3(x, y, 0));
        int index = Random.Range(0, spawnPoints.Length);
        SpawnPoint randomPoint = spawnPoints[index];

        return (directions[(int)randomPoint.cardinalPoint], randomPoint.transform.position,index);

    }
    //public Vector2 GetDirectionFromPosition(Vector2 position)
    //{
    //    Vector2 positionToCamera = Camera.main.WorldToScreenPoint(position);
    //    Vector2 direction = Vector2.zero;
    //    if (positionToCamera.x == 0)
    //    {
    //        direction = directions[3];
    //    }
    //    if (positionToCamera.y == 0)
    //    {
    //        direction = directions[2];
    //    }
    //    if (Mathf.Approximately(positionToCamera.x ,maxPosX))
    //    {
    //        direction = directions[1];
    //    }
    //    if (Mathf.Approximately(positionToCamera.y, maxPosY))
    //    {
    //        direction = directions[0];
    //    }
    //    return direction;
    //}
    public (Vector2 direction, Vector2 position) GetInfoFromPoint(int index)
    {
        if (index<0 || index >= spawnPoints.Length)
        {
            return (Vector2.zero, Vector2.zero);
        }
        return (directions[(int)spawnPoints[index].cardinalPoint], spawnPoints[index].transform.position);
    } 
}
