using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pop-up box used to ask the user to pick from a list of options.
/// </summary>
public class QuestionBox : MonoBehaviour
{
    [Tooltip("Dialog appearance control.")]
    public CanvasGroup group;
    [Tooltip("Title of the box.")]
    public TextMeshProUGUI titleText;
    [Tooltip("Lengthy text of the box.")]
    public TextMeshProUGUI descriptionText;
    [Tooltip("Where to instantiate the buttons.")]
    public Transform buttonInstantiationArea;
    [Tooltip("Prefab of the button to instantiate for each choice.")]
    public Button templateButton;

    TaskCompletionSource<string> tcs;

    void Awake()
    {
        group.alpha = 0;
        group.blocksRaycasts = group.interactable = false;
    }

    /// <summary>
    /// Shows the dialog with the given choices available.
    /// </summary>
    /// <param name="title">Short title for the dialog.</param>
    /// <param name="description">Lengthy description.</param>
    /// <param name="buttons">Each choice for which you want a button.</param>
    /// <returns>An awaitable task, containing the text of the clicked button.</returns>
    public Task<string> Show(string title, string description, params string[] buttons)
    {
        if (!string.IsNullOrEmpty(title))
        {
            titleText.text = title;
            titleText.gameObject.SetActive(true);
        }
        else
            titleText.gameObject.SetActive(false);

        if (!string.IsNullOrEmpty(description))
        {
            descriptionText.text = description;
            descriptionText.gameObject.SetActive(true);
        }
        else
            descriptionText.gameObject.SetActive(false);

        if (buttons == null || buttons.Length == 0)
            buttons = new string[] { "Ok" };

        foreach (string buttonName in buttons)
        {
            Button button = Instantiate(templateButton, buttonInstantiationArea);
            button.name = buttonName;
            button.GetComponentInChildren<TextMeshProUGUI>().text = buttonName;
            button.gameObject.SetActive(true);
        }

        group.alpha = 1;
        group.blocksRaycasts = group.interactable = true;
        tcs = new TaskCompletionSource<string>();
        return tcs.Task;
    }

    /// <summary>
    /// Called by one of the instantiated buttons when it is clicked.
    /// </summary>
    public void OnButtonClicked(Button button)
    {
        string chosen = button.name;
        print($"QuestionBox: {chosen}");
        group.alpha = 0;
        group.blocksRaycasts = group.interactable = false;

        foreach (Transform child in buttonInstantiationArea)
        {
            if (child.gameObject.activeSelf)
                Destroy(child.gameObject);
        }

        TaskCompletionSource<string> copyTcs = tcs;
        tcs = null;
        copyTcs.SetResult(chosen);
    }
}
