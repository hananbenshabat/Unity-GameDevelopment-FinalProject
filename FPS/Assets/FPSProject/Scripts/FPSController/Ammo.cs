using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Unity.FPS.FPSController
{
    static class AmmoTooltip
    {
        public const string
            startAmount = "Total ammo of this type avaliable at start.",
            ammoIcon = "Icon used in UI to resemble this type of ammo.";
    }

    [CreateAssetMenu(fileName = "New Ammo", menuName = "Ammo")]
    public class Ammo : ScriptableObject
    {
        [Tooltip(AmmoTooltip.startAmount)] public int startAmount = 10;
        [Tooltip(AmmoTooltip.ammoIcon)] public Sprite ammoIcon;
    }
}