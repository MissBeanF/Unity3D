using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Web;
using UnityEngine.UI;

[RequireComponent (typeof (AudioSource))]

public class GoogleVoiceSpeech : MonoBehaviour {

		public Text textBox;
        public Text rectextBox;

        struct ClipData
		    {
				public int samples;
		}

		const int HEADER_SIZE = 44;

		private int minFreq;
		private int maxFreq;

		private bool micConnected = false;

		//A handle to the attached AudioSource
		private AudioSource goAudioSource;

		public string apiKey;

        private string[] phrase = {"start", "stop","turn left","turn right","turn back","jump","English", "German", "French" };

        private float play_time;
        private bool end_flag = false;
        private bool start_flag = false;

        public static float MicLoudness;

        private string _device;

        private AudioClip _clipRecord;

        bool _isInitialized;
        public static bool speechOut = false;


        // Use this for initialization
        void Start () {
                InitMic();
                _isInitialized = true;
                play_time = 7.0f;
                        //Check if there is at least one microphone connected
                if (Microphone.devices.Length <= 0)
				{
						//Throw a warning message at the console if there isn't
						Debug.LogWarning("Microphone not connected!");
				}
				else //At least one microphone is present
				{
						//Set 'micConnected' to true
						micConnected = true;

						//Get the default microphone recording capabilities
						Microphone.GetDeviceCaps(null, out minFreq, out maxFreq);

						//According to the documentation, if minFreq and maxFreq are zero, the microphone supports any frequency...
						if(minFreq == 0 && maxFreq == 0)
						{
								//...meaning 44100 Hz can be used as the recording sampling rate
								maxFreq = 44100;
						}


						//Get the attached AudioSource component
						goAudioSource = this.GetComponent<AudioSource>();
				}
	    }

        void Update()
        {
            if (start_flag && end_flag)
            {
                InitMic();
                Debug.Log("Starting again!");
            }
            MicLoudness = LevelMax();

            if (MicLoudness > 0.001 && !start_flag)
            {
                Debug.Log(MicLoudness);
                Microphone.End(null);
                start_flag = true;
            }

            //If there is a microphone
            if (micConnected && start_flag)
            {
                //start_flag = true;
                //If the audio from any microphone isn't being recorded
                if ((!Microphone.IsRecording(null)) && (play_time > 0))
                {
                    ////Case the 'Record' button gets pressed
                    //if(GUI.Button(new Rect(Screen.width/2-100, Screen.height/2-25, 200, 50), "Record"))
                    //{
                    //		//Start recording and store the audio captured from the microphone at the AudioClip in the AudioSource
                    //		goAudioSource.clip = Microphone.Start( null, true, 7, maxFreq); //Currently set for a 7 second clip
                    //}
                    //play_time = 7.0f;
                    Debug.Log("Mic Connected");
                    textBox.text = "Start...";
                    goAudioSource.clip = Microphone.Start(null, true, 7, maxFreq);
                }
                else if (Microphone.IsRecording(null)) //Recording is in progress
                {
                    if (play_time < 0)
                        textBox.text = "Recording End";
                    else
                        textBox.text = "Recording in progress...";

                    play_time -= Time.deltaTime;
            
                    if (play_time < 0 && !end_flag)
                    {
                        end_flag = true;
                        
                        float filenameRand = UnityEngine.Random.Range(0.0f, 10.0f);

                        string filename = "testing" + filenameRand;

                        Microphone.End(null); //Stop the audio recording

                        Debug.Log("Recording Stopped");

                        if (!filename.ToLower().EndsWith(".wav"))
                        {
                            filename += ".wav";
                        }

                        var filePath = Path.Combine("testing/", filename);
                        filePath = Path.Combine(Application.persistentDataPath, filePath);
                        Debug.Log("Created filepath string: " + filePath);

                        // Make sure directory exists if user is saving to sub dir.
                        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                        SavWav.Save(filePath, goAudioSource.clip); //Save a temporary Wav File
                        Debug.Log("Saving @ " + filePath);
                        //Insert your API KEY here.
                        string apiURL = "https://speech.googleapis.com/v1/speech:recognize?&key=" + apiKey;
                        string Response;

                        Debug.Log("Uploading " + filePath);
                        Response = HttpUploadFile(apiURL, filePath, "file", "audio/wav; rate=44100");
                        Debug.Log("Response String: " + Response);

                        var jsonresponse = SimpleJSON.JSON.Parse(Response);

                        if (jsonresponse != null)
                        {
                            string resultString = jsonresponse["results"][0].ToString();
                            var jsonResults = SimpleJSON.JSON.Parse(resultString);
                            string transcripts="";
                            if (jsonResults == null)
                                transcripts = null;
                            else
                                transcripts = jsonResults["alternatives"][0]["transcript"].ToString();
                   
                            if (transcripts == null)
                            {
                                textBox.text = "NULL";
                                Debug.Log("NULL");
                                play_time = 7.0f;
                                end_flag = false;
                            }
                            else
                            {
                                speechOut = true;
                                TestTTS.sound_text = transcripts;
                                Debug.Log("transcript string: " + transcripts);
                                rectextBox.text = transcripts;
                                play_time = 7.0f;
                                end_flag = false;
                            }

                        }
                        //goAudioSource.Play(); //Playback the recorded audio

                        File.Delete(filePath); //Delete the Temporary Wav file

                        //}
                    }
            }
        
            else // No microphone
            {
                //Print a red "Microphone not connected!" message at the center of the screen
                Debug.Log("No Microphone");
                //GUI.contentColor = Color.red;
                //GUI.Label(new Rect(Screen.width / 2 - 100, Screen.height / 2 - 25, 200, 50), "Microphone not connected!");
            }
            
        }
    }

    public string HttpUploadFile(string url, string file, string paramName, string contentType) {

        System.Net.ServicePointManager.ServerCertificateValidationCallback += (o, certificate, chain, errors) => true;
        Debug.Log(string.Format("Uploading {0} to {1}", file, url));

        Byte[] bytes = File.ReadAllBytes(file);
        String file64 = Convert.ToBase64String(bytes,Base64FormattingOptions.None);

        Debug.Log(file64);

        try
        {

            var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "POST";

            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {

                string json = "{ \"config\": { \"languageCode\" : \"en-US\" }, \"audio\" : { \"content\" : \"" + file64 + "\"}}";

                Debug.Log(json);
                streamWriter.Write(json);
                streamWriter.Flush();
                streamWriter.Close();
            }

            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            Debug.Log(httpResponse);
            
            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                var result = streamReader.ReadToEnd();
                Debug.Log("Response:" + result);
                return result;
            }
        
        } catch (WebException ex) {
            var resp = new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
            Debug.Log("!!!"+resp);
 
        }


        return "empty";
		
	}

    void InitMic()
    {
        //if (_device == null) _device = Microphone.devices[0];
        _clipRecord = Microphone.Start(null, true, 600, 44100);
    }


    //AudioClip _clipRecord = new AudioClip();
    int _sampleWindow = 128;

    //get data from microphone into audioclip
    float LevelMax()
    {
        float levelMax = 0;
        float[] waveData = new float[_sampleWindow];
        int micPosition = Microphone.GetPosition(null) - (_sampleWindow + 1); // null means the first microphone
        if (micPosition < 0) return 0;
        _clipRecord.GetData(waveData, micPosition);
        // Getting a peak on the last 128 samples
        for (int i = 0; i < _sampleWindow; i++)
        {
            float wavePeak = waveData[i] * waveData[i];
            if (levelMax < wavePeak)
            {
                levelMax = wavePeak;
            }
        }
        return levelMax;
    }

    // make sure the mic gets started & stopped when application gets focused
    void OnApplicationFocus(bool focus)
    {
        if (focus)
        {
            //Debug.Log("Focus");

            if (!_isInitialized)
            {
                //Debug.Log("Init Mic");
                InitMic();
                _isInitialized = true;
            }
        }
        if (!focus)
        {
            //Debug.Log("Pause");
            //StopMicrophone();
            Debug.Log("Stop Mic");
            _isInitialized = false;

        }
    }

}
		
