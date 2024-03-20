using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;
using SimpleJSON;

public class LeaderboardCreationManager : MonoBehaviour
{
    [SerializeField] private TMP_InputField numberOfColumnsInput;
    [SerializeField] private TMP_InputField searchInput;
    //[SerializeField] private List<TMP_Dropdown> dataTypeDropdowns;

    [SerializeField] private TextMeshProUGUI errorText;
    [SerializeField] private TextMeshProUGUI keyText;
    [SerializeField] private TextMeshProUGUI generateButtonText;

    [SerializeField] private GameObject selectionPanel;
    [SerializeField] private GameObject columnsPanel;
    [SerializeField] private GameObject tablePanel;

    [SerializeField] private GameObject columnPrefab;
    //[SerializeField] private GameObject tableRowPrefab;
    [SerializeField] private GameObject columnTitlePrefab;
    [SerializeField] private GameObject entryRowPrefab;
    [SerializeField] private GameObject entryPrefab;

    [SerializeField] private Transform entryContent;

    [SerializeField] private List<GameObject> instantiatedColumnPrefabs;  

    private int numberOfColumns = 0;
    private string uniqueKey;
    private string currentLeaderboardKey; // Store the key of the current leaderboard being edited
    private bool isEditMode = false;

    private const string firebaseURL = "https://customizable-leaderboard-default-rtdb.firebaseio.com/leaderboards.json";

    // Generate A Unique Key
    public void OnCreateLeaderboardButtonClick()
    {
        // Generate unique key for the new leaderboard
        uniqueKey = GenerateUniqueKey();
        currentLeaderboardKey = uniqueKey;
        Debug.Log($"Unique Key: {uniqueKey}");
        keyText.text = $"Unique Key: {uniqueKey}";
        isEditMode = false;
        generateButtonText.text = "Generate";
    }

    private string GenerateUniqueKey()
    {
        // Generate unique key logic (e.g., using GUID)
        return System.Guid.NewGuid().ToString();
    }

    // Enter No. of Columns
    public void OnNumberOfColumnsEnterButtonClick()
    {
        // Get the number of columns from the input field
        numberOfColumns = int.Parse(numberOfColumnsInput.text);
        Debug.Log($"Number of Columns: {numberOfColumns}");

        bool onAllow = false;

        if (numberOfColumns <= 0 || numberOfColumns > 5)
        {
            errorText.gameObject.SetActive(true);
            errorText.text = "Please Enter a Number Between 0 and 5!";
            onAllow = false;
        } else
        {
            errorText.gameObject.SetActive(false);
            onAllow = true;
        }

        if (onAllow)
        {
            columnsPanel.SetActive(false);
            selectionPanel.SetActive(true);

            for (int i = 0; i < numberOfColumns; i++)
            {
                GameObject go = Instantiate(columnPrefab, selectionPanel.transform);
                go.GetComponentInChildren<TextMeshProUGUI>().text = $"Column {i + 1} Name/Type: ";
                instantiatedColumnPrefabs.Add(go);
            }
        }     
       
    }

    // Generate Leaderboard based on user inputs
    public void OnGenerateButtonClick()
    {
        if (!isEditMode)
        {
            StartCoroutine(CreateLeaderboardCoroutine());
        }
        else
        {
            OnEditConfirmButtonClick();
        }
    }

    private IEnumerator CreateLeaderboardCoroutine()
    {
        // Construct custom leaderboard configuration JSON
        string configurationJSON = ConstructConfigurationJSON(uniqueKey, numberOfColumns);

        // Send configuration to Firebase
        yield return SendConfigurationToFirebase(configurationJSON);

        DisplayLeaderboardTable();
    }

    // Clear/destroy prefabs and game objects in hierarchy
    public void OnExitLeaderboard()
    {
        for(int i = 0; i < instantiatedColumnPrefabs.Count; i++)
        {
            Destroy(instantiatedColumnPrefabs[i].gameObject);
        }

        if (instantiatedColumnPrefabs.Count > 0)
        {
            instantiatedColumnPrefabs.Clear();
        }
        
        for (int i = tablePanel.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(tablePanel.transform.GetChild(i).gameObject);
        }

        for (int i = entryContent.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(entryContent.transform.GetChild(i).gameObject);
        }

        numberOfColumnsInput.text = "";
        columnsPanel.SetActive(true);
        selectionPanel.SetActive(false);
    }

    // Method to handle the search/edit button click
    public void OnSearchEditButtonClick()
    {
        string searchKey = searchInput.text;
        StartCoroutine(SearchEditLeaderboardCoroutine(searchKey));
    }

    // Coroutine to search/edit the leaderboard
    private IEnumerator SearchEditLeaderboardCoroutine(string searchKey)
    {
        string searchURL = firebaseURL + "?orderBy=\"key\"&equalTo=\"" + searchKey + "\"&print=pretty";
        Debug.Log($"Search URL: {searchURL}");
        UnityWebRequest request = UnityWebRequest.Get(searchURL);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Failed to search leaderboard: " + request.error);
            // Display error message to the user
        }
        else
        {
            string jsonResult = request.downloadHandler.text;
            JSONNode leaderboardData = JSON.Parse(jsonResult);

            if (leaderboardData != null && leaderboardData.Count > 0)
            {
                // Found the leaderboard, parse the JSON data
                JSONNode firstLeaderboard = leaderboardData[0];

                // Retrieve the data from the first leaderboard entry
                // You might need to adjust this part based on the JSON structure
                currentLeaderboardKey = firstLeaderboard["key"];
                string numberOfColumnsString = firstLeaderboard["numberOfColumns"];

                int.TryParse(numberOfColumnsString, out numberOfColumns);

                //Debug.Log($"Number of Columns Input: {numberOfColumnsString}");
                

                // Populate the UI fields with the retrieved data
                numberOfColumnsInput.text = numberOfColumnsString;
                OnNumberOfColumnsEnterButtonClick(); // Update column UI based on the retrieved number of columns

                for (int i = 0; i < numberOfColumns; i++)
                {
                    GameObject columnPrefab = instantiatedColumnPrefabs[i];
                    TMP_InputField inputField = columnPrefab.GetComponentInChildren<TMP_InputField>();
                    TMP_Dropdown dropdown = columnPrefab.GetComponentInChildren<TMP_Dropdown>();

                    inputField.text = firstLeaderboard["columns"][i]["name"];
                    string dataType = firstLeaderboard["columns"][i]["dataType"];

                    Debug.Log($"Column {i + 1} Name: {inputField.text}");
                    Debug.Log($"Column {i + 1} Type: {dataType}");

                    // Find the index of the data type in the dropdown options and set it
                    for (int j = 0; j < dropdown.options.Count; j++)
                    {
                        if (dropdown.options[j].text == dataType)
                        {
                            dropdown.SetValueWithoutNotify(j);
                            break;
                        }
                    }
                }
            }
            else
            {
                // Leaderboard with the specified key not found
                Debug.LogWarning("Leaderboard with key " + searchKey + " not found.");
                // Display appropriate message to the user
            }

            DisplayLeaderboardTable();
        }
    }

    public void OnEditButtonClicked()
    {
        keyText.text = $"Unique Key: {currentLeaderboardKey}";
        generateButtonText.text = "Edit";
        isEditMode = true;
        OnExitLeaderboard();
    }

    // Method to handle the edit button click
    public void OnEditConfirmButtonClick()
    {
        StartCoroutine(EditLeaderboardCoroutine());
    }

    // Coroutine to edit the leaderboard
    private IEnumerator EditLeaderboardCoroutine()
    {
        // Construct custom leaderboard configuration JSON
        string configurationJSON = ConstructConfigurationJSON(currentLeaderboardKey, numberOfColumns);

        // Construct the URL for the specific leaderboard
        string leaderboardURL = "https://customizable-leaderboard-default-rtdb.firebaseio.com/leaderboards/" + currentLeaderboardKey + ".json";

        Debug.Log($"Edit URL: {leaderboardURL}");
        Debug.Log($"Configuration JSON: {configurationJSON}");

        // Send PUT request to update the leaderboard
        UnityWebRequest request = UnityWebRequest.Put(leaderboardURL, configurationJSON);
        request.method = UnityWebRequest.kHttpVerbPUT;
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Failed to update leaderboard: " + request.error);
        }
        else
        {
            Debug.Log("Leaderboard updated successfully!");
            DisplayLeaderboardTable();
        }
    }

    // Create JSON
    private string ConstructConfigurationJSON(string uniqueKey, int numberOfColumns)
    {
        // Construct custom leaderboard configuration JSON
        string configurationJSON = "{\"key\": \"" + uniqueKey + "\", \"numberOfColumns\": " + numberOfColumns + ", \"columns\": [";

        for (int i = 0; i < numberOfColumns; i++)
        {
            GameObject columnPrefab = instantiatedColumnPrefabs[i];
            TMP_InputField inputField = columnPrefab.GetComponentInChildren<TMP_InputField>();
            TMP_Dropdown dropdown = columnPrefab.GetComponentInChildren<TMP_Dropdown>();

            string columnName = inputField.text;
            string dataType = dropdown.options[dropdown.value].text;

            configurationJSON += "{\"name\": \"" + columnName + "\", \"dataType\": \"" + dataType + "\"}";

            if (i < numberOfColumns - 1)
            {
                configurationJSON += ",";
            }
        }

        configurationJSON += "]}";

        return configurationJSON;
    }

    // Method to display the leaderboard as a table
    private void DisplayLeaderboardTable()
    {
        // Populate the table with leaderboard data
        // You can fetch data from Firebase here and populate the table rows accordingly
        // For demonstration purposes, let's assume we have some sample data

        for (int i = 0; i < instantiatedColumnPrefabs.Count; i++)
        {
            GameObject columnTitle = Instantiate(columnTitlePrefab, tablePanel.transform);
            columnTitle.GetComponent<TextMeshProUGUI>().text = instantiatedColumnPrefabs[i].GetComponentInChildren<TMP_InputField>().text;
        }

        List<string[]> sampleData = new List<string[]>
        {
            /*new string[] { "Player 1", "100", "50", "true" },
            new string[] { "Player 2", "80", "60", "false" },
            new string[] { "Player 3", "120", "70", "true" }*/
        };

        // Instantiate table rows and populate them with data
        foreach (var rowData in sampleData)
        {
            GameObject entryRow = Instantiate(entryRowPrefab, entryContent.transform);
            foreach (var entry in rowData)
            {
                GameObject entryGo = Instantiate(entryPrefab, entryRow.transform);
                entryGo.GetComponent<TextMeshProUGUI>().text = entry;
            }
        }
    }

    private IEnumerator SendConfigurationToFirebase(string configurationJSON)
    {

        // Construct the URL with the GUID key
        string firebaseURLWithKey = "https://customizable-leaderboard-default-rtdb.firebaseio.com/leaderboards/" + uniqueKey + ".json";
        Debug.Log($"Firebase URL: {firebaseURLWithKey}");

        UnityWebRequest request = new UnityWebRequest(firebaseURLWithKey, "PUT");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(configurationJSON);
        request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Failed to create leaderboard: " + request.error);
        }
        else
        {
            Debug.Log("Leaderboard created successfully!");
        }
    }

    /*private IEnumerator SendConfigurationToFirebase(string configurationJSON)
    {
        UnityWebRequest request = new UnityWebRequest(firebaseURL, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(configurationJSON);
        request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Failed to create leaderboard: " + request.error);
        }
        else
        {
            Debug.Log("Leaderboard created successfully!");

            // Parse the JSON configuration to get the unique key
            JSONNode configuration = JSON.Parse(configurationJSON);
            string uniqueKey = configuration["key"];

            // Construct the JSON for columns
            JSONObject columnsJSON = new JSONObject();
            for (int i = 0; i < numberOfColumns; i++)
            {
                JSONObject columnJSON = new JSONObject();
                string dataType = GetDataTypeFromDropdown(dataTypeDropdowns[i]);
                columnJSON.Add("name", new JSONString($"Column {i + 1}"));
                columnJSON.Add("dataType", new JSONString(dataType));
                columnsJSON.Add($"column{i + 1}", columnJSON);
            }

            // Send columns data to Firebase
            string columnsJSONString = columnsJSON.ToString();
            UnityWebRequest updateRequest = new UnityWebRequest(firebaseURL + "/" + uniqueKey + "/columns.json", "PUT");
            byte[] columnsBodyRaw = System.Text.Encoding.UTF8.GetBytes(columnsJSONString);
            updateRequest.uploadHandler = (UploadHandler)new UploadHandlerRaw(columnsBodyRaw);
            updateRequest.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            updateRequest.SetRequestHeader("Content-Type", "application/json");
            yield return updateRequest.SendWebRequest();
        }
    }*/

    /*private string GetDataTypeFromDropdown(TMP_Dropdown dropdown)
    {
        // Get the selected data type from the dropdown
        switch (dropdown.value)
        {
            case 0:
                return "String";
            case 1:
                return "Int";
            case 2:
                return "Float";
            case 3:
                return "Bool";
            default:
                return "String"; // Default to string if no option is selected
        }
    }*/
}
