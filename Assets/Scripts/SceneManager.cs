using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SceneManager : MonoBehaviour
{
    public CanvasGroup helpMenu;


    //public CanvasGroup resetConfirmPanel;
    public bool isInHelp = false;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (isInHelp)
        {
            if (isInHelp && Input.GetKeyDown(KeyCode.Escape))
            {
                ToggleHelpPanel();
            }
            return; // Block all other input
        }
    }
    public void HideHelpMenu()
    {
        helpMenu.alpha = 0f;
        helpMenu.interactable = false;
        helpMenu.blocksRaycasts = false;
    }
    public void ToggleHelpPanel()
    {
        isInHelp = !isInHelp;
        helpMenu.alpha = isInHelp ? 1f : 0f;
        helpMenu.interactable = isInHelp;
        helpMenu.blocksRaycasts = isInHelp;
    }




}
