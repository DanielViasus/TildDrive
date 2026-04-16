using UnityEngine;

namespace TiltDrive.Core.Config
{
    [CreateAssetMenu(
        fileName = "VehicleConfig",
        menuName = "TiltDrive/Config/Vehicle Config"
    )]
    public class VehicleConfig : ScriptableObject
    {
        [Header("Motor")]
        [Min(0f)] public float rpmMinima = 800f;
        [Min(0f)] public float rpmMaxima = 7000f;
        [Min(0f)] public float rpmRalenti = 900f;

        [Header("Velocidad")]
        [Min(0f)] public float velocidadMaximaKmh = 180f;
        [Min(0f)] public float aceleracionBase = 8f;
        [Min(0f)] public float frenadoBase = 12f;

        [Header("Direccion")]
        [Min(0f)] public float anguloDireccionMaximo = 35f;
        [Min(0f)] public float sensibilidadDireccion = 5f;

        [Header("Transmision")]
        public int marchaInicial = 0;
        [Min(1)] public int marchaMaxima = 5;
        public bool tieneReversa = true;
    }
}