using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using SimpleJSON;

public class LeaderboardEntryManager : MonoBehaviour
{
    [SerializeField] private TMP_InputField[] dataInputFields;
    private string firebaseURL = "https://customizable-leaderboard-default-rtdb.firebaseio.com/";
    [SerializeField] private string uniqueKey = "YOUR_UNIQUE_LEADERBOARD_KEY"; // Set your unique leaderboard key

    [SerializeField] private GameObject tableRows;
    [SerializeField] private GameObject columnTitlePrefab;

    [SerializeField] private GameObject entryRowPrefab;
    [SerializeField] private GameObject entryPrefab;

    [SerializeField] private Transform entryContent;

    [System.Serializable]
    public class ColumnInfo
    {
        public string name;
        public string dataType;
    }

    // This method is triggered when the user clicks the "Enter" button
    public void OnEnterButtonClick()
    {
        StartCoroutine(FetchColumnInfos(uniqueKey, columnInfos =>
        {
            if (columnInfos == null)
            {
                Debug.LogError("Failed to fetch column information.");
                return;
            }

            Debug.Log("Column information fetched successfully.");
            Debug.Log("Constructing player data JSON...");
            string playerDataJson = ConstructPlayerDataJson(columnInfos);
            Debug.Log("Player data JSON constructed:\n" + playerDataJson);

            if (!string.IsNullOrEmpty(playerDataJson))
            {
                Debug.Log("Sending player data to Firebase...");
                StartCoroutine(SendPlayerDataToFirebase(uniqueKey, playerDataJson));
            }
        }));
    }

    // Fetches column information for the specified leaderboard
    private IEnumerator FetchColumnInfos(string leaderboardKey, System.Action<ColumnInfo[]> onColumnsFetched)
    {
        string url = $"{firebaseURL}leaderboards/{leaderboardKey}/columns.json";
        UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Error fetching column info: " + request.error);
            onColumnsFetched?.Invoke(null);
            yield break;
        }

        string jsonText = request.downloadHandler.text;
        JSONArray jsonArray = JSON.Parse(jsonText) as JSONArray;

        if (jsonArray == null)
        {
            Debug.LogError("Error parsing JSON for column info.");
            onColumnsFetched?.Invoke(null);
            yield break;
        }

        ColumnInfo[] columnInfos = ParseColumnInfos(jsonArray);
        onColumnsFetched?.Invoke(columnInfos);
    }

    // Custom method to parse JSON array into ColumnInfo objects
    private ColumnInfo[] ParseColumnInfos(JSONArray jsonArray)
    {
        List<ColumnInfo> columnInfos = new List<ColumnInfo>();

        foreach (JSONNode jsonNode in jsonArray)
        {
            string name = jsonNode["name"].Value;
            string dataType = jsonNode["dataType"].Value;

            ColumnInfo columnInfo = new ColumnInfo();
            columnInfo.name = name;
            columnInfo.dataType = dataType;

            columnInfos.Add(columnInfo);
        }

        return columnInfos.ToArray();
    }

    // Constructs a JSON string based on the input fields and column information
    private string ConstructPlayerDataJson(ColumnInfo[] columns)
    {
        if (dataInputFields.Length != columns.Length)
        {
            Debug.LogError("Mismatch between input fields and columns.");
            return "";
        }

        JSONObject data = new JSONObject();

        for (int i = 0; i < columns.Length; i++)
        {
            string fieldName = columns[i].name;
            string fieldValue = dataInputFields[i].text;

            data.Add(fieldName, fieldValue);
        }

        return data.ToString();
    }

    // Sends the constructed player data JSON to Firebase under the appropriate leaderboard
    private IEnumerator SendPlayerDataToFirebase(string leaderboardKey, string playerDataJson)
    {
        string url = $"{firebaseURL}leaderboards/{leaderboardKey}/entries.json";
        UnityWebRequest request = UnityWebRequest.Put(url, playerDataJson);
        request.method = "POST";
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {            
            Debug.Log("Player data added to Firebase successfully!");
            // Fetch the leaderboard data from Firebase
            yield return FetchLeaderboardData(uniqueKey);
        }
        else
        {
            Debug.LogError("Failed to send player data to Firebase: " + request.error);
        }
    }

    private IEnumerator FetchLeaderboardData(string leaderboardKey)
    {
        Debug.Log("Fetching..");

        // Construct the URL to fetch leaderboard data from Firebase
        string firebaseURL = "https://customizable-leaderboard-default-rtdb.firebaseio.com/leaderboards/" + leaderboardKey + ".json";

        UnityWebRequest request = UnityWebRequest.Get(firebaseURL);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Failed to fetch leaderboard data: " + request.error);
        }
        else
        {
            string jsonData = request.downloadHandler.text;
            JSONNode leaderboardData = JSON.Parse(jsonData);

            // Display the leaderboard table with fetched data
            DisplayLeaderboardTable(leaderboardData);
        }
    }

    // Method to display the leaderboard as a table
    private void DisplayLeaderboardTable(JSONNode leaderboardData)
    {
        if (leaderboardData == null)
        {
            Debug.LogWarning("No leaderboard data found.");
            return;
        }

        // Clear existing table content
        ClearTable();

        // Check if columns node exists
        if (!leaderboardData.HasKey("columns"))
        {
            Debug.LogWarning("No columns found in leaderboard data.");
            return;
        }

        // Get column names from the leaderboard data
        JSONNode columns = leaderboardData["columns"];

        // Populate the table with column names
        foreach (JSONNode column in columns)
        {
            string columnName = column["name"];
            GameObject columnTitle = Instantiate(columnTitlePrefab, tableRows.transform);
            columnTitle.GetComponent<TextMeshProUGUI>().text = columnName;
        }

        // Check if entries node exists
        if (!leaderboardData.HasKey("entries"))
        {
            Debug.LogWarning("No entries found in leaderboard data.");
            return;
        }

        // Populate the table with leaderboard entries
        JSONNode entries = leaderboardData["entries"];
        foreach (KeyValuePair<string, JSONNode> entry in entries)
        {
            // Create a new row in the table and populate it with the entry data
            GameObject entryRow = Instantiate(entryRowPrefab, entryContent);
            foreach (KeyValuePair<string, JSONNode> field in entry.Value)
            {
                string fieldName = field.Key;
                string fieldValue = field.Value;
                // Add each entry field to the row
                GameObject entryGo = Instantiate(entryPrefab, entryRow.transform);
                entryGo.GetComponent<TextMeshProUGUI>().text = fieldValue;
            }
        }
    }

    // Method to clear the table content
    private void ClearTable()
    {
        foreach (Transform child in tableRows.transform)
        {
            Destroy(child.gameObject);
        }
        foreach (Transform child in entryContent)
        {
            Destroy(child.gameObject);
        }
    }
}
