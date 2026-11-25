using UnityEngine;

public partial class GameManager
{
    internal int ExhaustPreviewVehicleType => Player!=null&&Player.Driving?Player.VehicleType:VehicleSelected;
}
