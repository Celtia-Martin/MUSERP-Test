using System.Collections;
using System.Collections.Generic;
using System.Timers;
using UnityEngine;
using UnityEngine.UI;

public class Character : MonoBehaviour
{
    //Properties(Editor)

    [SerializeField]
    private float arrowSpeed;
    [SerializeField]
    private float angle;
    [SerializeField]
    private float cooldownShot;

    //Other Properties

    private int ID;
    private bool isServer;
    private bool isMine;
    private int points;
    public Color color;

    //Delegates and timer 

    private Timer sendingPosition;
  
    private CustomUpdateDelegate customUpdate;
    private CustomUpdateDelegate customFixedUpdate;

    //References

    [SerializeField]
    private SpriteRenderer cursor;
    [SerializeField]
    private SpriteRenderer arrowSprite; //Para ponerle el color
    [SerializeField]
    private Transform arrowSpawn; //para spawnear los disparos
    [SerializeField]
    private GameObject arrowPivot; //para girarlo
    [SerializeField]
    private Text pointText; // Para tener conteo de sus puntos
    private Animator myAnimator;

    //State

    private Vector2 position;
    private bool moved = false;
    private bool canShot = true;

    //Reference

    public static Character myCharacter;
   
    #region Public Methods

    #region Mods
    public Color CharacterCreated(int ID, bool isMine, bool isServer)
    {

        this.isServer = isServer;
        this.ID = ID;
        this.color = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f), 1);
        this.isMine = isMine;
        points = 0;
        cursor.color = color;
        arrowSprite.color = color;
        pointText.color = color;
        if (isMine)
        {
            if (!isServer)
            {
                //sendingPosition = new Timer(20);
                //sendingPosition.Elapsed += ((e, t) => SendingPosition());
                //sendingPosition.AutoReset = true;
                //sendingPosition.Start();
                StartCoroutine(SendingPositionClientCoroutine());
            }
            else
            {
                //sendingPosition = new Timer(20);
                //sendingPosition.Elapsed += ((e, t) => SendingPositionServer());
                //sendingPosition.AutoReset = true;
                //sendingPosition.Start();
                StartCoroutine(SendingPositionServerCoroutine());

            }

            myCharacter = this;

        }
       
        return this.color;
    }

    //public void CharacterDispose()
    //{
    //    sendingPosition.Stop();
    //    customUpdate = null;
    //    gameObject.SetActive(false);
    //}
    public void Shot(Vector2 position)
    {
        Vector2 direction = position - (Vector2)transform.position;
        Shot shotInstance = PoolManager.singleton.getFromPool("Shot").GetComponent<Shot>();
        shotInstance.InitBullet(position, direction, color,isServer,this);
    }
    public void StartGame()
    {
        customUpdate += OnCustomUpdate;
        customFixedUpdate += OnCustomFixedUpdate;
        arrowSprite.gameObject.SetActive(true);
      
    }
    #endregion
    #region Getters and Setters
    public Vector2 getPosition()
    {
        return position;
    }
    public int getID() { return ID; }
    public void SetPosition(Vector2 position)
    {
        transform.position = new Vector3(position.x, position.y, 0);
        position = transform.position;

    }
    public void SetPoints(int points)
    {
      
        this.points = points;
        pointText.text = this.points.ToString();
    }
    public void AddPoints(int points)
    {
        if (points < 0)
        {
            myAnimator.SetBool("Damaged", true);
            StartCoroutine(HitState());
            SoundManager.OnSound(SoundManager.FXType.CharacterHit);
        }
        else
        {
            SoundManager.OnSound(SoundManager.FXType.EnemyDead);
        }
        this.points += points;
        pointText.text = this.points.ToString();
        if (isServer)
        {
            GameServer.instance?.SendPoints(points, ID);
        }
    }
    public int getPoints()
    {
        return points;
    }
    public void SetColor(Color color)
    {
        this.color = color;
        color.a = 1;
        cursor.color = color;
        arrowSprite.color = color;
        pointText.color = color;
    }
    #endregion
    #endregion
    #region Private Methods
    #region Unity Events
    private void Awake()
    {
        position = transform.position;
        myAnimator = GetComponent<Animator>();
    }
    private void Update()
    {
        customUpdate?.Invoke();
    }
    private void FixedUpdate()
    {
        customFixedUpdate?.Invoke();
    }
    #endregion
    #region CustomUpdates
    private void OnCustomUpdate()
    {
        //Server autoritativo: mandar input y el ya hace el resto
        //Mover el cursor
        transform.position = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);
      
        GetInputShot();

    }
    private void OnCustomFixedUpdate()
    {
        arrowPivot.transform.rotation = arrowPivot.transform.rotation * Quaternion.AngleAxis(angle * arrowSpeed * Time.deltaTime, Vector3.forward);
    }

    #endregion
    #region Auxiliar
    private void GetInputShot()
    {
        if (!canShot) return;

        if (Input.GetMouseButtonDown(0))
        {
            if (isServer)
            {

                GameServer.instance.SendShot(arrowSpawn.position, ID);
                Shot(arrowSpawn.position);

            }
            else
            {
                GameClient.instance.SendShotServer(arrowSpawn.position, ID);
                Shot(arrowSpawn.position);
            }
            StartCoroutine(ShotCooldown());
        }
    }
    //private void SendingPosition()
    //{
    //    moved = Mathf.Abs(Vector2.Distance(position, transform.position)) > Mathf.Epsilon;
    //    position = transform.position;
    //    if (moved) GameClient.instance.SendPositionToServer(position);

    //    moved = false;
    //}
    //private void SendingPositionServer()
    //{
    //    Debug.Log("p" + position);
    //    Debug.Log("tr" + transform.position);
    //    moved = Mathf.Abs(Vector2.Distance(position, transform.position)) > Mathf.Epsilon;
      
    //    Debug.Log(moved?"si":"no");
    //    position = transform.position;
    //    if (moved)
    //    {
    //        GameServer.instance.SendPositionServer(position, ID);
    //        Debug.Log("Enviado");
    //    }
    //    moved = false;
    //}


    private IEnumerator SendingPositionServerCoroutine()
    {
        while (isActiveAndEnabled)
        {
            yield return new WaitForSeconds(0.02f);

            moved = Mathf.Abs(Vector2.Distance(position, transform.position)) > Mathf.Epsilon;


            position = transform.position;
            if (moved)
            {
                GameServer.instance.SendPositionServer(position, ID);
                Debug.Log("Enviado");
            }
            moved = false;
        }
    }   private IEnumerator SendingPositionClientCoroutine()
    {
        while (isActiveAndEnabled)
        {
            yield return new WaitForSeconds(0.02f);
            moved = Mathf.Abs(Vector2.Distance(position, transform.position)) > Mathf.Epsilon;

            position = transform.position;
            if (moved)
            {
                GameClient.instance.SendPositionToServer(position);
                Debug.Log("Enviado");
            }
            moved = false;
        }
    }
    IEnumerator HitState()
    {
        yield return new WaitForSeconds(0.5f);
        myAnimator.SetBool("Damaged", false);
    }

    #endregion
    #region Coroutines
    private IEnumerator ShotCooldown()
    {
        canShot = false;
        yield return new WaitForSeconds(cooldownShot);
        canShot = true;
    }
    #endregion
    #region Events
    private void OnApplicationQuit()
    {
        sendingPosition?.Stop();
    }
    #endregion
    #endregion

}
