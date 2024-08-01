using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PotionSO", menuName = "ScriptableObject/PotionSO")]
public class PotionSO : ScriptableObject
{
    public PotionType potionType;
    public Transform potionPrefab;
    public GameObject potionParticle;
}
