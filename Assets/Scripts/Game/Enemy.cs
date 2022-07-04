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
    private Animator animator;

    //Spawn information
    public int spawnerIndex;

    private bool dead;


    private void Awake()
    {
        myRB = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    public int RemoveEnemyServer()
    {
        if (dead)
        {
            return 0;
        }
        dead = true;
        EnemySpawner.instance.RemoveEnemyServer(this);

        animator.SetBool("Dead", true);
        StartCoroutine(Dead());
        return points;

    }
    public void RemoveEnemyClient()
    {
        if (dead)
        {
            return;
        }
        dead = true;
        animator.SetBool("Dead", true);
        StartCoroutine(Dead());
    }
   public void InitEnemy(Vector2 position, Vector2 direction, bool isServer)
    {
        dead = false;
        animator.SetBool("Dead", false);
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
    IEnumerator Dead()
    {
     
        yield return new WaitForSeconds(1);
        PoolManager.singleton.addToPool(type, gameObject);
        PoolManager.singleton.addToPool(type, gameObject);
    }
    
}
