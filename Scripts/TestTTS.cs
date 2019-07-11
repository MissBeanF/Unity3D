using UnityEngine;
using System.Collections;
using System.Text.RegularExpressions;


/// <summary>
/// <author>Jefferson Reis</author>
/// <explanation>Works only on Android, or platform that supports mp3 files. To test, change the platform to Android.</explanation>
/// </summary>

public class TestTTS : MonoBehaviour
{
    public static string sound_text;

    public AudioSource _audio;

    void Start()
    {
        _audio = gameObject.GetComponent<AudioSource>();
        
    }

    void Update()
    {
        if (GoogleVoiceSpeech.speechOut)
        {
            StartCoroutine(DownloadTheAudio(sound_text));
            GoogleVoiceSpeech.speechOut = false;
        }
           
    }
    IEnumerator DownloadTheAudio(string soundtext)
    {
        Debug.Log("TTS started");
        string url = "https://translate.google.com/translate_tts?ie=UTF-8&total=1&idx=0&textlen=32&client=tw-ob&q=" + soundtext + "&tl=En-gb";
        WWW www = new WWW(url);
        yield return www;
        _audio.clip = www.GetAudioClip(false, true, AudioType.MPEG);
        _audio.Play();
    }
}
