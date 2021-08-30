using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.FPS.FPSController;


namespace MPro
{
    [CustomEditor(typeof(WeaponCollection))]
    public class WeaponCollectionEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            SerializedProperty weapons = serializedObject.FindProperty("weapons");

            SerializedProperty currentWeaponListSize = weapons.FindPropertyRelative("Array.size");
            SerializedProperty lastWeaponListSize = serializedObject.FindProperty("lastWeaponListSize");

            EditorGUILayout.PropertyField(currentWeaponListSize);

            // Default value for new Weapon fields
            if (currentWeaponListSize.intValue > lastWeaponListSize.intValue)
            {
                for (int i = lastWeaponListSize.intValue; i < currentWeaponListSize.intValue; i++)
                {
                    weapons.GetArrayElementAtIndex(i).objectReferenceValue = null;
                }
            }
            lastWeaponListSize.intValue = currentWeaponListSize.intValue;

            // Draw Weapon fields
            for (int i = 0; i < currentWeaponListSize.intValue; i++)
            {
                EditorGUILayout.PropertyField(weapons.GetArrayElementAtIndex(i));
            }

            serializedObject.ApplyModifiedProperties();
        }

    }
}