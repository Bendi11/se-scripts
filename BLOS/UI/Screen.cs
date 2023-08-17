using Sandbox.ModAPI.Ingame;
using VRageMath;

public partial class ShipCore {
    
}

/// Generic controller for a single screen that can accept input and draw sprites / text
public abstract class Screen {
    public struct Input {
        public Vector2 Rot;
        public Vector3 Move;
        public float Roll;

        public Input(IMyShipController control) {
            Rot = control.RotationIndicator;
            Move = control.MoveIndicator;
            Roll = control.RollIndicator;
        }
    }

    public IMyTextSurface Surface;
    
    /// Render the screen's contents
    public abstract void Render();
    /// Handle user input, use ShipCore.Time for time data
    public abstract bool Handle(Input input);
}
