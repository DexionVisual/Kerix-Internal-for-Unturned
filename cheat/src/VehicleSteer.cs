using System;
using System.Reflection;
using SDG.Unturned;
namespace Kerix
{
    public static class VehicleSteer
    {
        public static bool SteerEnabled;
        public static void OverrideDrivingInputs(PlayerMovement m, InteractableVehicle v)
        {
            if (v == null || v.asset == null) return;
            try
            {
                if (SteerEnabled)
                {
                    FieldInfo h = Movement.GetHorizontalField();
                    if (h != null && (byte)h.GetValue(m) == 2)
                        h.SetValue(m, (byte)15);
                }
            }
            catch { }
        }
    }
}
