using UnityEngine;

public interface ISender
{
    /// <summary>
    /// 请求产品
    /// </summary>
    public void RequestProduct(Product.ProductType productType, IReceiver receiver, float sendTime, int count);

    /// <summary>
    /// 停止产品请求
    /// </summary>
    /// <param name="receiver"></param>
    public void StopRequestProduct(IReceiver receiver);

    /// <summary>
    /// 查询可提供产品类型
    /// </summary>
    /// <returns></returns>
    public Product.ProductType[] ProductOfferings();

    /// <summary>
    /// 查询某类产品库存
    /// </summary>
    /// <returns></returns>
    public int GetProductCount(Product.ProductType productType);
}
