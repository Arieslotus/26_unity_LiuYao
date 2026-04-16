using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class EnableDepthTexture : MonoBehaviour
{

    void OnEnable()
    {
        UpdateDepthTexture();
    }

    void OnValidate()
    {
        UpdateDepthTexture();
    }

    void UpdateDepthTexture()
    {
        Camera cam = GetComponent<Camera>();
        if (cam != null)
        {

            cam.depthTextureMode |= DepthTextureMode.Depth;
            cam.depthTextureMode |= DepthTextureMode.DepthNormals;

            // **** 强制生成 depth ****
            cam.forceIntoRenderTexture = true;

            // 否则build 出来可能看不到
        }
    }

//    // 在Inspector中显示当前状态
//    void OnGUI()
//    {
//#if UNITY_EDITOR
//        if (Application.isEditor && !Application.isPlaying)
//        {
//            Camera cam = GetComponent<Camera>();
//            if (cam != null)
//            {
//                string status = (cam.depthTextureMode & DepthTextureMode.Depth) != 0 ?
//                    "深度图: 已开启" : "深度图: 未开启";

//                GUIStyle style = new GUIStyle(GUI.skin.label);
//                style.normal.textColor = Color.white;
//                style.fontSize = 12;

//                GUILayout.Label(status, style);
//            }
//        }
//#endif
//    }
}