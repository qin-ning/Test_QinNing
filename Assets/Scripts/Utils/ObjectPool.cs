using System.Collections.Generic;
using UnityEngine;

public static class ObjectPool
{
    private static Dictionary<Product.ProductType, Stack<Product>> freeProduct = new Dictionary<Product.ProductType, Stack<Product>>();

    /// <summary>
    /// ��ȡʵ��
    /// </summary>
    /// <param name="prefab"></param>
    /// <returns></returns>
    public static Product GetProduct(Product prefab)
    {
        Product product;
        if (!freeProduct.ContainsKey(prefab.productType) || freeProduct[prefab.productType].Count == 0)
        {
            product = GameObject.Instantiate(prefab);
            GameObject.DontDestroyOnLoad(product.gameObject);
            return product;
        }
        product = freeProduct[prefab.productType].Pop();
        product.gameObject.SetActive(true);
        return product;
    }

    /// <summary>
    /// ����ʵ��
    /// </summary>
    /// <param name="product"></param>
    public static void Recycle(Product product)
    {
        product.gameObject.SetActive(false);
        Stack<Product> stack;
        if (!freeProduct.TryGetValue(product.productType, out stack))
        {
            stack = new Stack<Product>();
            freeProduct.Add(product.productType, stack);
        }

        stack.Push(product);
    }
}
