using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class test : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Application.logMessageReceived += OnLog;
    }

    private void OnLog(string condition, string stackTrace, LogType type)
    {
        if (type.Equals(LogType.Error))
        {
            GetComponent<Text>().text += condition;
        }
    }
}
