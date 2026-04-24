using UnityEngine;
using static Warehouse;

public class Player : MonoBehaviour
{
    [SerializeField]
    private float moveSpeed = 5f;
    [SerializeField]
    private float rotateSpeed = 500f;

    [SerializeField]
    private Packsack packsack;

    CharacterController cc;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
    }

    void Update()

    {
        float h = -Input.GetAxisRaw("Horizontal");
        float v = -Input.GetAxisRaw("Vertical");

        // 输入方向
        Vector3 dir = new Vector3(h, 0, v).normalized;

        if (dir.magnitude > 0.1f)
        {
            // 朝移动方向旋转
            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRot,
                rotateSpeed * Time.deltaTime
                    );

            // 移动
            cc.Move(dir * moveSpeed * Time.deltaTime);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Warehouse warehouse = other.GetComponent<Warehouse>();
        if (warehouse != null)
        {
            if (warehouse.warehouseType == WarehouseType.Input)
            {
                packsack.RequestProduct(packsack.ProductOfferings()[0], warehouse, 0.3f);
            }
            else
            {
                warehouse.RequestProduct(warehouse.ProductOfferings()[0], packsack, 0.3f);
            }
        }
    }

    // 离开触发器时调用一次
    private void OnTriggerExit(Collider other)
    {

        Warehouse warehouse = other.GetComponent<Warehouse>();
        if (warehouse != null)
        {
            if (warehouse.warehouseType == WarehouseType.Input)
            {
                packsack.StopRequestProduct(warehouse);
            }
            else
            {
                warehouse.StopRequestProduct(packsack);
            }
        }
    }
}