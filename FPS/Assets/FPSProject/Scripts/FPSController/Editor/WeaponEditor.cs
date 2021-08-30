using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.FPS.FPSController;

namespace MPro
{
    [CustomEditor(typeof(Weapon))]
    public class WeaponEditor : Editor
    {
        // For calling functions in Weapon class
        Weapon editorTarget;

        WeaponSpace weaponSpace;

        void OnEnable()
        {
            editorTarget = (Weapon)target;
        }


        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            float currentHelpBoxPosition = 72f;
            weaponSpace = FindObjectOfType<WeaponSpace>();

            GUIStyle headerStyle = new GUIStyle();
            headerStyle.fontStyle = FontStyle.Bold;


            EditorGUILayout.PropertyField(serializedObject.FindProperty("enableOnStart"));

            if (serializedObject.FindProperty("enableOnStart").boolValue)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("loadedAtStart"));
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Visuals", headerStyle);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("weaponPrefab"));

            if (editorTarget.IsDecollideWeaponPrefabNeeded())
            {
                DecollideHelpBox(ref currentHelpBoxPosition);
            }

            if (editorTarget.IsRelayerWeaponPrefabNeeded())
            {
                RelayerHelpBox(ref currentHelpBoxPosition);

            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("barrelFlash"));

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Crosshair", headerStyle);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("crosshairSprite"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("crosshairColour"));

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Audio", headerStyle);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("barrelSound"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("reloadSound"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("switchInSound"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("switchOutSound"));

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Animation", headerStyle);
            GUILayout.BeginHorizontal();

                GUILayoutOption labelGUIOption = GUILayout.Width(EditorGUIUtility.labelWidth - 1);
                GUILayoutOption buttonGUIOption = GUILayout.Width(EditorGUIUtility.currentViewWidth - (EditorGUIUtility.labelWidth + 38));

                EditorGUILayout.LabelField("Animation Mode", labelGUIOption);

                if (!IsAnimationMode())
                {
                    if (GUILayout.Button("Enable", buttonGUIOption))
                    {
                        EnableAnimationMode();
                    }
                }
                else
                {
                    if (GUILayout.Button("Disable", buttonGUIOption))
                    {
                        DisableAnimationMode();
                    }
                }

            GUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("animatorController"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fireAnimationVarName"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("reloadAniamtionVarName"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("switchAnimationVarName"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("aimAnimationVarName"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("runAnimationVarName"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("walkAnimationVarName"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("jumpAnimationVarName"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("mouseXAnimationVarName"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("mouseYAnimationVarName"));

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Spawn Paths", headerStyle);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("barrelFlashSpawnName"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("projectileSpawnName"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("cartridgeSpawnName"));

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Attributes", headerStyle);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("outputType"));

            // Ray Mode
            if (serializedObject.FindProperty("outputType").enumValueIndex == (int)Weapon.OutputType.Ray)
            {
                SerializedProperty rayModeProperty = serializedObject.FindProperty("rayMode");
                EditorGUILayout.PropertyField(rayModeProperty.FindPropertyRelative("rayImpact"));
                EditorGUILayout.PropertyField(rayModeProperty.FindPropertyRelative("range"));
                EditorGUILayout.PropertyField(rayModeProperty.FindPropertyRelative("impactForce"));
                EditorGUILayout.PropertyField(rayModeProperty.FindPropertyRelative("damage"));
            }

            // Projectile Mode
            if (serializedObject.FindProperty("outputType").enumValueIndex == (int)Weapon.OutputType.Projectile)
            {
                SerializedProperty projectileModeProperty = serializedObject.FindProperty("projectileMode");
                EditorGUILayout.PropertyField(projectileModeProperty.FindPropertyRelative("projectileObject"));
                EditorGUILayout.PropertyField(projectileModeProperty.FindPropertyRelative("areaOfDamage"));
                EditorGUILayout.PropertyField(projectileModeProperty.FindPropertyRelative("launchForce"));
                EditorGUILayout.PropertyField(projectileModeProperty.FindPropertyRelative("damage"));
            }

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("firingType"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("roundsPerBurst"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fireRate"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("outputPerRound"));

            EditorGUILayout.Space();

            serializedObject.FindProperty("aimingSpread").floatValue = EditorGUILayout.Slider("Aiming Spread", serializedObject.FindProperty("aimingSpread").floatValue, 0f, 0.3f);
            serializedObject.FindProperty("spread").floatValue = EditorGUILayout.Slider("Hip Spread", serializedObject.FindProperty("spread").floatValue, 0f, 0.3f);
            serializedObject.FindProperty("movementSpread").floatValue = EditorGUILayout.Slider("Movement Spread", serializedObject.FindProperty("movementSpread").floatValue, 0f, 0.3f);

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("ammo"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("capacity"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ammoLossPerRound"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("reloadingType"));
            if (
                serializedObject.FindProperty("reloadingType").enumValueIndex == (int)Weapon.ReloadingType.Partial ||
                serializedObject.FindProperty("reloadingType").enumValueIndex == (int)Weapon.ReloadingType.PartialRepeat
                )
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("ammoAddedPerReload"));
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Animation Timing", headerStyle);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("aimingTime"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("reloadingTime"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("switchingTime"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("runningRecoveryTime"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("partialReloadInterruptionTime"), new GUIContent("Partial Reload Ending Time")); // ? rename var
            
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("Cartridge Ejection", headerStyle);
            SerializedProperty ejectedCartridgeProperty = serializedObject.FindProperty("ejectedCartridge");
            EditorGUILayout.PropertyField(ejectedCartridgeProperty.FindPropertyRelative("ejectedObject"));
            EditorGUILayout.PropertyField(ejectedCartridgeProperty.FindPropertyRelative("ejectionTrajectory"));
            EditorGUILayout.PropertyField(ejectedCartridgeProperty.FindPropertyRelative("ejectionForce"));

            serializedObject.ApplyModifiedProperties();
        }

        void DecollideHelpBox(ref float inspectorPosition)
        {
            float heightMod;

            if (EditorGUIUtility.currentViewWidth <= 373f) { heightMod = 40f; }
            else { heightMod = 40f; }

            float buttonY = inspectorPosition + heightMod;

            string
                buttonText = "Decollide",
                cancelButtonText = "Cancel",
                dialogTitle = "Decollide Weapon Prefab?",
                dialogText = "This will remove colliders from the Weapon Prefab and its children.",
                helpBoxText = "Remove Colliders from Weapon Prefab to prevent raycast firing disruption.";

            if (GUI.Button(new Rect(25f, buttonY, EditorGUIUtility.currentViewWidth - 38f, 20f), buttonText))
            {
                if (EditorUtility.DisplayDialog(dialogTitle, dialogText, buttonText, cancelButtonText))
                {
                    editorTarget.DecollideWeaponPrefab();
                }
            }

            EditorGUI.HelpBox(new Rect(18f, inspectorPosition, EditorGUIUtility.currentViewWidth - 24f, heightMod + 25f), helpBoxText, MessageType.Warning);

            inspectorPosition += heightMod + 25f;
            EditorGUILayout.Space(heightMod + 25f);
        }

        void RelayerHelpBox(ref float inspectorPosition)
        {
            float heightMod;

            if (EditorGUIUtility.currentViewWidth <= 373f) { heightMod = 40f; }
            else { heightMod = 40f; }

            float buttonY = inspectorPosition + heightMod;

            string
                buttonText = "Relayer",
                cancelButtonText = "Cancel",
                dialogTitle = "Relayer Weapon Prefab?",
                dialogText = "This will assign the Weapon Prefab and its children to the FirstPerson layer.",
                helpBoxText = "Assign Weapon Prefab to the FirstPerson layer to prevent rendering issues.";

            if (GUI.Button(new Rect(25f, buttonY, EditorGUIUtility.currentViewWidth - 38f, 20f), buttonText))
            {
                if (EditorUtility.DisplayDialog(dialogTitle, dialogText, buttonText, cancelButtonText))
                {
                    editorTarget.RelayerWeaponPrefab();
                }
            }

            EditorGUI.HelpBox(new Rect(18f, inspectorPosition, EditorGUIUtility.currentViewWidth - 24f, heightMod + 25f), helpBoxText, MessageType.Warning);

            inspectorPosition += heightMod + 25f;
            EditorGUILayout.Space(heightMod + 25f);
        }

        void EnableAnimationMode()
        {
            // Remove other instantiated weapon prefabs first
            DisableAnimationMode();

            GameObject instantiatedObject = Instantiate(editorTarget.weaponPrefab, weaponSpace.transform);
            weaponSpace.GetComponent<Animator>().runtimeAnimatorController = editorTarget.animatorController;

            // Removes "(clone)" from name of spawned weapon object, to prevent animations from disconnecting with object
            const int numberOfCharacterToRemove = 7;
            instantiatedObject.name = instantiatedObject.name.Remove(instantiatedObject.name.Length - numberOfCharacterToRemove); 
        }

        void DisableAnimationMode()
        {
            for (int i = 0; i < weaponSpace.transform.childCount; i++)
            {
                DestroyImmediate(weaponSpace.transform.GetChild(i).gameObject, true);
            }

            weaponSpace.GetComponent<Animator>().runtimeAnimatorController = null;
        }

        bool IsAnimationMode()
        {
            if (weaponSpace.transform.childCount != 1)
            {
                return false;
            }

            if (weaponSpace.transform.GetChild(0).name != editorTarget.name)
            {
                return false;
            }

            if (weaponSpace.GetComponent<Animator>().runtimeAnimatorController != editorTarget.animatorController)
            {
                return false;
            }

            return true;
        }
    }
}