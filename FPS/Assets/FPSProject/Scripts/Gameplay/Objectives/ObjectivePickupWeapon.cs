using Unity.FPS.Game;
using UnityEngine;

namespace Unity.FPS.Gameplay
{
    public class ObjectivePickupWeapon : Objective
    {
        [Tooltip("Item to pickup to complete the objective")]
        public GameObject ItemToPickup;

        protected override void Start()
        {
            base.Start();

            EventManager.AddListener<PickupWeaponEvent>(OnPickupEvent);
        }

        void OnPickupEvent(PickupWeaponEvent evt)
        {
            if (IsCompleted || evt.PickupWeapon.tag != "Weapon")
                return;

            // this will trigger the objective completion
            CompleteObjective(string.Empty, string.Empty, "Objective complete : " + Title);

            if (gameObject)
            {
                Destroy(gameObject);
            }
        }

        void OnDestroy()
        {
            EventManager.RemoveListener<PickupWeaponEvent>(OnPickupEvent);
        }
    }
}