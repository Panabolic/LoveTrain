using UnityEngine;
using TMPro;
using DG.Tweening;
using UnityEngine.UI;
using System.Collections.Generic;
using NUnit.Framework;
public class EventManager : MonoBehaviour
{
    public EventManager Instance;

    [SerializeField]
    private GameObject eventUIPanel;
    [SerializeField]
    private GameObject eventImage;
    [SerializeField]
    private GameObject eventSelections;
    [SerializeField]
    private GameObject eventTextBox;

    private void Awake()
    {
        Instance = this;

        if(Instance == null)
        {
            Instantiate(this);
        }
    }

    [SerializeField]
    private List<SO_Event> events;

    public void StartEvent(SO_Event e)
    {

    }

    public void TextPlay(string text)
    {

    }

    public void SelectionChoice(SO_Event e)
    {

    }
}
