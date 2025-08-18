
using System.Collections;
using Amazon;
using Amazon.Polly;
using Amazon.Polly.Model;
using Amazon.Runtime;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using System.Threading.Tasks;

public class TextToSpeech : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;

    private BasicAWSCredentials awsCredentials;
    private AmazonPollyClient pollyClient;
    private bool isSpeaking = false;

    private void Start()
    {
        // Using hardcoded AWS credentials for educational purposes (delete after use)
        awsCredentials = new BasicAWSCredentials("AKIAUPMYMYKVTIRCLSJE", "dViHeLUW7wlY5XaO9+WvzVOQqKMGXEFGqlCMtKdh");

        // Use correct AWS region where Polly is supported for your account
        pollyClient = new AmazonPollyClient(awsCredentials, RegionEndpoint.USEast1);
    }

    public async void GenerateAudio(string text)
    {
        var request = new SynthesizeSpeechRequest
        {
            Text = text,
            Engine = Engine.Standard,
            VoiceId = VoiceId.Matthew,
            OutputFormat = OutputFormat.Mp3
        };

        try
        {
            var response = await pollyClient.SynthesizeSpeechAsync(request);

            WriteIntoFile(response.AudioStream);

            string filePath = $"file://{Application.persistentDataPath}/audio.mp3";

            using (var www = UnityWebRequestMultimedia.GetAudioClip(filePath, AudioType.MPEG))
            {
                var operation = www.SendWebRequest();

                while (!operation.isDone)
                    await Task.Yield();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    var clip = DownloadHandlerAudioClip.GetContent(www);
                    audioSource.clip = clip;
                    audioSource.Play();
                    isSpeaking = true;
                    StartCoroutine(MonitorAudio());
                }
                else
                {
                    Debug.LogError($"Error loading audio file: {www.error}");
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error synthesizing speech: {ex.Message}");
        }
    }

    private IEnumerator MonitorAudio()
    {
        while (audioSource.isPlaying)
        {
            yield return null;
        }
        isSpeaking = false;
    }

    private void WriteIntoFile(Stream stream)
    {
        var filePath = $"{Application.persistentDataPath}/audio.mp3";

        using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
        {
            byte[] buffer = new byte[8 * 1024];
            int bytesRead;

            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                fileStream.Write(buffer, 0, bytesRead);
            }
        }

        Debug.Log($"Audio file saved to: {filePath}");
    }

    public bool IsSpeaking()
    {
        return isSpeaking;
    }
}

