using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class LeaderboardTemplateSelector : MonoBehaviour
{

    public enum Columns
    {
        ONE,
        TWO,
        THREE,
        FOUR,
        FIVE
    }

    public Columns columns;

    public List<GameObject> template;

    private LeaderboardCreationManager leaderboardCreationManager;

    // Start is called before the first frame update
    void Start()
    {
        leaderboardCreationManager = GameObject.Find("LeaderboardCreationManager").GetComponent<LeaderboardCreationManager>();

        switch (columns)
        {
            case Columns.ONE:
                template.Add(GameObject.Find("TemplatePanel01").transform.GetChild(0).gameObject);
                break;
            case Columns.TWO:
                template.Add(GameObject.Find("TemplatePanel02").transform.GetChild(0).gameObject);
                template.Add(GameObject.Find("TemplatePanel02").transform.GetChild(1).gameObject);
                break;
            case Columns.THREE:
                template.Add(GameObject.Find("TemplatePanel03").transform.GetChild(0).gameObject);
                template.Add(GameObject.Find("TemplatePanel03").transform.GetChild(1).gameObject);
                template.Add(GameObject.Find("TemplatePanel03").transform.GetChild(2).gameObject);
                break;
            case Columns.FOUR:
                template.Add(GameObject.Find("TemplatePanel04").transform.GetChild(0).gameObject);
                template.Add(GameObject.Find("TemplatePanel04").transform.GetChild(1).gameObject);
                template.Add(GameObject.Find("TemplatePanel04").transform.GetChild(2).gameObject);
                template.Add(GameObject.Find("TemplatePanel04").transform.GetChild(3).gameObject);
                break;
            case Columns.FIVE:
                template.Add(GameObject.Find("TemplatePanel05").transform.GetChild(0).gameObject);
                template.Add(GameObject.Find("TemplatePanel05").transform.GetChild(1).gameObject);
                template.Add(GameObject.Find("TemplatePanel05").transform.GetChild(2).gameObject);
                template.Add(GameObject.Find("TemplatePanel05").transform.GetChild(3).gameObject);
                template.Add(GameObject.Find("TemplatePanel05").transform.GetChild(4).gameObject);
                break;
        }
    }

    public void onTemplateButtonClick()
    {
        leaderboardCreationManager.OnCreateLeaderboardButtonClick();

        int columns = template.Count;

        leaderboardCreationManager.numberOfColumnsInput.text = columns.ToString();
        leaderboardCreationManager.OnNumberOfColumnsEnterButtonClick();

        for (int i = 0; i < columns; i++)
        {
            string columnText = template[i].GetComponent<TextMeshProUGUI>().text.ToString();
            

            string dataType = columnText.Substring(columnText.Length - 3);
            columnText = columnText.Remove(columnText.Length - 3);
            Debug.Log($"{columnText}: {dataType}");

            TMP_Dropdown dataTypeDd = leaderboardCreationManager.instantiatedColumnPrefabs[i].GetComponentInChildren<TMP_Dropdown>();

            switch (dataType)
            {
                case "(S)":
                    dataTypeDd.options[dataTypeDd.value].text = "String";
                    break;
                case "(I)":
                    dataTypeDd.options[dataTypeDd.value].text = "Int";
                    break;
                case "(F)":
                    dataTypeDd.options[dataTypeDd.value].text = "Float";
                    break;
                case "(B)":
                    dataTypeDd.options[dataTypeDd.value].text = "Bool";
                    break;
            }

            leaderboardCreationManager.instantiatedColumnPrefabs[i].GetComponentInChildren<TMP_InputField>().text = columnText;
          
        }

        leaderboardCreationManager.OnGenerateButtonClick();
    }
}
