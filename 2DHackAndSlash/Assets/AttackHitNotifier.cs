using System;
using UnityEngine;

public class AttackHitNotifier : MonoBehaviour
{
    public LayerMask TargetLayer;
    public event Action<Collider2D> OnHitTarget; // send collider reference
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(IsInLayerMask(collision.gameObject, TargetLayer))
        {
            OnHitTarget?.Invoke(collision);
            Debug.Log("attack has hit enemy");
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (IsInLayerMask(collision.gameObject, TargetLayer))
        {
            Debug.Log("attack finished hiting enemy");
        }
    }

    private bool IsInLayerMask(GameObject obj, LayerMask mask)
    {
        return (mask.value & (1 << obj.layer)) != 0;
    }
    public void TurnOnHitBox()
    {
        gameObject.SetActive(true);
    }
    public void TurnOffHitBox()
    {
        gameObject.SetActive(false);
    }
}
