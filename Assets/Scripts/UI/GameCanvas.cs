using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Windows;

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
    }

    private void Start()
    {
        producerWarningTextPrefab.gameObject.SetActive(false);
        EventCenter.onPacksackChanged.AddListener(RefreshPacksackInfo);
        EventCenter.onProducerWarning.AddListener(ProducerWarning);
    }

    private void RefreshPacksackInfo(int used, int capacity)
    {
        packsackInfoText.text = $"背包容量:{used}/{capacity}";
    }

    private void ProducerWarning(Producer producer, string warning)
    {
        TextMeshProUGUI textMeshProUGUI;
        if (!producerToWarning.TryGetValue(producer, out textMeshProUGUI))
        {
            textMeshProUGUI = GameObject.Instantiate(producerWarningTextPrefab, producerWarningTextPrefab.transform.parent);
            producerToWarning.Add(producer, textMeshProUGUI);
        }
        //清除遗留告警信息
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
