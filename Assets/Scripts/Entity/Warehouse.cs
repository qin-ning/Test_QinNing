using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;


/// <summary>
/// 仓库
/// </summary>
public class Warehouse : MonoBehaviour, IReceiver, ISender
{
    /// <summary>
    /// 仓库类型（输入/输出）
    /// </summary>
    public WarehouseType warehouseType;

    /// <summary>
    /// 仓库支持存储的类目
    /// </summary>
    [SerializeField]
    private List<ProductStore> productStores;

    /// <summary>
    /// 定义每只槽位宽度
    /// </summary>
    [SerializeField]
    private float slotWith;

    /// <summary>
    /// 定义每只槽位高度
    /// </summary>
    [SerializeField]
    private float slotHeight;

    /// <summary>
    /// 定义槽位列数(行数由各类产品容量计算)
    /// </summary>
    [SerializeField]
    private int columnCount;

    /// <summary>
    /// 存放产品的槽位
    /// </summary>
    [SerializeField]
    private Product[,] slots;

    [SerializeField]
    private WarehouseCanvas warehouseCanvas;

    /// <summary>
    /// 每类产品存放的槽位列范围(按列分区域存放各类产品)
    /// </summary>
    private Dictionary<Product.ProductType, RangeInt> productStoreColumnRange;

    void Awake()
    {
        int rowCountMax = 0;

        productStoreColumnRange = new Dictionary<Product.ProductType, RangeInt>();
        int priorEnd = -1;
        for (int i = 0; i < productStores.Count; i++)
        {
            int start = priorEnd + 1;
            int end = (int)((float)(i + 1) / productStores.Count * (columnCount - 1));
            priorEnd = end;
            productStoreColumnRange.Add(productStores[i].productTemplate.productType, new RangeInt(start, end - start));

            //该品类占用行数
            int rowCount = (int)MathF.Ceiling((float)productStores[i].capacity / productStoreColumnRange[productStores[i].productTemplate.productType].length);
            if (rowCountMax < rowCount)
                rowCountMax = rowCount;
        }
        slots = new Product[rowCountMax, columnCount];
    }

    void Start()
    {
        RefreshStoreUI();
    }

    #region ISender实现
    /// <summary>
    /// 请求产品
    /// </summary>
    public void RequestProduct(Product.ProductType productType, IReceiver receiver, float sendTime, int count = int.MaxValue)
    {
        List<Coroutine> coroutines;

        if (!postProductConductDir.TryGetValue(receiver, out coroutines))
        {
            coroutines = new List<Coroutine>();
            postProductConductDir.Add(receiver, coroutines);
        }

        Coroutine enumerator = StartCoroutine(SendProductConduct(productType, receiver, sendTime, count));
        coroutines.Add(enumerator);

    }
    private Dictionary<IReceiver, List<Coroutine>> postProductConductDir = new Dictionary<IReceiver, List<Coroutine>>();

    /// <summary>
    /// 停止产品请求
    /// </summary>
    /// <param name="receiver"></param>
    public void StopRequestProduct(IReceiver receiver)
    {
        if (postProductConductDir.ContainsKey(receiver))
        {
            for (int i = 0; i < postProductConductDir[receiver].Count; i++)
                StopCoroutine(postProductConductDir[receiver][i]);
            postProductConductDir.Remove(receiver);
        }
    }

    /// <summary>
    /// 查询可提供产品类型
    /// </summary>
    /// <returns></returns>
    public Product.ProductType[] ProductOfferings()
    {
        return productStores.Select(p => p.productTemplate.productType).ToArray();
    }

    /// <summary>
    /// 查询某类产品库存
    /// </summary>
    /// <returns></returns>
    public int GetProductCount(Product.ProductType productType)
    {
        ProductStore productStore = productStores.FirstOrDefault(x => x.productTemplate.productType == productType);
        if (productStore == null)
            return 0;
        return productStore.used;
    }
    #endregion

    #region IReceiver实现

    /// <summary>
    /// 开始接收产品（动画开始）
    /// </summary>
    /// <param name="product"></param>
    public void StartReceiv(Product product)
    {
        if (!receiveIngRecord.ContainsKey(product.productType))
            receiveIngRecord.Add(product.productType, 0);

        receiveIngRecord[product.productType] = receiveIngRecord[product.productType] + 1;
    }
    private Dictionary<Product.ProductType, int> receiveIngRecord = new Dictionary<Product.ProductType, int>();

    /// <summary>
    /// 产品接收完毕（动画结束）
    /// </summary>
    /// <param name="product"></param>
    public void ReceivDone(Product product)
    {
        receiveIngRecord[product.productType] = receiveIngRecord[product.productType] - 1;
        ProductStore productStore = productStores.FirstOrDefault(x => x.productTemplate.productType == product.productType);
        if (productStore == null)
        {
            Debug.LogError($"仓库试图存入不支持品类:{product.productType}");
            return;
        }
        Vector2Int slotIndex = GetTopSlotPointer(product.productType);
        MoveTopSlotPointerUp(product.productType);
        slots[slotIndex.x, slotIndex.y] = product;
        productStore.used++;
        RefreshStoreUI();
        product.transform.SetParent(transform);
    }

    /// <summary>
    /// 查询某类产品在仓库的空余容量
    /// </summary>
    /// <returns></returns>
    public int GetAvailCapacity(Product.ProductType productType)
    {
        ProductStore productStore = productStores.FirstOrDefault(x => x.productTemplate.productType == productType);
        if (productStore == null)
            return 0;
        int availCapacity = productStore.capacity - productStore.used;
        if (receiveIngRecord.ContainsKey(productType))
            availCapacity -= receiveIngRecord[productType];
        return availCapacity;
    }

    /// <summary>
    /// 获取槽位世界坐标
    /// </summary>
    /// <param name="slotIndex"></param>
    /// <returns></returns>
    public Vector3 GetSlotPos(Vector2Int slotIndex)
    {
        float offset = columnCount * slotWith * 0.5f;
        Vector3 vector3 = transform.position
                      + transform.right * (slotIndex.y * slotWith - offset)
                      + transform.up * slotIndex.x * slotHeight;
        return vector3;
    }

    /// <summary>
    /// 获取槽位rotation
    /// </summary>
    /// <param name="slotIndex"></param>
    /// <returns></returns>
    public Quaternion GetSlotRotation(Vector2Int slotIndex)
    {
        return transform.rotation;
    }
    #endregion

    IEnumerator SendProductConduct(Product.ProductType productType, IReceiver receiver, float sendTime, int count)
    {
        //发送产品时间间隔
        float sendInterval = sendTime;
        float timeCounter = 0;
        int sendCount = 0;
        while (true)
        {
            if (receiver.GetAvailCapacity(productType) <= 0)
            {
                //Debug.LogError("存储已满");
                yield return null;
                continue;
            }

            if (GetProductCount(productType) <= 0)
            {
                //Debug.LogError("仓库已空");
                yield return null;
                continue;
            }

            Product product = PopTopProduct(productType);
            receiver.StartReceiv(product);
            RefreshStoreUI();

            StartCoroutine(ProductSending(product, sendTime, delegate ()
            {
                Vector2Int slotIndex = receiver.GetTopSlotPointer(productType);
                Vector3 slotPos = receiver.GetSlotPos(slotIndex);
                return slotPos;
            }, delegate ()
            {
                Vector2Int slotIndex = receiver.GetTopSlotPointer(productType);
                return receiver.GetSlotRotation(slotIndex);
            }, delegate
            {
                receiver.ReceivDone(product);
            }));

            sendCount++;
            if (sendCount >= count)
            {
                yield break;
            }

            while (timeCounter < sendInterval)
            {
                yield return null;
                timeCounter += Time.deltaTime;
            }
            timeCounter = 0;
        }
    }
    IEnumerator ProductSending(Product product, float sendTime, Func<Vector3> targetPosFun, Func<Quaternion> targetQuaternionFun, Action callback)
    {
        Transform productTransform = product.transform;
        float timeCounter = 0;
        productTransform.SetParent(null);
        Quaternion quaternionStart = productTransform.rotation;
        Vector3 positionStart = productTransform.position;
        while (Vector3.SqrMagnitude(productTransform.position - targetPosFun()) > 0.1f)
        {
            yield return null;
            timeCounter += Time.deltaTime;
            productTransform.rotation = Quaternion.Lerp(quaternionStart, targetQuaternionFun(), timeCounter / sendTime);
            productTransform.position = Vector3.Lerp(positionStart, targetPosFun(), timeCounter / sendTime);
        }
        productTransform.rotation = targetQuaternionFun();
        productTransform.position = targetPosFun();

        callback();
    }

    /// <summary>
    /// 从存储槽位顶上拿元素，并移除
    /// </summary>
    /// <param name="productType"></param>
    /// <returns></returns>
    public Product PopTopProduct(Product.ProductType productType)
    {
        MoveTopSlotPointerDown(productType);
        Vector2Int slotIndex = GetTopSlotPointer(productType);
        Product product = slots[slotIndex.x, slotIndex.y];
        slots[slotIndex.x, slotIndex.y] = null;

        ProductStore productStore = productStores.FirstOrDefault(x => x.productTemplate.productType == productType);
        if (productStore != null)
        {
            productStore.used--;
        }
        return product;
    }

    /// <summary>
    /// 获取某类物品槽位顶部指针（指针位置当前存储为空）
    /// </summary>
    /// <param name="productType"></param>
    /// <returns></returns>
    public Vector2Int GetTopSlotPointer(Product.ProductType productType)
    {
        RangeInt columnRange = productStoreColumnRange[productType];
        Vector2Int topSlotPointer;
        if (topSlotPointerDir.ContainsKey(productType))
            topSlotPointer = topSlotPointerDir[productType];
        else
        {
            topSlotPointer = new Vector2Int(0, columnRange.start);
            topSlotPointerDir.Add(productType, topSlotPointer);
        }
        return topSlotPointer;
    }
    private Dictionary<Product.ProductType, Vector2Int> topSlotPointerDir = new Dictionary<Product.ProductType, Vector2Int>();

    /// <summary>
    /// 向上移动某类物品槽位顶部指针
    /// </summary>
    /// <param name="productType"></param>
    private void MoveTopSlotPointerUp(Product.ProductType productType)
    {
        RangeInt columnRange = productStoreColumnRange[productType];
        Vector2Int topSlotPointer;
        if (topSlotPointerDir.ContainsKey(productType))
            topSlotPointer = topSlotPointerDir[productType];
        else
        {
            topSlotPointer = new Vector2Int(0, columnRange.start);
            topSlotPointerDir.Add(productType, topSlotPointer);
        }

        topSlotPointer.y++;
        //该行槽位已满
        if (topSlotPointer.y > columnRange.end)
        {
            topSlotPointer.x++;
            topSlotPointer.y = columnRange.start;
        }
        topSlotPointerDir[productType] = topSlotPointer;
    }

    /// <summary>
    /// 向下移动某类物品槽位顶部指针
    /// </summary>
    /// <param name="productType"></param>
    private void MoveTopSlotPointerDown(Product.ProductType productType)
    {
        RangeInt columnRange = productStoreColumnRange[productType];
        Vector2Int topSlotPointer;
        if (topSlotPointerDir.ContainsKey(productType))
            topSlotPointer = topSlotPointerDir[productType];
        else
        {
            topSlotPointer = new Vector2Int(0, columnRange.start);
            topSlotPointerDir.Add(productType, topSlotPointer);
        }

        topSlotPointer.y--;
        //指针溢出该行起点
        if (topSlotPointer.y < columnRange.start)
        {
            topSlotPointer.x--;
            topSlotPointer.y = columnRange.end;
        }
        topSlotPointerDir[productType] = topSlotPointer;
    }

    /// <summary>
    /// 刷新库存UI
    /// </summary>
    private void RefreshStoreUI()
    {
        if (productStores == null || productStores.Count == 0)
        {
            warehouseCanvas.RefreshStatus("空");
            return;
        }

        string store = $"{productStores[0].productTemplate.productType}:{productStores[0].used}/{productStores[0].capacity}";
        for (int i = 1; i < productStores.Count; i++)
        {
            store += $"<br>{productStores[i].productTemplate.productType}:{productStores[i].used}/{productStores[i].capacity}";
        }
        warehouseCanvas.RefreshStatus(store);
    }


    public enum WarehouseType
    {
        Input,
        Output
    }
}

[Serializable]
public class ProductStore
{
    /// <summary>
    /// 产品模板
    /// </summary>
    public Product productTemplate;

    [NonSerialized]
    /// <summary>
    /// 已使用容量
    /// </summary>
    public int used;

    /// <summary>
    /// 总容量
    /// </summary>
    public int capacity;
}

