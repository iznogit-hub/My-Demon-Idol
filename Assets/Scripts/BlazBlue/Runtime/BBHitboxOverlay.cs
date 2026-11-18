using UnityEngine;
using DemonIdol.UI;

namespace DemonIdol.BlazBlue
{
    /// <summary>
    /// Debug boxes for BlazBlue mode. Draws the sim hit box and the body box. Classic draws nothing.
    /// </summary>
    public class BBHitboxOverlay : MonoBehaviour
    {
        public BBMatchRunner runner;

        void OnDrawGizmos()
        {
            Draw(false);
        }

        void OnGUI()
        {
            if (!SettingsPanel.ShowBlazBlueFrameMeter) return;
            if (runner == null || runner.Sim == null) return;
            BBFighterState a = runner.Sim.a;
            GUI.Label(new Rect(16f, 16f, 420f, 24f), "BB P1 hp " + a.hp + "  heat " + (a.heatMilli / 1000) + "  move " + a.moveId + " t" + a.moveTick);
        }

        void Draw(bool _)
        {
            if (runner == null || runner.Sim == null) return;
            if (SettingsPanel.ControlScheme != ControlScheme.BlazBlue4Button) return;
            DrawFighter(runner.Sim.a, Color.red);
            DrawFighter(runner.Sim.b, Color.yellow);
        }

        static void DrawFighter(BBFighterState f, Color color)
        {
            Gizmos.color = color;
            float x0, x1, y0, y1;
            BBRules.WorldBox(f.x, f.facing, 0f, 0.8f, 0.5f, 1.6f, out x0, out x1, out y0, out y1);
            Gizmos.DrawWireCube(new Vector3((x0 + x1) * 0.5f, (y0 + y1) * 0.5f, 0f), new Vector3(x1 - x0, y1 - y0, 0.05f));
        }
    }
}
