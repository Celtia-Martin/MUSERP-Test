using System;
using System.Collections;
using System.Collections.Generic;
using System.Timers;
using UnityEngine;
using UnityEngine.UI;

public class GameTimer
{

    public static GameTimer instance;

    private Timer timeCountDown;

    private Text textClock;

    private float initialSeconds = 10f;

    private Queue<Action> jobs;

    public GameTimer(Text textClock)
    {
        this.textClock = textClock;
        jobs = new Queue<Action>();
        this.textClock.text = DisplayTime(initialSeconds);
        timeCountDown = new Timer(1000);
        timeCountDown.Elapsed += (a, b) => OnSecond();
        timeCountDown.AutoReset = true;
        timeCountDown.Start();

    }
    public static void StartTimer(Text textClock)
    {
        instance = new GameTimer(textClock);

    }

    private void OnSecond()
    {
        initialSeconds += -1;
        jobs.Enqueue(() =>
        {
            textClock.text = DisplayTime(initialSeconds);
            if (initialSeconds == 0)
            {
                GameServer.OnGameEnd();
                timeCountDown.Stop();
            }
        });
     
    }
    private string DisplayTime(float timeToDisplay)
    {
        float minutes = Mathf.FloorToInt(timeToDisplay / 60);
        float seconds = Mathf.FloorToInt(timeToDisplay % 60);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }
    public void OnUpdate()
    {
        while (jobs.Count > 0)
        {
            jobs.Dequeue().Invoke();
        }
    }
}
