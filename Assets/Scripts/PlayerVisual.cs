using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerVisual : MonoBehaviour
{
    [Header("Refrences")]
    [SerializeField] private Player player;
    [SerializeField] private Image healthBarImage;


    private void Start()
    {
        player.OnHealthChanged.AddListener((args) =>
        {
            float filAmount = (args.health / args.maxHealth);

            healthBarImage.fillAmount = filAmount;
        });
    }
}
