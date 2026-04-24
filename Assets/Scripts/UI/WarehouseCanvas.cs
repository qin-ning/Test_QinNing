using TMPro;
using UnityEngine;

public class WarehouseCanvas : MonoBehaviour
{
    public TextMeshProUGUI statusText;

    public void RefreshStatus(string str)
    {
        statusText.text = str;
    }
}
