using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.FPS.FPSController;


namespace MPro
{
    [CustomEditor(typeof(CollectableObject))]
    public class CollectableObjectEditor : Editor
    {


        public override void OnInspectorGUI()
        {
            serializedObject.Update();




            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_CollectionType"));

            // Weapon
            if (serializedObject.FindProperty("m_CollectionType").enumValueIndex == (int)CollectableObject.CollectionType.Weapon)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Weapon"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Enable"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_AmmoInWeapon"));
            }

            // Ammo
            if (serializedObject.FindProperty("m_CollectionType").enumValueIndex == (int)CollectableObject.CollectionType.Ammo)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Ammo"));
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_AddToAmmoTotal"));

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_AfterCollectionObject"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_AfterCollectionDespawnTime"));

            serializedObject.ApplyModifiedProperties();
        }
    }
}