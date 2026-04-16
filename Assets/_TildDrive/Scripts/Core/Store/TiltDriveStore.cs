using UnityEngine;
using TiltDrive.Core.State;

namespace TiltDrive.Core.Store
{
    public class TiltDriveStore : MonoBehaviour
    {
        [Header("Estado global actual")]
        [SerializeField] private TiltDriveState estadoActual = new TiltDriveState();

        public TiltDriveState EstadoActual => estadoActual;

        private void Awake()
        {
            InicializarEstado();
            Debug.Log("[TiltDrive] Store inicializado correctamente.");
        }

        private void InicializarEstado()
        {
            estadoActual = new TiltDriveState();
        }
    }
}