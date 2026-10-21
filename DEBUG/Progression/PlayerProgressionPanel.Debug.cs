// Optional saved-XP test button; menu display owns only the attachment hook and error/status presentation.
using Godot;
using System;

public partial class PlayerProgressionPanel
{
    #region Development Grant
    private Button _grant;
    // =========================================================
    // Release builds never construct the optional development action.
    private void AttachDebugGrant()
    {
#if DEBUG
        if (!AllowDebugGrant) return;
        _grant = UIButtonFactory.Create("DEBUG: GRANT 600 PLAYER XP", GrantDebugXP, UIButtonStyle.Compact);
        _grant.TooltipText = "Development only. Grants and saves real XP to the selected profile.";
        AddChild(_grant);
#endif
    }
    // =========================================================
    // Development verification only; live mission results never call this button.
    private void GrantDebugXP()
    {
        try { ProgressionService.GrantXP(600); }
        catch (Exception error) { _status.Text = error.Message; _status.Visible = true; }
    }
    #endregion
}
