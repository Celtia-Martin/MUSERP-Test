using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//Debug class that displays messages in the console of the game
public class Console : MonoBehaviour
{
    //Singleton
    public static Console instance;

    //State
    private string log;

    //References
    [SerializeField]
    private Text uiLog;

    #region Unity Events
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    #endregion
    #region Methods
    public void WriteLine(string line)
    {
        try
        {
            log += "\n" + line;
            uiLog.text = log;

        }
        catch (Exception e)
        {
            Debug.LogWarning("Message can't be write because:" + e + " The message was:  " + line);
        }
    }
    #endregion


}
