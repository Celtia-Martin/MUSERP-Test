using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Class that manages the shot gameobeject
public class Shot : MonoBehaviour
{
    //References
    private Rigidbody2D myRB;
    private SpriteRenderer mySprite;
    private Character myCharacter;

    //Properties
    [SerializeField]
    private float bulletSpeed;
    [SerializeField]
    private float lifeTime;
    [SerializeField]
    private int pointsPlayer;

    private bool isServer;

    #region Unity Events
    private void Awake()
    {
        myRB = GetComponent<Rigidbody2D>();
        mySprite = GetComponent<SpriteRenderer>();
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isServer)
        {
            EliminateShot();
            return;
        }
        if (collision.tag.Equals("Enemy"))
        {
            int points = collision.gameObject.GetComponent<Enemy>().RemoveEnemyServer();
            myCharacter.AddPoints(points);
        }
        else if (collision.tag.Equals("Player"))
        {
            collision.gameObject.GetComponent<Character>().AddPoints(-pointsPlayer);
        }
        EliminateShot();
    }

    #endregion
    #region Methods
    #region Public
    public void InitBullet(Vector2 position, Vector2 direction, Color color, bool isServer, Character myCharacter)
    {
        this.isServer = isServer;
        this.myCharacter = myCharacter;
        SoundManager.OnSound(SoundManager.FXType.Shot);
        transform.position = position;
        myRB.velocity = bulletSpeed * direction.normalized;
        color.a = 1;
        mySprite.color = color;
        StartCoroutine(TimeToLive());


    }
    #endregion  
    #region Auxiliar
    private void EliminateShot()
    {
        PoolManager.singleton.addToPool("Shot", gameObject);
    }
    #endregion

    #endregion
    #region Coroutines
    private IEnumerator TimeToLive()
    {
        yield return new WaitForSeconds(lifeTime);
        EliminateShot();
    }

    #endregion

}
