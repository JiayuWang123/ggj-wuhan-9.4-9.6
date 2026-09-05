using UnityEngine;

public class CursorManager : MonoBehaviour
{
    [Header("自定义鼠标光标贴图")]
    public Texture2D customCursorTexture; // 你的鼠标指针图片

    [Header("光标点击热点坐标")]
    public Vector2 hotSpot = Vector2.zero; // 鼠标实际触发点击的中心点（通常是左上角 0,0）

    private void Start()
    {
        // 游戏启动时，默认设置一次自定义鼠标贴图
        SetCustomCursor();
    }

    /// <summary>
    /// 设置为自定义的鼠标贴图
    /// </summary>
    public void SetCustomCursor()
    {
        if (customCursorTexture != null)
        {
            // CursorMode.Auto 会根据不同平台自动选择最佳渲染方式
            Cursor.SetCursor(customCursorTexture, hotSpot, CursorMode.Auto);
        }
    }

    /// <summary>
    /// 恢复系统默认的鼠标光标
    /// </summary>
    public void ResetToDefaultCursor()
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

   
    public void LockAndHideCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    /// <summary>
    /// 显示鼠标并恢复自由移动（适合打开背包、主菜单、UI 界面）
    /// </summary>
    public void UnlockAndShowCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
