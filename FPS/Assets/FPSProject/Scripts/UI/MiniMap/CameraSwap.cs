using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor.Experimental.SceneManagement;
#endif

[ExecuteAlways]
public class CameraSwap : MonoBehaviour
{
#if UNITY_EDITOR
    Camera cam;
    bool showingPrefabScene = false;
    private void OnEnable()
    {
        cam = GetComponent<Camera>();
        PrefabStage.prefabStageOpened -= OnPrefabStageOpened;
        PrefabStage.prefabStageOpened += OnPrefabStageOpened;

        PrefabStage.prefabStageClosing -= OnPrefabStageClosing;
        PrefabStage.prefabStageClosing += OnPrefabStageClosing;
        OnPrefabStageOpened(PrefabStageUtility.GetCurrentPrefabStage());//check on recompile
    }
    private void OnDisable()
    {
        PrefabStage.prefabStageOpened -= OnPrefabStageOpened;
        PrefabStage.prefabStageClosing -= OnPrefabStageClosing;
        OnPrefabStageClosing(null);//ensure rendering regular scene again when closing and just before recompile
    }

    private void OnPrefabStageOpened(PrefabStage stage)
    {
        if (!showingPrefabScene)
        {
            if (stage != null)
            {
                cam.scene = stage.scene;//set camera to render preview scene
                showingPrefabScene = true;
            }
        }
    }
    private void OnPrefabStageClosing(PrefabStage stage)
    {
        if (showingPrefabScene)
        {
            cam.scene = SceneManager.GetActiveScene();//return to normal scene
            showingPrefabScene = false;
        }
    }
#endif
}