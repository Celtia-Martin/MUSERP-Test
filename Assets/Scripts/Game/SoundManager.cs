using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SoundManager : MonoBehaviour
{
    public static SoundManager instance;
    [SerializeField] private AudioSource[] fxManager;
    [SerializeField] private AudioSource musicManager;
    [SerializeField] private Button muteMusic;

    [SerializeField] private AudioClip[] sounds;
    private int currentManager = 0;
    public enum FXType
    {
        EnemyDead,
        CharacterHit,
        Shot
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

    }
    private void Start()
    {
        muteMusic.onClick.AddListener(() =>
        {
            musicManager.mute = !musicManager.mute;
            for (int i = 0; i < fxManager.Length; i++)
            {
                fxManager[i].mute = !fxManager[i].mute;
            }
        });
    }
    public static void OnSound(FXType type)
    {
        AudioSource current = instance.fxManager[instance.currentManager];
        instance.currentManager = (instance.currentManager + 1) % instance.fxManager.Length;
        current.clip = instance.sounds[(int)type];
        current.Play();
    }
}