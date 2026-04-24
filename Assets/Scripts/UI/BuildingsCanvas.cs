using TMPro;
using UnityEngine;

public class BuildingsCanvas : MonoBehaviour
{
    public TextMeshProUGUI describeText;

    public void RefreshDescribe(string str)
    {
        describeText.text = str;
    }

}
