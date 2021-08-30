using Unity.FPS.Game;
using UnityEngine;

namespace Unity.FPS.Gameplay
{
    public class ObjectivePickupGrenade : Objective
    {
        protected override void Start()
        {
            base.Start();

            EventManager.AddListener<PickupGrenadeEvent>(OnPickupEvent);
        }

        void OnPickupEvent(PickupGrenadeEvent evt)
        {
            if (IsCompleted || evt.PickupGrenade.tag != "Grenade")
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
            EventManager.RemoveListener<PickupGrenadeEvent>(OnPickupEvent);
        }
    }
}