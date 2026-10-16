using UnityEngine;
using TMPro;

public class MainMenuPC : MonoBehaviour
{
    [SerializeField] private TMP_InputField sessionInputField;
    [SerializeField] private TMP_InputField playerInputField;

    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetSessionName(string name)
    {
        name = sessionInputField.text.Trim();
        NetworkManager.Instance.SetSessionName(name);
    }
    public void SetPlayerName(string name)
    {
        name = playerInputField.text.Trim();
        NetworkManager.Instance.SetPlayerName(name);
    }

    public void StartGame()
    {
        NetworkManager.Instance.StartGame();
    }
}
