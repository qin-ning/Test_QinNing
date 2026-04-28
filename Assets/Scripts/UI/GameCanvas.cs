using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameCanvas : MonoBehaviour
{
    public static GameCanvas instance;

    [SerializeField]
    private TextMeshProUGUI packsackInfoText;

    [SerializeField]
    private TextMeshProUGUI producerWarningTextPrefab;


    void Awake()
    {
        instance = this;
        EventCenter.onPacksackChanged.AddListener(RefreshPacksackInfo);
        EventCenter.onProducerWarning.AddListener(ProducerWarning);
    }

    private void Start()
    {
        producerWarningTextPrefab.gameObject.SetActive(false);
    }

    private void RefreshPacksackInfo(int used, int capacity)
    {
        packsackInfoText.text = $"��������:{used}/{capacity}";
    }

    private void ProducerWarning(Producer producer, string warning)
    {
        TextMeshProUGUI textMeshProUGUI;
        if (!producerToWarning.TryGetValue(producer, out textMeshProUGUI))
        {
            textMeshProUGUI = GameObject.Instantiate(producerWarningTextPrefab, producerWarningTextPrefab.transform.parent);
            producerToWarning.Add(producer, textMeshProUGUI);
        }
        //��������澯��Ϣ
        if (warning is null)
        {
            textMeshProUGUI.gameObject.SetActive(false);
            return;
        }
        textMeshProUGUI.text = $" {producer.name}:{warning}";
        textMeshProUGUI.transform.SetAsFirstSibling();
        textMeshProUGUI.gameObject.SetActive(true);
    }
    private Dictionary<Producer, TextMeshProUGUI> producerToWarning = new Dictionary<Producer, TextMeshProUGUI>();
}
