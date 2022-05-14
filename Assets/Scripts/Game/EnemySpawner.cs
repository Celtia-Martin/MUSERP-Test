using Muse_RP.Hosts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Timers;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    //Singleton
    public static EnemySpawner instance;

    //References

    //Properties
    [SerializeField]
    private List<string> types;
    [SerializeField]
    private int minMSeconds;
    [SerializeField]
    private int maxMSeconds;

    private bool isServer;

    //Storage
    private List<Enemy> enemies;


    //Delegates
    private Timer spawningMonsters;
    private Queue<Action> jobs;

    #region Unity Events
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            enemies = new List<Enemy>();
            jobs = new Queue<Action>();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        while (jobs.Count > 0)
        {
            jobs.Dequeue().Invoke();
        }
    }
    #endregion
    #region Public Methods

    public void InitEnemySpawner(bool isServer)
    {
        this.isServer = isServer;
        if (!isServer) return;
        spawningMonsters = new Timer(UnityEngine.Random.Range(minMSeconds, maxMSeconds));
        spawningMonsters.AutoReset = true;
        spawningMonsters.Elapsed += (a,b)=> OnSpawn();
        spawningMonsters.Start();
        Debug.Log("Timer activado");
        
    }

    public void RemoveEnemyClient(int id)
    {
        if (enemies.Count < id) { return; }
        Debug.Log("Lenght " + enemies.Count + " id" + id);
        Enemy enemy = enemies[id];
        enemy.RemoveEnemyClient();
        enemies.RemoveAt(id);
    }
    public void RemoveEnemyServer(Enemy enemy)
    {
        int id = enemies.IndexOf(enemy);
        enemies.Remove(enemy);
        if (isServer)
        {
            GameServer.instance.SendDisappearEnemy(id);
        }
    }
    public void NewEnemyClient(int index,ushort type)
    {
        if (type >= types.Count)
        {
            return;
        }
        Enemy currentEnemy = PoolManager.singleton.getFromPool(types[type]).GetComponent<Enemy>();
        enemies.Add(currentEnemy);
        (Vector2 direction, Vector2 position)= SpawnerHelper.instance.GetInfoFromPoint(index);
        currentEnemy.InitEnemy(position,direction, false);
    }
    public void SendAllCurrentEnemies(Connection conn)
    {
        foreach(Enemy e in enemies)
        {
            GameServer.instance.SendSpawnEnemy(e.spawnerIndex,(ushort) types.IndexOf(e.getType()),conn);
        }
    }
    #endregion
    #region Private Methods
    private void OnSpawn()
    {
        jobs.Enqueue(OnSpawnJob);
    }
    private void OnSpawnJob()
    {
        Debug.Log("Spawn!");
        ushort type =(ushort)UnityEngine.Random.Range(0, types.Count);
        Enemy currentEnemy = PoolManager.singleton.getFromPool(types[type]).GetComponent<Enemy>();
        enemies.Add(currentEnemy);
        (Vector2 direction, Vector2 position,  int index) = SpawnerHelper.instance.GetRandomPosition();
        currentEnemy.InitEnemy(position, direction, true);
        currentEnemy.spawnerIndex = index;
        GameServer.instance.SendSpawnEnemy(index, type);
        spawningMonsters.Interval = UnityEngine.Random.Range(minMSeconds, maxMSeconds);
    }
    #endregion

}
