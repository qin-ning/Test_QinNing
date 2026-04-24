using UnityEngine;
using UnityEngine.Events;

public class EventCenter
{
    /// <summary>
    /// 背包状态变化事件
    /// </summary>
    public static UnityEvent<int, int> onPacksackChanged = new UnityEvent<int, int>();

    /// <summary>
    /// 工厂告警事件
    /// </summary>
    public static UnityEvent<Producer, string> onProducerWarning = new UnityEvent<Producer, string>();
}
