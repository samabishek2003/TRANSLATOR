using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro; // <- for TMP_Dropdown
using Newtonsoft.Json;

namespace OpenAI
{
    public class ChatGPT : MonoBehaviour
    {
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private TMP_InputField outputField;

        [SerializeField] private Button button;
        [SerializeField] private ScrollRect scroll;
        [SerializeField] private RectTransform sent;
        [SerializeField] private RectTransform received;
        [SerializeField] private TextToSpeech textToSpeech;

        [SerializeField] private TMP_Dropdown fromLanguageDropdown;
        [SerializeField] private TMP_Dropdown toLanguageDropdown;

        private float height;
        private OpenAIApi openai;

        private string jsonFileUrl = "";

        private void Start()
        {
            button.onClick.AddListener(SendReply);
            PopulateDropdowns();
            StartCoroutine(FetchAPIKeyFromGoogleDrive());
        }

        private void PopulateDropdowns()
        {
            List<string> languages = new List<string> { "English", "French", "German", "Spanish", "Italian", "Chinese", "Japanese", "Arabic", "Korean", "Hindi" };
            fromLanguageDropdown.ClearOptions();
            toLanguageDropdown.ClearOptions();
            fromLanguageDropdown.AddOptions(languages);
            toLanguageDropdown.AddOptions(languages);
        }

        private IEnumerator FetchAPIKeyFromGoogleDrive()
        {
            UnityWebRequest request = UnityWebRequest.Get(jsonFileUrl);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var jsonData = JsonConvert.DeserializeObject<AuthData>(request.downloadHandler.text);
                    openai = new OpenAIApi(jsonData.api_key, jsonData.organization);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError("Error parsing JSON: " + ex.Message);
                }
            }
            else
            {
                Debug.LogError("Failed to fetch JSON: " + request.error);
            }
        }

        private void AppendMessage(ChatMessage message)
        {
            scroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 0);
            var item = Instantiate(message.Role == "user" ? sent : received, scroll.content);
            item.GetChild(0).GetChild(0).GetComponent<Text>().text = message.Content;
            item.anchoredPosition = new Vector2(0, -height);
            LayoutRebuilder.ForceRebuildLayoutImmediate(item);
            height += item.sizeDelta.y;
            scroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            scroll.verticalNormalizedPosition = 0;
        }

        private async void SendReply()
{
    // Check if OpenAI API is initialized
    if (openai == null)
    {
        Debug.LogError("OpenAI API is not initialized.");
        return;
    }

    // Validate Dropdowns
    if (fromLanguageDropdown == null || toLanguageDropdown == null)
    {
        Debug.LogError("Language dropdowns are not assigned in the Inspector.");
        return;
    }

    // Ensure dropdowns have options
    if (fromLanguageDropdown.options.Count == 0 || toLanguageDropdown.options.Count == 0)
    {
        Debug.LogError("Language dropdowns have no options. Did PopulateDropdowns() run?");
        return;
    }

    // Ensure valid selection
    if (fromLanguageDropdown.value < 0 || toLanguageDropdown.value < 0)
    {
        Debug.LogError("Please select a language in both dropdowns.");
        return;
    }

    string fromLang = fromLanguageDropdown.options[fromLanguageDropdown.value].text;
    string toLang = toLanguageDropdown.options[toLanguageDropdown.value].text;

    // Validate inputField
    if (inputField == null)
    {
        Debug.LogError("Input field is not assigned.");
        return;
    }

    string originalText = inputField.text.Trim();
    if (string.IsNullOrEmpty(originalText))
    {
        Debug.LogWarning("Input text is empty.");
        return;
    }

    string prompt = $"Translate this from {fromLang} to {toLang}. Only return the translation with no extra text:\n\"{originalText}\"";

    var userMessage = new ChatMessage() { Role = "user", Content = prompt };
    AppendMessage(new ChatMessage { Role = "user", Content = originalText });

    button.enabled = false;
    inputField.text = "";
    inputField.interactable = false;

    var response = await openai.CreateChatCompletion(new CreateChatCompletionRequest()
    {
        Model = "gpt-4o-mini",
        Messages = new List<ChatMessage> { userMessage }
    });
if (response.Choices != null && response.Choices.Count > 0)
    {
        var message = response.Choices[0].Message;
        message.Content = message.Content.Trim();

        AppendMessage(message);

        if (outputField != null)
            outputField.text = message.Content;

        if (textToSpeech != null)
            textToSpeech.GenerateAudio(message.Content);
    }
    else
    {
        Debug.LogWarning("No response from OpenAI.");
    }

    button.enabled = true;
    inputField.interactable = true;
}

        public void TranslateWhisperOutput(string whisperText)
        {
            inputField.text = whisperText;
            SendReply();
        }

        private class AuthData
        {
            public string api_key;
            public string organization;
        }
    }
}
