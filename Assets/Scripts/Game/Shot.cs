using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shot : MonoBehaviour
{
    //References
    private Rigidbody2D myRB;
    private SpriteRenderer mySprite;
    private Character myCharacter;
    private ParticleSystem myParticle;
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
        myParticle = GetComponentInChildren<ParticleSystem>();
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
            //Eliminar enemigo (Mandar)
            //Puntos como enemigo
            int points = collision.gameObject.GetComponent<Enemy>().RemoveEnemyServer();
            myCharacter.AddPoints(points);
           
            //Mandar desaparicion
            //Mandar puntos
         

        }
        else if (collision.tag.Equals("Player"))
        {
            //Mandar puntos
            collision.gameObject.GetComponent<Character>().AddPoints(-pointsPlayer);
        
            //Set puntos
        }
        EliminateShot();
    }

    #endregion
    #region Methods
    #region Public
    public void InitBullet(Vector2 position, Vector2 direction, Color color, bool isServer,Character myCharacter)
    {
        this.isServer = isServer;
        this.myCharacter = myCharacter;
        SoundManager.OnSound(SoundManager.FXType.Shot);
        transform.position = position;
        myRB.velocity = bulletSpeed * direction.normalized;
        color.a = 1;
        mySprite.color = color;
        myParticle.startColor= color;
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
