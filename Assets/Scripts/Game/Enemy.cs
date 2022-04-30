using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    //Properties
    [SerializeField]
    private string type;
    [SerializeField]
    private float speed;
    [SerializeField]
    private int points;
    [SerializeField]
    private int lifeTime;

    //References
    private Rigidbody2D myRB;

    //Spawn information
    public int spawnerIndex;


    private void Awake()
    {
        myRB = GetComponent<Rigidbody2D>();
        
    }

    public int RemoveEnemyServer()
    {
        EnemySpawner.instance.RemoveEnemyServer(this);
        PoolManager.singleton.addToPool(type, gameObject);
        return points;

    }
    public void RemoveEnemyClient()
    {
        PoolManager.singleton.addToPool(type, gameObject);
    }
   public void InitEnemy(Vector2 position, Vector2 direction, bool isServer)
    {
        transform.position = position;
        myRB.velocity = direction.normalized * speed;
        if (isServer) StartCoroutine(TimeToLive());
    }
    public string getType() { return type; }
    private IEnumerator TimeToLive()
    {
        yield return new WaitForSeconds(lifeTime);
        RemoveEnemyServer();
    }
    
}
