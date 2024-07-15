using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModelManager : MonoBehaviour
{
    public List<Transform> models;
    int currentIndex = 0;
    // Start is called before the first frame update
    void Start()
    {
        foreach (var model in models)
        {
            model.gameObject.SetActive(false);
        };
        models[currentIndex].gameObject.SetActive(true);
    }
    // Update is called once per frame
    void Update()
    {
        
    }

    public void NextCharacter()
    {
        if (currentIndex < models.Count-1)
        {
            models[currentIndex].gameObject.SetActive(false);
            currentIndex++;
            models[currentIndex].gameObject.SetActive(true);
        }

    }

    public void PreviousCharacter()
    {
        if (currentIndex > 0)
        {
            models[currentIndex].gameObject.SetActive(false);
            currentIndex--;
            models[currentIndex].gameObject.SetActive(true);
        }
    }

    public void CloseApp()
    {
        Application.Quit();
    }
}
