using UnityEngine;

public interface IReceiver
{
    /// <summary>
    /// 开始接收产品（动画开始）
    /// </summary>
    /// <param name="product"></param>
    public void StartReceiv(Product product);

    /// <summary>
    /// 产品接收完毕（动画结束）
    /// </summary>
    /// <param name="product"></param>
    public void ReceivDone(Product product);

    /// <summary>
    /// 查询剩余空闲存储
    /// </summary>
    /// <returns></returns>
    public int GetAvailCapacity(Product.ProductType productType);

    /// <summary>
    /// 获取存储槽位顶部指针（指向槽位保持为空，准备存放新元素的位置）
    /// </summary>
    /// <param name="productType"></param>
    /// <returns></returns>
    public Vector2Int GetTopSlotPointer(Product.ProductType productType);

    /// <summary>
    /// 获取存储槽位世界坐标
    /// </summary>
    /// <param name="slotIndex"></param>
    /// <returns></returns>
    public Vector3 GetSlotPos(Vector2Int slotIndex);

    /// <summary>
    /// 获取存储槽位Rotation
    /// </summary>
    /// <param name="slotIndex"></param>
    /// <returns></returns>
    public Quaternion GetSlotRotation(Vector2Int slotIndex);
}
