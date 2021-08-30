using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Windows;
using UnityEditor.Animations;


namespace Unity.FPS.FPSController
{
    public class WeaponFiles : EditorWindow
    {
        static WeaponFiles window;
        static string weaponName = "";

        const string DefaultWeaponFolderPathInside = "Assets/FPSProject/Content/Animations/Weapons/DefaultWeapon/";

        [MenuItem("Assets/Create/Weapon Files", false, 0)]
        static void Initialise()
        {
            window = (WeaponFiles)GetWindow(typeof(WeaponFiles), true, "Create Weapon Files");
            window.position = new Rect(Screen.width / 2, Screen.height / 2, 250, 65);

            weaponName = "";
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("Weapon name:");
            weaponName = EditorGUILayout.TextField(weaponName);

            if (weaponName.Length > 0 && weaponName[0] != ' ' && weaponName[weaponName.Length - 1] != ' ')
            {
                if (GUILayout.Button("Create"))
                {
                    CreateWeaponFiles();

                    window.Close();
                }
            }
        }

        void CreateWeaponFiles()
        {
            // Create folder
            string folderPathOutside = AssetDatabase.GetAssetPath(Selection.activeObject);
            AssetDatabase.CreateFolder(folderPathOutside, weaponName);

            // Create files
            string folderPathInside = folderPathOutside + "/" + weaponName + "/";
            AssetDatabase.CopyAsset(DefaultWeaponFolderPathInside + "DefaultWeapon.asset", folderPathInside + weaponName + ".asset");
            AssetDatabase.CopyAsset(DefaultWeaponFolderPathInside + "DefaultWeapon.controller", folderPathInside + weaponName + ".controller");
            AssetDatabase.CopyAsset(DefaultWeaponFolderPathInside + "DefaultWeapon.prefab", folderPathInside + weaponName + ".prefab");
            AssetDatabase.CopyAsset(DefaultWeaponFolderPathInside + "Animations", folderPathInside + "Animations");

            // Rename animations
            string animationFolderPathInside = folderPathInside + "Animations/";
            AssetDatabase.RenameAsset(animationFolderPathInside + "DefaultWeaponAim.anim", weaponName + "Aim");
            AssetDatabase.RenameAsset(animationFolderPathInside + "DefaultWeaponFire.anim", weaponName + "Fire");
            AssetDatabase.RenameAsset(animationFolderPathInside + "DefaultWeaponHip.anim", weaponName + "Hip");
            AssetDatabase.RenameAsset(animationFolderPathInside + "DefaultWeaponIdle.anim", weaponName + "Idle");
            AssetDatabase.RenameAsset(animationFolderPathInside + "DefaultWeaponReload.anim", weaponName + "Reload");
            AssetDatabase.RenameAsset(animationFolderPathInside + "DefaultWeaponRun.anim", weaponName + "Run");
            AssetDatabase.RenameAsset(animationFolderPathInside + "DefaultWeaponSwitch.anim", weaponName + "Switch");

            // Apply animations within Animator Controller
            AnimatorController animatorController = (AnimatorController)AssetDatabase.LoadAssetAtPath(folderPathInside + weaponName + ".controller", typeof(AnimatorController));

            AnimatorStateMachine animatorStateMachine = animatorController.layers[0].stateMachine;
            ChildAnimatorState[] childAnimatorStates = animatorStateMachine.states;

            AnimatorState state;
            string stateName;

            AnimationClip
                animationClipIdle = (AnimationClip)AssetDatabase.LoadAssetAtPath(animationFolderPathInside + weaponName + "Idle.anim", typeof(AnimationClip)),
                animationClipFire = (AnimationClip)AssetDatabase.LoadAssetAtPath(animationFolderPathInside + weaponName + "Fire.anim", typeof(AnimationClip)),
                animationClipReload = (AnimationClip)AssetDatabase.LoadAssetAtPath(animationFolderPathInside + weaponName + "Reload.anim", typeof(AnimationClip)),
                animationClipRun = (AnimationClip)AssetDatabase.LoadAssetAtPath(animationFolderPathInside + weaponName + "Run.anim", typeof(AnimationClip)),
                animationClipSwitch = (AnimationClip)AssetDatabase.LoadAssetAtPath(animationFolderPathInside + weaponName + "Switch.anim", typeof(AnimationClip)),
                animationClipAim = (AnimationClip)AssetDatabase.LoadAssetAtPath(animationFolderPathInside + weaponName + "Aim.anim", typeof(AnimationClip)),
                animationClipHip = (AnimationClip)AssetDatabase.LoadAssetAtPath(animationFolderPathInside + weaponName + "Hip.anim", typeof(AnimationClip));



            // Base layer
            for (int i = 0; i < childAnimatorStates.Length; i++)
            {
                state = childAnimatorStates[i].state;
                stateName = state.name;

                if (stateName == "Idle")
                {
                    animatorController.SetStateEffectiveMotion(state, animationClipIdle);
                }
                else if (state.name == "Fire")
                {
                    animatorController.SetStateEffectiveMotion(state, animationClipFire);
                }
                else if (stateName == "Reload")
                {
                    animatorController.SetStateEffectiveMotion(state, animationClipReload);
                }
                else if (stateName == "Run")
                {
                    animatorController.SetStateEffectiveMotion(state, animationClipRun);
                }
                else if (stateName == "SwitchIn")
                {
                    animatorController.SetStateEffectiveMotion(state, animationClipSwitch);
                }
                else if (stateName == "SwitchOut")
                {
                    animatorController.SetStateEffectiveMotion(state, animationClipSwitch);
                }
            }

            animatorStateMachine = animatorController.layers[1].stateMachine;
            childAnimatorStates = animatorStateMachine.states;

            // Aim Influance layer
            for (int i = 0; i < childAnimatorStates.Length; i++)
            {
                state = childAnimatorStates[i].state;
                stateName = state.name;

                if (stateName == "Aim")
                {
                    animatorController.SetStateEffectiveMotion(state, animationClipAim);
                }
                else if (stateName == "Hip")
                {
                    animatorController.SetStateEffectiveMotion(state, animationClipHip);
                }
            }

            // Rename animation properties
            RenameAnimationProperties(animationClipIdle, weaponName);
            RenameAnimationProperties(animationClipFire, weaponName);
            RenameAnimationProperties(animationClipReload, weaponName);
            RenameAnimationProperties(animationClipRun, weaponName);
            RenameAnimationProperties(animationClipSwitch, weaponName);
            RenameAnimationProperties(animationClipAim, weaponName + "/Aimbody");
            RenameAnimationProperties(animationClipHip, weaponName + "/Aimbody");


            // Apply dependentcies to Weapon object
            Weapon weaponObject = (Weapon)AssetDatabase.LoadAssetAtPath(folderPathInside + weaponName + ".asset", typeof(Weapon));

            weaponObject.weaponPrefab = (GameObject)AssetDatabase.LoadAssetAtPath(folderPathInside + weaponName + ".prefab", typeof(GameObject));
            weaponObject.animatorController = animatorController;

            weaponObject.barrelFlashSpawnName = weaponName + "/Aimbody/[Flash]";
            weaponObject.projectileSpawnName = weaponName + "/Aimbody/[Pro]";
            weaponObject.cartridgeSpawnName = weaponName + "/Aimbody/[Cart]";

        }

        void RenameAnimationProperties(AnimationClip clip, string name)
        {
            EditorCurveBinding[] currentBindings;
            AnimationCurve currentCurve;

            currentBindings = AnimationUtility.GetCurveBindings(clip);

            for (int i = 0; i < currentBindings.Length; i++)
            {
                currentCurve = AnimationUtility.GetEditorCurve(clip, currentBindings[i]);
                AnimationUtility.SetEditorCurve(clip, currentBindings[i], null);

                currentBindings[i].path = name;
                AnimationUtility.SetEditorCurve(clip, currentBindings[i], currentCurve);
            }
        }
    }
}