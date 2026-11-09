using UnityEngine;

/// <summary>ทำให้ยุงบินช้าลงชั่วคราว (ใช้กับ Bug Spray) หมดเวลาแล้วคืนความเร็วเดิม</summary>
public class SlowEffect : MonoBehaviour
{
    private Mosquito mosquito;
    private SpriteRenderer spriteRenderer;
    private float originalSpeed;
    private Color originalColor;
    private float endTime;

    private static readonly Color SlowTint = new Color(0.6f, 1f, 0.6f);

    public static void Apply(Mosquito target, float speedMultiplier, float duration)
    {
        if (target == null) return;

        SlowEffect effect = target.GetComponent<SlowEffect>();
        if (effect == null)
        {
            effect = target.gameObject.AddComponent<SlowEffect>();
            effect.mosquito = target;
            effect.originalSpeed = target.flySpeed;
            target.flySpeed *= speedMultiplier;

            effect.spriteRenderer = target.GetComponent<SpriteRenderer>();
            if (effect.spriteRenderer != null)
            {
                effect.originalColor = effect.spriteRenderer.color;
                effect.spriteRenderer.color = effect.originalColor * SlowTint;
            }
        }

        // โดนซ้ำ = ต่อเวลา ไม่ช้าลงซ้อน
        effect.endTime = Time.time + duration;
    }

    private void Update()
    {
        if (Time.time >= endTime) Destroy(this);
    }

    private void OnDestroy()
    {
        if (mosquito != null) mosquito.flySpeed = originalSpeed;
        if (spriteRenderer != null) spriteRenderer.color = originalColor;
    }
}
