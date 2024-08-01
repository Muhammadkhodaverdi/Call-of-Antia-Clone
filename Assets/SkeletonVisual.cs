using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SkeletonVisual : MonoBehaviour
{
    private readonly string DIE = "Die";
    private readonly string ATTACK = "Attack";
    private readonly string DAMAGE = "Damage";

    [Header("Refrences")]
    [SerializeField] private Skeleton skeleton;
    [SerializeField] private Animator animator;
    [SerializeField] private Image healthBarImage;
    [SerializeField] private CanvasGroup healthBarCanvasGroup;

    private void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (skeleton == null) skeleton = GetComponentInParent<Skeleton>();
    }

    private void Start()
    {
        skeleton.OnDie += (senedr, args) =>
        {
            animator.Play(DIE);
        };
        skeleton.OnAttack += (senedr, args) =>
        {
            animator.Play(ATTACK);
        };
        skeleton.OnTakeDamage += (senedr, args) =>
        {
            animator.Play(DAMAGE);
        };

        skeleton.OnHealthChanged += (sender, args) =>
        {
            if (args.health == args.maxHealth)
            {
                healthBarCanvasGroup.alpha = 0f;
            }
            else if (args.health < args.maxHealth && healthBarCanvasGroup.alpha == 0)
            {
                float delay = 0.2f;
                StartCoroutine(Fade(healthBarCanvasGroup, 1, delay));
            }
            else if (args.health <= 0)
            {
                float delay = 0.2f;
                StartCoroutine(Fade(healthBarCanvasGroup, 0, delay));
            }
            float filAmount = (args.health / args.maxHealth);

            healthBarImage.fillAmount = filAmount;
        };
    }

    private IEnumerator Fade(CanvasGroup canvasGroup, float to, float delay)
    {
        yield return new WaitForSeconds(delay);

        float elapsed = 0f;
        float duration = 0.5f;
        float from = canvasGroup.alpha;
        while (elapsed < duration)
        {
            canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            elapsed += Time.deltaTime;

            yield return null;
        }

        canvasGroup.alpha = to;
    }
}
