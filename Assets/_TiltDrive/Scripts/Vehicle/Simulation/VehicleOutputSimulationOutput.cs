using System;
using UnityEngine;

namespace TiltDrive.VehicleSystem
{
    [Serializable]
    public class VehicleOutputSimulationOutput
    {
        [Header("Estado")]
        public VehicleOutputState state = new VehicleOutputState();

        [Tooltip("Texto corto de diagnostico del tick actual.")]
        public string diagnosticMessage = string.Empty;

        public VehicleOutputState ToVehicleOutputState()
        {
            return state;
        }
    }
}
