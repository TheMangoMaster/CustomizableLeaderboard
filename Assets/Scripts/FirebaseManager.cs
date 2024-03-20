using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class FirebaseManager : MonoBehaviour
{
    private string firebaseURL = "https://customizable-leaderboard-default-rtdb.firebaseio.com";

    void Start()
    {
        StartCoroutine(GetDataFromFirebase());
    }

    IEnumerator GetDataFromFirebase()
    {
        UnityWebRequest www = UnityWebRequest.Get(firebaseURL);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Failed to fetch data from Firebase: " + www.error);
        }
        else
        {
            Debug.Log("Successfully fetched date from Firebase: " + www.result);

            string jsonData = www.downloadHandler.text;
            // Parse and process jsonData
        }
    }
}
