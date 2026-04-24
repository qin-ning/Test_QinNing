using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Producer : MonoBehaviour, ISender, IReceiver
{
    /// <summary>
    /// 输入配置:(空则表示不用输入即可产出)
    /// 子项InputConfig数量设置：产出一个产品所需的每个输入产品数 
    /// </summary>
    [SerializeField]
    private List<InputConfig> inputConfigs;

    /// <summary>
    /// 产出产品(模板)
    /// </summary>
    [SerializeField]
    private Product outputProductTemplate;

    /// <summary>
    /// 产出速度（秒/每个）
    /// </summary>
    [SerializeField]
    private float producSpeed;

    /// <summary>
    /// 输入仓库
    /// </summary>
    [SerializeField]
    private Warehouse inputWarehouse;

    /// <summary>
    /// 输出仓库
    /// </summary>
    [SerializeField]
    private Warehouse outputWarehouse;

    [SerializeField]
    private BuildingsCanvas buildingsCanvas;


    private void Start()
    {
        RefreshDescribeUI();

        StartCoroutine(ProducerConduct());
    }

    #region ISender实现
    /// <summary>
    /// 请求产品
    /// </summary>
    public void RequestProduct(Product.ProductType productType, IReceiver receiver, float sendTime, int count = int.MaxValue)
    {

    }

    /// <summary>
    /// 停止产品请求
    /// </summary>
    /// <param name="receiver"></param>
    public void StopRequestProduct(IReceiver receiver)
    {

    }

    /// <summary>
    /// 查询可提供产品类型
    /// </summary>
    /// <returns></returns>
    public Product.ProductType[] ProductOfferings()
    {
        return new Product.ProductType[1] { outputProductTemplate.productType };
    }

    /// <summary>
    /// 查询某类产品库存
    /// </summary>
    /// <returns></returns>
    public int GetProductCount(Product.ProductType productType)
    {
        return 0;
    }

    #endregion

    #region IReceiver实现

    /// <summary>
    /// 开始接收产品（动画开始）
    /// </summary>
    /// <param name="product"></param>
    public void StartReceiv(Product product)
    {

    }

    /// <summary>
    /// 产品接收完毕（动画结束）
    /// </summary>
    /// <param name="product"></param>
    public void ReceivDone(Product product)
    {
        if (!receivTemp.ContainsKey(product.productType))
            receivTemp.Add(product.productType, 0);
        receivTemp[product.productType]++;
        ObjectPool.Recycle(product);
    }

    /// <summary>
    /// 查询剩余空闲存储
    /// </summary>
    /// <returns></returns>
    public int GetAvailCapacity(Product.ProductType productType)
    {
        return int.MaxValue;
    }

    public Vector2Int GetTopSlotPointer(Product.ProductType productType)
    {
        return Vector2Int.zero;
    }

    public Vector3 GetSlotPos(Vector2Int slotIndex)
    {
        return transform.position;
    }

    public Quaternion GetSlotRotation(Vector2Int slotIndex)
    {
        return transform.rotation;
    }

    #endregion

    /// <summary>
    /// 产品输出到仓库
    /// </summary>
    private void Output()
    {
        Product product = ObjectPool.GetProduct(outputProductTemplate);
        outputWarehouse.StartReceiv(product);
        StartCoroutine(ProductSending(product, delegate ()
        {
            Vector2Int slotIndex = outputWarehouse.GetTopSlotPointer(product.productType);
            Vector3 slotPos = outputWarehouse.GetSlotPos(slotIndex);
            return slotPos;
        }, delegate ()
        {
            Vector2Int slotIndex = outputWarehouse.GetTopSlotPointer(product.productType);
            return outputWarehouse.GetSlotRotation(slotIndex);
        }, delegate { outputWarehouse.ReceivDone(product); }));
    }

    private void RefreshDescribeUI()
    {
        string desc = "";
        if (inputConfigs != null && inputConfigs.Count > 0)
        {
            desc = "输入输出比例:";
            string[] name = new string[inputConfigs.Count + 1];
            string[] ratio = new string[inputConfigs.Count + 1];
            for (int i = 0; i < inputConfigs.Count; i++)
            {
                name[i] = inputConfigs[i].productTemplate.productType.ToString();
                ratio[i] = inputConfigs[i].ratio.ToString();
            }
            name[inputConfigs.Count] = outputProductTemplate.productType.ToString();
            ratio[inputConfigs.Count] = "1";
            desc += name[0];
            for (int i = 1; i < name.Length; i++)
                desc += $":{name[i]}";
            desc += $"={ratio[0]}";
            for (int i = 1; i < ratio.Length; i++)
                desc += $":{ratio[i]}";

        }
        else
            desc = "输入:无";
        desc += $"<br>输出:{outputProductTemplate.productType}";
        desc += $"<br>生产速度:{producSpeed}(秒/每个)";
        buildingsCanvas.RefreshDescribe(desc);
    }

    /// <summary>
    /// 用于控制生产的协程
    /// </summary>
    /// <returns></returns>
    IEnumerator ProducerConduct()
    {
        float timeCouter = 0;
        uint warningMark = 0;
        while (true)
        {
            //输入端就绪状态
            bool inputReady = true;
            //判断入产品的库存是否达到要求数量
            if (inputConfigs != null && inputConfigs.Count > 0)
            {
                for (int i = 0; i < inputConfigs.Count; i++)
                {
                    //该输入产品的库存达不到要求数量
                    if (inputWarehouse.GetProductCount(inputConfigs[i].productTemplate.productType) < inputConfigs[i].ratio)
                    {
                        inputReady = false;
                        break;
                    }
                }
            }
            if (!inputReady)
            {
                //通知UI
                warningMark = 1;
                EventCenter.onProducerWarning.Invoke(this, "输入产品的库存达不到要求数量");
                yield return null;
                continue;
            }

            //输出端就绪状态
            bool outputReady = outputWarehouse.GetAvailCapacity(outputProductTemplate.productType) > 0;
            if (!outputReady)
            {
                //通知UI
                warningMark = 2;
                EventCenter.onProducerWarning.Invoke(this, "输出仓库已存储空间已满");
                yield return null;
                continue;
            }
            //清除告警
            if (warningMark != 0)
                EventCenter.onProducerWarning.Invoke(this, null);
            warningMark = 0;

            //准备生产
            //按输入配置比例获取输入产品
            for (int i = 0; i < inputConfigs.Count; i++)
            {
                inputWarehouse.RequestProduct(inputConfigs[i].productTemplate.productType, this, producSpeed / inputConfigs[i].ratio, inputConfigs[i].ratio);
            }
            //等待输入产品到达工厂
            bool receivDone = false;
            while (!receivDone)
            {
                yield return null;
                timeCouter += Time.deltaTime;
                receivDone = true;
                for (int i = 0; i < inputConfigs.Count; i++)
                {
                    if (!receivTemp.ContainsKey(inputConfigs[i].productTemplate.productType) || receivTemp[inputConfigs[i].productTemplate.productType] < inputConfigs[i].ratio)
                    {
                        receivDone = false;
                        break;
                    }
                }
            }
            receivTemp.Clear();

            //生产
            Output();

            while (timeCouter < producSpeed)
            {
                yield return null;
                timeCouter += Time.deltaTime;
            }
            timeCouter = 0;
        }

    }
    Dictionary<Product.ProductType, int> receivTemp = new Dictionary<Product.ProductType, int>();

    /// <summary>
    /// 控制产品发送动画的协程
    /// </summary>
    /// <returns></returns>
    IEnumerator ProductSending(Product product, Func<Vector3> targetPosFun, Func<Quaternion> targetQuaternionFun, Action callback)
    {
        Transform productTransform = product.transform;
        productTransform.position = transform.position;
        float timeCounter = 0;
        productTransform.SetParent(null);
        while (Vector3.SqrMagnitude(productTransform.position - targetPosFun()) > 0.1f)
        {
            yield return null;
            timeCounter += Time.deltaTime;
            //控制发送动画的时间与生产时间相同
            productTransform.rotation = Quaternion.Lerp(productTransform.rotation, targetQuaternionFun(), timeCounter / producSpeed);
            productTransform.position = Vector3.Lerp(transform.position, targetPosFun(), timeCounter / producSpeed);
        }
        productTransform.rotation = targetQuaternionFun();
        productTransform.position = targetPosFun();
        callback();
    }
}

[Serializable]
public class InputConfig
{
    /// <summary>
    /// 产品模板
    /// </summary>
    public Product productTemplate;
    /// <summary>
    /// 投入比例
    /// </summary>
    public int ratio;
}