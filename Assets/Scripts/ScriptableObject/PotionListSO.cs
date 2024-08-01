using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//[CreateAssetMenu(fileName = "PotionListSO",menuName = "ScriptableObject/PotionListSO")]
public class PotionListSO : ScriptableObject
{
    public List<PotionSO> potionSOList;

    public GameObject GetRandomPotionPrefab()
    {
        int randomIndex = Random.Range(0, potionSOList.Count);

        return potionSOList[randomIndex].potionPrefab.gameObject;
    }

    public GameObject GetPotionParticle(PotionType potionType)
    {
        foreach (PotionSO potionSO in potionSOList)
        {
            if (potionSO.potionType == potionType)
            {
                return potionSO.potionParticle;
            }
        }
        return null;
    }
}
