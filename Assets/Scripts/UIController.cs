using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    [Header("External")]
    [SerializeField] private GameController _gameCont;

    [Header("Stages")]
    [SerializeField] private GameObject _setupParent;
    [SerializeField] private GameObject _gameplayParent;

    [Header("Character Creation")]
    [SerializeField] private TextMeshProUGUI _firstNameLabel;
    [SerializeField] private TextMeshProUGUI _lastNameLabel;
    [SerializeField] private TMP_InputField[] _nameTextFields;
    [SerializeField] private Button _charCreateContinue;
    [SerializeField] private GameObject[] _detailsEntryObjs;
    [SerializeField] private TextMeshProUGUI[] _detailsLabels;
    [SerializeField] private TMP_Dropdown[] _detailsDropdowns;
    [SerializeField] private TMP_InputField[] _detailsTextFields;

    [Header("Specific Input Fields")]
    [SerializeField] private TMP_InputField _firstName;
    [SerializeField] private TMP_InputField _lastName;
    [SerializeField] private TMP_Dropdown _gender;
    [SerializeField] private TMP_Dropdown _year;
    [SerializeField] private TMP_InputField _hometown;
    [SerializeField] private TMP_InputField _major;
    [SerializeField] private TMP_InputField _physicalAppearance;
    [SerializeField] private TMP_InputField _background;
    [SerializeField] private TMP_InputField _personality;
    [SerializeField] private TMP_InputField _perceivedPersonality;

    [Header("Gameplay")]
    [SerializeField] private TextMeshProUGUI _mainText;
    [SerializeField] private ScrollRect _mainScroll;

    [Header("Text Input")]
    [SerializeField] private GameObject _textInputParent;
    [SerializeField] private TMP_InputField _textInput;
    [SerializeField] private Button _continueButton;

    [Header("Button Input")]
    [SerializeField] private GameObject _buttonInputParent;
    [SerializeField] private Button[] _optionButtons;
    [SerializeField] private TextMeshProUGUI[] _buttonTexts;

    [Header("Chat Input")]
    [SerializeField] private GameObject _chatInputParent;
    [SerializeField] private TMP_InputField _chatInput;
    [SerializeField] private Button _chatContinue;
    [SerializeField] private Button _chatEndConversation;

    private void Start()
    {
        _setupParent.SetActive(true);
        _gameplayParent.SetActive(false);
    }

    #region Setup

    public void OnNameFieldInput(string _)
    {
        _charCreateContinue.interactable = AllFieldsFilled(_nameTextFields);
    }

    public string GetPlayerCharacterName()
    {
        return string.Join(' ', _nameTextFields.Select(field => field.text.Trim()));
    }

    public string GetPlayerCharacterForename() { return _firstName.text.Trim(); }
    public string GetPlayerCharacterSurname() { return _lastName.text.Trim(); }

    public void SetUpDetailsSection(string firstName, string lastName)
    {
        _firstNameLabel.text = $"First Name: {firstName}";
        _lastNameLabel.text = $"Last Name: {lastName}";
        foreach (var field in _nameTextFields)
            field.gameObject.SetActive(false);
        foreach (var obj in _detailsEntryObjs)
            obj.SetActive(true);
        foreach (var textbox in _detailsLabels)
            textbox.text = string.Join(firstName, textbox.text.Split('$', System.StringSplitOptions.None));
        _charCreateContinue.interactable = false;
    }

    public void OnDetailsFieldInput(string _)
    {
        _charCreateContinue.interactable = AllFieldsFilled(_detailsTextFields)
            && AllDropdownsFilled(_detailsDropdowns);
    }

    public string GetGender() { return _gender.options[_gender.value].text; }
    public string GetYear() { return _year.options[_year.value].text; }
    public string GetHometown() { return _hometown.text.Trim(); }
    public string GetMajor() { return _major.text.Trim(); }
    public string GetPhysicalAppearance() { return _physicalAppearance.text.Trim(); }
    public string GetBackground() { return _background.text.Trim(); }
    public string GetPersonality() { return _personality.text.Trim(); }
    public string GetPerceivedPersonality() { return _perceivedPersonality.text.Trim(); }


    private bool AllFieldsFilled(TMP_InputField[] texts)
    {
        foreach (var textbox in texts)
        {
            if (string.IsNullOrEmpty(textbox.text))
                return false;
        }

        return true;
    }

    private bool AllDropdownsFilled(TMP_Dropdown[] dropdowns)
    {
        foreach (var dropdown in dropdowns)
        {
            if (dropdown.value == 0)
                return false;
        }

        return true;
    }

    #endregion

    #region Gameplay

    public void ShowMainGame()
    {
        _setupParent.SetActive(false);
        _gameplayParent.SetActive(true);
        _gameCont.ShowIntroText();
    }

    public void AddText(string newText)
    {
        _mainText.text += newText;
        //_mainScroll.normalizedPosition = Vector2.zero;
    }

    public void AllowTextInput(Action<string> onComplete)
    {
        _textInputParent.SetActive(true);
        _continueButton.onClick.AddListener(() => OnSubmitTextInput(onComplete));
    }

    public void AllowChatInput(Action<string> onComplete)
    {
        _chatInputParent.SetActive(true);
        _chatContinue.onClick.AddListener(() => OnSubmitChatInput(onComplete));
        _continueButton.onClick.AddListener(() => OnChatEndConversation(onComplete));
    }

    public void OnSubmitTextInput(Action<string> onComplete)
    {
        var text = _textInput.text;
        _continueButton.onClick.RemoveAllListeners();
        _textInputParent.SetActive(false);
        onComplete?.Invoke(text);
    }

    public void ShowButtonOptions(string[] buttonTexts, Action<string> onComplete)
    {
        for (int i = 0; i < _buttonTexts.Length; i++)
        {
            var button = _optionButtons[i];
            var textbox = _buttonTexts[i];
            var text = buttonTexts[i];
            textbox.text = text;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnSubmitButtonInput(text, onComplete));
            button.gameObject.SetActive(true);
        }
    }

    public void OnSubmitButtonInput(string chosenText, Action<string> onComplete)
    {
        foreach (var button in _optionButtons)
            button.gameObject.SetActive(false);
        _buttonInputParent.SetActive(false);

        onComplete?.Invoke(chosenText);
    }

    public void OnSubmitChatInput(Action<string> onComplete)
    {
        var text = _chatInput.text;
        onComplete?.Invoke(text);
        CloseChat();
    }

    public void OnChatEndConversation(Action<string> onComplete)
    {
        onComplete?.Invoke(string.Empty);
        CloseChat();
    }

    private void CloseChat()
    {
        _chatContinue.onClick.RemoveAllListeners();
        _chatEndConversation.onClick.RemoveAllListeners();
        _chatInputParent.SetActive(false);
    }

    #endregion
}
