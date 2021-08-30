using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Animations;
using Unity.FPS.Game;



namespace Unity.FPS.FPSController
{
    static class WeaponTooltip
    {
        public const string
            enableOnStart = "If enabled, this weapon can be equiped from start.",
            loadedAtStart = "If enabled, this weapon will be fully loaded from start.",

            weaponPrefab = "GameObject for visualising the weapon.",
            barrelFlash = "GameObject that resembles the weapon's barral flash. Often involves partical effects.",

            crosshairSprite = "Sprite to display at centre of the screen when not aiming.",
            crosshairColour = "Colour of crosshair sprite.",

            barrelSound = "Sound played when firing weapon.",
            reloadSound = "Sound played when reloading weapon.",
            switchInSound = "Sound played when switching-in the weapon.",
            switchOutSound = "Sound played when switching-out the weapon.",

            animatorController = "Animator Controller use for the weapon's animations.",
            fireAnimationVarName = "Name of the fire parameter, within weapon's animator controller. Allows system to control animations.",
            reloadAniamtionVarName = "Name of the reload parameter, within weapon's animator controller. Allows system to control animations.",
            switchAnimationVarName = "Name of the switch parameter, within weapon's animator controller. Allows system to control animations.",
            aimAnimationVarName = "Name of the aim parameter, within weapon's animator controller. Allows system to control animations.",
            runAnimationVarName = "Name of the run parameter, within weapon's animator controller. Allows system to control animations.",
            walkAnimationVarName = "Name of the walk parameter, within weapon's animator controller. Allows system to control animations.",
            jumpAnimationVarName = "Name of the jump parameter, within weapon's animator controller. Allows system to control animations.",
            mouseXAnimationVarName = "Name of the mouse X parameter, within weapon's animator controller. Allows system to control animations.",
            mouseYAnimationVarName = "Name of the mouse Y parameter, within weapon's animator controller. Allows system to control animations.",

            barrelFlashSpawnName = "Path of weapon's barrel flash spawn. Example: Default_Weapon/Aimbody/[Flash]",
            projectileSpawnName = "Path of weapon's projectile spawn. Example: Default_Weapon/Aimbody/[Pro]",
            cartridgeSpawnName = "Path of weapon's cartridge spawn. Example: Default_Weapon/Aimbody/[Cart]",

            outputType = "Type of projectile behaviour when firing.",

            firingType = "Type of firing responce on button press.",
            roundsPerBurst = "Times fired in serial per button press.",
            fireRate = "Maximum rounds fired per second allowed.",
            outputPerRound = "Projectiles launched in parallel per round.",

            aimingSpread = "Amount of offset from the centre of aiming applied to output when aiming.",
            spread = "Amount of offset from the centre of aiming applied to output when not aiming and moving.",
            movementSpread = "Amount of offset from the centre of aiming applied to output when not aiming, but moving.",


            damage = "Amount of damage applied to the target hit, if it is damageable",
            ammo = "Ammo object used with this weapon.",
            capacity = "Maximum amount of ammo that can be loaded in weapon.",
            ammoLossPerRound = "Amount of ammo used per round.",
            reloadingType = "Type of reloading behaviour.",
            ammoAddedPerReload = "Amount of ammo loaded in weapon per reload.",

            aimingTime = "Amount of time to aim or un-aim weapon.",
            reloadingTime = "Amount of time to reload weapon.",
            switchingTime = "Amount of time to switch weapon.",
            runningRecoveryTime = "Amount of time firing is re-enabled after running.",
            partialReloadInterruptionTime = "Amount of time firing is re-enabled after partial reloading is finished or interrupted.",

            rayImpact = "GameObject to spawn where firing ray hits surface.",
            range = "Range of firing ray.",
            impactForce = "Amount of force applied to rigidbody hit by firing ray.",

            projectileObject = "GameObject to launch when firing.",
            areaOfDamage = "Area of damage. Keep empty if you don't want area damage",
            launchForce = "Amount of force applied to launched GameObject.",

            ejectedObject = "GameObject to spawn that resembles ejected ammo cartridge.",
            ejectionTrajectory = "Direction that ejected ammo cartridge is launched towards.",
            ejectionForce = "Amount of force applied to ejected ammo cartridge when spawned.";
    }


    [CreateAssetMenu(fileName = "New Weapon", menuName = "Weapon")]
    public class Weapon : ScriptableObject
    {

        public enum OutputType { Ray, Projectile }
        public enum FiringType { Semi, Auto }
        public enum ReloadingType { Full, Partial, PartialRepeat }

        [Tooltip(WeaponTooltip.enableOnStart)] public bool enableOnStart = true;
        [Tooltip(WeaponTooltip.loadedAtStart)] public bool loadedAtStart = true;

        [Tooltip(WeaponTooltip.weaponPrefab)] public GameObject weaponPrefab;
        [Tooltip(WeaponTooltip.barrelFlash)] public GameObject barrelFlash;

        [Tooltip(WeaponTooltip.crosshairSprite)] public Sprite crosshairSprite;
        [Tooltip(WeaponTooltip.crosshairColour)] public Color crosshairColour = Color.white;

        [Tooltip(WeaponTooltip.barrelSound)] public AudioClip barrelSound;
        [Tooltip(WeaponTooltip.reloadSound)] public AudioClip reloadSound;
        [Tooltip(WeaponTooltip.switchInSound)] public AudioClip switchInSound;
        [Tooltip(WeaponTooltip.switchOutSound)] public AudioClip switchOutSound;

        [Tooltip(WeaponTooltip.animatorController)] public AnimatorController animatorController;
        [Tooltip(WeaponTooltip.fireAnimationVarName)] public string fireAnimationVarName = "Firing";
        [Tooltip(WeaponTooltip.reloadAniamtionVarName)] public string reloadAniamtionVarName = "Reloading";
        [Tooltip(WeaponTooltip.switchAnimationVarName)] public string switchAnimationVarName = "Switching";
        [Tooltip(WeaponTooltip.aimAnimationVarName)] public string aimAnimationVarName = "Aiming";
        [Tooltip(WeaponTooltip.runAnimationVarName)] public string runAnimationVarName = "Running";
        [Tooltip(WeaponTooltip.walkAnimationVarName)] public string walkAnimationVarName = "Walking";
        [Tooltip(WeaponTooltip.jumpAnimationVarName)] public string jumpAnimationVarName = "Jumping";
        [Tooltip(WeaponTooltip.mouseXAnimationVarName)] public string mouseXAnimationVarName = "Mouse X";
        [Tooltip(WeaponTooltip.mouseYAnimationVarName)] public string mouseYAnimationVarName = "Mouse Y";

        // Sub-object referancing (for internal barralFlash, projectileSpawn, cartageSpawn)
        [Tooltip(WeaponTooltip.barrelFlashSpawnName)] public string barrelFlashSpawnName;
        [Tooltip(WeaponTooltip.projectileSpawnName)] public string projectileSpawnName;
        [Tooltip(WeaponTooltip.cartridgeSpawnName)] public string cartridgeSpawnName;

        [Tooltip(WeaponTooltip.outputType)] public OutputType outputType = OutputType.Ray;

        [Tooltip(WeaponTooltip.firingType)] public FiringType firingType = FiringType.Semi;
        [Tooltip(WeaponTooltip.roundsPerBurst)] public int roundsPerBurst = 1;
        [Tooltip(WeaponTooltip.fireRate)] public float fireRate = 10f;
        [Tooltip(WeaponTooltip.outputPerRound)] public int outputPerRound = 1;

        [Tooltip(WeaponTooltip.aimingSpread)] public float aimingSpread = 0.01f;
        [Tooltip(WeaponTooltip.spread)] public float spread = 0.1f;
        [Tooltip(WeaponTooltip.movementSpread)] public float movementSpread = 0.15f;

        [Tooltip(WeaponTooltip.ammo)] public Ammo ammo;
        [Tooltip(WeaponTooltip.capacity)] public int capacity = 10;
        [Tooltip(WeaponTooltip.ammoLossPerRound)] public int ammoLossPerRound = 1;
        [Tooltip(WeaponTooltip.reloadingType)] public ReloadingType reloadingType = ReloadingType.Full;
        [Tooltip(WeaponTooltip.ammoAddedPerReload)] public int ammoAddedPerReload = 1;

        [Tooltip(WeaponTooltip.aimingTime)] public float aimingTime = 0.25f;
        [Tooltip(WeaponTooltip.reloadingTime)] public float reloadingTime = 1f;
        [Tooltip(WeaponTooltip.switchingTime)] public float switchingTime = 1f;
        [Tooltip(WeaponTooltip.runningRecoveryTime)] public float runningRecoveryTime = 0.25f;
        [Tooltip(WeaponTooltip.partialReloadInterruptionTime)] public float partialReloadInterruptionTime = 0.25f;

        [System.Serializable]
        public class RayMode
        {
            [Tooltip(WeaponTooltip.rayImpact)] public GameObject rayImpact;
            [Tooltip(WeaponTooltip.range)] public float range = 100f;
            [Tooltip(WeaponTooltip.impactForce)] public float impactForce = 100f;
            [Tooltip(WeaponTooltip.damage)] public float damage = 10f;
        }

        [System.Serializable]
        public class ProjectileMode
        {
            [Tooltip(WeaponTooltip.projectileObject)] public GameObject projectileObject;
            [Tooltip(WeaponTooltip.areaOfDamage)] public DamageArea areaOfDamage;
            [Tooltip(WeaponTooltip.launchForce)] public float launchForce = 100f;
            [Tooltip(WeaponTooltip.damage)] public float damage = 10f;
        }

        [System.Serializable]
        public class EjectedCartridge
        {
            [Tooltip(WeaponTooltip.ejectedObject)] public GameObject ejectedObject;
            [Tooltip(WeaponTooltip.ejectionTrajectory)] public Vector3 ejectionTrajectory = new Vector3(-1f, 0f, 0f);
            [Tooltip(WeaponTooltip.ejectionForce)] public float ejectionForce = 5f;
        }

        public RayMode rayMode;
        public ProjectileMode projectileMode;
        public EjectedCartridge ejectedCartridge;


        public bool IsDecollideWeaponPrefabNeeded()
        {
            if (weaponPrefab == null)
            {
                return false;
            }

            Collider collider = weaponPrefab.GetComponent<Collider>();
            Collider[] childColliders = weaponPrefab.GetComponentsInChildren<Collider>();

            if (collider != null || childColliders.Length > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool IsRelayerWeaponPrefabNeeded()
        {
            if (weaponPrefab == null)
            {
                return false;
            }

            if (weaponPrefab.layer != 8)
            {
                return true;
            }

            for (int i = 0; i < weaponPrefab.transform.childCount; i++)
            {
                if (weaponPrefab.transform.GetChild(i).gameObject.layer != 8)
                {
                    return true;
                }
            }

            return false;
        }

        public void DecollideWeaponPrefab()
        {
            Collider collider = weaponPrefab.GetComponent<Collider>();
            Collider[] childColliders = weaponPrefab.GetComponentsInChildren<Collider>();

            DestroyImmediate(collider, true);

            foreach (Collider c in childColliders)
            {
                DestroyImmediate(c, true);
            }
        }

        public void RelayerWeaponPrefab()
        {
            weaponPrefab.layer = 8;

            for (int i = 0; i < weaponPrefab.transform.childCount; i++)
            {
                weaponPrefab.transform.GetChild(i).gameObject.layer = 8;
            }
        }
    }
}