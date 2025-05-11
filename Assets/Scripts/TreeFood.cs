using UnityEngine;

public class TreeFood : MonoBehaviour
{
    [Header("Food Resource Settings")]
    public float maxFood = 10f;      public float currentFood;

    void Start()
    {
                currentFood = maxFood;
    }

    public float GetFoodAmount()
    {
        return currentFood;
    }

    public void ConsumeFood(float amount)
    {
        currentFood = Mathf.Max(currentFood - amount, 0);
    }

    public void RegenerateFood()
    {
        currentFood = maxFood;
    }
}
