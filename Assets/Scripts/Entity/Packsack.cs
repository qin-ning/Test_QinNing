using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Animations;
using UnityEngine;
using static UnityEngine.UIElements.UxmlAttributeDescription;

public class Packsack : MonoBehaviour, IReceiver, ISender
{
    /// <summary>
    /// 背包容量
    /// </summary>
    [SerializeField]
    private int capacity;

    /// <summary>
    /// 已使用容量
    /// </summary>
    [SerializeField]
    private int used;

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
    /// 定义槽位列数(行数由背包容量计算)
    /// </summary>
    [SerializeField]
    private int columnCount;

    /// <summary>
    /// 存放产品的槽位
    /// </summary>
    [SerializeField]
    private Product[,] slots;

    void Awake()
    {
        int rowCount = (int)MathF.Ceiling((float)capacity / columnCount);
        slots = new Product[rowCount, columnCount];
    }

    #region ISender实现
    /// <summary>
    /// 请求产品
    /// </summary>
    public void RequestProduct(Product.ProductType productType, IReceiver receiver, float sendTime, int count = int.MaxValue)
    {
        if (!postProductConductDir.ContainsKey(receiver))
        {
            Coroutine enumerator = StartCoroutine(SendProductConduct(receiver, sendTime));
            postProductConductDir.Add(receiver, enumerator);
        }
    }
    private Dictionary<IReceiver, Coroutine> postProductConductDir = new Dictionary<IReceiver, Coroutine>();

    /// <summary>
    /// 停止产品请求
    /// </summary>
    /// <param name="receiver"></param>
    public void StopRequestProduct(IReceiver receiver)
    {
        if (postProductConductDir.ContainsKey(receiver))
        {
            StopCoroutine(postProductConductDir[receiver]);
            postProductConductDir.Remove(receiver);
        }
    }

    /// <summary>
    /// 查询可提供产品类型
    /// </summary>
    /// <returns></returns>
    public Product.ProductType[] ProductOfferings()
    {
        return new Product.ProductType[] { Product.ProductType.N1, Product.ProductType.N2 };
    }

    /// <summary>
    /// 查询某类产品库存
    /// </summary>
    /// <returns></returns>
    public int GetProductCount(Product.ProductType productType)
    {
        int count = 0;

        foreach (var item in slots)
        {
            if (item != null && item.productType == productType)
                count++;
        }

        return count;
    }
    #endregion

    #region IReceiver实现

    /// <summary>
    /// 开始接收产品（动画开始）
    /// </summary>
    /// <param name="product"></param>
    public void StartReceiv(Product product)
    {
        receiveIngCount++;
    }
    private int receiveIngCount;

    /// <summary>
    /// 产品接收完毕（动画结束）
    /// </summary>
    /// <param name="product"></param>
    public void ReceivDone(Product product)
    {
        receiveIngCount--;
        Vector2Int slotIndex = GetTopSlotPointer();
        MoveTopSlotPointerUp();
        slots[slotIndex.x, slotIndex.y] = product;
        used++;
        product.transform.SetParent(transform);

        EventCenter.onPacksackChanged.Invoke(used, capacity);
    }

    /// <summary>
    /// 查询某类产品在仓库的空余容量
    /// </summary>
    /// <returns></returns>
    public int GetAvailCapacity(Product.ProductType productType)
    {
        return capacity - used - receiveIngCount;
    }

    /// <summary>
    /// 获取某类物品槽位顶部指针
    /// </summary>
    /// <param name="productType"></param>
    /// <returns></returns>
    public Vector2Int GetTopSlotPointer(Product.ProductType productType = Product.ProductType.None)
    {
        return topSlotPointer;
    }
    private Vector2Int topSlotPointer;

    /// <summary>
    /// 获取槽位世界坐标
    /// </summary>
    /// <param name="slotIndex"></param>
    /// <returns></returns>
    public Vector3 GetSlotPos(Vector2Int slotIndex)
    {
        Vector3 vector3 = transform.position
                  + transform.right * slotIndex.y * slotWith
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

    /// <summary>
    /// 清除背包所有物体
    /// </summary>
    public void Clear()
    {
        int row = slots.GetLength(0);
        int col = slots.GetLength(1);

        for (int i = 0; i < row; i++)
        {
            for (int j = 0; j < col; j++)
            {
                if (slots[i, j] == null)
                    continue;
                ObjectPool.Recycle(slots[i, j]);
                slots[i, j] = null; // 清空
            }
        }

        //Array.Clear(slots, 0, slots.Length);
        topSlotPointer = Vector2Int.zero;
        used = 0;
        EventCenter.onPacksackChanged.Invoke(used, capacity);
    }

    /// <summary>
    /// 从顶上拿产品，并移除
    /// </summary>
    /// <returns></returns>
    public Product PopTopProduct()
    {
        MoveTopSlotPointerDown();
        Vector2Int slotPointer = GetTopSlotPointer();
        Product product = slots[slotPointer.x, slotPointer.y];
        slots[slotPointer.x, slotPointer.y] = null;
        used--;
        EventCenter.onPacksackChanged.Invoke(used, capacity);
        return product;
    }

    /// <summary>
    /// 查看顶上产品，不移除
    /// </summary>
    /// <returns></returns>
    public Product PeekTopProduct()
    {
        Vector2Int slotPointer = topSlotPointer;
        slotPointer.y--;
        //指针溢出该行起点
        if (slotPointer.y < 0)
        {
            slotPointer.x--;
            slotPointer.y = columnCount - 1;
        }
        return slots[slotPointer.x, slotPointer.y];
    }

    /// <summary>
    /// 向上移动某类物品槽位顶部指针
    /// </summary>
    /// <param name="productType"></param>
    private void MoveTopSlotPointerUp()
    {
        topSlotPointer.y++;
        //该行槽位已满
        if (topSlotPointer.y > columnCount - 1)
        {
            topSlotPointer.x++;
            topSlotPointer.y = 0;
        }
    }

    /// <summary>
    /// 向下移动某类物品槽位顶部指针
    /// </summary>
    /// <param name="productType"></param>
    private void MoveTopSlotPointerDown()
    {
        topSlotPointer.y--;
        //指针溢出该行起点
        if (topSlotPointer.y < 0)
        {
            topSlotPointer.x--;
            topSlotPointer.y = columnCount - 1;
        }
    }

    IEnumerator SendProductConduct(IReceiver receiver, float sendTime)
    {
        //发送产品时间间隔
        float sendInterval = sendTime;
        float timeCounter = 0;

        while (true)
        {
            if (used <= 0)
            {
                yield return null;
                continue;
            }

            if (receiver.GetAvailCapacity(PeekTopProduct().productType) <= 0)
            {
                yield return null;
                continue;
            }
            Product product = PopTopProduct();
            receiver.StartReceiv(product);
            StartCoroutine(ProductSending(product, sendTime, delegate ()
            {
                Vector2Int slotIndex = receiver.GetTopSlotPointer(product.productType);
                Vector3 slotPos = receiver.GetSlotPos(slotIndex);
                return slotPos;
            }, delegate ()
            {
                Vector2Int slotIndex = receiver.GetTopSlotPointer(product.productType);
                return receiver.GetSlotRotation(slotIndex);
            }, delegate
            {
                receiver.ReceivDone(product);
            }));

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
}
