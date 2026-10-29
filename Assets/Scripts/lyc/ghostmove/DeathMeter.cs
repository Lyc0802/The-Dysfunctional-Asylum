using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class DeathMeter : MonoBehaviour
{
    [Header("死亡值")]
    public float maxDeath = 100f;
    public float gainPerSecond = 10f;     
    public float recoverPerSecond = 0f;   
    public bool isGaining = false;        
    [Header("測試")]
    public bool testKey = true;           

    public UnityEvent onDeath;            

    public float DeathValue { get; private set; }
    public bool IsDead { get; private set; }
    public float Percent => maxDeath > 0f ? DeathValue / maxDeath : 0f;   // 0～1

    void Update()
    {
        if (IsDead) return;

        bool gaining = isGaining;
        if (testKey && Keyboard.current != null && Keyboard.current.kKey.isPressed)
            gaining = true;

        float rate = gaining ? gainPerSecond : -recoverPerSecond;
        AddDeath(rate * Time.deltaTime);
    }

    public void AddDeath(float amount)
    {
        if (IsDead) return;

        DeathValue = Mathf.Clamp(DeathValue + amount, 0f, maxDeath);

        if (DeathValue >= maxDeath)
        {
            IsDead = true;
            onDeath?.Invoke();
        }
    }

    public void ResetDeath()
    {
        DeathValue = 0f;
        IsDead = false;
    }
}