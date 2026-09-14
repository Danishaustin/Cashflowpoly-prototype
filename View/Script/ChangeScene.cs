using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ChangeScene : MonoBehaviour
{
    public static ChangeScene Instance;
    private Coroutine loadSceneCoroutine;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void ChangeToScene(int sceneID)
    {
        if (loadSceneCoroutine != null)
        {
            StopCoroutine(loadSceneCoroutine);
        }

        loadSceneCoroutine = StartCoroutine(ChangeToSceneAsyncRoutine(sceneID));
    }

    private IEnumerator ChangeToSceneAsyncRoutine(int sceneID)
    {
        Application.backgroundLoadingPriority = ThreadPriority.High;

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneID, LoadSceneMode.Single);
        if (operation == null)
        {
            loadSceneCoroutine = null;
            yield break;
        }

        while (!operation.isDone)
        {
            yield return null;
        }

        loadSceneCoroutine = null;
    }
}
